using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using System.Text.RegularExpressions;

namespace BarcodeAutoSwitch.Core.Services;

/// <summary>
/// Parses raw barcode scanner input into structured <see cref="BarcodeReading"/> instances.
///
/// Serial (COM-port) scanners prepend a single-char identifier (A/B/M/N) before the barcode.
/// </summary>
public class BarcodeParser : IBarcodeParser
{
    // Special barcodes used to send control signals via the scanner
    private const string EnableDisableToggleCode = "111111111100000011111111";
    private const string CheckPortCode = "111111111122222211111111";

    // Italian Codice Fiscale: 6 letters + 2 digits + letter + 2 digits + letter + 3 digits + letter = 16 chars
    private static readonly Regex CfRegex =
        new(@"^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$", RegexOptions.Compiled);

    public BarcodeReading Parse(string rawInput, bool hasIdentifierPrefix)
    {
        if (string.IsNullOrEmpty(rawInput))
            return new BarcodeReading(rawInput, rawInput, '\0', BarcodeType.Unknown);
        if (hasIdentifierPrefix)
        {
            if (rawInput.Length < 2)
                return new BarcodeReading(rawInput, rawInput, '\0', BarcodeType.Unknown);

            char identifier = rawInput[0];
            string code = rawInput[1..];
            BarcodeType type = identifier switch
            {
                'A' => BarcodeType.EAN8,
                'M' => BarcodeType.ISSN13Plus5,
                'B' => BarcodeType.EAN13,
                'N' => BarcodeType.Interleaved2of5,
                _ => BarcodeType.Unknown
            };
            // CF check overrides the identifier-based type
            if (IsCf(code)) type = BarcodeType.CodiceFiscale;
            return new BarcodeReading(rawInput, code, identifier, type);
        }
        else
        {
            // no prefix — infer type from content
            string code = rawInput;
            BarcodeType type = IsCf(code) ? BarcodeType.CodiceFiscale : InferBarcodeType(code);
            return new BarcodeReading(rawInput, code, '\0', type);
        }
    }

    public bool IsControlCode(string rawInput, out ControlCodeType controlType, bool hasIdentifierPrefix)
    {
        if (rawInput.Length < 1)
        {
            controlType = ControlCodeType.None;
            return false;
        }

        // If there's an identifier prefix, the actual code starts from index 1
        string codeToCheck = hasIdentifierPrefix && rawInput.Length >= 2 ? rawInput[1..] : rawInput;

        if (codeToCheck == EnableDisableToggleCode)
        {
            controlType = ControlCodeType.EnableDisableToggle;
            return true;
        }

        if (codeToCheck == CheckPortCode)
        {
            controlType = ControlCodeType.CheckPort;
            return true;
        }

        controlType = ControlCodeType.None;
        return false;
    }

    private static bool IsCf(string code) =>
        code.Length == 16 && CfRegex.IsMatch(code.ToUpperInvariant());

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
