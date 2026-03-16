using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BarcodeAutoSwitch.UI.ViewModels;

/// <summary>
/// ViewModel for the "Gestisci dispositivi" dialog.
/// Holds the editable list of configured devices; the View handles opening the
/// "Aggiungi" sub-dialog and feeds confirmed devices back via <see cref="AddDevice"/>.
/// </summary>
public class DeviceManagementViewModel
{
    public ObservableCollection<SavedDevice> Devices { get; }

    public DeviceManagementViewModel(IReadOnlyList<SavedDevice> currentDevices)
    {
        Devices = new ObservableCollection<SavedDevice>(currentDevices);
    }

    public void AddDevice(SavedDevice device)
    {
        if (!Devices.Any(d => string.Equals(d.DeviceId, device.DeviceId, StringComparison.OrdinalIgnoreCase)))
            Devices.Add(device);
    }

    public void RemoveDevice(SavedDevice device) => Devices.Remove(device);
}
