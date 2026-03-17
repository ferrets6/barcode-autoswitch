namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IDialogService
{
    /// <summary>
    /// Shows a non-modal "process not found" alert.
    /// The dialog closes automatically when <paramref name="autoCloseToken"/> is cancelled
    /// (i.e. on the next barcode scan) or when the user clicks "Chiudi" / the window X.
    /// </summary>
    void ShowProcessNotFound(string processName, CancellationToken autoCloseToken);
}