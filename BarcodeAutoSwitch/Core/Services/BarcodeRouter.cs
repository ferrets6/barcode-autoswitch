using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Services;

/// <summary>
/// Routes a barcode to the correct destination by evaluating a prioritised
/// list of <see cref="IRoutingStrategy"/> instances (Strategy pattern).
/// The first strategy that can handle the reading wins.
/// </summary>
public class BarcodeRouter : IBarcodeRouter
{
    private readonly IReadOnlyList<IRoutingStrategy> _strategies;

    public BarcodeRouter(IEnumerable<IRoutingStrategy> strategies)
    {
        _strategies = strategies.ToList();
    }

    public BarcodeDestination Route(BarcodeReading reading)
    {
        foreach (var strategy in _strategies)
        {
            if (strategy.CanHandle(reading))
                return strategy.GetDestination(reading);
        }

        // Fallback (should never reach here if DefaultRoutingStrategy is registered)
        return BarcodeDestination.NegozioFacile;
    }
}
