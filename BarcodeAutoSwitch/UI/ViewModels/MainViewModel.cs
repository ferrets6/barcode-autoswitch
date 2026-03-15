using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfApplication = System.Windows.Application;
using Thread = System.Threading.Thread;

namespace BarcodeAutoSwitch.UI.ViewModels;

/// <summary>
/// Main application ViewModel. Owns the barcode processing pipeline and
/// exposes only presentation state to the view — no Win32 / UI code here.
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISerialPortService _serialPort;
    private readonly IBarcodeParser     _parser;
    private readonly IBarcodeRouter     _router;
    private readonly IWindowSwitcher    _windowSwitcher;
    private readonly IKeyboardSender    _keyboard;
    private readonly IAppSettings       _settings;

    private bool   _isAutoSwitchEnabled       = true;
    private bool   _isBrowserVisible          = true;
    private string _pendingAdriaticaPressCode = string.Empty;

    // ── Public events for things the View must do ─────────────────────────────
    /// <summary>Raised when a newspaper barcode arrives and the browser must receive Alt+T.</summary>
    public event EventHandler? BarcodeForAdriaticaPress;
    /// <summary>Raised when the user (or a barcode) requests the COM-port dialog.</summary>
    public event EventHandler? RequestChangeCOMPort;

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand ToggleAutoSwitchCommand  { get; }
    public ICommand ShowHideConsoleCommand   { get; }
    public ICommand ChangeCOMPortCommand     { get; }

    // ── Bindable properties ───────────────────────────────────────────────────
    public bool IsAutoSwitchEnabled
    {
        get => _isAutoSwitchEnabled;
        private set
        {
            if (_isAutoSwitchEnabled == value) return;
            _isAutoSwitchEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(EnableDisableButtonText));
        }
    }

    public bool IsBrowserVisible
    {
        get => _isBrowserVisible;
        private set
        {
            if (_isBrowserVisible == value) return;
            _isBrowserVisible = value;
            OnPropertyChanged();
        }
    }

    public string StatusText             => IsAutoSwitchEnabled ? "Attivo"    : "Non attivo";
    public string StatusColor            => IsAutoSwitchEnabled ? "Green"     : "Red";
    public string EnableDisableButtonText => IsAutoSwitchEnabled ? "Disattiva" : "Attiva";

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainViewModel(
        ISerialPortService serialPort,
        IBarcodeParser     parser,
        IBarcodeRouter     router,
        IWindowSwitcher    windowSwitcher,
        IKeyboardSender    keyboard,
        IAppSettings       settings)
    {
        _serialPort     = serialPort;
        _parser         = parser;
        _router         = router;
        _windowSwitcher = windowSwitcher;
        _keyboard       = keyboard;
        _settings       = settings;

        ToggleAutoSwitchCommand = new RelayCommand(ToggleAutoSwitch);
        ShowHideConsoleCommand  = new RelayCommand(Win32Console.ShowHide);
        ChangeCOMPortCommand    = new RelayCommand(OnChangeCOMPort);

        _serialPort.DataReceived  += HandleDataReceived;
        _serialPort.ErrorReceived += HandleErrorReceived;

        bool opened = _serialPort.Open(_settings.SelectedSerialPort);
        IsBrowserVisible = opened;
    }

    // ── Barcode pipeline ──────────────────────────────────────────────────────
    private void HandleDataReceived(object? sender, string rawData)
    {
        if (_parser.IsControlCode(rawData, out var controlType))
        {
            if (controlType == ControlCodeType.EnableDisableToggle)
                WpfApplication.Current.Dispatcher.Invoke(ToggleAutoSwitch);
        }
        else
        {
            ProcessBarcode(rawData);
        }
    }

    private void ProcessBarcode(string rawData)
    {
        var reading         = _parser.Parse(rawData);
        bool sendToKeyboard = false;

        if (IsAutoSwitchEnabled)
        {
            var destination = _router.Route(reading);
            switch (destination)
            {
                case BarcodeDestination.AdriaticaPress:
                    _pendingAdriaticaPressCode = reading.CodeValue;
                    _windowSwitcher.BringToFront("BarcodeAutoSwitch");
                    WpfApplication.Current.Dispatcher.Invoke(() =>
                        BarcodeForAdriaticaPress?.Invoke(this, EventArgs.Empty));
                    return; // keyboard send is done inside SendAdriaticaPressKey()

                case BarcodeDestination.NegozioFacile:
                    sendToKeyboard = _windowSwitcher.BringToFront(_settings.NegozioFacileProcessName);
                    break;
            }
        }
        else
        {
            sendToKeyboard = true;
        }

        if (sendToKeyboard)
        {
            _keyboard.SendText(reading.CodeValue);
            _keyboard.SendKey("{ENTER}");
        }
    }

    private void HandleErrorReceived(object? sender, string error)
    {
        Console.WriteLine($"Errore porta seriale: {error}");
        WpfApplication.Current.Dispatcher.Invoke(() => IsBrowserVisible = false);
    }

    // ── Command implementations ───────────────────────────────────────────────
    private void ToggleAutoSwitch()
    {
        IsAutoSwitchEnabled = !IsAutoSwitchEnabled;
        Console.WriteLine($"Auto Switch: {(IsAutoSwitchEnabled ? "abilitato" : "disabilitato")}");
    }

    private void OnChangeCOMPort()
    {
        _serialPort.Close();
        RequestChangeCOMPort?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called by the View after the COM-port dialog closes.</summary>
    public void ApplyNewCOMPort(string portName)
    {
        _settings.SelectedSerialPort = portName;
        _settings.Save();
        bool opened = _serialPort.Open(portName);
        IsBrowserVisible = opened;
    }

    // ── Adriatica Press barcode send ──────────────────────────────────────────
    /// <summary>Returns the currently configured serial port name.</summary>
    public string GetCurrentPort() => _settings.SelectedSerialPort;

    /// <summary>
    /// Called by the View once it has focused the browser.
    /// Sends Alt+T to trigger the input field in Adriatica Press, then sends
    /// the pending barcode code and Enter.
    /// </summary>
    public void SendAdriaticaPressKey()
    {
        _keyboard.SendAlt('T');

        var code = _pendingAdriaticaPressCode;
        _pendingAdriaticaPressCode = string.Empty;

        if (!string.IsNullOrEmpty(code))
        {
            // Small delay so Adriatica Press has time to open its input field
            Thread.Sleep(150);
            _keyboard.SendText(code);
            _keyboard.SendKey("{ENTER}");
        }
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _serialPort.DataReceived  -= HandleDataReceived;
        _serialPort.ErrorReceived -= HandleErrorReceived;
        _serialPort.Dispose();
    }
}
