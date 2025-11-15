using Lfm.Shared.Models.Results;

namespace Lfm.Framework.Abstractions;

/// <summary>
/// Generic cache interface for storing and retrieving domain items
/// Implementations handle serialization/deserialization internally
/// </summary>
/// <typeparam name="TKey">Type of cache key (typically string for domain items)</typeparam>
/// <typeparam name="TItem">Type of item being cached</typeparam>
public interface ICache<TKey, TItem> where TItem : class
{
    /// <summary>
    /// Store an item in the cache with optional expiry
    /// </summary>
    /// <param name="key">Unique cache key</param>
    /// <param name="item">Item to cache</param>
    /// <param name="expiryMinutes">Minutes until cache expires (0 for never expire)</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> StoreAsync(TKey key, TItem item, int expiryMinutes = 10);

    /// <summary>
    /// Retrieve an item from the cache if it exists and hasn't expired
    /// </summary>
    /// <param name="key">Cache key to retrieve</param>
    /// <returns>Item if found and valid, or Result error if not found/expired</returns>
    Task<Result<TItem>> RetrieveAsync(TKey key);

    /// <summary>
    /// Check if an item exists in cache and is still valid
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <returns>True if valid cache entry exists, false otherwise</returns>
    Task<Result<bool>> ExistsAsync(TKey key);

    /// <summary>
    /// Remove a specific item from cache
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <returns>Success result</returns>
    Task<Result> RemoveAsync(TKey key);

    /// <summary>
    /// Remove all expired cache entries
    /// </summary>
    /// <returns>Number of entries removed</returns>
    Task<Result<int>> CleanupExpiredAsync();

    /// <summary>
    /// Perform intelligent cleanup (expired entries + LRU if needed)
    /// </summary>
    /// <returns>Number of entries removed</returns>
    Task<Result<int>> CleanupAsync();

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    /// <returns>Success result</returns>
    Task<Result> ClearAllAsync();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <returns>Statistics about cache state</returns>
    Task<Result<CacheInfo>> GetInfoAsync();
}

/// <summary>
/// Generic cache statistics information
/// </summary>
public class CacheInfo
{
    /// <summary>
    /// Total number of cache entries
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Total size in bytes
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Number of expired entries
    /// </summary>
    public int ExpiredEntries { get; set; }

    /// <summary>
    /// Oldest cache entry timestamp
    /// </summary>
    public DateTime? OldestEntry { get; set; }

    /// <summary>
    /// Newest cache entry timestamp
    /// </summary>
    public DateTime? NewestEntry { get; set; }

    /// <summary>
    /// Cache location/identifier (e.g., directory path)
    /// </summary>
    public string? Location { get; set; }
}
