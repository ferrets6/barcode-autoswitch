using System.Runtime.InteropServices;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>Utility for toggling the attached console window visibility.</summary>
internal static class Win32Console
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handler, bool add);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool ConsoleCtrlDelegate(uint ctrlType);

    private const int  SW_HIDE    = 0;
    private const int  SW_RESTORE = 9;
    private const uint CTRL_CLOSE_EVENT = 2;

    private static bool _isVisible = false;
    // Strong reference required — GC would collect a local delegate
    private static readonly ConsoleCtrlDelegate _ctrlHandler = OnCtrlEvent;

    public static void ShowHide()
    {
        IntPtr hWnd = GetConsoleWindow();

        // WinExe has no console by default — allocate one on first call
        if (hWnd == IntPtr.Zero)
        {
            AllocConsole();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var writer = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);
            Console.SetError(writer);
            // Hide instead of terminating when the user clicks X
            SetConsoleCtrlHandler(_ctrlHandler, true);
            _isVisible = true;
            return; // console is already visible after AllocConsole
        }

        ShowWindow(hWnd, _isVisible ? SW_HIDE : SW_RESTORE);
        _isVisible = !_isVisible;
    }

    private static bool OnCtrlEvent(uint ctrlType)
    {
        if (ctrlType == CTRL_CLOSE_EVENT)
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
            _isVisible = false;
            return true; // handled — do not terminate the process
        }
        return false;
    }
}