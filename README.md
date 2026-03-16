# BarcodeAutoSwitch

WPF application (.NET 8, x64) that listens on one or more barcode scanners connected via COM port and automatically routes each barcode to the correct application window.

- **Newspaper barcodes** (ISSN prefix `977`, types `M`/`B`) → Adriatica Press (embedded Chromium browser, sends Alt+T + barcode code)
- **All other barcodes** → NegozioFacile (external process, types barcode via keyboard emulation)
- **Control barcodes** → toggle auto-switch on/off, or test a device

Multiple devices can be active simultaneously; barcodes from any device feed the same pipeline.

## Requirements

| | |
|---|---|
| OS | Windows 10 or later (64-bit) |
| Runtime | [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8) (or use self-contained publish) |
| Hardware | Barcode scanner connected via COM port (serial or USB-to-serial adapter) |

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
      "DisplayName": "COM3  (USB\\VID_067B&PID_2303)",
      "HasIdentifierPrefix": true,
      "TrimTrailingZeros": false
    }
  ]
}
```

Per-device fields:

| Field | Description |
|---|---|
| `HardwareId` | VID/PID for USB-to-serial adapters — the app re-finds the correct COM port automatically even if its number changes between reboots |
| `HasIdentifierPrefix` | `true` if this scanner prepends a single-char type identifier before each barcode (see [Scanner protocol](#scanner-protocol-com-port)). `false` if the scanner sends the raw barcode value only; the type is then inferred from the content. Default: `true`. |
| `TrimTrailingZeros` | `true` if this scanner appends trailing zeros to its output. Only affects activation-code recognition — regular barcodes are unaffected. |

You can add entries manually for testing; the file is gitignored.

## Scanner protocol (COM port)

The scanner must be configured to send data terminated by `\r\n` (CR+LF).

### With identifier prefix (`HasIdentifierPrefix: true`)

The scanner prepends a single-character type identifier before each barcode:

| Prefix | Barcode type |
|---|---|
| `A` | EAN-8 |
| `B` | EAN-13 |
| `M` | ISSN 13+5 |
| `N` | Interleaved 2 of 5 |

Example: scanner sends `B9771234567890` → identifier `B`, code `9771234567890`.

### Without identifier prefix (`HasIdentifierPrefix: false`)

The scanner sends the raw barcode value only (no leading character).
The barcode type is inferred from length and content:

| Pattern | Inferred type |
|---|---|
| 8 digits | EAN-8 |
| 13 digits starting with `977` | ISSN 13+5 |
| 13 digits (other) | EAN-13 |
| 18 digits starting with `977` | ISSN 13+5 with add-on |
| 14 digits | Interleaved 2 of 5 |

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
├── Infrastructure/      # SerialPortService, ComPortEnumerator,
│                        # AppSettings, AppLogger, KeyboardSender, WindowSwitcher
├── UI/
│   ├── Behaviours/      # WPF attached behaviours
│   ├── Converters/      # Value converters
│   ├── Commands/        # RelayCommand, RelayCommand<T>
│   └── ViewModels/      # MainViewModel, AddDeviceViewModel, DeviceManagementViewModel
└── Windows/             # MainWindow, DeviceManagementWindow,
                         # AddDeviceWindow, DebugLogWindow
tests/
├── BarcodeAutoSwitch.UnitTests/        # xUnit + Moq + FluentAssertions
└── BarcodeAutoSwitch.IntegrationTests/
```
