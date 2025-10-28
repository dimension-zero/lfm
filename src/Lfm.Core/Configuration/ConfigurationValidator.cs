using Lfm.Shared.Models.Results;
using Microsoft.Extensions.Logging;

namespace Lfm.Core.Configuration;

/// <summary>
/// Validates LfmConfig to ensure all settings are valid before use.
/// Uses Result&lt;T&gt; pattern for consistent error handling.
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result<LfmConfig> Validate(LfmConfig config)
    {
        if (config == null)
        {
            return Result<LfmConfig>.Fail(ErrorType.ConfigurationError,
                "Configuration is null",
                "Config object cannot be null");
        }

        var errors = new List<string>();

        // API Configuration
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            errors.Add("ApiKey is required - get one from https://www.last.fm/api/account/create");
        }

        if (config.ApiThrottleMs < 0)
        {
            errors.Add($"ApiThrottleMs must be non-negative (got: {config.ApiThrottleMs})");
        }

        if (config.ApiThrottleMs > 10000)
        {
            _logger.LogWarning("ApiThrottleMs is very high ({ThrottleMs}ms) - may cause slow operations", config.ApiThrottleMs);
        }

        if (config.ParallelApiCalls < 1)
        {
            errors.Add($"ParallelApiCalls must be at least 1 (got: {config.ParallelApiCalls})");
        }

        if (config.ParallelApiCalls > 10)
        {
            _logger.LogWarning("ParallelApiCalls is very high ({Parallel}) - may hit rate limits", config.ParallelApiCalls);
        }

        // Search Configuration
        if (config.NormalSearchDepth < 1)
        {
            errors.Add($"NormalSearchDepth must be at least 1 (got: {config.NormalSearchDepth})");
        }

        if (config.DeepSearchTimeoutSeconds < 1)
        {
            errors.Add($"DeepSearchTimeoutSeconds must be at least 1 (got: {config.DeepSearchTimeoutSeconds})");
        }

        // Circuit Breaker Configuration
        if (config.CircuitBreakerEnabled)
        {
            if (config.CircuitBreakerFailureThreshold < 1)
            {
                errors.Add($"CircuitBreakerFailureThreshold must be at least 1 (got: {config.CircuitBreakerFailureThreshold})");
            }

            if (config.CircuitBreakerSuccessThreshold < 1)
            {
                errors.Add($"CircuitBreakerSuccessThreshold must be at least 1 (got: {config.CircuitBreakerSuccessThreshold})");
            }

            if (config.CircuitBreakerOpenTimeoutSeconds < 1)
            {
                errors.Add($"CircuitBreakerOpenTimeoutSeconds must be at least 1 (got: {config.CircuitBreakerOpenTimeoutSeconds})");
            }
        }

        // Cache Configuration
        if (config.CacheEnabled)
        {
            if (config.CacheExpiryMinutes < 0)
            {
                errors.Add($"CacheExpiryMinutes must be non-negative (got: {config.CacheExpiryMinutes})");
            }

            if (config.MaxCacheSizeMB < 0)
            {
                errors.Add($"MaxCacheSizeMB must be non-negative (got: {config.MaxCacheSizeMB})");
            }

            if (config.MaxCacheFiles < 0)
            {
                errors.Add($"MaxCacheFiles must be non-negative (got: {config.MaxCacheFiles})");
            }

            if (config.MaxCacheAgeDays < 0)
            {
                errors.Add($"MaxCacheAgeDays must be non-negative (got: {config.MaxCacheAgeDays})");
            }

            if (config.CleanupIntervalHours < 1)
            {
                errors.Add($"CleanupIntervalHours must be at least 1 (got: {config.CleanupIntervalHours})");
            }
        }

        // Display Configuration
        if (config.DefaultLimit < 1)
        {
            errors.Add($"DefaultLimit must be at least 1 (got: {config.DefaultLimit})");
        }

        if (config.DefaultLimit > 10000)
        {
            _logger.LogWarning("DefaultLimit is very high ({Limit}) - may cause slow operations", config.DefaultLimit);
        }

        // Mixtape Configuration
        if (config.DefaultMixtapeBias < 0 || config.DefaultMixtapeBias > 1)
        {
            errors.Add($"DefaultMixtapeBias must be between 0 and 1 (got: {config.DefaultMixtapeBias})");
        }

        if (config.DefaultMinPlays < 0)
        {
            errors.Add($"DefaultMinPlays must be non-negative (got: {config.DefaultMinPlays})");
        }

        if (config.MaxMixtapeSampleSize < 1)
        {
            errors.Add($"MaxMixtapeSampleSize must be at least 1 (got: {config.MaxMixtapeSampleSize})");
        }

        if (config.DateRangeDiversityMultiplier < 1)
        {
            errors.Add($"DateRangeDiversityMultiplier must be at least 1 (got: {config.DateRangeDiversityMultiplier})");
        }

        if (config.MaxEmptyWindows < 1)
        {
            errors.Add($"MaxEmptyWindows must be at least 1 (got: {config.MaxEmptyWindows})");
        }

        // Tag Filtering Configuration
        if (config.EnableTagFiltering)
        {
            if (config.TagFilterThreshold < 0)
            {
                errors.Add($"TagFilterThreshold must be non-negative (got: {config.TagFilterThreshold})");
            }

            if (config.MaxTagLookups < 0)
            {
                errors.Add($"MaxTagLookups must be non-negative (got: {config.MaxTagLookups})");
            }

            if (config.ExcludedTags == null)
            {
                errors.Add("ExcludedTags cannot be null when EnableTagFiltering is true");
            }
        }

        // Spotify Configuration
        if (!string.IsNullOrWhiteSpace(config.Spotify.ClientId) || !string.IsNullOrWhiteSpace(config.Spotify.ClientSecret))
        {
            // If either is set, both must be set
            if (string.IsNullOrWhiteSpace(config.Spotify.ClientId))
            {
                errors.Add("Spotify.ClientId is required when Spotify.ClientSecret is set");
            }

            if (string.IsNullOrWhiteSpace(config.Spotify.ClientSecret))
            {
                errors.Add("Spotify.ClientSecret is required when Spotify.ClientId is set");
            }

            if (config.Spotify.RateLimitDelayMs < 0)
            {
                errors.Add($"Spotify.RateLimitDelayMs must be non-negative (got: {config.Spotify.RateLimitDelayMs})");
            }

            if (config.Spotify.SearchTimeoutMs < 100)
            {
                errors.Add($"Spotify.SearchTimeoutMs must be at least 100ms (got: {config.Spotify.SearchTimeoutMs})");
            }

            if (config.Spotify.MaxRetries < 0)
            {
                errors.Add($"Spotify.MaxRetries must be non-negative (got: {config.Spotify.MaxRetries})");
            }
        }

        // Sonos Configuration
        if (!string.IsNullOrWhiteSpace(config.Sonos.HttpApiBaseUrl))
        {
            if (!Uri.TryCreate(config.Sonos.HttpApiBaseUrl, UriKind.Absolute, out var sonosUri))
            {
                errors.Add($"Sonos.HttpApiBaseUrl is not a valid URL (got: {config.Sonos.HttpApiBaseUrl})");
            }
            else if (sonosUri.Scheme != "http" && sonosUri.Scheme != "https")
            {
                errors.Add($"Sonos.HttpApiBaseUrl must use http or https scheme (got: {sonosUri.Scheme})");
            }

            if (config.Sonos.TimeoutMs < 100)
            {
                errors.Add($"Sonos.TimeoutMs must be at least 100ms (got: {config.Sonos.TimeoutMs})");
            }

            if (config.Sonos.RoomCacheDurationMinutes < 1)
            {
                errors.Add($"Sonos.RoomCacheDurationMinutes must be at least 1 (got: {config.Sonos.RoomCacheDurationMinutes})");
            }
        }

        // Album Enrichment Configuration
        if (config.AlbumEnrichment.Enabled)
        {
            if (config.AlbumEnrichment.TimeoutMs < 100)
            {
                errors.Add($"AlbumEnrichment.TimeoutMs must be at least 100ms (got: {config.AlbumEnrichment.TimeoutMs})");
            }

            if (config.AlbumEnrichment.EnricherPriority == null || config.AlbumEnrichment.EnricherPriority.Count == 0)
            {
                errors.Add("AlbumEnrichment.EnricherPriority cannot be empty when enrichment is enabled");
            }
        }

        // Local Files Configuration
        if (config.DataSource == DataSourceMode.LocalFiles || config.DataSource == DataSourceMode.Merged)
        {
            if (config.LocalFilePaths == null || config.LocalFilePaths.Count == 0)
            {
                errors.Add($"LocalFilePaths cannot be empty when DataSource is {config.DataSource}");
            }
            else
            {
                foreach (var path in config.LocalFilePaths)
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        errors.Add("LocalFilePaths contains empty or whitespace-only path");
                    }
                    else if (!File.Exists(path) && !Directory.Exists(path))
                    {
                        _logger.LogWarning("LocalFilePath does not exist: {Path}", path);
                    }
                }
            }
        }

        // Return result
        if (errors.Count > 0)
        {
            var errorMessage = $"Configuration validation failed with {errors.Count} error(s)";
            var details = string.Join("; ", errors);
            _logger.LogError("Configuration validation failed: {Details}", details);

            return Result<LfmConfig>.Fail(ErrorType.ConfigurationError, errorMessage, details);
        }

        _logger.LogDebug("Configuration validation passed");
        return Result<LfmConfig>.Ok(config);
    }
}
