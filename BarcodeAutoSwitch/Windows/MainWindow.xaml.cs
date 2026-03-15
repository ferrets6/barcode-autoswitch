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
        viewModel.RequestChangeCOMPort      += OnRequestChangeCOMPort;
        viewModel.PropertyChanged           += OnViewModelPropertyChanged;

        // Set initial browser address once the browser is loaded
        Browser.IsBrowserInitializedChanged += OnBrowserInitialized;
    }

    private void OnBrowserInitialized(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (Browser.IsBrowserInitialized && _viewModel.IsBrowserVisible)
        {
            // Navigate to Adriatica Press; address is injected via App
            Browser.Address = App.AdriaticaPressVenditaUrl;
            Browser.LoadingStateChanged += OnLoadingStateChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsBrowserVisible) && _viewModel.IsBrowserVisible)
        {
            Browser.IsBrowserInitializedChanged += OnBrowserInitialized;
        }
    }

    private void OnLoadingStateChanged(object? sender, CefSharp.LoadingStateChangedEventArgs args)
    {
        if (args.IsLoading) return;

        Dispatcher.Invoke(() =>
        {
            string address = Browser.Address ?? string.Empty;
            // Keep users inside the Adriatica Press application
            if (!address.Contains(App.AdriaticaPressLoginUrl, StringComparison.OrdinalIgnoreCase)
                && address != App.AdriaticaPressVenditaUrl)
            {
                Browser.Address = App.AdriaticaPressVenditaUrl;
            }
        });
    }

    private void OnBarcodeForAdriaticaPress(object? sender, EventArgs e)
    {
        // Focus the browser so the Alt+T keystroke lands in it
        this.Focus();
        Browser.Focus();
        _viewModel.SendAdriaticaPressKey();
    }

    private void OnRequestChangeCOMPort(object? sender, EventArgs e)
    {
        var dialog = new ComPortWindow(_viewModel);
        dialog.ShowDialog();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.BarcodeForAdriaticaPress -= OnBarcodeForAdriaticaPress;
        _viewModel.RequestChangeCOMPort      -= OnRequestChangeCOMPort;
        _viewModel.PropertyChanged           -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        CefSharp.Cef.Shutdown();
        base.OnClosed(e);
    }
}
