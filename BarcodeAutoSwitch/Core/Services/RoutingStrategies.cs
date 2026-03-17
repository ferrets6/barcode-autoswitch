using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Services;

/// <summary>Routes newspapers/magazines (prefix 977, type M or B) to Adriatica Press.</summary>
public class NewspaperRoutingStrategy : IRoutingStrategy
{
    public bool CanHandle(BarcodeReading reading) =>
        reading.CodeValue.Length >= 3 &&
        reading.CodeValue.StartsWith("977", StringComparison.Ordinal) &&
        (reading.BarcodeType == BarcodeType.ISSN13Plus5 || reading.BarcodeType == BarcodeType.EAN13);

    public BarcodeDestination GetDestination(BarcodeReading reading) => BarcodeDestination.AdriaticaPress;
}

/// <summary>
/// Codice Fiscale: never switch windows. The barcode is left where the focus already is.
/// </summary>
public class CodiceFiscaleRoutingStrategy : IRoutingStrategy
{
    public bool CanHandle(BarcodeReading reading) =>
        reading.BarcodeType == BarcodeType.CodiceFiscale;

    public BarcodeDestination GetDestination(BarcodeReading reading) => BarcodeDestination.DoNotSwitch;
}

/// <summary>Catch-all: everything else goes to NegozioFacile.</summary>
public class DefaultRoutingStrategy : IRoutingStrategy
{
    public bool CanHandle(BarcodeReading reading) => true;

    public BarcodeDestination GetDestination(BarcodeReading reading) => BarcodeDestination.NegozioFacile;
}
