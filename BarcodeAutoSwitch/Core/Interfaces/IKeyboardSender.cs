namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IKeyboardSender
{
    void SendText(string text);
    void SendKey(string key);
    void SendAlt(char key);
}
