namespace BarcodeAutoSwitch.Core.Models;

public enum BarcodeDestination
{
    AdriaticaPress,
    NegozioFacile,
    DoNotSwitch,  // Type the barcode in the currently focused window without switching
    Ignore        // Reserved: discard the barcode entirely (no output, no switch)
}
