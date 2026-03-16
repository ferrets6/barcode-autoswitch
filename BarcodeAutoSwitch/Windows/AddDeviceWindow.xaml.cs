using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.ViewModels;
using System.Windows;

namespace BarcodeAutoSwitch.Windows;

public partial class AddDeviceWindow : Window
{
    private readonly AddDeviceViewModel _viewModel;

    public AddDeviceWindow(Func<BarcodeDeviceType, IBarcodeInputService> serviceFactory)
    {
        // Build the unified device list: COM ports first, then USB HID keyboards
        var devices = new List<BarcodeDeviceInfo>();

        foreach (var port in ComPortEnumerator.GetAvailablePorts())
            devices.Add(new BarcodeDeviceInfo(port.PortName, port.DisplayName, BarcodeDeviceType.SerialPort, port.HardwareId));

        foreach (var hid in RawInputBarcodeService.GetAvailableDevices())
            devices.Add(hid);

        var parser   = new BarcodeParser();
        _viewModel   = new AddDeviceViewModel(devices, serviceFactory, parser);
        DataContext  = _viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Returns the confirmed device as a <see cref="SavedDevice"/> to persist,
    /// or <c>null</c> if the dialog was cancelled or no device was selected.
    /// </summary>
    public SavedDevice? ConfirmedDevice
    {
        get
        {
            var sel = _viewModel.SelectedDevice;
            if (sel == null) return null;
            return new SavedDevice
            {
                DeviceId           = sel.DeviceId,
                Type               = sel.Type,
                HardwareId         = sel.HardwareId,
                DisplayName        = sel.DisplayName,
                TrimTrailingZeros  = _viewModel.TrimTrailingZeros,
                IsAvailable        = true
            };
        }
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
