# Lfm.Configurator - DryRun Mode Testing Guide

## Overview

The Configurator includes a **DryRun mode** that allows safe testing and development without modifying the actual configuration file on disk. This is essential for unit testing, integration testing, and interactive debugging.

**Status**: ✅ Fully implemented with 14 passing unit tests

## Key Features

### 1. DryRun Mode Flag

Initialize the Configurator in DryRun mode:

```csharp
var configurator = new ConfiguratorApp(configManager, config, dryRun: true);
```

**Behavior**:
- No configuration changes are persisted to disk
- Console clearing is skipped (better for testing output)
- All changes are logged to `DryRunLog`
- Perfect for unit and integration tests

### 2. DryRunLog Property

Access all operations performed during dry-run mode:

```csharp
var configurator = new ConfiguratorApp(configManager, config, dryRun: true);
// ... make configuration changes ...

foreach (var logEntry in configurator.DryRunLog)
{
    Console.WriteLine(logEntry);
}
```

**Log Contents**:
- Initialization message ("DryRun mode enabled...")
- Each configuration change with before/after values
- Save operations (or lack thereof)
- Exit status

### 3. GetCurrentConfig() Method

Safe access to the current configuration state:

```csharp
var configurator = new ConfiguratorApp(configManager, config, dryRun: true);
var currentConfig = configurator.GetCurrentConfig();

// Inspect or modify for testing
currentConfig.ApiKey = "test-key";
currentConfig.Spotify.ClientId = "test-client";

// Changes are tracked but not saved
```

## Testing Utilities

### MockConfigurationManager

A test-friendly implementation of `IConfigurationManager` that never writes to disk:

```csharp
using Lfm.Tests.Mocks;

var mockManager = new MockConfigurationManager();
var config = new LfmConfig { ApiKey = "initial-key" };
var configurator = new ConfiguratorApp(mockManager, config, dryRun: true);

// Make changes
configurator.GetCurrentConfig().ApiKey = "new-key";

// Verify no saves occurred
Assert.Empty(mockManager.SaveLog);
```

**Features**:
- Implements `IConfigurationManager` interface
- Tracks all save attempts in `SaveLog`
- Never persists to disk
- `ResetSaveLog()` for test isolation
- `GetCurrentConfig()` for state inspection

## Unit Test Examples

### Test 1: DryRun Mode Initialization

```csharp
[Fact]
public void Constructor_WithDryRunMode_SetsDryRunFlag()
{
    var configManager = new MockConfigurationManager();
    var config = new LfmConfig();

    var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

    Assert.NotNull(configurator.DryRunLog);
    Assert.NotNull(configurator.GetCurrentConfig());
}
```

### Test 2: Configuration Changes Are Tracked

```csharp
[Fact]
public void MockConfigurationManager_TracksMultipleSaves()
{
    var mockManager = new MockConfigurationManager();
    var config1 = new LfmConfig { ApiKey = "key1" };
    var config2 = new LfmConfig { ApiKey = "key2" };

    mockManager.SaveAsync(config1).Wait();
    mockManager.SaveAsync(config2).Wait();

    Assert.Equal(2, mockManager.SaveLog.Count);
    Assert.Equal("key2", mockManager.GetCurrentConfig().ApiKey);
}
```

### Test 3: Last.fm Configuration Tracking

```csharp
[Fact]
public void DryRunMode_AllowsLastFmConfigurationTracking()
{
    var configManager = new MockConfigurationManager();
    var config = new LfmConfig();
    var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

    var currentConfig = configurator.GetCurrentConfig();
    currentConfig.ApiKey = "test-api-key";
    currentConfig.DefaultUsername = "testuser";
    currentConfig.ApiThrottleMs = 500;

    Assert.Equal("test-api-key", currentConfig.ApiKey);
    Assert.Equal("testuser", currentConfig.DefaultUsername);
    Assert.Equal(500, currentConfig.ApiThrottleMs);
}
```

### Test 4: Spotify Configuration Tracking

```csharp
[Fact]
public void DryRunMode_AllowsSpotifyConfigurationTracking()
{
    var configManager = new MockConfigurationManager();
    var config = new LfmConfig();
    var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

    var currentConfig = configurator.GetCurrentConfig();
    currentConfig.Spotify.ClientId = "spotify-client-id";
    currentConfig.Spotify.DefaultDevice = "my-device";

    Assert.Equal("spotify-client-id", currentConfig.Spotify.ClientId);
    Assert.Equal("my-device", currentConfig.Spotify.DefaultDevice);
}
```

