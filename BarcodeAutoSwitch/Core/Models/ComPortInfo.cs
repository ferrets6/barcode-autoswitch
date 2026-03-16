namespace BarcodeAutoSwitch.Core.Models;

/// <summary>A COM serial port together with its USB hardware identifier (if detectable).</summary>
public record ComPortInfo(
    string  PortName,
    string? HardwareId,   // e.g. "USB\VID_067B&PID_2303"  — null when not USB-backed
    string  DisplayName); // e.g. "COM3 (USB\VID_067B&PID_2303)"
