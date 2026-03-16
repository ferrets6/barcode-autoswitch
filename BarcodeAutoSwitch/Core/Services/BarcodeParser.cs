using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Services;

/// <summary>
/// Parses raw barcode scanner input into structured <see cref="BarcodeReading"/> instances.
///
/// Serial scanners prepend a single-char identifier (A/B/M/N) before the barcode.
/// USB HID scanners send the raw barcode without any prefix; the type is inferred
/// from the content when <paramref name="hasIdentifierPrefix"/> is false.
/// </summary>
public class BarcodeParser : IBarcodeParser
{
    // Special barcodes used to send control signals via the scanner
    private const string EnableDisableToggleCode = "111111111100000011111111";
    private const string CheckPortCode           = "111111111122222200000000";

    public BarcodeReading Parse(string rawInput, bool hasIdentifierPrefix = true)
    {
        if (string.IsNullOrEmpty(rawInput))
            return new BarcodeReading(rawInput, rawInput, '\0', BarcodeType.Unknown);

        if (hasIdentifierPrefix)
        {
            if (rawInput.Length < 2)
                return new BarcodeReading(rawInput, rawInput, '\0', BarcodeType.Unknown);

            char   identifier = rawInput[0];
            string code       = rawInput[1..];
            BarcodeType type  = identifier switch
            {
                'A' => BarcodeType.EAN8,
                'M' => BarcodeType.ISSN13Plus5,
                'B' => BarcodeType.EAN13,
                'N' => BarcodeType.Interleaved2of5,
                _   => BarcodeType.Unknown
            };
            return new BarcodeReading(rawInput, code, identifier, type);
        }
        else
        {
            // USB HID: no prefix — infer type from content
            string      code = rawInput;
            BarcodeType type = InferBarcodeType(code);
            return new BarcodeReading(rawInput, code, '\0', type);
        }
    }

    public bool IsControlCode(string rawInput, out ControlCodeType controlType, bool trimTrailingZeros = false)
    {
        if (rawInput.Length < 1)
        {
            controlType = ControlCodeType.None;
            return false;
        }

        // Candidates: full input (USB HID — no prefix) and input minus first char (serial — has identifier prefix)
        string[] candidates = rawInput.Length >= 2
            ? [rawInput, rawInput[1..]]
            : [rawInput];

        foreach (string candidate in candidates)
        {
            if (candidate == EnableDisableToggleCode)
            {
                controlType = ControlCodeType.EnableDisableToggle;
                return true;
            }

            string norm = trimTrailingZeros ? candidate.TrimEnd('0') : candidate;
            if (norm == CheckPortCode.TrimEnd('0'))
            {
                controlType = ControlCodeType.CheckPort;
                return true;
            }
        }

        controlType = ControlCodeType.None;
        return false;
    }

    /// <summary>
    /// Infers <see cref="BarcodeType"/> from barcode content when no identifier prefix is available.
    /// </summary>
    private static BarcodeType InferBarcodeType(string code)
    {
        if (!code.All(char.IsDigit)) return BarcodeType.Unknown;

        return code.Length switch
        {
            8  => BarcodeType.EAN8,
            13 => code.StartsWith("977", StringComparison.Ordinal)
                      ? BarcodeType.ISSN13Plus5
                      : BarcodeType.EAN13,
            // ISSN with 5-digit add-on (13 + 5 = 18 digits)
            18 => code.StartsWith("977", StringComparison.Ordinal)
                      ? BarcodeType.ISSN13Plus5
                      : BarcodeType.EAN13,
            14 => BarcodeType.Interleaved2of5,
            _  => BarcodeType.Unknown
        };
    }
}
