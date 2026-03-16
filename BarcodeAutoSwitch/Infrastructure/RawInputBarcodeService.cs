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
    private const uint   RIDI_DEVICENAME          = 0x20000007;
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

    [DllImport("user32.dll")]
    private static extern uint GetRawInputDeviceList(
        [MarshalAs(UnmanagedType.LPArray)] RAWINPUTDEVICELIST[]? pRawInputDeviceList,
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
                }
            };

            if (!RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
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
                }
            };
            RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
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
            if (raw.header.dwType != RIM_TYPEKEYBOARD) return;

            // Filter by specific device when requested
            if (_deviceId != AnyDeviceId)
            {
                string name = GetDeviceName(raw.header.hDevice);
                if (!string.Equals(name, _deviceId, StringComparison.OrdinalIgnoreCase))
                    return;
            }

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
        uint count = 0;
        GetRawInputDeviceList(null, ref count, (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>());
        if (count == 0) return [];

        var list = new RAWINPUTDEVICELIST[count];
        GetRawInputDeviceList(list, ref count, (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>());

        var result = new List<BarcodeDeviceInfo>();
        int idx = 1;
        foreach (var dev in list)
        {
            if (dev.dwType != RIM_TYPEKEYBOARD) continue;
            string id = GetDeviceName(dev.hDevice);
            if (string.IsNullOrEmpty(id)) continue;
            result.Add(new BarcodeDeviceInfo(id, BuildFriendlyName(id, idx++), BarcodeDeviceType.UsbHid));
        }
        return result;
    }

    private static string BuildFriendlyName(string devicePath, int index)
    {
        // Path looks like \\?\HID#VID_0483&PID_0011#6&...
        // Extract the hardware ID segment for a friendlier display name
        var parts = devicePath.Split('#');
        if (parts.Length >= 2)
            return $"USB Tastiera {index} ({parts[1].Replace('&', ' ')})";
        return $"USB Tastiera {index}";
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
