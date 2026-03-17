using BarcodeAutoSwitch.Core.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Brings a named process window to the foreground using Win32 P/Invoke.
/// Isolated here so the rest of the application never touches P/Invoke directly.
/// </summary>
public class WindowSwitcher : IWindowSwitcher
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern int SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private const int SW_RESTORE  = 9;
    private const int SW_MINIMIZE = 6;

    public bool BringToFront(string processName)
    {
        Console.WriteLine($"[FOCUS] Finestra attiva: '{GetForegroundTitle()}'");

        var process = Process.GetProcessesByName(processName).FirstOrDefault();
        if (process is null)
        {
            Console.WriteLine($"[FOCUS] Processo '{processName}' non trovato.");
            return false;
        }

        Console.WriteLine($"[FOCUS] Processo '{processName}' trovato (PID={process.Id}, hWnd=0x{process.MainWindowHandle:X})");

        // Restore if minimised / hidden
        if (process.MainWindowHandle == IntPtr.Zero)
        {
            ShowWindow(process.Handle, SW_RESTORE);
            Console.WriteLine($"[FOCUS] Processo '{processName}' ripristinato (hWnd era zero).");
        }

        int result = SetForegroundWindow(process.MainWindowHandle);
        Console.WriteLine($"[FOCUS] SetForegroundWindow: {(result != 0 ? "OK" : "FALLITO (result=0)")}");
        Console.WriteLine($"[FOCUS] Finestra attiva dopo: '{GetForegroundTitle()}'");
        return true;
    }

    public static string GetForegroundTitle()
    {
        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return "(nessuna)";
        var sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, sb.Capacity);
        GetWindowThreadProcessId(hWnd, out uint pid);
        string title = sb.ToString();
        return string.IsNullOrEmpty(title) ? $"(senza titolo, PID={pid})" : $"{title} [PID={pid}]";
    }
}
