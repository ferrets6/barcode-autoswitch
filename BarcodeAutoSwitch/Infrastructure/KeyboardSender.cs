using BarcodeAutoSwitch.Core.Interfaces;
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
        // Escape special SendKeys characters
        string escaped = EscapeForSendKeys(text);
        SendKeys.SendWait(escaped);
    }

    public void SendKey(string key) => SendKeys.SendWait(key);

    public void SendAlt(char key) => SendKeys.SendWait($"%{key}");

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
