namespace BarcodeAutoSwitch.Core.Models;

public enum BarcodeDeviceType { SerialPort, UsbHid }

/// <summary>Describes a physical barcode-scanner device available for selection.</summary>
public record BarcodeDeviceInfo(
    string  DeviceId,
    string  DisplayName,
    BarcodeDeviceType Type,
    string? HardwareId = null);  // populated for USB-backed COM ports
