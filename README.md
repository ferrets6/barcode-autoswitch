# BarcodeAutoSwitch

A Windows desktop application that automatically routes barcode scanner input to the correct point-of-sale application based on the scanned item type.

## Overview

BarcodeAutoSwitch monitors a serial COM port for barcode scanner data, identifies the barcode type, and automatically brings the appropriate application to the foreground — sending the barcode as keyboard input. Newspaper barcodes are routed to **Adriatica Press**; all other items go to **NegozioFacile**.

## Features

- **Automatic application switching** — detects barcode type and switches focus to the target app
- **Serial port monitoring** — listens continuously on a configurable COM port (9600 baud)
- **Barcode type detection** — identifies EAN-8, EAN-13, ISSN 13+5, and Interleaved 2 of 5 codes
- **Newspaper routing** — ISSN codes starting with `977` are sent to Adriatica Press
- **Keyboard simulation** — sends barcode data + Enter to the active application via `SendKeys`
- **Enable/Disable toggle** — switch via UI button or by scanning the special toggle barcode
- **COM port selector** — change the active port at runtime with visual validation feedback
- **Embedded browser** — hosts the Adriatica Press web interface via CefSharp (Chromium)
- **Settings persistence** — selected COM port is saved between sessions

## Requirements

- Windows 7 or later
- .NET Framework 4.5.2
- A serial barcode scanner connected to a COM port
- NegozioFacile installed (for non-newspaper items)

## Configuration

Settings are stored in `BarcodeAutoSwitch/App.config`:

| Setting | Default | Description |
|---|---|---|
| `AdriaticaPressVenditaURL` | `http://www.adriaticapress.it/Vendita.htm` | Adriatica Press sell page |
| `AdriaticaPressLoginURL` | `http://www.adriaticapress.it/Login.htm` | Adriatica Press login page |
| `AdriaticaPressAfterLoginURL` | `http://www.adriaticapress.it/HomeEdicola.htm` | Adriatica Press home after login |
| `NegozioFacileProcessName` | `NegozioFacile` | Process name of the NegozioFacile application |

The COM port is persisted in user settings (default: `COM6`) and can be changed from the UI.

### Serial port defaults (hardcoded)

| Parameter | Value |
|---|---|
| Baud rate | 9600 |
| Data bits | 8 |
| Parity | None |
| Stop bits | 1 |
| Handshake | RTS |

## Special Barcodes

| Barcode | Action |
|---|---|
| `111111111100000011111111` | Toggle enable/disable |
| `111111111122222200000000` | Test current COM port |

## Building

1. Open `BarcodeAutoSwitch.sln` in Visual Studio (2013 or later)
2. Restore NuGet packages (automatic on build)
3. Select the desired platform (`x86` or `x64`) and configuration (`Debug` / `Release`)
4. Build the solution — output goes to `BarcodeAutoSwitch/bin/`

**NuGet dependencies:**

- `CefSharp.Wpf` 71.0.0
- `CefSharp.Common` 71.0.0
- `cef.redist.x86` / `cef.redist.x64` 3.3578.1863
- `System.Windows.Interactivity.WPF` 2.0.20525

## Usage

1. Run `BarcodeAutoSwitch.exe`
2. If COM6 is not the correct port, click **Cambia porta COM** and select the right one
3. The status indicator shows **green** (active) or **red** (disabled)
4. Scan items — the application will automatically switch focus and forward the barcode

## Project Structure

```
BarcodeAutoSwitch/
├── BarcodeAutoSwitch/
│   ├── MainWindow.xaml(.cs)       # Main UI and core logic
│   ├── ComPortWindow.xaml(.cs)    # COM port selection dialog
│   ├── App.xaml(.cs)              # Application entry point
│   ├── App.config                 # Application settings
│   ├── Behaviours/                # WPF custom behaviors
│   ├── Converter/                 # WPF value converters
│   ├── Utils/
│   │   ├── COMPort.cs             # Serial port wrapper
│   │   └── WindowControl.cs       # Win32 window management
│   └── Resources/                 # Images and icons
└── BarcodeAutoSwitch.sln
```

## Author

Federico Ferretti — Metisoft (2019)
