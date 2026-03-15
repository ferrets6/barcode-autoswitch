using BarcodeAutoSwitch.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace BarcodeAutoSwitch.Infrastructure;

public class AppSettings : IAppSettings
{
    private readonly IConfiguration _config;
    private string _selectedSerialPort;

    private static readonly string UserSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarcodeAutoSwitch", "usersettings.json");

    public AppSettings(IConfiguration config)
    {
        _config = config;
        _selectedSerialPort = config["Application:DefaultSerialPort"] ?? "COM1";
        LoadUserSettings();
    }

    public string AdriaticaPressVenditaUrl     => _config["AdriaticaPress:VenditaUrl"]     ?? string.Empty;
    public string AdriaticaPressLoginUrl       => _config["AdriaticaPress:LoginUrl"]       ?? string.Empty;
    public string AdriaticaPressAfterLoginUrl  => _config["AdriaticaPress:AfterLoginUrl"]  ?? string.Empty;
    public string NegozioFacileProcessName     => _config["Application:NegozioFacileProcessName"] ?? "NegozioFacile";

    public string SelectedSerialPort
    {
        get => _selectedSerialPort;
        set => _selectedSerialPort = value;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UserSettingsPath)!);
        var json = JsonSerializer.Serialize(new { SelectedSerialPort = _selectedSerialPort });
        File.WriteAllText(UserSettingsPath, json);
    }

    private void LoadUserSettings()
    {
        if (!File.Exists(UserSettingsPath)) return;
        try
        {
            var json = File.ReadAllText(UserSettingsPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("SelectedSerialPort", out var prop))
                _selectedSerialPort = prop.GetString() ?? _selectedSerialPort;
        }
        catch
        {
            // keep default on any parse error
        }
    }
}
