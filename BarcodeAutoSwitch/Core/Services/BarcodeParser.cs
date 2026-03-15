using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Services;

/// <summary>
/// Parses raw barcode scanner input into structured <see cref="BarcodeReading"/> instances.
/// The scanner prepends a single-char code identifier before the actual barcode value.
/// </summary>
public class BarcodeParser : IBarcodeParser
{
    // Special barcodes used to send control signals via the scanner
    private const string EnableDisableToggleCode = "111111111100000011111111";
    private const string CheckPortCode           = "111111111122222200000000";

    public BarcodeReading Parse(string rawInput)
    {
        if (string.IsNullOrEmpty(rawInput) || rawInput.Length < 2)
            return new BarcodeReading(rawInput, rawInput, '\0', BarcodeType.Unknown);

        char identifier = rawInput[0];
        string code     = rawInput[1..];

        BarcodeType type = identifier switch
        {
            'A' => BarcodeType.EAN8,
            'M' => BarcodeType.ISSN13Plus5,
            'B' => BarcodeType.EAN13,
            'N' => BarcodeType.Interleaved2of5,
            _   => BarcodeType.Unknown
        };

        return new BarcodeReading(rawInput, code, identifier, type);
    }

    public bool IsControlCode(string rawInput, out ControlCodeType controlType)
    {
        if (rawInput.Length < 2)
        {
            controlType = ControlCodeType.None;
            return false;
        }

        string code = rawInput[1..];

        if (code == EnableDisableToggleCode)
        {
            controlType = ControlCodeType.EnableDisableToggle;
            return true;
        }

        if (code == CheckPortCode)
        {
            controlType = ControlCodeType.CheckPort;
            return true;
        }

        controlType = ControlCodeType.None;
        return false;
    }
}
