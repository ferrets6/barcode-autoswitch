namespace BarcodeAutoSwitch.Core.Models;

public enum BarcodeType
{
    EAN8,            // A prefix
    ISSN13Plus5,     // M prefix (newspapers, magazines)
    EAN13,           // B prefix
    Interleaved2of5, // N prefix (scratch cards)
    Unknown
}

public record BarcodeReading(
    string RawValue,
    string CodeValue,
    char CodeIdentifier,
    BarcodeType BarcodeType);
