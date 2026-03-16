using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.UI.ViewModels;
using FluentAssertions;

namespace BarcodeAutoSwitch.UnitTests.UI.ViewModels;

public class DeviceManagementViewModelTests
{
    private static SavedDevice Device(string id) =>
        new() { DeviceId = id, Type = BarcodeDeviceType.SerialPort, DisplayName = id };

    [Fact]
    public void Constructor_PopulatesDevices_FromInitialList()
    {
        var sut = new DeviceManagementViewModel(new[] { Device("COM1"), Device("COM2") });
        sut.Devices.Should().HaveCount(2);
    }

    [Fact]
    public void AddDevice_AppendsDevice_WhenNotAlreadyPresent()
    {
        var sut = new DeviceManagementViewModel(new[] { Device("COM1") });
        sut.AddDevice(Device("COM2"));
        sut.Devices.Should().HaveCount(2);
        sut.Devices.Should().Contain(d => d.DeviceId == "COM2");
    }

    [Fact]
    public void AddDevice_Deduplicates_WhenSameIdAdded()
    {
        var sut = new DeviceManagementViewModel(new[] { Device("COM1") });
        sut.AddDevice(Device("COM1"));
        sut.Devices.Should().HaveCount(1);
    }

    [Fact]
    public void AddDevice_Deduplicates_CaseInsensitive()
    {
        var sut = new DeviceManagementViewModel(new[] { Device("COM1") });
        sut.AddDevice(new SavedDevice { DeviceId = "com1", Type = BarcodeDeviceType.SerialPort, DisplayName = "com1" });
        sut.Devices.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveDevice_RemovesExistingDevice()
    {
        var dev = Device("COM1");
        var sut = new DeviceManagementViewModel(new[] { dev, Device("COM2") });
        sut.RemoveDevice(dev);
        sut.Devices.Should().HaveCount(1);
        sut.Devices.Should().NotContain(d => d.DeviceId == "COM1");
    }

    [Fact]
    public void Constructor_EmptyList_ResultsInEmptyDevices()
    {
        var sut = new DeviceManagementViewModel(Array.Empty<SavedDevice>());
        sut.Devices.Should().BeEmpty();
    }
}
