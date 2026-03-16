namespace BarcodeAutoSwitch.Core.Models;

public enum BarcodeDeviceType { SerialPort }

/// <summary>Describes a physical barcode-scanner device available for selection.</summary>
public record BarcodeDeviceInfo(
    string  DeviceId,
    string  DisplayName,
    BarcodeDeviceType Type,
    bool HasIdentifierPrefix = false);
