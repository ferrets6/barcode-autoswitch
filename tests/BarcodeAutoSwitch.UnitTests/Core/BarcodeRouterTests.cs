using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using BarcodeAutoSwitch.Core.Services;
using FluentAssertions;

namespace BarcodeAutoSwitch.UnitTests.Core;

public class BarcodeRouterTests
{
    private readonly BarcodeRouter _sut = new(new IRoutingStrategy[]
    {
        new CodiceFiscaleRoutingStrategy(),
        new NewspaperRoutingStrategy(),
        new DefaultRoutingStrategy()
    });

    // ── Newspaper routing ─────────────────────────────────────────────────────

    [Theory]
    [InlineData('M', "9771234567890", BarcodeType.ISSN13Plus5)]
    [InlineData('B', "9771234567890", BarcodeType.EAN13)]
    public void Route_NewsaperBarcode_ReturnsAdriaticaPress(
        char id, string code, BarcodeType type)
    {
        var reading = new BarcodeReading(id + code, code, id, type);
        _sut.Route(reading).Should().Be(BarcodeDestination.AdriaticaPress);
    }

    // ── Non-newspaper routing ─────────────────────────────────────────────────

    [Theory]
    [InlineData('A', "12345678",        BarcodeType.EAN8)]
    [InlineData('B', "1234567890123",   BarcodeType.EAN13)]   // EAN13 without 977
    [InlineData('N', "123456789",       BarcodeType.Interleaved2of5)]
    [InlineData('M', "8001234567890",   BarcodeType.ISSN13Plus5)]  // ISSN without 977
    public void Route_NonNewspaper_ReturnsNegozioFacile(
        char id, string code, BarcodeType type)
    {
        var reading = new BarcodeReading(id + code, code, id, type);
        _sut.Route(reading).Should().Be(BarcodeDestination.NegozioFacile);
    }

    // ── Codice Fiscale routing ────────────────────────────────────────────────

    [Fact]
    public void Route_CodiceFiscale_ReturnsIgnore()
    {
        const string cf = "RSSMRA80A01H501U";
        var reading = new BarcodeReading("A" + cf, cf, 'A', BarcodeType.CodiceFiscale);
        _sut.Route(reading).Should().Be(BarcodeDestination.DoNotSwitch);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Route_977PrefixButTypeEAN8_ReturnsNegozioFacile()
    {
        // EAN8 can't realistically start with 977, but we test the rule boundary
        var reading = new BarcodeReading("A977" , "977", 'A', BarcodeType.EAN8);
        _sut.Route(reading).Should().Be(BarcodeDestination.NegozioFacile);
    }

    [Fact]
    public void Route_EmptyCode_DoesNotThrow()
    {
        var reading = new BarcodeReading("B", "", 'B', BarcodeType.EAN13);
        var act = () => _sut.Route(reading);
        act.Should().NotThrow();
    }
}
