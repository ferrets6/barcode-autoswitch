using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.UI.ViewModels;
using FluentAssertions;
using Moq;
using BarcodeAutoSwitch.Core.Interfaces;

namespace BarcodeAutoSwitch.UnitTests.UI.ViewModels;

public class ComPortViewModelTests : IDisposable
{
    private readonly Mock<ISerialPortService> _serialPort = new();
    private readonly BarcodeParser            _parser     = new();

    private EventHandler<string>? _dataHandler;

    public ComPortViewModelTests()
    {
        _serialPort.Setup(p => p.GetAvailablePorts())
                   .Returns(new List<string> { "COM1", "COM2", "COM3" });
        _serialPort.Setup(p => p.Open(It.IsAny<string>())).Returns(true);
        _serialPort.SetupAdd(p => p.DataReceived += It.IsAny<EventHandler<string>>())
                   .Callback<EventHandler<string>>(h => _dataHandler += h);
    }

    private ComPortViewModel CreateSut(string current = "COM1") =>
        new(_serialPort.Object, _parser, current);

    [Fact]
    public void Constructor_PopulatesAvailablePorts()
    {
        var sut = CreateSut();
        sut.AvailablePorts.Should().BeEquivalentTo("COM1", "COM2", "COM3");
        sut.Dispose();
    }

    [Fact]
    public void Constructor_SetsSelectedPort_ToCurrentPort()
    {
        var sut = CreateSut("COM2");
        sut.SelectedPort.Should().Be("COM2");
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
    public void SelectedPort_Changed_ClosesAndReopensPort()
    {
        var sut = CreateSut("COM1");
        sut.SelectedPort = "COM3";

        _serialPort.Verify(p => p.Close(), Times.AtLeastOnce);
        _serialPort.Verify(p => p.Open("COM3"), Times.Once);
        sut.Dispose();
    }

    public void Dispose() { }
}
