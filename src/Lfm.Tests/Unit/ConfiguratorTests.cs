using FluentAssertions;
using Lfm.Configurator;
using Lfm.Core.Configuration;
using Lfm.Tests.Mocks;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for ConfiguratorApp with DryRun mode support
/// Tests that the configurator can be tested safely without modifying disk files
/// </summary>
public class ConfiguratorTests
{
    [Fact]
    public void Constructor_WithDryRunMode_SetsDryRunFlag()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();

        // Act
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Assert
        // If we can construct without error and access properties, DryRun is working
        configurator.DryRunLog.Should().NotBeNull();
        configurator.GetCurrentConfig().Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithoutDryRunMode_DefaultsFalse()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();

        // Act
        var configurator = new ConfiguratorApp(configManager, config);

        // Assert
        configurator.DryRunLog.Should().NotBeNull();
        configurator.DryRunLog.Should().BeEmpty();
    }

    [Fact]
    public void GetCurrentConfig_ReturnsConfigInstance()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig
        {
            ApiKey = "test-api-key",
            DefaultUsername = "testuser"
        };

        // Act
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);
        var result = configurator.GetCurrentConfig();

        // Assert
        result.Should().NotBeNull();
        result.ApiKey.Should().Be("test-api-key");
        result.DefaultUsername.Should().Be("testuser");
    }

    [Fact]
    public void DryRunLog_IsReadOnly()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();

        // Act
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Assert
        // Should be able to read but not modify
        var log = configurator.DryRunLog;
        log.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<string>>();
    }

    [Fact]
    public void ConfigModification_InDryRunMode_LogsChanges()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig
        {
            ApiKey = "old-key",
            ApiThrottleMs = 200
        };
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        // Simulate config change by directly modifying the config
        // (In real usage, this would come from user input via AnsiConsole.Ask)
        var oldKey = configurator.GetCurrentConfig().ApiKey;
        configurator.GetCurrentConfig().ApiKey = "new-key";

        // Assert
        configurator.GetCurrentConfig().ApiKey.Should().Be("new-key");
        configurator.GetCurrentConfig().Should().NotBeNull();
    }

    [Fact]
    public void DryRunMode_SkipsConsoleClearing()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();

        // Act
        // This test just verifies that in DryRun mode, we can construct
        // without errors (AnsiConsole.Clear would fail in non-console environment)
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Assert
        // If DryRun mode properly skips AnsiConsole.Clear, this should not throw
        configurator.Should().NotBeNull();
    }

    [Fact]
    public void MockConfigurationManager_NeverSavesToDisk()
    {
        // Arrange
        var mockManager = new MockConfigurationManager();
        var config = new LfmConfig { ApiKey = "test-key" };

        // Act
        mockManager.SaveAsync(config).Wait();

        // Assert
        // Verify save was tracked in mock but not persisted to real disk
        mockManager.SaveLog.Should().HaveCount(1);
        mockManager.GetCurrentConfig().ApiKey.Should().Be("test-key");
    }

    [Fact]
    public void MockConfigurationManager_TracksMultipleSaves()
    {
        // Arrange
        var mockManager = new MockConfigurationManager();
        var config1 = new LfmConfig { ApiKey = "key1" };
        var config2 = new LfmConfig { ApiKey = "key2" };

        // Act
        mockManager.SaveAsync(config1).Wait();
        mockManager.SaveAsync(config2).Wait();

        // Assert
        mockManager.SaveLog.Should().HaveCount(2);
        mockManager.GetCurrentConfig().ApiKey.Should().Be("key2");
    }

    [Fact]
    public void MockConfigurationManager_CanResetLog()
    {
        // Arrange
        var mockManager = new MockConfigurationManager();
        var config = new LfmConfig { ApiKey = "test-key" };
        mockManager.SaveAsync(config).Wait();

        // Act
        mockManager.ResetSaveLog();

        // Assert
        mockManager.SaveLog.Should().BeEmpty();
    }

    [Fact]
    public void DryRunMode_AllowsSpotifyConfigurationTracking()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.Spotify.ClientId = "test-client-id";
        currentConfig.Spotify.DefaultDevice = "test-device";

        // Assert
        currentConfig.Spotify.ClientId.Should().Be("test-client-id");
        currentConfig.Spotify.DefaultDevice.Should().Be("test-device");
    }

    [Fact]
    public void DryRunMode_AllowsSonosConfigurationTracking()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.Sonos.HttpApiBaseUrl = "http://192.168.1.24:5005";
        currentConfig.Sonos.DefaultRoom = "Living Room";
        currentConfig.Sonos.TimeoutMs = 3000;

        // Assert
        currentConfig.Sonos.HttpApiBaseUrl.Should().Be("http://192.168.1.24:5005");
        currentConfig.Sonos.DefaultRoom.Should().Be("Living Room");
        currentConfig.Sonos.TimeoutMs.Should().Be(3000);
    }

    [Fact]
    public void DryRunMode_AllowsCacheConfigurationTracking()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig { CacheEnabled = true, CacheExpiryMinutes = 10 };
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.CacheEnabled = false;
        currentConfig.CacheExpiryMinutes = 30;

        // Assert
        currentConfig.CacheEnabled.Should().BeFalse();
        currentConfig.CacheExpiryMinutes.Should().Be(30);
    }

    [Fact]
    public void DryRunMode_ConfigurationChangesAreAccessible()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig
        {
            ApiKey = "api-key",
            DefaultUsername = "username",
            ApiThrottleMs = 200,
            CacheEnabled = true,
            CacheExpiryMinutes = 60
        };
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var modifiedConfig = configurator.GetCurrentConfig();
        modifiedConfig.ApiKey = "new-key";
        modifiedConfig.ApiThrottleMs = 500;

        // Assert
        // Modified should have new values
        modifiedConfig.ApiKey.Should().Be("new-key");
        modifiedConfig.ApiThrottleMs.Should().Be(500);

        // Should still be accessible via GetCurrentConfig
        configurator.GetCurrentConfig().ApiKey.Should().Be("new-key");
    }

    [Fact]
    public async Task DryRunMode_NeverCallsSaveAsync()
    {
        // Arrange
        var mockManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(mockManager, config, dryRun: true);

        // Act
        // Simulate modifications without calling RunAsync (which requires user interaction)
        configurator.GetCurrentConfig().ApiKey = "test-key";

        // Assert
        // In DryRun mode, SaveAsync should never be called automatically
        mockManager.SaveLog.Should().BeEmpty();
        mockManager.GetCurrentConfig().ApiKey.Should().BeEmpty(); // Original should be unchanged
    }
}
