using BarcodeAutoSwitch.Core.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

    private const int SW_RESTORE  = 9;
    private const int SW_MINIMIZE = 6;

    public bool BringToFront(string processName)
    {
        var process = Process.GetProcessesByName(processName).FirstOrDefault();
        if (process is null)
        {
            Console.WriteLine($"Processo '{processName}' non trovato.");
            return false;
        }

        // Restore if minimised / hidden
        if (process.MainWindowHandle == IntPtr.Zero)
        {
            ShowWindow(process.Handle, SW_RESTORE);
            Console.WriteLine($"Processo '{processName}' ripristinato.");
        }

        SetForegroundWindow(process.MainWindowHandle);
        Console.WriteLine($"Focus impostato su '{processName}'.");
        return true;
    }
}
