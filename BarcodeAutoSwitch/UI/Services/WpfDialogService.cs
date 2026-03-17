using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Windows;
using WpfApplication = System.Windows.Application;

namespace BarcodeAutoSwitch.UI.Services;

public sealed class WpfDialogService : IDialogService
{
    public void ShowProcessNotFound(string processName, CancellationToken autoCloseToken)
    {
        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new ProcessNotFoundDialog(processName, autoCloseToken);
            dialog.Owner = App.Current.MainWindow;
            dialog.Show();
        });
    }
}