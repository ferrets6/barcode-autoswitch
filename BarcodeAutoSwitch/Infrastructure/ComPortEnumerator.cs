using BarcodeAutoSwitch.Core.Models;
using Microsoft.Win32;
using System.IO.Ports;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Enumerates available COM serial ports and, where possible, resolves each
/// port's USB hardware identifier (VID/PID) from the Windows registry.
///
/// This lets the app re-find the correct COM port even if its number changes
/// (e.g. COM3 → COM5 after plugging into a different USB hub).
/// </summary>
public static class ComPortEnumerator
{
    /// <summary>
    /// Returns all COM ports currently visible to the OS, each annotated with
    /// its USB hardware ID when the backing device is USB-based.
    /// </summary>
    public static IReadOnlyList<ComPortInfo> GetAvailablePorts()
    {
        var hwMap = BuildHardwareIdMap();
        return SerialPort.GetPortNames()
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Select(p =>
            {
                bool found = hwMap.TryGetValue(p, out var hw);
                string display = found ? $"{p}  ({hw})" : p;
                return new ComPortInfo(p, found ? hw : null, display);
            })
            .ToList();
    }

    /// <summary>
    /// Given a stored hardware ID (e.g. "USB\VID_067B&PID_2303"), returns the
    /// COM port name currently assigned to that device, or <c>null</c> if the
    /// device is not connected.
    ///
    /// Matching is VID/PID-level: "USB\VID_067B&PID_2303" matches both
    /// "USB\VID_067B&PID_2303" and "USB\VID_067B&PID_2303&MI_00" (composite).
    /// </summary>
    public static string? GetPortForHardwareId(string hardwareId)
    {
        foreach (var (port, hw) in BuildHardwareIdMap())
        {
            if (HardwareIdMatches(hardwareId, hw))
                return port;
        }
        return null;
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>Builds a map: portName → hardwareId by scanning the USB registry branch.</summary>
    private static Dictionary<string, string> BuildHardwareIdMap()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try { ScanRegistryBranch(@"SYSTEM\CurrentControlSet\Enum\USB",     "USB",     result); } catch { /* ignore per-branch errors */ }
        try { ScanRegistryBranch(@"SYSTEM\CurrentControlSet\Enum\FTDIBUS", "FTDIBUS", result); } catch { /* FTDI USB-serial adapters */ }
        return result;
    }

    private static void ScanRegistryBranch(string keyPath, string prefix,
                                           Dictionary<string, string> result)
    {
        using var rootKey = Registry.LocalMachine.OpenSubKey(keyPath);
        if (rootKey == null) return;

        foreach (string hwId in rootKey.GetSubKeyNames()) // e.g. "VID_067B&PID_2303"
        {
            using var hwKey = rootKey.OpenSubKey(hwId);
            if (hwKey == null) continue;

            foreach (string instance in hwKey.GetSubKeyNames())
            {
                using var paramsKey = hwKey.OpenSubKey($@"{instance}\Device Parameters");
                if (paramsKey?.GetValue("PortName") is string portName
                    && portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't overwrite if already found (first match wins)
                    result.TryAdd(portName, $@"{prefix}\{hwId}");
                }
            }
        }
    }

    /// <summary>
    /// Returns true when <paramref name="stored"/> and <paramref name="actual"/>
    /// refer to the same device, ignoring composite-device interface suffixes.
    /// e.g. stored="USB\VID_A&PID_B" matches actual="USB\VID_A&PID_B&MI_00"
    /// </summary>
    private static bool HardwareIdMatches(string stored, string actual)
    {
        if (string.Equals(stored, actual, StringComparison.OrdinalIgnoreCase))
            return true;

        // stored is shorter (no &MI_xx) and actual has it
        if (actual.StartsWith(stored, StringComparison.OrdinalIgnoreCase)
            && actual.Length > stored.Length && actual[stored.Length] == '&')
            return true;

        // actual is shorter and stored has the suffix
        if (stored.StartsWith(actual, StringComparison.OrdinalIgnoreCase)
            && stored.Length > actual.Length && stored[actual.Length] == '&')
            return true;

        return false;
    }
}
