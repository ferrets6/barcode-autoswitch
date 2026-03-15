using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.ViewModels;
using System.Windows;

namespace BarcodeAutoSwitch.Windows;

public partial class ComPortWindow : Window
{
    private readonly ComPortViewModel _viewModel;
    private readonly MainViewModel    _mainViewModel;

    public ComPortWindow(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        // Each dialog gets its own serial-port service so it doesn't interfere
        // with the main window's port (which is already closed by ChangeCOMPortCommand)
        var serialPort = new SerialPortService();
        var parser     = new BarcodeParser();

        _viewModel  = new ComPortViewModel(serialPort, parser, mainViewModel.GetCurrentPort());
        DataContext = _viewModel;

        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel.SelectedPort is not null)
            _mainViewModel.ApplyNewCOMPort(_viewModel.SelectedPort);

        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
