using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IAppSettings
{
    string AdriaticaPressVenditaUrl    { get; }
    string AdriaticaPressLoginUrl      { get; }
    string AdriaticaPressAfterLoginUrl { get; }
    string NegozioFacileProcessName    { get; }
    bool   TrimTrailingZeros           { get; }

    /// <summary>All barcode-scanner devices the user has configured.</summary>
    List<SavedDevice> ConfiguredDevices { get; set; }

    void Save();
}
