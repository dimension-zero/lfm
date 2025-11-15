using Lfm.Shared.Models.Results;

namespace Lfm.Framework.Abstractions;

/// <summary>
/// Provides access to user's history/interaction data for a domain
/// Implementations retrieve user's listening history, tasting history, reading history, etc.
/// </summary>
/// <typeparam name="TItem">The domain item type (Track, Wine, Book, etc.)</typeparam>
public interface IUserHistoryProvider<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// Get a user's complete history for items (e.g., all tracks listened to)
    /// </summary>
    /// <param name="userId">The user identifier (Last.fm username, Goodreads ID, etc.)</param>
    /// <param name="limit">Maximum number of items to retrieve</param>
    /// <returns>List of items the user has interacted with</returns>
    Task<Result<List<TItem>>> GetUserHistoryAsync(string userId, int limit = int.MaxValue);

    /// <summary>
    /// Get user's top items in a specific period
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="period">Time period for aggregation (e.g., "overall", "1month", "3months")</param>
    /// <param name="count">Number of top items to retrieve</param>
    /// <returns>List of top items ranked by interaction count</returns>
    Task<Result<List<TItem>>> GetTopItemsAsync(string userId, string period = "overall", int count = 50);

    /// <summary>
    /// Get user's items in a specific date range
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="from">Start date (inclusive)</param>
    /// <param name="to">End date (inclusive)</param>
    /// <param name="count">Number of items to retrieve</param>
    /// <returns>List of items from the specified date range</returns>
    Task<Result<List<TItem>>> GetUserHistoryForDateRangeAsync(
        string userId,
        DateTime from,
        DateTime to,
        int count = 50);

    /// <summary>
    /// Get user's top items ranked by interaction count (play count, read count, etc.)
    /// Helper method used for filtering and analysis
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="maxItems">Maximum number of items to analyze</param>
    /// <returns>Dictionary mapping item name to interaction count</returns>
    Task<Result<Dictionary<string, int>>> GetUserInteractionCountsAsync(
        string userId,
        int maxItems = int.MaxValue);
}
