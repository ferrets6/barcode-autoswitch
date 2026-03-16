using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.ViewModels;
using System.Windows;

namespace BarcodeAutoSwitch.Windows;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        viewModel.BarcodeForAdriaticaPress += OnBarcodeForAdriaticaPress;
        viewModel.RequestManageDevices     += OnRequestManageDevices;
        viewModel.PropertyChanged          += OnViewModelPropertyChanged;

        // Detect HTTP 5xx from the Adriatica Press server
        var requestHandler = new HttpErrorRequestHandler();
        requestHandler.ServerError += OnAdriaticaPressServerError;
        Browser.RequestHandler = requestHandler;

        Browser.IsBrowserInitializedChanged += OnBrowserInitialized;
    }

    private void OnBrowserInitialized(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (Browser.IsBrowserInitialized && _viewModel.IsBrowserVisible)
        {
            Browser.Address = App.AdriaticaPressVenditaUrl;
            Browser.LoadingStateChanged += OnLoadingStateChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsBrowserVisible) && _viewModel.IsBrowserVisible)
            Browser.IsBrowserInitializedChanged += OnBrowserInitialized;
    }

    private void OnLoadingStateChanged(object? sender, CefSharp.LoadingStateChangedEventArgs args)
    {
        if (args.IsLoading) return;

        Dispatcher.Invoke(() =>
        {
            string address = Browser.Address ?? string.Empty;
            if (!address.Contains(App.AdriaticaPressLoginUrl, StringComparison.OrdinalIgnoreCase)
                && address != App.AdriaticaPressVenditaUrl)
            {
                Browser.Address = App.AdriaticaPressVenditaUrl;
            }
        });
    }

    private void OnBarcodeForAdriaticaPress(object? sender, EventArgs e)
    {
        this.Focus();
        Browser.Focus();
    }

    private void OnRequestManageDevices(object? sender, EventArgs e)
    {
        IBarcodeInputService ServiceFactory(BarcodeDeviceType t) =>
            t == BarcodeDeviceType.UsbHid
                ? new RawInputBarcodeService()
                : (IBarcodeInputService)new SerialPortService();

        var dialog = new DeviceManagementWindow(_viewModel, ServiceFactory) { Owner = this };
        dialog.ShowDialog();
    }

    private void OnAdriaticaPressServerError(int statusCode)
    {
        Console.WriteLine($"[HTTP] Adriatica Press ha risposto con HTTP {statusCode}");
        Dispatcher.BeginInvoke(() =>
            System.Windows.MessageBox.Show(
                $"Adriatica Press ha risposto con un errore (HTTP {statusCode}).\n\nIl problema non riguarda questa applicazione.",
                "Errore Adriatica Press",
                MessageBoxButton.OK,
                MessageBoxImage.Warning));
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.BarcodeForAdriaticaPress -= OnBarcodeForAdriaticaPress;
        _viewModel.RequestManageDevices     -= OnRequestManageDevices;
        _viewModel.PropertyChanged          -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        CefSharp.Cef.Shutdown();
        base.OnClosed(e);
    }
}
