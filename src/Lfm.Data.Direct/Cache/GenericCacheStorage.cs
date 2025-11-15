using System.Text.Json;
using Lfm.Framework.Abstractions;
using Lfm.Shared.Models.Results;
using Microsoft.Extensions.Logging;

namespace Lfm.Data.Direct.Cache;

/// <summary>
/// Generic cache implementation that works with any domain type
/// Handles JSON serialization/deserialization internally
/// Wraps ICacheStorage for consistent storage behavior
/// </summary>
/// <typeparam name="TItem">Type of item being cached</typeparam>
public class GenericCacheStorage<TItem> : ICache<string, TItem> where TItem : class
{
    private readonly ICacheStorage _storage;
    private readonly ILogger<GenericCacheStorage<TItem>> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public GenericCacheStorage(
        ICacheStorage storage,
        ILogger<GenericCacheStorage<TItem>> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Store an item in the cache with optional expiry
    /// </summary>
    public async Task<Result> StoreAsync(string key, TItem item, int expiryMinutes = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result.Fail(
                    ErrorType.ValidationError,
                    "Cache key cannot be null or empty");

            if (item == null)
                return Result.Fail(
                    ErrorType.ValidationError,
                    "Item to cache cannot be null");

            // Serialize item to JSON
            var json = JsonSerializer.Serialize(item, _jsonOptions);

            // Store in underlying storage
            var stored = await _storage.StoreAsync(key, json, expiryMinutes);

            if (!stored)
                return Result.Fail(
                    ErrorType.UnknownError,
                    $"Failed to store item with key '{key}' in cache");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing item with key '{Key}' in cache", key);
            return Result.Fail(
                ErrorType.UnknownError,
                $"Failed to store cache item: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieve an item from the cache
    /// </summary>
    public async Task<Result<TItem>> RetrieveAsync(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result<TItem>.Fail(
                    ErrorType.ValidationError,
                    "Cache key cannot be null or empty");

            // Retrieve JSON from storage
            var json = await _storage.RetrieveAsync(key);

            if (string.IsNullOrEmpty(json))
                return Result<TItem>.Fail(
                    ErrorType.DataError,
                    $"Cache entry not found or expired for key '{key}'");

            // Deserialize JSON to item
            var item = JsonSerializer.Deserialize<TItem>(json, _jsonOptions);

            if (item == null)
                return Result<TItem>.Fail(
                    ErrorType.DataError,
                    $"Failed to deserialize cache entry for key '{key}'");

            return Result<TItem>.Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item with key '{Key}' from cache", key);
            return Result<TItem>.Fail(
                ErrorType.UnknownError,
                $"Failed to retrieve cache item: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a cache entry exists and is valid
    /// </summary>
    public async Task<Result<bool>> ExistsAsync(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result<bool>.Fail(
                    ErrorType.ValidationError,
                    "Cache key cannot be null or empty");

            var exists = await _storage.ExistsAsync(key);
            return Result<bool>.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key '{Key}'", key);
            return Result<bool>.Fail(
                ErrorType.UnknownError,
                $"Failed to check cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove a cache entry
    /// </summary>
    public async Task<Result> RemoveAsync(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result.Fail(
                    ErrorType.ValidationError,
                    "Cache key cannot be null or empty");

            var removed = await _storage.RemoveAsync(key);

            if (!removed)
                return Result.Fail(
                    ErrorType.UnknownError,
                    $"Failed to remove cache entry for key '{key}'");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key '{Key}'", key);
            return Result.Fail(
                ErrorType.UnknownError,
                $"Failed to remove cache entry: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove all expired cache entries
    /// </summary>
    public async Task<Result<int>> CleanupExpiredAsync()
    {
        try
        {
            var removed = await _storage.CleanupExpiredAsync();
            return Result<int>.Ok(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired cache entries");
            return Result<int>.Fail(
                ErrorType.UnknownError,
                $"Failed to cleanup expired entries: {ex.Message}");
        }
    }

    /// <summary>
    /// Perform intelligent cache cleanup
    /// </summary>
    public async Task<Result<int>> CleanupAsync()
    {
        try
        {
            var removed = await _storage.CleanupAsync();
            return Result<int>.Ok(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing cache cleanup");
            return Result<int>.Fail(
                ErrorType.UnknownError,
                $"Failed to cleanup cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    public async Task<Result> ClearAllAsync()
    {
        try
        {
            var cleared = await _storage.ClearAllAsync();

            if (!cleared)
                return Result.Fail(
                    ErrorType.UnknownError,
                    "Failed to clear cache");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache entries");
            return Result.Fail(
                ErrorType.UnknownError,
                $"Failed to clear cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<Result<CacheInfo>> GetInfoAsync()
    {
        try
        {
            var stats = await _storage.GetStatisticsAsync();

            var info = new CacheInfo
            {
                TotalEntries = stats.TotalEntries,
                TotalSizeBytes = stats.TotalSizeBytes,
                ExpiredEntries = stats.ExpiredEntries,
                OldestEntry = stats.OldestEntry,
                NewestEntry = stats.NewestEntry,
                Location = stats.CacheDirectory
            };

            return Result<CacheInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return Result<CacheInfo>.Fail(
                ErrorType.UnknownError,
                $"Failed to get cache info: {ex.Message}");
        }
    }
}
