using Lfm.Shared.Models.Results;

namespace Lfm.Framework.Abstractions;

/// <summary>
/// Provides access to item data (fetching details, searching, etc.)
/// Implementations query APIs for item metadata
/// </summary>
/// <typeparam name="TItem">The domain item type</typeparam>
public interface IItemProvider<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// Get detailed information about a specific item
    /// </summary>
    /// <param name="itemId">The unique identifier of the item</param>
    /// <returns>Item with full details populated</returns>
    Task<Result<TItem>> GetItemDetailsAsync(string itemId);

    /// <summary>
    /// Search for items by query string
    /// </summary>
    /// <param name="query">Search query (artist name, book title, wine name, etc.)</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching items</returns>
    Task<Result<List<TItem>>> SearchItemsAsync(string query, int limit = 50);

    /// <summary>
    /// Get similar or related items based on a source item
    /// Domain-specific: recommendations from algorithms, related books from genre, etc.
    /// </summary>
    /// <param name="sourceItemId">The item to find similarities for</param>
    /// <param name="limit">Maximum number of similar items</param>
    /// <returns>List of similar items ranked by relevance</returns>
    Task<Result<List<TItem>>> GetSimilarItemsAsync(string sourceItemId, int limit = 50);

    /// <summary>
    /// Check if an item exists in the data source
    /// </summary>
    /// <param name="itemId">The item identifier</param>
    /// <returns>True if item exists, false otherwise</returns>
    Task<Result<bool>> ItemExistsAsync(string itemId);
}
