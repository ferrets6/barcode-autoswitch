using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using FluentAssertions;

namespace BarcodeAutoSwitch.UnitTests.Core;

public class BarcodeParserTests
{
    private readonly BarcodeParser _sut = new();

    // ── Parse ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("A12345678",  "12345678",  'A', BarcodeType.EAN8)]
    [InlineData("M9771234567", "9771234567", 'M', BarcodeType.ISSN13Plus5)]
    [InlineData("B9771234567890", "9771234567890", 'B', BarcodeType.EAN13)]
    [InlineData("N12345678901234", "12345678901234", 'N', BarcodeType.Interleaved2of5)]
    [InlineData("Z99999",      "99999",      'Z', BarcodeType.Unknown)]
    public void Parse_KnownPrefixes_ReturnsCorrectBarcodeType(
        string raw, string expectedCode, char expectedId, BarcodeType expectedType)
    {
        var result = _sut.Parse(raw);

        result.CodeIdentifier.Should().Be(expectedId);
        result.CodeValue.Should().Be(expectedCode);
        result.BarcodeType.Should().Be(expectedType);
        result.RawValue.Should().Be(raw);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsUnknown()
    {
        var result = _sut.Parse(string.Empty);
        result.BarcodeType.Should().Be(BarcodeType.Unknown);
    }

    [Fact]
    public void Parse_SingleChar_ReturnsUnknown()
    {
        var result = _sut.Parse("A");
        result.BarcodeType.Should().Be(BarcodeType.Unknown);
    }

    // ── IsControlCode ─────────────────────────────────────────────────────────

    [Fact]
    public void IsControlCode_EnableDisableToggle_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("X111111111100000011111111", out var type);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.EnableDisableToggle);
    }

    [Fact]
    public void IsControlCode_CheckPort_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("X111111111122222200000000", out var type);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_NormalBarcode_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("B9771234567890", out var type);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }

    [Fact]
    public void IsControlCode_TooShort_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("X", out var type);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }
}
