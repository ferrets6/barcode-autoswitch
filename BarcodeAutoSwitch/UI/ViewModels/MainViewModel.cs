using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Infrastructure;
using BarcodeAutoSwitch.UI.Commands;
using BarcodeAutoSwitch.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfApplication = System.Windows.Application;
using Thread = System.Threading.Thread;

namespace BarcodeAutoSwitch.UI.ViewModels;

/// <summary>
/// Main application ViewModel.
/// Manages a list of active barcode-input services (one per configured device)
/// that all feed the same processing pipeline simultaneously.
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IBarcodeParser   _parser;
    private readonly IBarcodeRouter   _router;
    private readonly IWindowSwitcher  _windowSwitcher;
    private readonly IKeyboardSender  _keyboard;
    private readonly IAppSettings     _settings;
    private readonly Func<BarcodeDeviceType, IBarcodeInputService> _serviceFactory;

    // All currently-open input services (one per active configured device)
    private readonly List<IBarcodeInputService> _activeServices = new();

    private bool _isAutoSwitchEnabled = true;
    private bool _isBrowserVisible    = true;

    // ── Events for the View ───────────────────────────────────────────────────
    public event EventHandler? BarcodeForAdriaticaPress;
    /// <summary>Raised when the user clicks "Gestisci dispositivi".</summary>
    public event EventHandler? RequestManageDevices;

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand ToggleAutoSwitchCommand { get; }
    public ICommand ShowDebugLogCommand     { get; }
    public ICommand ManageDevicesCommand    { get; }

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

    public string StatusText              => IsAutoSwitchEnabled ? "Attivo"    : "Non attivo";
    public string StatusColor             => IsAutoSwitchEnabled ? "Green"     : "Red";
    public string EnableDisableButtonText => IsAutoSwitchEnabled ? "Disattiva" : "Attiva";

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainViewModel(
        IBarcodeParser                               parser,
        IBarcodeRouter                               router,
        IWindowSwitcher                              windowSwitcher,
        IKeyboardSender                              keyboard,
        IAppSettings                                 settings,
        Func<BarcodeDeviceType, IBarcodeInputService> serviceFactory)
    {
        _parser         = parser;
        _router         = router;
        _windowSwitcher = windowSwitcher;
        _keyboard       = keyboard;
        _settings       = settings;
        _serviceFactory = serviceFactory;

        ToggleAutoSwitchCommand = new RelayCommand(ToggleAutoSwitch);
        ShowDebugLogCommand     = new RelayCommand(DebugLogWindow.ShowHide);
        ManageDevicesCommand    = new RelayCommand(OnManageDevices);

        LoadAndStartDevicesFromSettings();
    }

    // ── Device management ─────────────────────────────────────────────────────

    private void LoadAndStartDevicesFromSettings()
    {
        DisposeActiveServices();

        bool anyOpened = false;

        foreach (var device in _settings.ConfiguredDevices)
        {
            string resolvedId = ResolveDeviceId(device);
            var service = _serviceFactory(device.Type);
            bool opened = service.Open(resolvedId);

            device.IsAvailable = opened;

            if (opened)
            {
                service.DataReceived  += HandleDataReceived;
                service.ErrorReceived += HandleErrorReceived;
                _activeServices.Add(service);
                anyOpened = true;
            }
            else
            {
                Console.WriteLine($"[DEVICES] Dispositivo non disponibile: {device.DisplayName} ({device.DeviceId})");
                service.Dispose();
            }
        }

        // Browser shown when: no devices configured (fresh install) OR at least one opened
        IsBrowserVisible = _settings.ConfiguredDevices.Count == 0 || anyOpened;
    }

    private void DisposeActiveServices()
    {
        foreach (var svc in _activeServices)
        {
            svc.DataReceived  -= HandleDataReceived;
            svc.ErrorReceived -= HandleErrorReceived;
            svc.Close();
            svc.Dispose();
        }
        _activeServices.Clear();
    }

    private static string ResolveDeviceId(SavedDevice device) => device.DeviceId;

    private void OnManageDevices()
    {
        // Close all services; the dialog manages its own test services independently.
        // When the dialog closes, ApplyDeviceList / ReopenFromSettings rebuilds them.
        DisposeActiveServices();
        RequestManageDevices?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves the new device list and opens all found services.
    /// Called by the device-management dialog when the user confirms changes.
    /// </summary>
    public void ApplyDeviceList(IReadOnlyList<SavedDevice> devices)
    {
        _settings.ConfiguredDevices = devices.ToList();
        _settings.Save();
        LoadAndStartDevicesFromSettings();
    }

    /// <summary>
    /// Re-opens services from the current settings without saving.
    /// Called when the management dialog is closed without making changes.
    /// </summary>
    public void ReopenFromSettings() => LoadAndStartDevicesFromSettings();

    /// <summary>Returns the configured devices (with IsAvailable flags set at last startup).</summary>
    public IReadOnlyList<SavedDevice> GetConfiguredDevices() => _settings.ConfiguredDevices;

    // ── Barcode pipeline ──────────────────────────────────────────────────────

    private void HandleDataReceived(object? sender, string rawData)
    {
        Console.WriteLine($"[INPUT] Dati ricevuti: '{rawData}' (lunghezza: {rawData.Length})");

        if (_parser.IsControlCode(rawData, out var controlType))
        {
            Console.WriteLine($"[INPUT] Codice di controllo: {controlType}");
            if (controlType == ControlCodeType.EnableDisableToggle)
                RunOnUiThread(ToggleAutoSwitch);
        }
        else
        {
            ProcessBarcode(rawData);
        }
    }

    private void ProcessBarcode(string rawData)
    {
        var reading = _parser.Parse(rawData);
        Console.WriteLine($"[BARCODE] Tipo={reading.BarcodeType} | ID='{reading.CodeIdentifier}' | Codice='{reading.CodeValue}'");

        bool sendToKeyboard = false;

        if (IsAutoSwitchEnabled)
        {
            var destination = _router.Route(reading);
            Console.WriteLine($"[ROUTING] Destinazione: {destination}");
            switch (destination)
            {
                case BarcodeDestination.AdriaticaPress:
                    _windowSwitcher.BringToFront("BarcodeAutoSwitch");
                    RunOnUiThread(() => BarcodeForAdriaticaPress?.Invoke(this, EventArgs.Empty));
                    Console.WriteLine($"[KEYBOARD] Invio Alt+T al browser");
                    _keyboard.SendAlt('T');
                    Thread.Sleep(100);
                    Console.WriteLine($"[KEYBOARD] Invio codice '{reading.CodeValue}'");
                    _keyboard.SendText(reading.CodeValue);
                    _keyboard.SendKey("{ENTER}");
                    Thread.Sleep(100);
                    Console.WriteLine("[KEYBOARD] Invio completato.");
                    return;

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
            Console.WriteLine($"[KEYBOARD] Invio codice '{reading.CodeValue}' a NegozioFacile");
            _keyboard.SendText(reading.CodeValue);
            _keyboard.SendKey("{ENTER}");
        }
    }

    private void HandleErrorReceived(object? sender, string error)
    {
        Console.WriteLine($"Errore dispositivo: {error}");
        // Don't hide browser on single device error when others are still active
    }

    // ── Command implementations ───────────────────────────────────────────────

    private void ToggleAutoSwitch()
    {
        IsAutoSwitchEnabled = !IsAutoSwitchEnabled;
        Console.WriteLine($"Auto Switch: {(IsAutoSwitchEnabled ? "abilitato" : "disabilitato")}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void RunOnUiThread(Action action)
    {
        var d = WpfApplication.Current.Dispatcher;
        if (d.CheckAccess()) action();
        else d.Invoke(action);
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => DisposeActiveServices();
}
