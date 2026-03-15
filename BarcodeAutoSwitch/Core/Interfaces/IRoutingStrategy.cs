using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Interfaces;

/// <summary>Strategy pattern: each implementation handles a specific barcode routing rule.</summary>
public interface IRoutingStrategy
{
    bool CanHandle(BarcodeReading reading);
    BarcodeDestination GetDestination(BarcodeReading reading);
}
