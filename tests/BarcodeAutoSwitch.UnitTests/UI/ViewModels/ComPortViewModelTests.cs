using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace BarcodeAutoSwitch.UnitTests.UI.ViewModels;

/// <summary>
/// Tests for <see cref="AddDeviceViewModel"/>.
/// File kept as ComPortViewModelTests for project compatibility.
/// </summary>
public class AddDeviceViewModelTests : IDisposable
{
    private readonly Mock<IBarcodeInputService> _testService = new();
    private readonly BarcodeParser              _parser      = new();

    private EventHandler<string>? _dataHandler;

    private readonly IReadOnlyList<BarcodeDeviceInfo> _devices = new[]
    {
        new BarcodeDeviceInfo("COM1", "COM1", BarcodeDeviceType.SerialPort),
        new BarcodeDeviceInfo("COM2", "COM2", BarcodeDeviceType.SerialPort),
        new BarcodeDeviceInfo("COM3", "COM3", BarcodeDeviceType.SerialPort),
    };

    public AddDeviceViewModelTests()
    {
        _testService.Setup(s => s.Open(It.IsAny<string>())).Returns(true);
        _testService.SetupAdd(s => s.DataReceived += It.IsAny<EventHandler<string>>())
                    .Callback<EventHandler<string>>(h => _dataHandler += h);
        _testService.SetupRemove(s => s.DataReceived -= It.IsAny<EventHandler<string>>())
                    .Callback<EventHandler<string>>(h => _dataHandler -= h);
    }

    private AddDeviceViewModel CreateSut() =>
        new(_devices, _ => _testService.Object, _parser);

    [Fact]
    public void Constructor_PopulatesAvailableDevices()
    {
        var sut = CreateSut();
        sut.AvailableDevices.Should().HaveCount(3);
        sut.Dispose();
    }

    [Fact]
    public void Constructor_SetsSelectedDevice_ToFirst()
    {
        var sut = CreateSut();
        sut.SelectedDevice?.DeviceId.Should().Be("COM1");
        sut.Dispose();
    }

    [Fact]
    public void Constructor_InitialTestResult_IsIdle()
    {
        var sut = CreateSut();
        sut.TestResult.Should().Be(PortTestResult.Idle);
        sut.Dispose();
    }

    [Fact]
    public void InitialTestResultText_ContainsScansiona()
    {
        var sut = CreateSut();
        sut.TestResultText.Should().Contain("Scansiona");
        sut.Dispose();
    }

    [Fact]
    public void InitialIsAddEnabled_IsFalse()
    {
        var sut = CreateSut();
        sut.IsAddEnabled.Should().BeFalse();
        sut.Dispose();
    }

    [Fact]
    public void SelectedDevice_Changed_ClosesAndOpensNewDevice()
    {
        var sut    = CreateSut();
        var newDev = _devices.First(d => d.DeviceId == "COM3");

        sut.SelectedDevice = newDev;

        _testService.Verify(s => s.Close(), Times.AtLeastOnce);
        _testService.Verify(s => s.Open("COM3"), Times.AtLeastOnce);
        sut.Dispose();
    }

    public void Dispose() { }
}
