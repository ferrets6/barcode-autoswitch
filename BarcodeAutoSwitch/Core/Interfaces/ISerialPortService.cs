namespace BarcodeAutoSwitch.Core.Interfaces;

public interface ISerialPortService : IDisposable
{
    bool IsOpen { get; }
    string PortName { get; }

    event EventHandler<string> DataReceived;
    event EventHandler<string> ErrorReceived;

    bool Open(string portName);
    void Close();
    IReadOnlyList<string> GetAvailablePorts();
}
