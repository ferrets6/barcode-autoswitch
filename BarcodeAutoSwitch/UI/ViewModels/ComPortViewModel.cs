using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfApplication = System.Windows.Application;

namespace BarcodeAutoSwitch.UI.ViewModels;

public enum PortTestResult { Idle, Testing, Ok, Failed }

/// <summary>
/// ViewModel for the COM-port selection dialog.
/// Handles port enumeration and test-barcode detection.
/// </summary>
public class ComPortViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISerialPortService _serialPort;
    private readonly IBarcodeParser     _parser;

    private string?        _selectedPort;
    private PortTestResult _testResult = PortTestResult.Idle;
    private bool           _suppressSelectionChanged;

    public ObservableCollection<string> AvailablePorts { get; } = new();

    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (_selectedPort == value || _suppressSelectionChanged) return;
            _selectedPort = value;
            OnPropertyChanged();
            OpenSelectedPort();
        }
    }

    public PortTestResult TestResult
    {
        get => _testResult;
        private set { _testResult = value; OnPropertyChanged(); }
    }

    public ComPortViewModel(ISerialPortService serialPort, IBarcodeParser parser, string currentPort)
    {
        _serialPort = serialPort;
        _parser     = parser;

        foreach (var p in serialPort.GetAvailablePorts())
            AvailablePorts.Add(p);

        _serialPort.DataReceived += OnDataReceived;

        // Select current port without firing the changed handler that closes/reopens
        _suppressSelectionChanged = true;
        _selectedPort = currentPort;
        OnPropertyChanged(nameof(SelectedPort));
        _suppressSelectionChanged = false;

        OpenSelectedPort();
    }

    private void OpenSelectedPort()
    {
        if (_selectedPort is null) return;
        TestResult = PortTestResult.Idle;
        _serialPort.Close();
        _serialPort.Open(_selectedPort);
    }

    private void OnDataReceived(object? sender, string rawData)
    {
        if (!_parser.IsControlCode(rawData, out var controlType)) return;

        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            TestResult = controlType == ControlCodeType.CheckPort
                ? PortTestResult.Ok
                : PortTestResult.Failed;
        });

        if (controlType == ControlCodeType.CheckPort)
            _serialPort.Close();
    }

    public void Dispose()
    {
        _serialPort.DataReceived -= OnDataReceived;
        _serialPort.Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
