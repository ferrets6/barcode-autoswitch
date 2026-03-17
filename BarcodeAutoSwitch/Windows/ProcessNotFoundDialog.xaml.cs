using System.IO;
using System.Windows;
using System.Windows.Media;

namespace BarcodeAutoSwitch.Windows;

public partial class ProcessNotFoundDialog : Window
{
    private readonly MediaPlayer? _player;

    public ProcessNotFoundDialog(string processName, CancellationToken autoCloseToken)
    {
        InitializeComponent();
        MessageText.Text =
            $"Processo '{processName}' non trovato.\n\nAprire il programma e riprovare.";

        _player = PlayBeep();

        // Close automatically when the next barcode scan cancels the token
        autoCloseToken.Register(() =>
        {
            if (Dispatcher.CheckAccess()) Close();
            else Dispatcher.Invoke(Close);
        }, useSynchronizationContext: false);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private static MediaPlayer? PlayBeep()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "beepKO.mp3");
            if (!File.Exists(path)) return null;

            var player = new MediaPlayer();
            player.MediaOpened += (_, _) => player.Play();
            player.Open(new Uri(path, UriKind.Absolute));
            return player;
        }
        catch
        {
            return null; // audio is best-effort
        }
    }
}