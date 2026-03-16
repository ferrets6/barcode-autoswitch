using BarcodeAutoSwitch.Core.Models;

namespace BarcodeAutoSwitch.Core.Interfaces;

public interface IBarcodeParser
{
    BarcodeReading Parse(string rawInput, bool hasIdentifierPrefix);
    bool IsControlCode(string rawInput, out ControlCodeType controlType, bool hasIdentifierPrefix);
}
