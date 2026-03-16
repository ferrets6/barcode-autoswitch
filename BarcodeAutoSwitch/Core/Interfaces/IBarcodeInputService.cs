namespace BarcodeAutoSwitch.Core.Interfaces;

/// <summary>
/// Common interface for all barcode input sources (COM serial port, USB HID, etc.).
/// All implementations expose the same event-driven pipeline so the rest of the
/// application does not need to know which physical device is attached.
/// </summary>
public interface IBarcodeInputService : IDisposable
{
    bool   IsOpen    { get; }
    string DeviceId  { get; }

    event EventHandler<string> DataReceived;
    event EventHandler<string> ErrorReceived;

    bool Open(string deviceId);
    void Close();
}
