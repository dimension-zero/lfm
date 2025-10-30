using System.Text.Json;
using Microsoft.Extensions.Logging;
using Lfm.Shared.Models.Results;
using Lfm.Integration.Sonos.Models;

namespace Lfm.Core.Configuration;

public enum UnicodeSupport
{
    Auto,     // Auto-detect terminal capabilities
    Enabled,  // Force Unicode symbols
    Disabled  // Force ASCII fallbacks
}

public enum PlayerType
{
    Spotify,
    Sonos
}

public enum DataSourceMode
{
    LastFm,       // Use Last.fm API only
    LocalFiles,   // Use local file exports only (Spotify, YouTube Music)
    Merged        // Combine Last.fm API + local files
}

public enum EnrichmentMode
{
    Optional,     // Enrichment only runs if explicitly requested
    BestEffort,   // Try enrichment, gracefully degrade on failure
    Mandatory     // Fail parsing if enrichment unavailable
}

public class LfmConfig
{
    public int ConfigVersion { get; set; } = 1;  // Schema version for migration tracking
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultUsername { get; set; } = string.Empty;
    public string DefaultPeriod { get; set; } = "overall";
    public int DefaultLimit { get; set; } = 10;
    public int ApiThrottleMs { get; set; } = 200;
    public int ParallelApiCalls { get; set; } = 5;
    public int NormalSearchDepth { get; set; } = 10000;
    public int DeepSearchTimeoutSeconds { get; set; } = 300;

    // Circuit Breaker Configuration
    public bool CircuitBreakerEnabled { get; set; } = true;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerSuccessThreshold { get; set; } = 2;
    public int CircuitBreakerOpenTimeoutSeconds { get; set; } = 60;

    // Cache Configuration
    public bool CacheEnabled { get; set; } = true;
    public int CacheExpiryMinutes { get; set; } = 10;
    public int MaxCacheSizeMB { get; set; } = 100;
    public int MaxCacheFiles { get; set; } = 10000;
    public int MaxCacheAgeDays { get; set; } = 30;
    public DateTime LastCacheCleanup { get; set; } = DateTime.MinValue;
    public int CleanupIntervalHours { get; set; } = 6;
    
    // Display Configuration
    public UnicodeSupport UnicodeSymbols { get; set; } = UnicodeSupport.Auto;

    // Playlist Diversity Configuration
    public int DateRangeDiversityMultiplier { get; set; } = 10;
    public int MaxEmptyWindows { get; set; } = 5;

    // Mixtape Configuration
    public float DefaultMixtapeBias { get; set; } = 0.3f;
    public int DefaultMinPlays { get; set; } = 0;
    public int MaxMixtapeSampleSize { get; set; } = 50000;

    // Tag Filtering Configuration
    public List<string> ExcludedTags { get; set; } = new()
    {
        "classical", "classical music", "opera", "symphony", "orchestral",
        "baroque", "romantic era", "contemporary classical", "chamber music",
        "choral", "classical crossover"
    };

    public int TagFilterThreshold { get; set; } = 30;
    public bool EnableTagFiltering { get; set; } = false;
    public int MaxTagLookups { get; set; } = 20;

    // Spotify Configuration
    public SpotifyConfig Spotify { get; set; } = new();

    // Sonos Configuration
    public SonosConfig Sonos { get; set; } = new();

    // Player Selection
    public PlayerType DefaultPlayer { get; set; } = PlayerType.Spotify;

    // Data Source Configuration
    public DataSourceMode DataSource { get; set; } = DataSourceMode.LastFm;
    public List<string> LocalFilePaths { get; set; } = new();

    // Album Enrichment Configuration
    public AlbumEnrichmentConfig AlbumEnrichment { get; set; } = new();

    // Debug settings
    public bool EnableApiDebugLogging { get; set; } = false;
}

public class SpotifyConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string DefaultDevice { get; set; } = string.Empty;
    public int RateLimitDelayMs { get; set; } = 100;
    public int SearchTimeoutMs { get; set; } = 5000;
    public int MaxRetries { get; set; } = 3;
    public bool FallbackToLooseSearch { get; set; } = true;
}

public class AlbumEnrichmentConfig
{
    /// <summary>
    /// Enable album enrichment for local files that lack album metadata.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Enrichment behavior mode.
    /// Optional: Only enrich if explicitly requested
    /// BestEffort: Try enrichment, fall back to null on errors
    /// Mandatory: Fail if enrichment unavailable
    /// </summary>
    public EnrichmentMode Mode { get; set; } = EnrichmentMode.BestEffort;

    /// <summary>
    /// Priority order of enrichers to try (first successful wins).
    /// Valid values: "LastFm", "Spotify", "MusicBrainz", "YouTubeCsv", "YouTubeApi"
    /// </summary>
    public List<string> EnricherPriority { get; set; } = new()
    {
        "LastFm",       // Fastest, already integrated
        "Spotify",      // Fast, already integrated
        "YouTubeCsv",   // No API calls, offline
        "MusicBrainz"   // Comprehensive, open database
    };

    /// <summary>
    /// Timeout per enricher query in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Cache enrichment results to avoid repeated API calls.
    /// </summary>
    public bool CacheResults { get; set; } = true;

    /// <summary>
    /// YouTube Data API v3 key (required for YouTubeApi enricher).
    /// </summary>
    public string YouTubeApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Path to YouTube Music library CSV for local lookup.
    /// </summary>
    public string YouTubeLibraryCsvPath { get; set; } = string.Empty;
}

