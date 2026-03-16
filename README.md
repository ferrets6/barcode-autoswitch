# BarcodeAutoSwitch

WPF application (.NET 8, x64) that listens on one or more barcode scanners and automatically routes each barcode to the correct application window.

- **Newspaper barcodes** (ISSN prefix `977`, types `M`/`B`) → Adriatica Press (embedded Chromium browser, sends Alt+T + barcode code)
- **All other barcodes** → NegozioFacile (external process, types barcode via keyboard emulation)
- **Control barcodes** → toggle auto-switch on/off, or test a device

Supported scanner types:
- **Serial / USB-to-serial** (COM port, e.g. Prolific PL2303)
- **USB HID keyboard-emulating** scanners (Raw Input API — works even when the app is not in focus)

Multiple devices can be active simultaneously; barcodes from any device feed the same pipeline.

## Requirements

| | |
|---|---|
| OS | Windows 10 or later (64-bit) |
| Runtime | [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8) (or use self-contained publish) |
| Hardware | Barcode scanner (serial or USB HID) |

## Run locally

```bash
dotnet run --project BarcodeAutoSwitch/BarcodeAutoSwitch.csproj -r win-x64
```

## Build / Publish

```bash
# Debug build
dotnet build BarcodeAutoSwitch/BarcodeAutoSwitch.csproj -r win-x64

# Release — self-contained (no .NET required on target machine)
dotnet publish BarcodeAutoSwitch/BarcodeAutoSwitch.csproj -c Release -r win-x64 --self-contained true
```

Output in `BarcodeAutoSwitch/bin/Release/net8.0-windows/win-x64/publish/`.

## Configuration

`appsettings.json` is committed with production defaults.
For local development overrides create **`BarcodeAutoSwitch/appsettings.local.json`** (gitignored):

```json
{
  "Application": {
    "NegozioFacileProcessName": "notepad"
  }
}
```

Key settings:

| Key | Description |
|---|---|
| `AdriaticaPress.VenditaUrl` | URL of the Adriatica Press sales page |
| `AdriaticaPress.LoginUrl` | URL used to detect the login page |
| `Application.NegozioFacileProcessName` | Process name of NegozioFacile (.exe without extension) |

### User settings (persisted per-user)

Configured devices are saved in `%LOCALAPPDATA%\BarcodeAutoSwitch\usersettings.json`.
The file is created automatically when you add the first device via **Gestisci dispositivi**.

Example:

```json
{
  "ConfiguredDevices": [
    {
      "DeviceId": "COM3",
      "Type": "SerialPort",
      "HardwareId": "USB\\VID_067B&PID_2303",
      "DisplayName": "COM3  (USB\\VID_067B&PID_2303)"
    }
  ]
}
```

For USB-to-serial adapters the `HardwareId` (VID/PID) is stored so the app re-finds the correct COM port even if its number changes between reboots.
You can add entries manually for testing; the file is gitignored.

## Tests

```bash
dotnet test tests/BarcodeAutoSwitch.UnitTests   -r win-x64
dotnet test tests/BarcodeAutoSwitch.IntegrationTests -r win-x64
```

## Debug log

Click **Mostra debug** in the status bar to open a live log window.

## Project structure

```
BarcodeAutoSwitch/
├── Core/
│   ├── Interfaces/      # IBarcodeInputService, IBarcodeParser, …
│   ├── Models/          # BarcodeReading, SavedDevice, BarcodeDeviceInfo, PortTestResult, …
│   └── Services/        # BarcodeParser, BarcodeRouter, routing strategies
├── Infrastructure/      # SerialPortService, RawInputBarcodeService, ComPortEnumerator,
│                        # AppSettings, AppLogger, KeyboardSender, WindowSwitcher
├── UI/
│   ├── Behaviours/      # WPF attached behaviours
│   ├── Converters/      # Value converters
│   ├── Commands/        # RelayCommand, RelayCommand<T>
│   └── ViewModels/      # MainViewModel, AddDeviceViewModel, DeviceManagementViewModel
└── Windows/             # MainWindow, ComPortWindow (DeviceManagementWindow),
                         # AddDeviceWindow, DebugLogWindow
tests/
├── BarcodeAutoSwitch.UnitTests/        # xUnit + Moq + FluentAssertions
└── BarcodeAutoSwitch.IntegrationTests/
```
