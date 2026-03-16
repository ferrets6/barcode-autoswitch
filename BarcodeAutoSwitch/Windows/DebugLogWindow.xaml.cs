using BarcodeAutoSwitch.Infrastructure;
using System.ComponentModel;
using System.Windows;

namespace BarcodeAutoSwitch.Windows;

public partial class DebugLogWindow : Window
{
    private static DebugLogWindow? _instance;

    /// <summary>Shows the window if hidden; hides it if visible. Creates it on first call.</summary>
    public static void ShowHide()
    {
        if (_instance == null || !_instance.IsLoaded)
        {
            _instance = new DebugLogWindow();
            _instance.Show();
        }
        else if (_instance.IsVisible)
        {
            _instance.Hide();
        }
        else
        {
            _instance.Show();
            _instance.Activate();
        }
    }

    private DebugLogWindow()
    {
        InitializeComponent();
        AppLogger.MessageLogged += AppendLine;
    }

    private void AppendLine(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            LogBox.AppendText(message + "\n");
            Scroll.ScrollToBottom();
        });
    }

    private void OnCopyAll(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(LogBox.Text))
            System.Windows.Clipboard.SetText(LogBox.Text);
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
    }

    // Hide instead of closing so the window can be reopened later
    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
