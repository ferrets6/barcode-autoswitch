using System.Runtime.InteropServices;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>Utility for toggling the attached console window visibility.</summary>
internal static class Win32Console
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool DeleteMenu(IntPtr hWnd, uint uPosition, uint uFlags);

    private const int  SW_HIDE    = 0;
    private const int  SW_RESTORE = 9;
    private const uint MF_BYCOMMAND = 0x00000000;
    private const uint SC_CLOSE     = 0xF060;

    private static bool _isVisible = true;

    public static void ShowHide()
    {
        IntPtr hWnd = GetConsoleWindow();

        // WinExe has no console by default — allocate one on first call
        if (hWnd == IntPtr.Zero)
        {
            AllocConsole();
            // Redirect stdout/stderr to the new console
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var writer = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);
            Console.SetError(writer);
            hWnd = GetConsoleWindow();
            if (hWnd == IntPtr.Zero) return;
            _isVisible = true; // newly created console is visible
        }

        // Disable the close button so the user can't accidentally kill the app
        IntPtr hMenu = GetSystemMenu(hWnd, false);
        if (hMenu != IntPtr.Zero)
            DeleteMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);

        ShowWindow(hWnd, _isVisible ? SW_HIDE : SW_RESTORE);
        _isVisible = !_isVisible;
    }
}