public interface IConfigurationManager
{
    Task<LfmConfig> LoadAsync();
    Task<Result<LfmConfig>> LoadWithValidationAsync();
    Task SaveAsync(LfmConfig config);
    string GetConfigPath();
}

public class ConfigurationManager : IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly IConfigurationValidator? _validator;
    private readonly string _configPath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public ConfigurationManager(ILogger<ConfigurationManager> logger, IConfigurationValidator? validator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator;

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "lfm"
        );

        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "config.json");
    }

    public string GetConfigPath() => _configPath;

    private void BackupConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupPath = $"{_configPath}.backup-v0.{timestamp}";
                File.Copy(_configPath, backupPath);
                _logger.LogInformation("Config backed up to {BackupPath}", backupPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create config backup, continuing with migration");
        }
    }

    private string MigrateV0ToV1(string json, JsonElement root)
    {
        _logger.LogInformation("Migrating config from v0 to v1 (DefaultPlayer string → enum)");

        try
        {
            // Parse as dictionary for manipulation
            var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (configDict == null)
            {
                throw new InvalidOperationException("Failed to parse config as dictionary for migration");
            }

            // Migrate DefaultPlayer from string to enum-compatible string
            if (configDict.TryGetValue("defaultPlayer", out var playerValue))
            {
                if (playerValue.ValueKind == JsonValueKind.String)
                {
                    var oldValue = playerValue.GetString();
                    var newValue = oldValue?.ToLower() switch
                    {
                        "spotify" => "Spotify",
                        "text" => "Spotify",      // text mode = no playback = default to Spotify
                        "sonos" => "Sonos",
                        _ => "Spotify"            // Unknown values default to Spotify
                    };

                    _logger.LogInformation("Migrating defaultPlayer: '{OldValue}' → '{NewValue}'", oldValue, newValue);
                    configDict["defaultPlayer"] = JsonDocument.Parse($"\"{newValue}\"").RootElement;
                }
            }

            // Add version field
            configDict["configVersion"] = JsonDocument.Parse("1").RootElement;

            // Serialize back to JSON
            var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return JsonSerializer.Serialize(configDict, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration v0→v1 failed");
            throw;
        }
    }

    private async Task<LfmConfig> LoadWithMigrationAsync(string json)
    {
        // Step 1: Try current schema first
        try
        {
            var config = JsonSerializer.Deserialize<LfmConfig>(json, GetJsonOptions());
            if (config != null)
            {
                return config;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Current schema deserialization failed, attempting migration: {Message}", ex.Message);
        }

        // Step 2: Detect version from raw JSON
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int version = root.TryGetProperty("configVersion", out var versionElement)
            ? versionElement.GetInt32()
            : 0; // Legacy configs have no version field

        _logger.LogInformation("Detected config version: {Version}", version);

        // Step 3: Apply migrations based on version
        if (version == 0)
        {
            BackupConfig();
            json = MigrateV0ToV1(json, root);

            // Save migrated config immediately
            var migratedConfig = JsonSerializer.Deserialize<LfmConfig>(json, GetJsonOptions());
            if (migratedConfig != null)
            {
                await SaveAsync(migratedConfig);
                _logger.LogInformation("Migrated config saved successfully");
                return migratedConfig;
            }
        }

        // Step 4: Deserialize migrated config
        var finalConfig = JsonSerializer.Deserialize<LfmConfig>(json, GetJsonOptions());
        if (finalConfig == null)
        {
            throw new InvalidOperationException("Config deserialization returned null after migration");
        }

        return finalConfig;
    }

    public async Task<LfmConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Config file not found, creating default config at {Path}", _configPath);
                var defaultConfig = new LfmConfig();
                await SaveAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configPath);

            // Try current schema first
            try
            {
                var config = JsonSerializer.Deserialize<LfmConfig>(json, GetJsonOptions());
                if (config != null)
                {
                    return config;
                }
            }
            catch (JsonException)
            {
                // Deserialization failed - attempt migration
                _logger.LogInformation("Config deserialization failed, attempting migration");
                return await LoadWithMigrationAsync(json);
            }

            // If we get here, config was null but no exception was thrown
            _logger.LogWarning("Config deserialization returned null, attempting migration");
            return await LoadWithMigrationAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {Path}", _configPath);
            throw new InvalidOperationException($"Failed to load config from {_configPath}. Check logs for details.", ex);
        }
    }

    public async Task<Result<LfmConfig>> LoadWithValidationAsync()
    {
        try
        {
            // Load configuration
            var config = await LoadAsync();

            // Validate if validator is available
            if (_validator != null)
            {
                var validationResult = _validator.Validate(config);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }
            }
            else
            {
                _logger.LogDebug("Configuration validator not available - skipping validation");
            }

            return Result<LfmConfig>.Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading and validating configuration");
            return Result<LfmConfig>.Fail(ErrorType.ConfigurationError,
                "Failed to load and validate configuration",
                ex.Message);
        }
    }

    public async Task SaveAsync(LfmConfig config)
    {
        const int maxRetries = 3;
        var delays = new[] { 50, 100, 200 }; // Exponential backoff in milliseconds

        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(config, GetJsonOptions());

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    await File.WriteAllTextAsync(_configPath, json);
                    _logger.LogDebug("Configuration saved to {Path}", _configPath);
                    return; // Success
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    // File is locked, retry with exponential backoff
                    _logger.LogWarning("Config file locked, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                        delays[attempt], attempt + 1, maxRetries);
                    await Task.Delay(delays[attempt]);
                }
            }

            // If we get here, all retries failed - make one final attempt and let it throw
            await File.WriteAllTextAsync(_configPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to {Path} after {Retries} attempts", _configPath, maxRetries);
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }
}