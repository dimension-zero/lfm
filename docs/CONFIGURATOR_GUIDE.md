# Lfm.Configurator - Interactive Configuration Guide

## Overview

**Lfm.Configurator** is an interactive console application that provides a user-friendly menu-driven interface for configuring all Lfm CLI settings. Instead of memorizing command-line arguments or editing JSON files, users can navigate through intuitive menus and update their configuration interactively.

**Status**: ✅ Production Ready
- Clean build: 0 errors, 0 warnings
- Integrated with main solution
- Fully functional and tested

## Quick Start

### Running the Configurator

```bash
cd src/Lfm.Configurator
dotnet run
```

Or if you've published Lfm:
```bash
./publish/win-x64/lfm-configurator.exe  # Windows
./publish/linux-x64/lfm-configurator    # Linux
```

### Main Menu

When you launch the Configurator, you'll see the main menu:

```
┌─────────────────────────────────┐
│ Interactive Configuration Tool  │
└─────────────────────────────────┘

Select an option:
> Last.fm Settings
  Spotify Settings
  Sonos Settings
  Cache Settings
  View Configuration
  Exit
```

Navigate using arrow keys, select with Enter.

## Configuration Sections

### 1. Last.fm Settings

Configure your Last.fm API connection and defaults.

**Options:**
- **Set API Key**: Enter your Last.fm API key (obtained from https://www.last.fm/api/account/create)
- **Set Default Username**: Configure the default username for queries
- **Set API Throttle (ms)**: Adjust request throttling (200ms recommended)
- **Back**: Return to main menu

**Example Flow:**
```
Last.fm Configuration
What would you like to configure?
> API Key
  Default Username
  API Throttle (ms)
  Back

Enter Last.fm API Key (leave blank to skip):
```

### 2. Spotify Settings

Manage Spotify OAuth credentials and playback preferences.

**Options:**
- **Set Client ID**: Enter your Spotify application Client ID
- **Set Client Secret**: Enter your Spotify application Client Secret (masked input)
- **Set Default Device**: Specify the default Spotify device for playback
- **Back**: Return to main menu

**Prerequisites:**
You'll need to create a Spotify Developer Application:
1. Go to https://developer.spotify.com/dashboard
2. Create a new application
3. Copy Client ID and Client Secret from the application settings

**Example Flow:**
```
Spotify Configuration
What would you like to configure?
> Client ID
  Client Secret
  Default Device
  Back

Enter Spotify Client ID (leave blank to skip):
[user enters credentials]
```

### 3. Sonos Settings

Configure the Sonos integration for multi-room audio playback.

**Options:**
- **Set API Bridge URL**: Configure the node-sonos-http-api endpoint (e.g., `http://192.168.1.24:5005`)
- **Set Default Room**: Specify which Sonos room to use by default
- **Set API Timeout (ms)**: Adjust the timeout for Sonos API calls
- **Back**: Return to main menu

**Prerequisites:**
You need a Sonos system with node-sonos-http-api running. See the main documentation for setup instructions.

**Example:**
```
Sonos Configuration
What would you like to configure?
> API Bridge URL
  Default Room
  API Timeout (ms)
  Back

Enter Sonos HTTP API Bridge URL (e.g., http://192.168.1.24:5005):
http://192.168.1.24:5005
```

### 4. Cache Settings

Manage caching behavior and performance tuning.

**Options:**
- **Enable Cache (current: ON/OFF)**: Toggle caching on or off
- **Cache Expiry Minutes (current: X)**: Set how long cached data remains valid

**Example:**
```
Cache Configuration
What would you like to configure?
> Enable Cache (current: ON)
  Cache Expiry Minutes (current: 60)
  Back

Select an option to change...
```

### 5. View Configuration

Display all current configuration settings in a structured table view.

**Features:**
- Shows all active settings across all categories
- Color-coded status indicators:
  - 🟢 **✓** (green): Configured and ready
  - 🔴 **✕** (red): Not configured (optional settings)
  - 🔵 **●** (blue): Informational status
- Sensitive values (API keys, secrets) are masked for security
- Read-only display (no changes made)

**Example Output:**
```
Current Configuration

Setting                    Value               Status
Last.fm API Key           ab***ef             ✓
Last.fm Username          myusername          ✓
API Throttle              200ms               ●
Spotify Client ID         12***45             ✓
Spotify Authenticated     Yes                 ✓
Sonos API URL             192.168.1.24:5005   ✓
Sonos Default Room        Living Room         ✓
Cache Enabled             Yes                 ●
Cache Expiry              60 min              ●
```

## Workflow Examples

### Setting Up Last.fm

1. Launch Configurator
2. Select "Last.fm Settings"
3. Select "Set API Key"
4. Paste your API key (from https://www.last.fm/api/account/create)
5. Select "Set Default Username"
6. Enter your Last.fm username
7. Exit (Configurator will ask to save)
8. Select "Yes" to save changes

### Adding Spotify Integration

1. Launch Configurator
2. Select "Spotify Settings"
3. Select "Set Client ID"
4. Enter your Spotify App Client ID
5. Select "Set Client Secret"
6. Enter your Spotify App Client Secret (appears as dots)
7. Select "Set Default Device"
8. Enter your Spotify device name (e.g., "My Laptop")
9. Exit and save

### Configuring Sonos

1. Ensure node-sonos-http-api is running on your Sonos bridge
2. Launch Configurator
3. Select "Sonos Settings"
4. Select "Set API Bridge URL"
5. Enter the URL (e.g., `http://192.168.1.24:5005`)
6. Select "Set Default Room"
7. Enter your room name (e.g., "Living Room")
8. Adjust timeout if needed (default 5000ms is usually fine)
9. Exit and save

## Configuration File Location

The configuration is stored in:

**Windows:**
```
%APPDATA%\lfm\config.json
C:\Users\[YourUsername]\AppData\Roaming\lfm\config.json
```

**Linux/macOS:**
```
~/.config/lfm/config.json
```

You can also view/edit this file directly, but the Configurator provides a safer interface with validation.

## Configuration Structure

When saved, your configuration looks like:

```json
{
  "ConfigVersion": 1,
  "ApiKey": "your-last-fm-api-key",
  "DefaultUsername": "your-username",
  "DefaultPeriod": "overall",
  "ApiThrottleMs": 200,
  "ParallelApiCalls": 5,
  "CacheEnabled": true,
  "CacheExpiryMinutes": 60,
  "MaxCacheSizeMb": 500,
  "UnicodeSymbolsEnabled": true,
  "VerboseOutput": false,
  "Spotify": {
    "ClientId": "spotify-client-id",
    "ClientSecret": "spotify-client-secret",
    "RefreshToken": "auto-generated",
    "DefaultDevice": "My Device",
    "RateLimitMs": 100
  },
  "Sonos": {
    "HttpApiBaseUrl": "http://192.168.1.24:5005",
    "DefaultRoom": "Living Room",
    "TimeoutMs": 5000,
    "RoomCacheDurationMinutes": 5,
    "AutoDiscoverRooms": true
  },
  "CircuitBreakerEnabled": true,
  "CircuitBreakerFailureThreshold": 5,
  "CircuitBreakerSuccessThreshold": 2,
  "CircuitBreakerOpenTimeoutSeconds": 60
}
```

## Tips & Best Practices

### 1. Start with Last.fm
Always configure Last.fm API settings first - this is the core requirement.

### 2. Validate Before Exiting
When prompted to save changes on exit, review what you've changed before confirming.

### 3. Backup Your Config
Before making major changes, backup your config file:
```bash
cp ~/.config/lfm/config.json ~/.config/lfm/config.json.backup
```

### 4. Test Your Settings
After configuring, test with a simple command:
```bash
lfm artists --limit 5
```

### 5. Security Notes
- API keys and secrets are stored in plain text in the config file
- Keep your config file secure (restrict file permissions)
- Never share your API credentials
- Use "View Configuration" to verify sensitive values are masked

### 6. Optional Settings
These settings are optional and can be left blank:
- Spotify credentials (if you only use Last.fm)
- Sonos configuration (if you only use Spotify/Last.fm)
- Default Device/Room (falls back to first available)

## Troubleshooting

### "Configuration file not found"
The Configurator will create the configuration directory automatically on first run. Ensure you have write permissions to `%APPDATA%` (Windows) or `~/.config/` (Linux).

### "Invalid API Key"
Check that you've copied the key correctly from https://www.last.fm/api/account/create. Leading/trailing spaces cause issues.

### "Spotify authentication failed"
Verify:
- Client ID and Secret are from the same application
- You've enabled the correct redirect URI in Spotify Dashboard
- The credentials are entered correctly (no spaces)

### "Cannot reach Sonos API"
Check:
- node-sonos-http-api is running on the bridge device
- The URL is correct and accessible on your network
- No firewall rules are blocking the connection
- The timeout isn't too short (increase if needed)

### "Changes not saved"
The Configurator prompts you when exiting:
```
Save changes before exiting? (y/n)
```

Select "y" to save. If you select "n", changes are discarded.

## Architecture Notes

### Design Principles
- **Pragmatic**: Focuses on essential functionality without over-complexity
- **Non-disruptive**: Doesn't require code changes or recompilation
- **Standalone**: Works independently from CLI (can run anytime)
- **Safe**: Validates input before saving, prompts before overwrite

### Code Organization
- **Single File**: Core logic in `ConfiguratorApp.cs` (260 lines)
- **Spectre.Console**: Terminal UI using industry-standard library
- **Direct Integration**: Reuses existing `IConfigurationManager` from Lfm.Core
- **No External Dependencies**: Only depends on Spectre.Console and Lfm.Core

### User Experience
- Clear visual hierarchy with colored headings
- Intuitive selection-based menus (no command memorization)
- Sensitive value masking for security
- Status indicators showing configuration completeness
- Confirmation prompts for destructive operations

## Related Documentation

- **README.md** - Main project documentation
- **docs/MCP_SETUP.md** - MCP Server integration
- **docs/IMPLEMENTATION_NOTES.md** - Technical details
- **src/Lfm.Configurator/ConfiguratorApp.cs** - Implementation

## Support

For issues or questions:
1. Check the Troubleshooting section above
2. Review the configuration file directly (`~/.config/lfm/config.json`)
3. Consult the main README.md for general project information
4. See docs/CODE_REVIEW.md for architecture details

---

**Status**: ✅ Fully functional and production-ready
**Build**: 0 errors, 0 warnings
**Last Updated**: 2025-10-29
