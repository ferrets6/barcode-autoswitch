using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace BarcodeAutoSwitch.UnitTests.UI.ViewModels;

public class MainViewModelTests : IDisposable
{
    private readonly Mock<ISerialPortService> _serialPort   = new();
    private readonly Mock<IWindowSwitcher>    _windowSwitcher = new();
    private readonly Mock<IKeyboardSender>    _keyboard     = new();
    private readonly Mock<IAppSettings>       _settings     = new();
    private readonly BarcodeParser            _parser       = new();
    private readonly BarcodeRouter            _router       = new(new IRoutingStrategy[]
    {
        new NewspaperRoutingStrategy(),
        new DefaultRoutingStrategy()
    });

    private readonly MainViewModel _sut;

    // Capture events fired on DataReceived
    private EventHandler<string>? _dataReceivedHandler;

    public MainViewModelTests()
    {
        _settings.Setup(s => s.SelectedSerialPort).Returns("COM1");
        _settings.Setup(s => s.NegozioFacileProcessName).Returns("notepad");
        _serialPort.Setup(p => p.Open(It.IsAny<string>())).Returns(true);
        _serialPort.Setup(p => p.GetAvailablePorts()).Returns(new List<string> { "COM1" });
        _serialPort.SetupAdd(p => p.DataReceived += It.IsAny<EventHandler<string>>())
                   .Callback<EventHandler<string>>(h => _dataReceivedHandler += h);

        _sut = new MainViewModel(_serialPort.Object, _parser, _router,
                                 _windowSwitcher.Object, _keyboard.Object, _settings.Object);
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void InitialState_IsAutoSwitchEnabled_True()
        => _sut.IsAutoSwitchEnabled.Should().BeTrue();

    [Fact]
    public void InitialState_StatusText_IsAttivo()
        => _sut.StatusText.Should().Be("Attivo");

    [Fact]
    public void InitialState_StatusColor_IsGreen()
        => _sut.StatusColor.Should().Be("Green");

    [Fact]
    public void InitialState_ButtonText_IsDisattiva()
        => _sut.EnableDisableButtonText.Should().Be("Disattiva");

    // ── Toggle ────────────────────────────────────────────────────────────────

    [Fact]
    public void ToggleAutoSwitchCommand_Disables_WhenEnabled()
    {
        _sut.ToggleAutoSwitchCommand.Execute(null);

        _sut.IsAutoSwitchEnabled.Should().BeFalse();
        _sut.StatusText.Should().Be("Non attivo");
        _sut.StatusColor.Should().Be("Red");
        _sut.EnableDisableButtonText.Should().Be("Attiva");
    }

    [Fact]
    public void ToggleAutoSwitchCommand_ReEnables_WhenDisabled()
    {
        _sut.ToggleAutoSwitchCommand.Execute(null); // disable
        _sut.ToggleAutoSwitchCommand.Execute(null); // re-enable

        _sut.IsAutoSwitchEnabled.Should().BeTrue();
        _sut.StatusText.Should().Be("Attivo");
    }

    [Fact]
    public void PropertyChanged_Raised_WhenToggled()
    {
        var changed = new List<string>();
        _sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        _sut.ToggleAutoSwitchCommand.Execute(null);

        changed.Should().Contain(nameof(MainViewModel.IsAutoSwitchEnabled));
        changed.Should().Contain(nameof(MainViewModel.StatusText));
        changed.Should().Contain(nameof(MainViewModel.StatusColor));
        changed.Should().Contain(nameof(MainViewModel.EnableDisableButtonText));
    }

    // ── Port open failure ─────────────────────────────────────────────────────

    [Fact]
    public void WhenPortFailsToOpen_IsBrowserVisible_IsFalse()
    {
        _serialPort.Setup(p => p.Open(It.IsAny<string>())).Returns(false);

        var vm = new MainViewModel(_serialPort.Object, _parser, _router,
                                   _windowSwitcher.Object, _keyboard.Object, _settings.Object);

        vm.IsBrowserVisible.Should().BeFalse();
        vm.Dispose();
    }

    // ── ApplyNewCOMPort ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyNewCOMPort_SavesSettings_AndOpensPort()
    {
        _sut.ApplyNewCOMPort("COM3");

        _settings.VerifySet(s => s.SelectedSerialPort = "COM3");
        _settings.Verify(s => s.Save(), Times.Once);
        _serialPort.Verify(p => p.Open("COM3"), Times.AtLeastOnce);
    }

    public void Dispose() => _sut.Dispose();
}
