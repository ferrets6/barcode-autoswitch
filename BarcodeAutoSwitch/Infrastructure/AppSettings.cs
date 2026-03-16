using BarcodeAutoSwitch.Core.Interfaces;
using BarcodeAutoSwitch.Core.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarcodeAutoSwitch.Infrastructure;

public class AppSettings : IAppSettings
{
    private readonly IConfiguration _config;
    private List<SavedDevice> _configuredDevices = new();

    private static readonly string UserSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarcodeAutoSwitch", "usersettings.json");

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() }
    };

    public AppSettings(IConfiguration config)
    {
        _config = config;
        LoadUserSettings();
    }

    public string AdriaticaPressVenditaUrl    => _config["AdriaticaPress:VenditaUrl"]    ?? string.Empty;
    public string AdriaticaPressLoginUrl      => _config["AdriaticaPress:LoginUrl"]      ?? string.Empty;
    public string AdriaticaPressAfterLoginUrl => _config["AdriaticaPress:AfterLoginUrl"] ?? string.Empty;
    public string NegozioFacileProcessName    => _config["Application:NegozioFacileProcessName"] ?? "NegozioFacile";

    public List<SavedDevice> ConfiguredDevices
    {
        get => _configuredDevices;
        set => _configuredDevices = value;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UserSettingsPath)!);
        var json = JsonSerializer.Serialize(new { ConfiguredDevices = _configuredDevices }, _jsonOpts);
        File.WriteAllText(UserSettingsPath, json);
    }

    private void LoadUserSettings()
    {
        if (!File.Exists(UserSettingsPath)) return;
        try
        {
            var json = File.ReadAllText(UserSettingsPath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("ConfiguredDevices", out var devicesEl))
            {
                // Current format — deserialize directly
                _configuredDevices =
                    JsonSerializer.Deserialize<List<SavedDevice>>(devicesEl.GetRawText(), _jsonOpts)
                    ?? new List<SavedDevice>();
            }
            else
            {
                // ── Migrate from previous single-device format ────────────────
                string deviceId = _config["Application:DefaultSerialPort"] ?? "COM1";

                if (doc.RootElement.TryGetProperty("SelectedSerialPort", out var oldPort))
                    deviceId = oldPort.GetString() ?? deviceId;
                if (doc.RootElement.TryGetProperty("SelectedDeviceId", out var idEl))
                    deviceId = idEl.GetString() ?? deviceId;

                if (!string.IsNullOrEmpty(deviceId))
                {
                    _configuredDevices = new List<SavedDevice>
                    {
                        new SavedDevice
                        {
                            DeviceId    = deviceId,
                            Type        = BarcodeDeviceType.SerialPort,
                            DisplayName = deviceId
                        }
                    };
                }
            }
        }
        catch
        {
            // keep empty list on any parse error
        }
    }
}
