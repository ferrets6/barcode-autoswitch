using System.Text.Json.Serialization;

namespace BarcodeAutoSwitch.Core.Models;

/// <summary>
/// A barcode-scanner device that has been configured and saved by the user.
/// Persisted in usersettings.json (never committed — stored in %LocalAppData%).
///
/// To add a device manually for testing, edit the file at:
///   %LocalAppData%\BarcodeAutoSwitch\usersettings.json
/// and add an entry like:
/// {
///   "ConfiguredDevices": [
///     {
///       "DeviceId": "COM3",
///       "Type": "SerialPort",
///       "HardwareId": "USB\\VID_067B&PID_2303",
///       "DisplayName": "Lettore barcode (COM3)"
///     }
///   ]
/// }
/// </summary>
public class SavedDevice
{
    /// <summary>COM port name (e.g. "COM3") or USB Raw Input device path.</summary>
    public string DeviceId { get; set; } = string.Empty;

    public BarcodeDeviceType Type { get; set; } = BarcodeDeviceType.SerialPort;

    /// <summary>
    /// Hardware identifier for COM-port devices, e.g. "USB\VID_067B&PID_2303".
    /// When present, the app resolves the current COM port by hardware ID at
    /// startup, so port-number changes (e.g. COM3 → COM5) are handled automatically.
    /// </summary>
    public string? HardwareId { get; set; }

    /// <summary>Human-readable label shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// When true, trailing zeros are stripped from the raw barcode before
    /// checking whether it matches the CheckPort activation code.
    /// Only affects activation-code recognition — normal barcode parsing is unchanged.
    /// </summary>
    public bool TrimTrailingZeros { get; set; } = false;

    /// <summary>
    /// When true, the scanner prepends a single-char identifier before the barcode
    /// (e.g. 'B' for EAN-13, 'M' for ISSN) — typical of serial/COM-port scanners.
    /// When false (default for USB HID), the full raw input is the barcode value
    /// and the type is inferred from the content.
    /// </summary>
    public bool HasIdentifierPrefix { get; set; } = true;

    /// <summary>
    /// Set at runtime by the startup probe; not persisted to JSON.
    /// True = device was found and opened successfully at last startup.
    /// </summary>
    [JsonIgnore]
    public bool IsAvailable { get; set; } = true;
}
