using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;

namespace BarcodeAutoSwitch.UnitTests.Core;

public class BarcodeParserTests
{
    private readonly BarcodeParser _sut = new();

    // ── Parse (hasIdentifierPrefix = true) ────────────────────────────────────

    [Theory]
    [InlineData("A12345678",       "12345678",       'A', BarcodeType.EAN8)]
    [InlineData("M9771234567",     "9771234567",     'M', BarcodeType.ISSN13Plus5)]
    [InlineData("B9771234567890",  "9771234567890",  'B', BarcodeType.EAN13)]
    [InlineData("N12345678901234", "12345678901234", 'N', BarcodeType.Interleaved2of5)]
    [InlineData("Z99999",          "99999",          'Z', BarcodeType.Unknown)]
    public void Parse_WithPrefix_KnownIdentifiers_ReturnsCorrectType(
        string raw, string expectedCode, char expectedId, BarcodeType expectedType)
    {
        var result = _sut.Parse(raw, hasIdentifierPrefix: true);

        result.CodeIdentifier.Should().Be(expectedId);
        result.CodeValue.Should().Be(expectedCode);
        result.BarcodeType.Should().Be(expectedType);
        result.RawValue.Should().Be(raw);
    }

    [Fact]
    public void Parse_WithPrefix_SingleChar_ReturnsUnknown()
    {
        var result = _sut.Parse("A", hasIdentifierPrefix: true);
        result.BarcodeType.Should().Be(BarcodeType.Unknown);
    }

    // ── Parse (hasIdentifierPrefix = false) ───────────────────────────────────

    [Theory]
    [InlineData("12345678",           BarcodeType.EAN8)]
    [InlineData("9771234567890",      BarcodeType.ISSN13Plus5)]   // 13 digits, starts with 977
    [InlineData("9781234567890",      BarcodeType.EAN13)]          // 13 digits, not 977
    [InlineData("977123456789012345", BarcodeType.ISSN13Plus5)]   // 18 digits, starts with 977
    [InlineData("12345678901234",     BarcodeType.Interleaved2of5)]
    [InlineData("999",                BarcodeType.Unknown)]        // unrecognised length
    [InlineData("ABCDEFGH",           BarcodeType.Unknown)]        // non-digits
    public void Parse_WithoutPrefix_InfersTypeFromContent(string raw, BarcodeType expectedType)
    {
        var result = _sut.Parse(raw, hasIdentifierPrefix: false);

        result.CodeValue.Should().Be(raw);
        result.CodeIdentifier.Should().Be('\0');
        result.RawValue.Should().Be(raw);
        result.BarcodeType.Should().Be(expectedType);
    }

    // ── Parse (edge cases, both modes) ────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_EmptyString_ReturnsUnknown(bool hasPrefix)
    {
        var result = _sut.Parse(string.Empty, hasIdentifierPrefix: hasPrefix);
        result.BarcodeType.Should().Be(BarcodeType.Unknown);
    }

    // ── IsControlCode (hasIdentifierPrefix = true) ────────────────────────────

    [Fact]
    public void IsControlCode_WithPrefix_EnableDisableToggle_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("X111111111100000011111111", out var type, hasIdentifierPrefix: true);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.EnableDisableToggle);
    }

    [Fact]
    public void IsControlCode_WithPrefix_CheckPort_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("X111111111122222211111111", out var type, hasIdentifierPrefix: true);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_WithPrefix_NormalBarcode_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("B9771234567890", out var type, hasIdentifierPrefix: true);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }

    // ── IsControlCode (hasIdentifierPrefix = false) ───────────────────────────

    [Fact]
    public void IsControlCode_WithoutPrefix_EnableDisableToggle_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("111111111100000011111111", out var type, hasIdentifierPrefix: false);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.EnableDisableToggle);
    }

    [Fact]
    public void IsControlCode_WithoutPrefix_CheckPort_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("111111111122222211111111", out var type, hasIdentifierPrefix: false);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_WithoutPrefix_NormalBarcode_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("9771234567890", out var type, hasIdentifierPrefix: false);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }

    // ── IsControlCode (edge cases) ────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsControlCode_TooShort_ReturnsFalse(bool hasPrefix)
    {
        bool result = _sut.IsControlCode("X", out var type, hasIdentifierPrefix: hasPrefix);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }
}