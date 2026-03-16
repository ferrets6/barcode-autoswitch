using BarcodeAutoSwitch.Core.Interfaces;
using System.IO.Ports;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Wraps <see cref="SerialPort"/> and exposes a clean event-driven interface.
/// Implements <see cref="ISerialPortService"/> so it can be replaced by a
/// mock/stub in tests without touching any serial hardware.
/// </summary>
public class SerialPortService : IBarcodeInputService
{
    private SerialPort? _port;
    private const string NewLine = "\r\n";

    public bool   IsOpen   => _port?.IsOpen   ?? false;
    public string DeviceId => _port?.PortName ?? string.Empty;

    public event EventHandler<string> DataReceived  = delegate { };
    public event EventHandler<string> ErrorReceived = delegate { };

    public bool Open(string portName)
    {
        Close();
        try
        {
            _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.RequestToSend,
                NewLine   = NewLine
            };
            _port.DataReceived  += OnDataReceived;
            _port.ErrorReceived += OnErrorReceived;
            _port.Open();
            Console.WriteLine($"Porta seriale aperta: {portName}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore apertura porta {portName}: {ex.Message}");
            _port = null;
            return false;
        }
    }

    public void Close()
    {
        if (_port is null) return;
        try
        {
            _port.DataReceived  -= OnDataReceived;
            _port.ErrorReceived -= OnErrorReceived;
            if (_port.IsOpen) _port.Close();
        }
        catch { /* ignore on close */ }
        finally
        {
            _port.Dispose();
            _port = null;
        }
    }

    public IReadOnlyList<string> GetAvailablePorts() =>
        SerialPort.GetPortNames().OrderBy(p => p).ToList();

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port is null || !_port.IsOpen) return;
        try
        {
            string data = _port.ReadLine();
            DataReceived(this, data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore lettura porta: {ex.Message}");
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        Console.WriteLine($"Errore seriale: {e.EventType}");
        ErrorReceived(this, e.EventType.ToString());
    }

    public void Dispose() => Close();
}
