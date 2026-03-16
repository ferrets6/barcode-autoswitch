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

    // ── IsControlCode — serial (has identifier prefix) ────────────────────────

    [Fact]
    public void IsControlCode_Serial_EnableDisableToggle_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("X111111111100000011111111", out var type);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.EnableDisableToggle);
    }

    [Fact]
    public void IsControlCode_Serial_CheckPort_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("X111111111122222200000000", out var type);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_Serial_CheckPortWithExtraZeros_TrimEnabled_ReturnsTrue()
    {
        // Scanner pads output: "111111111122222200000000" + extra zeros, wrapped with serial prefix
        bool result = _sut.IsControlCode("X11111111112222220000000000000", out var type, trimTrailingZeros: true);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_Serial_CheckPortWithExtraZeros_TrimDisabled_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("X11111111112222220000000000000", out var type, trimTrailingZeros: false);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }

    // ── IsControlCode — USB HID (no identifier prefix) ───────────────────────

    [Fact]
    public void IsControlCode_UsbHid_CheckPort_ExactMatch_ReturnsTrue()
    {
        // USB HID scanner sends raw code without prefix
        bool result = _sut.IsControlCode("111111111122222200000000", out var type);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_UsbHid_CheckPortWithExtraZeros_TrimEnabled_ReturnsTrue()
    {
        bool result = _sut.IsControlCode("11111111112222220000000000000", out var type, trimTrailingZeros: true);

        result.Should().BeTrue();
        type.Should().Be(ControlCodeType.CheckPort);
    }

    [Fact]
    public void IsControlCode_UsbHid_CheckPortWithExtraZeros_TrimDisabled_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("11111111112222220000000000000", out var type, trimTrailingZeros: false);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }

    // ── IsControlCode — general ───────────────────────────────────────────────

    [Fact]
    public void IsControlCode_NormalBarcode_ReturnsFalse()
    {
        bool result = _sut.IsControlCode("B9771234567890", out var type);

        result.Should().BeFalse();
        type.Should().Be(ControlCodeType.None);
    }

    [Fact]
    public void IsControlCode_TrimEnabled_DoesNotAffectNormalBarcodes()
    {
        // Trim must not accidentally match a normal barcode that happens to end in zeros
        bool result = _sut.IsControlCode("B9771234500000", out var type, trimTrailingZeros: true);

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
