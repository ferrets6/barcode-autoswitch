using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Barcode input service for USB HID keyboard-emulating barcode scanners.
/// Uses the Win32 Raw Input API (WM_INPUT) to capture keystrokes from a specific
/// device even when the application is not in the foreground (RIDEV_INPUTSINK).
///
/// THREADING: Must be instantiated and opened on the WPF UI thread, because it
/// creates an HwndSource message-only window on that thread. DataReceived is also
/// raised on the UI thread.
/// </summary>
public sealed class RawInputBarcodeService : IBarcodeInputService
{
    // ── Win32 constants ───────────────────────────────────────────────────────
    private const int    WM_INPUT                 = 0x00FF;
    private const uint   RIDEV_INPUTSINK          = 0x00000100;
    private const uint   RIDEV_REMOVE             = 0x00000001;
    private const uint   RID_INPUT                = 0x10000003;
    private const uint   RIM_TYPEKEYBOARD         = 1;
    private const uint   RIM_TYPEHID              = 2;
    private const ushort HID_USAGE_PAGE_POS       = 0x8C;
    private const ushort HID_USAGE_POS_SCANNER    = 0x02;
    private const uint   RIDI_DEVICENAME          = 0x20000007;
    private const uint   RIDI_DEVICEINFO          = 0x2000000B;
    // RID_DEVICE_INFO offsets: cbSize=0, dwType=4, union starts at 8;
    // RID_DEVICE_INFO_HID.usUsagePage at union+12=20, usUsage at union+14=22
    private const int    RID_INFO_SIZE            = 32;
    private const int    RID_INFO_USAGE_PAGE_OFF  = 20;
    private const int    RID_INFO_USAGE_OFF       = 22;
    private const ushort HID_USAGE_PAGE_GENERIC   = 0x01;
    private const ushort HID_USAGE_GENERIC_KEYBOARD = 0x06;
    private const uint   RI_KEY_BREAK             = 1;   // key-up flag in RAWKEYBOARD.Flags
    private const ushort VK_RETURN                = 0x0D;
    private const ushort VK_BACK                  = 0x08;
    private const ushort VK_SHIFT                 = 0x10;
    private const ushort VK_CAPITAL               = 0x14;

