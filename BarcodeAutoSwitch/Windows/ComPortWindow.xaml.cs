using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BarcodeAutoSwitch.Windows;

/// <summary>
/// Dialog for managing configured barcode-scanner devices (add / remove).
/// File kept as ComPortWindow for project compatibility; class is DeviceManagementWindow.
/// </summary>
public partial class DeviceManagementWindow : Window
{
    private readonly DeviceManagementViewModel                    _viewModel;
    private readonly MainViewModel                                _mainViewModel;
    private readonly Func<BarcodeDeviceType, IBarcodeInputService> _serviceFactory;

    public DeviceManagementWindow(
        MainViewModel mainViewModel,
        Func<BarcodeDeviceType, IBarcodeInputService> serviceFactory)
    {
        _mainViewModel  = mainViewModel;
        _serviceFactory = serviceFactory;

        _viewModel  = new DeviceManagementViewModel(mainViewModel.GetConfiguredDevices());
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void OnRemoveDevice(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Button)sender).Tag is SavedDevice device)
            _viewModel.RemoveDevice(device);
    }

    private void OnAddDevice(object sender, RoutedEventArgs e)
    {
        var addWindow = new AddDeviceWindow(_serviceFactory) { Owner = this };
        if (addWindow.ShowDialog() == true && addWindow.ConfirmedDevice != null)
        {
            _viewModel.AddDevice(addWindow.ConfirmedDevice);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        // Always apply current list (whether changed or not).
        // MainViewModel saves and reopens all services.
        _mainViewModel.ApplyDeviceList(_viewModel.Devices.ToList());
        base.OnClosed(e);
    }
}
