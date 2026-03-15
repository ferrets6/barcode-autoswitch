# BarcodeAutoSwitch

WPF application (.NET 8, x64) that listens on a serial COM port for barcode scanner input and automatically routes each barcode to the correct application window.

- **Newspaper barcodes** (ISSN prefix `977`, types `M`/`B`) → Adriatica Press (embedded Chromium browser, sends Alt+T + barcode code)
- **All other barcodes** → NegozioFacile (external process, types barcode via keyboard emulation)
- **Control barcodes** → toggle auto-switch on/off, or test the COM port

## Requirements

| | |
|---|---|
| OS | Windows 10 or later (64-bit) |
| Runtime | [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8) (or use self-contained publish) |
| Hardware | Barcode scanner connected via USB serial port |

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
    "NegozioFacileProcessName": "notepad",
    "DefaultSerialPort": "COM3"
  }
}
```

See `appsettings.local.json.example` for reference.

Key settings:

| Key | Description |
|---|---|
| `AdriaticaPress.VenditaUrl` | URL of the Adriatica Press sales page |
| `AdriaticaPress.LoginUrl` | URL used to detect the login page |
| `Application.NegozioFacileProcessName` | Process name of NegozioFacile (.exe without extension) |
| `Application.DefaultSerialPort` | COM port opened on startup (e.g. `COM3`) |

The selected COM port is persisted per-user in `%LOCALAPPDATA%\BarcodeAutoSwitch\usersettings.json`.

## Tests

```bash
dotnet test tests/BarcodeAutoSwitch.UnitTests   -r win-x64
dotnet test tests/BarcodeAutoSwitch.IntegrationTests -r win-x64
```

## Debug console

Click **Show Console** in the status bar to open a debug console window with live log output (`[SERIALE]`, `[BARCODE]`, `[ROUTING]`, `[KEYBOARD]`, `[FOCUS]` prefixes).

## Project structure

```
BarcodeAutoSwitch/
├── Core/
│   ├── Interfaces/      # Contracts (ISerialPortService, IKeyboardSender, …)
│   ├── Models/          # BarcodeReading, BarcodeDestination, ControlCodeType
│   └── Services/        # BarcodeParser, BarcodeRouter, routing strategies
├── Infrastructure/      # SerialPortService, KeyboardSender, WindowSwitcher, Win32Console, AppSettings
├── UI/
│   ├── Behaviours/      # WPF attached behaviours
│   ├── Converters/      # Value converters
│   ├── Commands/        # RelayCommand
│   └── ViewModels/      # MainViewModel, ComPortViewModel
└── Windows/             # MainWindow.xaml, ComPortWindow.xaml (code-behind only)
tests/
├── BarcodeAutoSwitch.UnitTests/        # 32 unit tests (xUnit + Moq + FluentAssertions)
└── BarcodeAutoSwitch.IntegrationTests/ # 10 integration tests
```
