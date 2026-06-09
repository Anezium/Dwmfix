# DwmFix

DwmFix is a native Windows tray utility that keeps Desktop Window Manager composition active on selected displays. It is a clean .NET rewrite of the original Python/PyQt prototype.

The app renders a tiny click-through layered window on the target monitor. The window is almost fully transparent, does not activate, does not appear in Alt+Tab, and can be started automatically with Windows.

## Features

- Native tray app, no Python runtime.
- Single-instance behavior. Launching the app again opens settings.
- Auto-targets secondary displays by default.
- Optional explicit monitor selection.
- Normal and boost rendering modes.
- Configurable render FPS.
- Per-user autostart through the Windows Run registry key.
- JSON settings in `%AppData%\DwmFix\settings.json`.
- GitHub Actions build that publishes a self-contained `DwmFix.exe`.

## Usage

1. Run `DwmFix.exe`.
2. Use the tray icon to enable or disable the fix, toggle boost mode, choose monitors, or open settings.
3. Enable `Start with Windows` when the behavior looks good on your setup.

## Build

Requirements:

- Windows
- .NET 10 SDK

```powershell
dotnet restore DwmFix.sln
dotnet build DwmFix.sln -c Release
dotnet run --project tests/DwmFix.Core.SmokeTests/DwmFix.Core.SmokeTests.csproj -c Release --no-build
dotnet publish src/DwmFix.App/DwmFix.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
```

The executable is written to `artifacts/win-x64/DwmFix.exe`.

## Attribution

This repository is forked from `Arccalc/Dwmfix` and remains MIT licensed. See `NOTICE.md` for rewrite notes.
