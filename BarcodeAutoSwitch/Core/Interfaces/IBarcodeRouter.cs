using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IBarcodeRouter
{
    BarcodeDestination Route(BarcodeReading reading);
}
