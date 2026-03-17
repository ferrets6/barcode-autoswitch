using System.Windows;

namespace BarcodeAutoSwitch.Windows;

public partial class ProcessNotFoundDialog : Window
{
    public ProcessNotFoundDialog(string processName, CancellationToken autoCloseToken)
    {
        InitializeComponent();
        MessageText.Text =
            $"Processo '{processName}' non trovato.\n\nAprire il programma e riprovare.";

        // Close automatically when the next barcode scan cancels the token
        autoCloseToken.Register(() =>
        {
            if (Dispatcher.CheckAccess()) Close();
            else Dispatcher.Invoke(Close);
        }, useSynchronizationContext: false);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}