namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IAppSettings
{
    string AdriaticaPressVenditaUrl { get; }
    string AdriaticaPressLoginUrl { get; }
    string AdriaticaPressAfterLoginUrl { get; }
    string NegozioFacileProcessName { get; }
    string SelectedSerialPort { get; set; }
    void Save();
}