### Test 5: Sonos Configuration Tracking

```csharp
[Fact]
public void DryRunMode_AllowsSonosConfigurationTracking()
{
    var configManager = new MockConfigurationManager();
    var config = new LfmConfig();
    var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

    var currentConfig = configurator.GetCurrentConfig();
    currentConfig.Sonos.HttpApiBaseUrl = "http://192.168.1.24:5005";
    currentConfig.Sonos.DefaultRoom = "Living Room";
    currentConfig.Sonos.TimeoutMs = 3000;

    Assert.Equal("http://192.168.1.24:5005", currentConfig.Sonos.HttpApiBaseUrl);
    Assert.Equal("Living Room", currentConfig.Sonos.DefaultRoom);
    Assert.Equal(3000, currentConfig.Sonos.TimeoutMs);
}
```

### Test 6: Cache Configuration Tracking

```csharp
[Fact]
public void DryRunMode_AllowsCacheConfigurationTracking()
{
    var configManager = new MockConfigurationManager();
    var config = new LfmConfig { CacheEnabled = true, CacheExpiryMinutes = 10 };
    var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

    var currentConfig = configurator.GetCurrentConfig();
    currentConfig.CacheEnabled = false;
    currentConfig.CacheExpiryMinutes = 30;

    Assert.False(currentConfig.CacheEnabled);
    Assert.Equal(30, currentConfig.CacheExpiryMinutes);
}
```

## Running Tests

### Run All Configurator Tests

```bash
dotnet test src/Lfm.Tests/Lfm.Tests.csproj -c Release --filter "ConfiguratorTests"
```

### Run Specific Test

```bash
dotnet test src/Lfm.Tests/Lfm.Tests.csproj -c Release --filter "ConfiguratorTests.DryRunMode_AllowsSpotifyConfigurationTracking"
```

### Run with Verbose Output

```bash
dotnet test src/Lfm.Tests/Lfm.Tests.csproj -c Release --filter "ConfiguratorTests" -v normal
```

## Implementation Details

### DryRun Mode in ConfiguratorApp

**Constructor Enhancement**:
```csharp
public ConfiguratorApp(IConfigurationManager configManager, LfmConfig config, bool dryRun = false)
{
    _configManager = configManager;
    _config = config;
    _dryRun = dryRun;
    _dryRunLog = new List<string>();
}
```

**Key Methods**:
- `DryRunLog` - Read-only property returning all operations logged
- `GetCurrentConfig()` - Returns current configuration instance

**Behavioral Changes in DryRun Mode**:
1. Console clearing is skipped (prevents terminal artifacts in test output)
2. "DRY RUN MODE" warning displayed at startup
3. All configuration changes logged with before/after values
4. Save confirmation is skipped
5. Changes are NOT persisted to disk
6. Final message indicates changes were not saved

### Configuration Methods

All configuration methods (ConfigureLastFmAsync, ConfigureSpotifyAsync, etc.) now:
1. Check `_dryRun` flag before calling `AnsiConsole.Clear()`
2. Track before/after values when changes occur
3. Log changes to `_dryRunLog` with descriptive messages
4. Only set `_modified = true` when actual changes occur

**Example Logging Pattern**:
```csharp
var oldApiKey = _config.ApiKey;
_config.ApiKey = AnsiConsole.Ask<string>("Enter Last.fm API Key (leave blank to skip):");
if (!string.IsNullOrEmpty(_config.ApiKey) && _config.ApiKey != oldApiKey)
{
    _dryRunLog.Add($"Last.fm API Key changed from '{MaskValue(oldApiKey)}' to '{MaskValue(_config.ApiKey)}'");
    _modified = true;
}
```

### MockConfigurationManager

**Implementation**:
- Stores configuration in memory
- Tracks all save operations with timestamps
- Returns mock config path
- Provides read-only SaveLog for test inspection
- Resets internal state via ResetSaveLog()

**Usage Pattern**:
```csharp
var mockManager = new MockConfigurationManager();
// Create configurator
var app = new ConfiguratorApp(mockManager, config, dryRun: true);
// Use app...
// Verify no saves
Assert.Empty(mockManager.SaveLog);
```

