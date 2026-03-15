namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IWindowSwitcher
{
    /// <summary>Brings the main window of the named process to the foreground. Returns true if successful.</summary>
    bool BringToFront(string processName);
}
