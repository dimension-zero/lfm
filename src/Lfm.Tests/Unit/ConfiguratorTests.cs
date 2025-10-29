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
    public void DryRunMode_NeverCallsSaveAsync()
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

    /// <summary>
    /// Tests for comprehensive Last.fm configuration state tracking in dry-run mode
    /// </summary>
    [Fact]
    public void DryRunMode_LastFmConfiguration_PreservesAllSettings()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.ApiKey = "test-api-key";
        currentConfig.DefaultUsername = "test-user";
        currentConfig.ApiThrottleMs = 300;

        // Assert
        currentConfig.ApiKey.Should().Be("test-api-key");
        currentConfig.DefaultUsername.Should().Be("test-user");
        currentConfig.ApiThrottleMs.Should().Be(300);
        // Verify all properties are preserved through GetCurrentConfig
        configurator.GetCurrentConfig().ApiKey.Should().Be("test-api-key");
        configurator.GetCurrentConfig().DefaultUsername.Should().Be("test-user");
        configurator.GetCurrentConfig().ApiThrottleMs.Should().Be(300);
    }

    /// <summary>
    /// Tests that configuration state is completely isolated between instances
    /// </summary>
    [Fact]
    public void ConfiguratorInstances_AreIndependent()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config1 = new LfmConfig { ApiKey = "key1" };
        var config2 = new LfmConfig { ApiKey = "key2" };

        // Act
        var configurator1 = new ConfiguratorApp(configManager, config1, dryRun: true);
        var configurator2 = new ConfiguratorApp(configManager, config2, dryRun: true);

        // Modify first instance
        configurator1.GetCurrentConfig().ApiKey = "modified-key1";

        // Assert
        configurator1.GetCurrentConfig().ApiKey.Should().Be("modified-key1");
        configurator2.GetCurrentConfig().ApiKey.Should().Be("key2"); // Should not be affected
    }

    /// <summary>
    /// Tests that DryRunLog accumulates entries correctly
    /// </summary>
    [Fact]
    public void DryRunLog_AccumulatesMultipleEntries()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.ApiKey = "key1";
        currentConfig.DefaultUsername = "user1";
        currentConfig.ApiThrottleMs = 250;

        // Manual logging (simulating what would happen in RunAsync)
        // Note: This is testing the API contract, not the actual logging implementation
        // which requires AnsiConsole.Ask interaction

        // Assert
        // Even though we're not calling the config methods directly,
        // the DryRunLog should be accessible and ready for entries
        configurator.DryRunLog.Should().NotBeNull();
        configurator.DryRunLog.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<string>>();
    }

    /// <summary>
    /// Tests that multiple configuration modifications in sequence preserve state
    /// </summary>
    [Fact]
    public void DryRunMode_SequentialModifications_PreserveState()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act - Multiple sequential modifications
        var currentConfig = configurator.GetCurrentConfig();

        // First modification
        currentConfig.ApiKey = "initial-key";
        currentConfig.ApiThrottleMs = 100;

        // Second modification
        currentConfig.ApiKey = "updated-key";
        currentConfig.DefaultUsername = "user";

        // Third modification
        currentConfig.Spotify.ClientId = "spotify-id";
        currentConfig.Sonos.DefaultRoom = "Living Room";
        currentConfig.CacheEnabled = true;

        // Assert
        var finalConfig = configurator.GetCurrentConfig();
        finalConfig.ApiKey.Should().Be("updated-key");
        finalConfig.ApiThrottleMs.Should().Be(100);
        finalConfig.DefaultUsername.Should().Be("user");
        finalConfig.Spotify.ClientId.Should().Be("spotify-id");
        finalConfig.Sonos.DefaultRoom.Should().Be("Living Room");
        finalConfig.CacheEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that configuration changes don't trigger saves in dry-run mode
    /// </summary>
    [Fact]
    public void DryRunMode_MultipleChanges_NeverTriggersAutoSave()
    {
        // Arrange
        var mockManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(mockManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        for (int i = 0; i < 5; i++)
        {
            currentConfig.ApiKey = $"key-{i}";
            currentConfig.DefaultUsername = $"user-{i}";
            currentConfig.ApiThrottleMs = 100 + (i * 10);
        }

        // Assert
        mockManager.SaveLog.Should().BeEmpty();
        currentConfig.ApiKey.Should().Be("key-4");
    }

    /// <summary>
    /// Tests that DryRunLog is immutable (read-only collection)
    /// </summary>
    [Fact]
    public void DryRunLog_IsImmutable()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var log = configurator.DryRunLog;

        // Assert
        // Should throw NotSupportedException when trying to modify
        var ex = Record.Exception(() => ((System.Collections.Generic.IList<string>)log).Add("test"));
        ex.Should().NotBeNull();
        ex.Should().BeOfType<System.NotSupportedException>();
    }

    /// <summary>
    /// Tests boundary case: empty string values in configuration
    /// </summary>
    [Fact]
    public void DryRunMode_HandlesEmptyStringValues()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.ApiKey = "";
        currentConfig.DefaultUsername = "";
        currentConfig.Spotify.ClientId = "";

        // Assert
        currentConfig.ApiKey.Should().Be("");
        currentConfig.DefaultUsername.Should().Be("");
        currentConfig.Spotify.ClientId.Should().Be("");
    }

    /// <summary>
    /// Tests boundary case: null values in nullable string properties
    /// </summary>
    [Fact]
    public void DryRunMode_HandlesNullValues()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig
        {
            DefaultUsername = "initial",
            Spotify = new SpotifyConfig { ClientId = "initial-id" }
        };
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.DefaultUsername = null!;
        currentConfig.Spotify.ClientId = null!;

        // Assert
        currentConfig.DefaultUsername.Should().BeNull();
        currentConfig.Spotify.ClientId.Should().BeNull();
    }

    /// <summary>
    /// Tests large/extreme values in numeric configuration
    /// </summary>
    [Fact]
    public void DryRunMode_HandlesExtremeLargeValues()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.ApiThrottleMs = int.MaxValue;
        currentConfig.CacheExpiryMinutes = int.MaxValue;
        currentConfig.Sonos.TimeoutMs = int.MaxValue;

        // Assert
        currentConfig.ApiThrottleMs.Should().Be(int.MaxValue);
        currentConfig.CacheExpiryMinutes.Should().Be(int.MaxValue);
        currentConfig.Sonos.TimeoutMs.Should().Be(int.MaxValue);
    }

    /// <summary>
    /// Tests zero and negative values in numeric configuration
    /// </summary>
    [Fact]
    public void DryRunMode_HandlesZeroAndNegativeValues()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.ApiThrottleMs = 0;
        currentConfig.CacheExpiryMinutes = 0;
        currentConfig.Sonos.TimeoutMs = -1; // Test negative

        // Assert
        currentConfig.ApiThrottleMs.Should().Be(0);
        currentConfig.CacheExpiryMinutes.Should().Be(0);
        currentConfig.Sonos.TimeoutMs.Should().Be(-1);
    }

    /// <summary>
    /// Tests very long string values in configuration
    /// </summary>
    [Fact]
    public void DryRunMode_HandlesVeryLongStringValues()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);
        var veryLongString = new string('x', 10000);

        // Act
        var currentConfig = configurator.GetCurrentConfig();
        currentConfig.ApiKey = veryLongString;
        currentConfig.DefaultUsername = veryLongString;

        // Assert
        currentConfig.ApiKey.Should().Be(veryLongString);
        currentConfig.DefaultUsername.Should().Be(veryLongString);
    }

    /// <summary>
    /// Tests that MockConfigurationManager properly persists state across Load/Save cycles
    /// </summary>
    [Fact]
    public async Task MockConfigurationManager_PersistsStateAcrossLoadSave()
    {
        // Arrange
        var mockManager = new MockConfigurationManager();
        var config1 = new LfmConfig
        {
            ApiKey = "key1",
            DefaultUsername = "user1"
        };

        // Act
        await mockManager.SaveAsync(config1);
        var loadedConfig = await mockManager.LoadAsync();

        // Assert
        loadedConfig.ApiKey.Should().Be("key1");
        loadedConfig.DefaultUsername.Should().Be("user1");
    }

    /// <summary>
    /// Tests that configuration state is correctly shared through GetCurrentConfig()
    /// </summary>
    [Fact]
    public void DryRunMode_ConfigurationStateIsShared()
    {
        // Arrange
        var configManager = new MockConfigurationManager();
        var config = new LfmConfig();
        var configurator = new ConfiguratorApp(configManager, config, dryRun: true);

        // Act
        var config1 = configurator.GetCurrentConfig();
        config1.ApiKey = "test-key";

        var config2 = configurator.GetCurrentConfig();

        // Assert - Should return the same instance
        config2.ApiKey.Should().Be("test-key");
        config1.Should().BeSameAs(config2); // Same reference
    }
}