    // ── Win32 structs ─────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint   dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint   dwType;
        public uint   dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint   Message;
        public nuint  ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUT
    {
        public RAWINPUTHEADER header;
        public RAWKEYBOARD    keyboard; // valid when header.dwType == RIM_TYPEKEYBOARD
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint   dwType;
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        [MarshalAs(UnmanagedType.LPArray)] RAWINPUTDEVICE[] pRawInputDevice,
        uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(
        IntPtr hRawInput, uint uiCommand, IntPtr pData,
        ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(
        IntPtr pRawInputDeviceList,
        ref uint puiNumDevices, uint cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern int ToUnicodeEx(
        uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    // ── Instance state ────────────────────────────────────────────────────────
    private HwndSource?    _hwndSource;
    private string         _deviceId  = string.Empty;
    private readonly StringBuilder _buffer = new();
    private bool           _shiftDown;
    private bool           _disposed;

    public bool   IsOpen   => _hwndSource != null;
    public string DeviceId => _deviceId;

    public event EventHandler<string> DataReceived  = delegate { };
    public event EventHandler<string> ErrorReceived = delegate { };

    // ── IBarcodeInputService ──────────────────────────────────────────────────

    /// <param name="deviceId">
    ///   Device path returned by <see cref="GetAvailableDevices"/>, or
    ///   <see cref="AnyDeviceId"/> to receive input from every keyboard device.
    /// </param>
    public bool Open(string deviceId)
    {
        Close();
        try
        {
            // Message-only window: receives WM_INPUT without showing any UI
            var p = new HwndSourceParameters("RawInputSource", 0, 0)
            {
                ParentWindow = new IntPtr(-3), // HWND_MESSAGE
                WindowStyle  = 0
            };
            _hwndSource = new HwndSource(p);
            _hwndSource.AddHook(WndProc);

            var rid = new[]
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = HID_USAGE_PAGE_GENERIC,
                    usUsage     = HID_USAGE_GENERIC_KEYBOARD,
                    dwFlags     = RIDEV_INPUTSINK,
                    hwndTarget  = _hwndSource.Handle
                },
                new RAWINPUTDEVICE
                {
                    usUsagePage = HID_USAGE_PAGE_POS,
                    usUsage     = HID_USAGE_POS_SCANNER,
                    dwFlags     = RIDEV_INPUTSINK,
                    hwndTarget  = _hwndSource.Handle
                }
            };

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                Console.WriteLine($"[USB] Registrazione Raw Input fallita (errore {Marshal.GetLastWin32Error()})");
                Close();
                return false;
            }

            _deviceId = deviceId;
            Console.WriteLine($"[USB] Dispositivo aperto: {deviceId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[USB] Errore apertura dispositivo: {ex.Message}");
            Close();
            return false;
        }
    }

    public void Close()
    {
        if (_hwndSource == null) return;

        try
        {
            // Unregister so we stop receiving WM_INPUT
            var rid = new[]
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = HID_USAGE_PAGE_GENERIC,
                    usUsage     = HID_USAGE_GENERIC_KEYBOARD,
                    dwFlags     = RIDEV_REMOVE,
                    hwndTarget  = IntPtr.Zero
                },
                new RAWINPUTDEVICE
                {
                    usUsagePage = HID_USAGE_PAGE_POS,
                    usUsage     = HID_USAGE_POS_SCANNER,
                    dwFlags     = RIDEV_REMOVE,
                    hwndTarget  = IntPtr.Zero
                }
            };
            RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        }
        catch { /* ignore on close */ }

        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
        _hwndSource = null;
        _buffer.Clear();
        _shiftDown = false;
        _deviceId  = string.Empty;
        Console.WriteLine("[USB] Dispositivo chiuso");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
    }

    // ── Message loop ──────────────────────────────────────────────────────────

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
            ProcessRawInput(lParam);
        return IntPtr.Zero;
    }

    private void ProcessRawInput(IntPtr hRawInput)
    {
        uint size = 0;
        GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size,
                        (uint)Marshal.SizeOf<RAWINPUTHEADER>());
        if (size == 0) return;

        var buf = Marshal.AllocHGlobal((int)size);
        try
        {
            uint written = GetRawInputData(hRawInput, RID_INPUT, buf, ref size,
                                           (uint)Marshal.SizeOf<RAWINPUTHEADER>());
            if (written != size) return;

            var raw = Marshal.PtrToStructure<RAWINPUT>(buf);

            // Filter by specific device when requested
            if (_deviceId != AnyDeviceId)
            {
                string name = GetDeviceName(raw.header.hDevice);
                if (!string.Equals(name, _deviceId, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            if (raw.header.dwType == RIM_TYPEHID)
            {
                ProcessHidReport(buf);
                return;
            }

            if (raw.header.dwType != RIM_TYPEKEYBOARD) return;

            ushort vkey  = raw.keyboard.VKey;
            bool   isUp  = (raw.keyboard.Flags & RI_KEY_BREAK) != 0;

            // Track shift state from the raw input itself (we may not have focus)
            if (vkey == VK_SHIFT)
            {
                _shiftDown = !isUp;
                return;
            }

            if (isUp) return; // only handle key-down events

            if (vkey == VK_RETURN)
            {
                string barcode = _buffer.ToString();
                _buffer.Clear();
                if (!string.IsNullOrEmpty(barcode))
                {
                    Console.WriteLine($"[USB] Barcode ricevuto: '{barcode}'");
                    DataReceived(this, barcode);
                }
                return;
            }

            if (vkey == VK_BACK)
            {
                if (_buffer.Length > 0) _buffer.Length--;
                return;
            }

            char c = VKeyToChar(vkey, raw.keyboard.MakeCode, _shiftDown);
            if (c != '\0')
                _buffer.Append(c);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    // ── POS HID report parsing ────────────────────────────────────────────────

    /// <summary>
    /// Parses a raw HID report from a POS barcode scanner (Usage Page 0x8C).
    ///
    /// Most scanners use the format: [reportId] [dataLen] [barcodeBytes…]
    /// where reportId &lt; 0x20. Falls back to extracting all printable ASCII bytes.
    /// </summary>
    private void ProcessHidReport(IntPtr buf)
    {
        int  headerSize = Marshal.SizeOf<RAWINPUTHEADER>();
        uint sizeHid    = (uint)Marshal.ReadInt32(buf, headerSize);      // dwSizeHid
        uint count      = (uint)Marshal.ReadInt32(buf, headerSize + 4);  // dwCount
        if (count == 0 || sizeHid == 0) return;

        // bRawData starts right after the two DWORDs
        int  dataOffset  = headerSize + 8;
        var  reportBytes = new byte[sizeHid];
        Marshal.Copy(buf + dataOffset, reportBytes, 0, (int)sizeHid);

        // Try standard POS HID format: byte[0]=reportId (<0x20), byte[1]=length, bytes[2..]=data
        if (sizeHid >= 3 && reportBytes[0] < 0x20)
        {
            int dataLen = reportBytes[1];
            if (dataLen > 0 && dataLen <= sizeHid - 2)
            {
                string barcode = Encoding.ASCII.GetString(reportBytes, 2, dataLen).TrimEnd('\0', '\r', '\n');
                if (!string.IsNullOrEmpty(barcode))
                {
                    Console.WriteLine($"[POS-HID] Barcode ricevuto: '{barcode}'");
                    DataReceived(this, barcode);
                    return;
                }
            }
        }

        // Fallback: accumulate printable ASCII; flush on empty/padding report
        bool anyPrintable = false;
        for (int i = 0; i < sizeHid; i++)
        {
            byte b = reportBytes[i];
            if (b == 0x00) break;
            if (b == 0x0D || b == 0x0A)          // CR / LF = end of barcode
            {
                if (_buffer.Length > 0)
                {
                    string barcode = _buffer.ToString();
                    _buffer.Clear();
                    Console.WriteLine($"[POS-HID] Barcode ricevuto: '{barcode}'");
                    DataReceived(this, barcode);
                }
                return;
            }
            if (b >= 0x20 && b < 0x7F) { _buffer.Append((char)b); anyPrintable = true; }
        }

        if (!anyPrintable && _buffer.Length > 0)
        {
            // No printable bytes in this report → the previous data is a complete barcode
            string barcode = _buffer.ToString();
            _buffer.Clear();
            Console.WriteLine($"[POS-HID] Barcode ricevuto: '{barcode}'");
            DataReceived(this, barcode);
        }
    }

    // ── Key mapping ───────────────────────────────────────────────────────────

    private static char VKeyToChar(ushort vkey, ushort scanCode, bool shift)
    {
        // Use ToUnicodeEx for accurate layout-aware mapping
        bool capsLock = (GetKeyState(VK_CAPITAL) & 1) != 0;
        var keyState = new byte[256];
        if (shift)    keyState[VK_SHIFT]   = 0x80;
        if (capsLock) keyState[VK_CAPITAL] = 0x01;

        var sb     = new StringBuilder(4);
        var layout = GetKeyboardLayout(0);
        int result = ToUnicodeEx(vkey, scanCode, keyState, sb, sb.Capacity, 0, layout);
        return result == 1 ? sb[0] : '\0';
    }

    // ── Device enumeration ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all HID keyboard-class devices visible to Raw Input.
    /// Typically includes both the built-in keyboard and any USB barcode scanners.
    /// </summary>
    public static IReadOnlyList<BarcodeDeviceInfo> GetAvailableDevices()
    {
        Console.WriteLine($"[USB-ENUM] BUILD 2026-03-16b — GetAvailableDevices chiamato");

        uint count      = 0;
        uint structSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();

        // Prima chiamata con IntPtr.Zero per ottenere il conteggio
        GetRawInputDeviceList(IntPtr.Zero, ref count, structSize);
        if (count == 0) return [];

        // Alloca buffer non gestito — evita il bug di marshaling di [MarshalAs(LPArray)]
        // dove i campi IntPtr nelle struct vengono azzerati al ritorno
        IntPtr buf = Marshal.AllocHGlobal((int)(count * structSize));
        try
        {
            uint ret = GetRawInputDeviceList(buf, ref count, structSize);
            Console.WriteLine($"[USB-ENUM] ret={ret}, count={count}, structSize={structSize}, win32err={Marshal.GetLastWin32Error()}");

            // Pass 1: collect VID/PID of all type=2 POS scanner devices (usage page 0x8C).
            // Used in pass 2 to correctly label keyboard-emulating scanner interfaces.
            var posScannerVidPids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (uint i = 0; i < count; i++)
            {
                var dev = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(buf + (int)(i * structSize));
                if (dev.dwType != RIM_TYPEHID) continue;
                var (upPage, _) = GetHidUsage(dev.hDevice);
                if (upPage != HID_USAGE_PAGE_POS) continue;
                string vidPid = ExtractVidPid(GetDeviceName(dev.hDevice));
                if (!string.IsNullOrEmpty(vidPid)) posScannerVidPids.Add(vidPid);
            }

            // Pass 2: build the list
            var result = new List<BarcodeDeviceInfo>();
            int idx    = 1;
            for (uint i = 0; i < count; i++)
            {
                var    dev  = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(buf + (int)(i * structSize));
                string id   = GetDeviceName(dev.hDevice);

                if (dev.dwType == RIM_TYPEHID)
                {
                    var (upPage, _) = GetHidUsage(dev.hDevice);
                    if (upPage != HID_USAGE_PAGE_POS)
                    {
                        Console.WriteLine($"[USB-ENUM]   [{i}] SKIP  type=HID usagePage=0x{upPage:X2} path={id}");
                        continue;
                    }
                }
                else if (dev.dwType == RIM_TYPEKEYBOARD)
                {
                    // Exclude system keyboards that can never be a barcode scanner
                    if (IsSystemKeyboard(id))
                    {
                        Console.WriteLine($"[USB-ENUM]   [{i}] SKIP  system keyboard path={id}");
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine($"[USB-ENUM]   [{i}] SKIP  type={dev.dwType} path={id}");
                    continue;
                }

                if (string.IsNullOrEmpty(id)) continue;

                // A type=1 keyboard interface sharing a VID/PID with a POS scanner interface
                // is the keyboard-emulation channel of that same scanner — label it accordingly
                bool isKnownScanner = dev.dwType == RIM_TYPEKEYBOARD
                                      && posScannerVidPids.Contains(ExtractVidPid(id));

                string name = BuildFriendlyName(id, idx++, dev.dwType, isKnownScanner);
                Console.WriteLine($"[USB-ENUM]   [{i}] ADD   type={dev.dwType} name='{name}' path={id}");
                result.Add(new BarcodeDeviceInfo(id, name, BarcodeDeviceType.UsbHid));
            }
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    private static string BuildFriendlyName(string devicePath, int index, uint dwType, bool isKnownScanner = false)
    {
        // Path looks like \\?\HID#VID_0483&PID_0011#6&...
        // Extract the hardware ID segment for a friendlier display name
        var    parts  = devicePath.Split('#');
        string hwPart = parts.Length >= 2 ? parts[1].Replace('&', ' ') : string.Empty;
        string label  = dwType == RIM_TYPEHID || isKnownScanner ? "USB Scanner" : "USB Tastiera";
        return string.IsNullOrEmpty(hwPart) ? $"{label} {index}" : $"{label} {index} ({hwPart})";
    }

    /// <summary>
    /// Extracts "VID_xxxx&PID_yyyy" from a Raw Input device path.
    /// Returns empty string if no VID/PID is found (e.g. ACPI or system devices).
    /// </summary>
    private static string ExtractVidPid(string devicePath)
    {
        // Path format: \\?\HID#VID_0C2E&PID_1001&MI_00#...
        var parts = devicePath.Split('#');
        if (parts.Length < 2) return string.Empty;
        var tokens = parts[1].Split('&');
        string? vid = Array.Find(tokens, t => t.StartsWith("VID_", StringComparison.OrdinalIgnoreCase));
        string? pid = Array.Find(tokens, t => t.StartsWith("PID_", StringComparison.OrdinalIgnoreCase));
        return vid != null && pid != null ? $"{vid}&{pid}" : string.Empty;
    }

    /// <summary>
    /// Returns true for system-level keyboards that are never barcode scanners
    /// (built-in laptop keyboard, on-screen keyboard, ACPI HID, etc.).
    /// </summary>
    private static bool IsSystemKeyboard(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath)) return true;
        // ACPI devices (built-in keyboard), Microsoft virtual keyboard RID
        return devicePath.Contains("ACPI#", StringComparison.OrdinalIgnoreCase)
            || devicePath.Contains("Microsoft Keyboard", StringComparison.OrdinalIgnoreCase)
            || devicePath.Contains("Microsoft Mouse", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns the HID usage page and usage for a device, or (0,0) on failure.</summary>
    private static (ushort usagePage, ushort usage) GetHidUsage(IntPtr hDevice)
    {
        var infoBuf = Marshal.AllocHGlobal(RID_INFO_SIZE);
        try
        {
            Marshal.WriteInt32(infoBuf, RID_INFO_SIZE); // cbSize
            uint infoSize = (uint)RID_INFO_SIZE;
            uint ret = GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, infoBuf, ref infoSize);
            if (ret == uint.MaxValue) return (0, 0);
            ushort usagePage = (ushort)Marshal.ReadInt16(infoBuf, RID_INFO_USAGE_PAGE_OFF);
            ushort usage     = (ushort)Marshal.ReadInt16(infoBuf, RID_INFO_USAGE_OFF);
            return (usagePage, usage);
        }
        finally
        {
            Marshal.FreeHGlobal(infoBuf);
        }
    }

    private static string GetDeviceName(IntPtr hDevice)
    {
        uint size = 0;
        GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref size);
        if (size == 0) return string.Empty;

        // size is in characters (Unicode)
        var buf = Marshal.AllocHGlobal((int)(size * 2));
        try
        {
            GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, buf, ref size);
            return Marshal.PtrToStringUni(buf)?.TrimEnd('\0') ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>Pass as <c>deviceId</c> to receive input from ANY keyboard device.</summary>
    public const string AnyDeviceId = "*";
}
