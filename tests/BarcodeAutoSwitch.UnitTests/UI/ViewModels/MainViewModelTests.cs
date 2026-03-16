using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using BarcodeAutoSwitch.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace BarcodeAutoSwitch.UnitTests.UI.ViewModels;

public class MainViewModelTests : IDisposable
{
    private readonly Mock<IBarcodeInputService> _service        = new();
    private readonly Mock<IWindowSwitcher>      _windowSwitcher = new();
    private readonly Mock<IKeyboardSender>      _keyboard       = new();
    private readonly Mock<IAppSettings>         _settings       = new();
    private readonly BarcodeParser              _parser         = new();
    private readonly BarcodeRouter              _router         = new(new IRoutingStrategy[]
    {
        new NewspaperRoutingStrategy(),
        new DefaultRoutingStrategy()
    });

    private readonly List<SavedDevice> _configuredDevices = new()
    {
        new SavedDevice { DeviceId = "COM1", Type = BarcodeDeviceType.SerialPort, DisplayName = "COM1" }
    };

    private EventHandler<string>? _dataReceivedHandler;

    private readonly MainViewModel _sut;

    public MainViewModelTests()
    {
        var storedDevices = _configuredDevices;
        _settings.SetupSet(s => s.ConfiguredDevices = It.IsAny<List<SavedDevice>>())
                 .Callback<List<SavedDevice>>(v => storedDevices = v);
        _settings.Setup(s => s.ConfiguredDevices).Returns(() => storedDevices);
        _settings.Setup(s => s.NegozioFacileProcessName).Returns("notepad");

        _service.Setup(p => p.Open(It.IsAny<string>())).Returns(true);
        _service.SetupAdd(p => p.DataReceived += It.IsAny<EventHandler<string>>())
                .Callback<EventHandler<string>>(h => _dataReceivedHandler += h);

        _sut = new MainViewModel(_parser, _router,
                                 _windowSwitcher.Object, _keyboard.Object, _settings.Object,
                                 _ => _service.Object);
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

    [Fact]
    public void InitialState_IsBrowserVisible_True_WhenDeviceOpens()
        => _sut.IsBrowserVisible.Should().BeTrue();

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
        _sut.ToggleAutoSwitchCommand.Execute(null);
        _sut.ToggleAutoSwitchCommand.Execute(null);

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

    // ── Device open failure ───────────────────────────────────────────────────

    [Fact]
    public void WhenAllDevicesFailToOpen_IsBrowserVisible_IsFalse()
    {
        var failService = new Mock<IBarcodeInputService>();
        failService.Setup(p => p.Open(It.IsAny<string>())).Returns(false);

        var devices = new List<SavedDevice>
        {
            new() { DeviceId = "COM1", Type = BarcodeDeviceType.SerialPort, DisplayName = "COM1" }
        };
        _settings.Setup(s => s.ConfiguredDevices).Returns(devices);

        var vm = new MainViewModel(_parser, _router,
                                   _windowSwitcher.Object, _keyboard.Object, _settings.Object,
                                   _ => failService.Object);

        vm.IsBrowserVisible.Should().BeFalse();
        vm.Dispose();
    }

    [Fact]
    public void WhenNoDevicesConfigured_IsBrowserVisible_IsTrue()
    {
        _settings.Setup(s => s.ConfiguredDevices).Returns(new List<SavedDevice>());

        var vm = new MainViewModel(_parser, _router,
                                   _windowSwitcher.Object, _keyboard.Object, _settings.Object,
                                   _ => _service.Object);

        vm.IsBrowserVisible.Should().BeTrue();
        vm.Dispose();
    }

    // ── ApplyDeviceList ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyDeviceList_SavesSettings_AndOpensDevices()
    {
        var newDevices = new List<SavedDevice>
        {
            new() { DeviceId = "COM3", Type = BarcodeDeviceType.SerialPort, DisplayName = "COM3" }
        };

        _sut.ApplyDeviceList(newDevices);

        _settings.VerifySet(s => s.ConfiguredDevices = It.IsAny<List<SavedDevice>>(), Times.Once);
        _settings.Verify(s => s.Save(), Times.Once);
        _service.Verify(p => p.Open("COM3"), Times.AtLeastOnce);
    }

    // ── GetConfiguredDevices ──────────────────────────────────────────────────

    [Fact]
    public void GetConfiguredDevices_ReturnsSettingsDevices()
    {
        var devices = _sut.GetConfiguredDevices();

        devices.Should().HaveCount(1);
        devices[0].DeviceId.Should().Be("COM1");
        devices[0].Type.Should().Be(BarcodeDeviceType.SerialPort);
    }

    public void Dispose() => _sut.Dispose();
}
