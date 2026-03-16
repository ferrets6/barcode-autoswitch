using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfApplication = System.Windows.Application;

namespace BarcodeAutoSwitch.UI.ViewModels;

/// <summary>
/// ViewModel for the "Aggiungi dispositivo" dialog.
/// Opens a test service for the selected device and waits for the test barcode.
/// The "Aggiungi" button in the view is enabled only when <see cref="IsAddEnabled"/> is true.
/// </summary>
public class AddDeviceViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IBarcodeParser _parser;
    private readonly Func<BarcodeDeviceType, IBarcodeInputService> _serviceFactory;

    private IBarcodeInputService? _testService;
    private BarcodeDeviceInfo?    _selectedDevice;
    private PortTestResult        _testResult = PortTestResult.Idle;
    private bool                  _suppressSelectionChanged;
    private bool                  _hasIdentifierPrefix = true;

    public ObservableCollection<BarcodeDeviceInfo> AvailableDevices { get; } = new();

    public BarcodeDeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (_selectedDevice == value || _suppressSelectionChanged) return;
            _selectedDevice = value;
            OnPropertyChanged();
            OpenSelectedDevice();
        }
    }

    public bool HasIdentifierPrefix
    {
        get => _hasIdentifierPrefix;
        set
        {
            if (_hasIdentifierPrefix == value) return;
            _hasIdentifierPrefix = value;
            OnPropertyChanged();
        }
    }

    public PortTestResult TestResult
    {
        get => _testResult;
        private set
        {
            _testResult = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TestResultText));
            OnPropertyChanged(nameof(TestResultColor));
            OnPropertyChanged(nameof(IsAddEnabled));
        }
    }

    public string TestResultText => _testResult switch
    {
        PortTestResult.Ok     => "✓  Dispositivo corretto",
        PortTestResult.Failed => "✗  Dispositivo non corretto",
        _                     => "Scansiona il barcode per testare il dispositivo"
    };

    public string TestResultColor => _testResult switch
    {
        PortTestResult.Ok     => "Green",
        PortTestResult.Failed => "Red",
        _                     => "Gray"
    };

    public bool IsAddEnabled => _testResult == PortTestResult.Ok;

    public AddDeviceViewModel(
        IReadOnlyList<BarcodeDeviceInfo> availableDevices,
        Func<BarcodeDeviceType, IBarcodeInputService> serviceFactory,
        IBarcodeParser parser)
    {
        _serviceFactory = serviceFactory;
        _parser         = parser;

        foreach (var d in availableDevices)
            AvailableDevices.Add(d);

        // Pre-select first device without triggering test open
        _suppressSelectionChanged = true;
        _selectedDevice = AvailableDevices.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedDevice));
        _suppressSelectionChanged = false;

        OpenSelectedDevice();
    }

    private void OpenSelectedDevice()
    {
        TestResult = PortTestResult.Idle;
        CloseTestService();

        if (_selectedDevice == null) return;

        _testService = _serviceFactory(_selectedDevice.Type);
        _testService.DataReceived += OnDataReceived;
        _testService.Open(_selectedDevice.DeviceId);
    }

    private void OnDataReceived(object? sender, string rawData)
    {
        bool isControl = _parser.IsControlCode(rawData, out var controlType, _hasIdentifierPrefix);
        Console.WriteLine($"[VALIDAZIONE] Letto: '{rawData}' (len={rawData.Length} hasIdentifierPrefix={_hasIdentifierPrefix}) → isControlCode={isControl}, tipo={controlType}");
        if (!isControl) return;

        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            TestResult = controlType == ControlCodeType.CheckPort
                ? PortTestResult.Ok
                : PortTestResult.Failed;
        });

        if (TestResult == PortTestResult.Ok)
            CloseTestService();
    }

    private void CloseTestService()
    {
        if (_testService == null) return;
        _testService.DataReceived -= OnDataReceived;
        _testService.Close();
        _testService.Dispose();
        _testService = null;
    }

    public void Dispose() => CloseTestService();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
