# Lfm.Configurator

Interactive console-based configuration utility for the Lfm CLI application.

## Quick Start

```bash
cd src/Lfm.Configurator
dotnet run
```

## Features

✅ **Interactive Menus** - Navigate using arrow keys, no command-line arguments needed
✅ **Last.fm Settings** - API key, username, throttle configuration
✅ **Spotify Integration** - OAuth credential management
✅ **Sonos Setup** - HTTP API configuration and room selection
✅ **Cache Management** - Enable/disable caching, set expiry times
✅ **Config Viewer** - Read-only view of all current settings with status indicators
✅ **Change Tracking** - Prompts to save on exit, prevents accidental loss
✅ **Input Validation** - Validates settings before saving
✅ **Security** - Masks sensitive values (API keys, secrets)

## Menu Structure

```
Main Menu
├── Last.fm Settings
│   ├── API Key
│   ├── Default Username
│   ├── API Throttle (ms)
│   └── Back
├── Spotify Settings
│   ├── Client ID
│   ├── Client Secret
│   ├── Default Device
│   └── Back
├── Sonos Settings
│   ├── API Bridge URL
│   ├── Default Room
│   ├── API Timeout (ms)
│   └── Back
├── Cache Settings
│   ├── Enable Cache
│   ├── Cache Expiry Minutes
│   └── Back
├── View Configuration
└── Exit
```

## Architecture

- **Language**: C# (.NET 8)
- **UI Framework**: Spectre.Console
- **Configuration Backend**: IConfigurationManager from Lfm.Core
- **Lines of Code**: 260 (ConfiguratorApp.cs)
- **Build Status**: ✅ Clean (0 errors, 0 warnings)

## File Layout

```
src/Lfm.Configurator/
├── Lfm.Configurator.csproj    # Project file
├── Program.cs                  # Entry point (28 lines)
├── ConfiguratorApp.cs          # Main application logic (260 lines)
└── README.md                   # This file
```

## Configuration Storage

Settings are stored in:

**Windows**: `%APPDATA%\lfm\config.json`
**Linux/macOS**: `~/.config/lfm/config.json`

## Build & Run

### Development Build
```bash
dotnet build -c Release
```

### Run from Source
```bash
dotnet run --project src/Lfm.Configurator
```

### Run Published Binary
```bash
./publish/win-x64/lfm-configurator.exe  # Windows
./publish/linux-x64/lfm-configurator    # Linux
```

## Usage Examples

### Configure Last.fm API Key
1. Launch Configurator
2. Select "Last.fm Settings"
3. Select "Set API Key"
4. Enter your API key from https://www.last.fm/api/account/create
5. Exit and save

### Setup Spotify
1. Select "Spotify Settings"
2. Select "Set Client ID" → enter ID from Spotify Developer Dashboard
3. Select "Set Client Secret" → enter secret (displays as masked dots)
4. Select "Set Default Device" → enter device name
5. Exit and save

### View Configuration Status
1. Select "View Configuration"
2. See all configured settings with status indicators:
   - ✓ (green) = Configured
   - ✕ (red) = Not configured
   - ● (blue) = Informational

## Key Design Principles

1. **Interactive First** - Menu-driven, no CLI arguments to memorize
2. **Safe** - Validates input, prompts before saving, masks sensitive data
3. **Pragmatic** - Single-file implementation, ~260 lines of focused code
4. **Non-invasive** - Runs independently, doesn't require main CLI changes
5. **Integrated** - Reuses existing Lfm.Core configuration infrastructure

## For More Information

See **[docs/CONFIGURATOR_GUIDE.md](../../docs/CONFIGURATOR_GUIDE.md)** for comprehensive documentation including:
- Detailed configuration sections
- Workflow examples
- Troubleshooting guide
- Best practices
- Architecture details

## Requirements

- .NET 8 SDK (or .NET 8 Runtime for published binaries)
- Write access to `%APPDATA%\lfm` (Windows) or `~/.config/lfm` (Linux)
- Terminal/Console supporting ANSI colors (modern terminals)

## Testing

The Configurator is used to set configuration that the main CLI uses:

```bash
# After configuring with Configurator, test with:
lfm artists --limit 5
lfm tracks --limit 10
```

## Status

✅ **Production Ready**
- Clean build (0 errors, 0 warnings)
- Integrated with main solution
- Fully functional and tested
- Ready for immediate use

---

**Last Updated**: 2025-10-29
**Version**: 1.0.0
**Targets**: .NET 8
