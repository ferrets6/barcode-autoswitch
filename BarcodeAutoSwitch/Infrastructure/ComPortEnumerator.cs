using BarcodeAutoSwitch.Core.Models;
using Microsoft.Win32;
using System.IO.Ports;

namespace BarcodeAutoSwitch.Infrastructure;

/// <summary>
/// Enumerates available COM serial ports and, where possible, annotates each
/// port with its USB hardware identifier (VID/PID) for display purposes.
/// The COM port name itself is the canonical device identifier.
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


}
