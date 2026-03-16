using System.IO;
using System.Text;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Captures all Console.WriteLine output and forwards it to subscribed listeners
/// (e.g. the WPF debug log window). Call <see cref="Initialize"/> once at startup
/// to redirect Console.Out; after that every Console.WriteLine is captured.
/// </summary>
public static class AppLogger
{
    public static event Action<string>? MessageLogged;

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Console.SetOut(new LogWriter());
    }

    private sealed class LogWriter : TextWriter
    {
        private readonly StringBuilder _line = new();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (value == '\n')
                Flush();
            else if (value != '\r')
                _line.Append(value);
        }

        public override void WriteLine(string? value)
        {
            _line.Append(value);
            Flush();
        }

        public override void WriteLine()
        {
            Flush();
        }

        public override void Flush()
        {
            if (_line.Length == 0) return;
            var msg = $"[{DateTime.Now:HH:mm:ss}] {_line}";
            _line.Clear();
            MessageLogged?.Invoke(msg);
        }
    }
}
