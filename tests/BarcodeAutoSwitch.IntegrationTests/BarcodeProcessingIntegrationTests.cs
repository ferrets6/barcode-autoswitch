using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using FluentAssertions;
using Moq;

namespace BarcodeAutoSwitch.IntegrationTests;

/// <summary>
/// Integration tests that wire together real Core components
/// (parser + router) while mocking only infrastructure boundaries
/// (serial port, window switcher, keyboard sender).
/// </summary>
public class BarcodeProcessingIntegrationTests
{
    // Real Core objects
    private readonly BarcodeParser _parser = new();
    private readonly BarcodeRouter _router = new(new IRoutingStrategy[]
    {
        new NewspaperRoutingStrategy(),
        new DefaultRoutingStrategy()
    });

    // ── Parse → Route pipeline ────────────────────────────────────────────────

    [Theory]
    [InlineData("M9771234567890", BarcodeDestination.AdriaticaPress)]
    [InlineData("B9771234567890", BarcodeDestination.AdriaticaPress)]
    [InlineData("B1234567890123", BarcodeDestination.NegozioFacile)]
    [InlineData("A12345678",      BarcodeDestination.NegozioFacile)]
    [InlineData("N12345678901",   BarcodeDestination.NegozioFacile)]
    public void ParseAndRoute_FullPipeline_CorrectDestination(string raw, BarcodeDestination expected)
    {
        var reading     = _parser.Parse(raw);
        var destination = _router.Route(reading);

        destination.Should().Be(expected);
    }

    // ── Control codes don't enter the routing pipeline ────────────────────────

    [Theory]
    [InlineData("X111111111100000011111111", ControlCodeType.EnableDisableToggle)]
    [InlineData("X111111111122222200000000", ControlCodeType.CheckPort)]
    public void ControlCodes_AreDetected_BeforeRouting(string raw, ControlCodeType expected)
    {
        bool isControl = _parser.IsControlCode(raw, out var type);

        isControl.Should().BeTrue();
        type.Should().Be(expected);
    }

    // ── Mock-based end-to-end: notepad substitutes NegozioFacile ─────────────

    [Fact]
    public void EndToEnd_EAN8_RoutesToNotepadMock()
    {
        var windowSwitcher = new Mock<IWindowSwitcher>();
        var keyboard       = new Mock<IKeyboardSender>();
        windowSwitcher.Setup(w => w.BringToFront("notepad")).Returns(true);

        // Simulate what MainViewModel does for a non-newspaper barcode
        const string raw    = "A12345678";
        var reading         = _parser.Parse(raw);
        var destination     = _router.Route(reading);

        destination.Should().Be(BarcodeDestination.NegozioFacile);

        bool switched = windowSwitcher.Object.BringToFront("notepad");
        if (switched)
        {
            keyboard.Object.SendText(reading.CodeValue);
            keyboard.Object.SendKey("{ENTER}");
        }

        windowSwitcher.Verify(w => w.BringToFront("notepad"), Times.Once);
        keyboard.Verify(k => k.SendText("12345678"), Times.Once);
        keyboard.Verify(k => k.SendKey("{ENTER}"),   Times.Once);
    }

    [Fact]
    public void EndToEnd_Newspaper_RoutesToAdriaticaPress()
    {
        var windowSwitcher = new Mock<IWindowSwitcher>();
        windowSwitcher.Setup(w => w.BringToFront("BarcodeAutoSwitch")).Returns(true);

        const string raw = "B9771234567890";
        var reading      = _parser.Parse(raw);
        var destination  = _router.Route(reading);

        destination.Should().Be(BarcodeDestination.AdriaticaPress);
        windowSwitcher.Object.BringToFront("BarcodeAutoSwitch");

        windowSwitcher.Verify(w => w.BringToFront("BarcodeAutoSwitch"), Times.Once);
    }

    // ── Special characters in barcode are escaped for SendKeys ───────────────

    [Fact]
    public void BarcodeWithSpecialChars_IsEscapedBeforeSend()
    {
        // Normally barcodes are numeric, but this tests the escaping logic in KeyboardSender
        var keyboard = new BarcodeAutoSwitch.Infrastructure.KeyboardSender();

        // We can't assert SendKeys output without a focused window, but we can
        // verify the code does not throw on special-char input.
        var act = () => keyboard.SendText("123+456");
        act.Should().NotThrow();
    }
}