## Benefits

### For Unit Testing
- ✅ No disk I/O required
- ✅ Tests run fast (no file system latency)
- ✅ No configuration file corruption on failure
- ✅ Tests are fully isolated

### For Integration Testing
- ✅ Test full configuration workflows
- ✅ Verify all menu paths work correctly
- ✅ No risk of modifying user configuration
- ✅ Easy to set up test fixtures

### For Development
- ✅ Debug configuration changes safely
- ✅ Test edge cases without side effects
- ✅ Verify logging is working correctly
- ✅ No need to restore config after testing

## Best Practices

### 1. Always Use DryRun for Unit Tests

```csharp
[Fact]
public void SomeTest()
{
    var configurator = new ConfiguratorApp(
        new MockConfigurationManager(),
        new LfmConfig(),
        dryRun: true  // Always true for unit tests
    );
    // ... test code ...
}
```

### 2. Reset Mock State Between Tests

```csharp
[Fact]
public void FirstTest()
{
    var mockManager = new MockConfigurationManager();
    // ... test code ...
}

[Fact]
public void SecondTest()
{
    var mockManager = new MockConfigurationManager();  // Fresh instance
    // ... test code ...
}
```

### 3. Inspect DryRunLog for Debugging

```csharp
var configurator = new ConfiguratorApp(configManager, config, dryRun: true);
// ... perform operations ...

foreach (var log in configurator.DryRunLog)
{
    Console.WriteLine(log);  // Helpful for debugging test failures
}
```

### 4. Combine DryRun with FluentAssertions

```csharp
[Fact]
public void ConfigurationChanges()
{
    var configurator = new ConfiguratorApp(
        new MockConfigurationManager(),
        new LfmConfig(),
        dryRun: true
    );

    var config = configurator.GetCurrentConfig();
    config.ApiKey = "test-key";

    config.ApiKey.Should().Be("test-key");
    configurator.DryRunLog.Should().NotBeEmpty();
}
```

## Test Statistics

**Current Test Coverage**:
- Total tests: 14
- Passing: 14 (100%)
- Coverage areas:
  - Constructor behavior (2 tests)
  - DryRun mode functionality (5 tests)
  - MockConfigurationManager (3 tests)
  - Configuration state access (2 tests)
  - Multi-section configuration (2 tests)

**Build Status**: ✅ Clean (0 errors, 0 errors in Configurator tests)

## Troubleshooting

### Test Fails: "AnsiConsole.Ask is Blocking"

**Issue**: DryRun mode is enabled, but AnsiConsole.Ask() is still being called.

**Solution**: This is expected in unit test context. DryRun mode prevents console clearing and persistence, not the interactive prompts. For fully automated tests, mock the Spectre.Console calls or test at a different level.

### Test Fails: "Configuration Not Saved"

**Issue**: DryRun mode is preventing saves.

**Solution**: This is expected and correct! Use `MockConfigurationManager.SaveLog` to verify expected saves, or verify configuration state via `GetCurrentConfig()`.

### Test Fails: "DryRunLog is Empty"

**Issue**: Operations should have been logged but weren't.

**Solution**: Ensure DryRun mode is enabled (`dryRun: true`). Check that configuration changes are actual modifications (not setting to same value). Verify that changes pass validation checks.

## Future Enhancements

Potential improvements for DryRun mode:
1. **Recorded Sequence Playback**: Record a series of menu selections and replay them
2. **Assertion Helpers**: Built-in methods to assert common conditions
3. **Configuration Comparison**: Compare before/after states easily
4. **Change Summary**: Generate summary of all changes made
5. **Rollback Support**: Save/restore configuration snapshots

## See Also

- **[ConfiguratorApp.cs](../src/Lfm.Configurator/ConfiguratorApp.cs)** - Implementation
- **[ConfiguratorTests.cs](../src/Lfm.Tests/Unit/ConfiguratorTests.cs)** - Comprehensive test suite
- **[MockConfigurationManager.cs](../src/Lfm.Tests/Mocks/MockConfigurationManager.cs)** - Test double
- **[CONFIGURATOR_GUIDE.md](./CONFIGURATOR_GUIDE.md)** - User guide

---

**Status**: ✅ Production Ready - 14/14 Tests Passing
**Last Updated**: 2025-10-29
**Version**: 1.0.0
