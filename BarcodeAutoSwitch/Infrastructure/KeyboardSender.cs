using BarcodeAutoSwitch.Core.Interfaces;
using System.Threading;
using System.Windows.Forms;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Sends keyboard input to the currently focused window using WinForms SendKeys.
/// Isolated behind <see cref="IKeyboardSender"/> so tests can mock it without
/// actually sending keystrokes to the OS.
/// </summary>
public class KeyboardSender : IKeyboardSender
{
    public void SendText(string text)
    {
        string escaped = EscapeForSendKeys(text);
        Console.WriteLine($"[KEYS] SendText raw='{text}' → escaped='{escaped}' | Thread={Environment.CurrentManagedThreadId} IsUI={Thread.CurrentThread.GetApartmentState() == ApartmentState.STA}");
        SendKeys.SendWait(escaped);
        Console.WriteLine($"[KEYS] SendText completato");
    }

    public void SendKey(string key)
    {
        Console.WriteLine($"[KEYS] SendKey '{key}'");
        SendKeys.SendWait(key);
    }

    public void SendAlt(char key)
    {
        Console.WriteLine($"[KEYS] SendAlt '%{key}' | Thread={Environment.CurrentManagedThreadId} IsUI={Thread.CurrentThread.GetApartmentState() == ApartmentState.STA}");
        SendKeys.SendWait($"%{key}");
        Console.WriteLine($"[KEYS] SendAlt completato");
    }

    /// <summary>
    /// Escapes characters that have special meaning in SendKeys syntax
    /// (+, ^, %, ~, parentheses, brackets).
    /// </summary>
    private static string EscapeForSendKeys(string input)
    {
        // Characters that must be wrapped in braces for SendKeys
        const string specialChars = "+-^%~()[]{}";
        var sb = new System.Text.StringBuilder(input.Length * 2);
        foreach (char c in input)
        {
            if (specialChars.Contains(c))
                sb.Append('{').Append(c).Append('}');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
