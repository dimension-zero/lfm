using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using Lfm.Core.Attributes;
using Lfm.Data.Direct.Cache;

namespace Lfm.Tests.Mocks;

/// <summary>
/// In-memory implementation of ICacheStorage for testing.
/// Faster than file I/O and allows verification of cache operations.
/// </summary>
public class InMemoryCacheStorage : ICacheStorage
{
    private readonly Dictionary<string, (string data, DateTime expiry)> _cache = new();

    /// <summary>
    /// Number of times StoreAsync was called
    /// </summary>
    public int StoreCount { get; private set; }

    /// <summary>
    /// Number of times RetrieveAsync was called
    /// </summary>
    public int RetrieveCount { get; private set; }

    /// <summary>
    /// Number of times RemoveAsync was called
    /// </summary>
    public int RemoveCount { get; private set; }

    /// <summary>
    /// Number of times ClearAllAsync was called
    /// </summary>
    public int ClearAllCount { get; private set; }

    /// <summary>
    /// Direct access to cache contents for test verification
    /// </summary>
    public IReadOnlyDictionary<string, (string data, DateTime expiry)> Cache => _cache;

    public Task<bool> StoreAsync(string key, string jsonData, int expiryMinutes = 10)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (string.IsNullOrWhiteSpace(jsonData))
            throw new ArgumentException("Data cannot be null or empty", nameof(jsonData));

        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        _cache[key] = (jsonData, expiry);
        StoreCount++;

        return Task.FromResult(true);
    }

    [SuppressMessage("SilentFailure", "SF001", Justification = "Null return indicates cache miss, not error. Test mock matches FileCacheStorage behavior.")]
    public Task<string?> RetrieveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        RetrieveCount++;

        if (!_cache.TryGetValue(key, out var cached))
            return Task.FromResult<string?>(null);

        // Check expiration
        if (DateTime.UtcNow > cached.expiry)
        {
            _cache.Remove(key);
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(cached.data);
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (!_cache.TryGetValue(key, out var cached))
            return Task.FromResult(false);

        // Check expiration
        if (DateTime.UtcNow > cached.expiry)
        {
            _cache.Remove(key);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<bool> RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        _cache.Remove(key);
        RemoveCount++;

        return Task.FromResult(true);
    }

    public Task<int> CleanupExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache.Where(kv => now > kv.Value.expiry).Select(kv => kv.Key).ToList();

        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }

        return Task.FromResult(expiredKeys.Count);
    }

    public Task<int> CleanupAsync()
    {
        // For in-memory cache, just cleanup expired entries
        return CleanupExpiredAsync();
    }

    public Task<bool> ClearAllAsync()
    {
        _cache.Clear();
        ClearAllCount++;

        return Task.FromResult(true);
    }

    public Task<CacheStatistics> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var stats = new CacheStatistics
        {
            TotalEntries = _cache.Count,
            TotalFiles = _cache.Count * 2, // Simulate .json + .meta
            ExpiredEntries = _cache.Count(kv => now > kv.Value.expiry),
            TotalSizeBytes = _cache.Values.Sum(v => v.data.Length),
            CacheDirectory = "(in-memory)",
            OldestEntry = _cache.Count > 0 ? _cache.Values.Min(v => v.expiry) : null,
            NewestEntry = _cache.Count > 0 ? _cache.Values.Max(v => v.expiry) : null
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Reset all operation counters (for test isolation)
    /// </summary>
    public void ResetCounters()
    {
        StoreCount = 0;
        RetrieveCount = 0;
        RemoveCount = 0;
        ClearAllCount = 0;
    }

    /// <summary>
    /// Manually expire a cache entry (for testing expiration logic)
    /// </summary>
    public void ExpireKey(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            _cache[key] = (cached.data, DateTime.UtcNow.AddSeconds(-1));
        }
    }
}
