using Lfm.Shared.Models.Results;
using Lfm.Framework.Models;

namespace Lfm.Framework.Abstractions;

/// <summary>
/// Core recommendation algorithm for a domain
/// Generates personalized recommendations based on user's history
/// </summary>
/// <typeparam name="TItem">The domain item type</typeparam>
public interface IRecommendationAlgorithm<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// Generate recommendations based on user's history in a time period
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="period">Time period for analysis (e.g., "overall", "1month")</param>
    /// <param name="analysisLimit">How many of user's top items to analyze for recommendations</param>
    /// <param name="recommendationLimit">How many recommendations to return</param>
    /// <param name="filterThreshold">Minimum interaction count for filtering results</param>
    /// <returns>List of recommendations ranked by score</returns>
    Task<Result<List<RecommendationResult<TItem>>>> GenerateRecommendationsAsync(
        string userId,
        string period = "overall",
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0);

    /// <summary>
    /// Generate recommendations based on user's history in a date range
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="from">Start date (inclusive)</param>
    /// <param name="to">End date (inclusive)</param>
    /// <param name="analysisLimit">How many of user's top items to analyze</param>
    /// <param name="recommendationLimit">How many recommendations to return</param>
    /// <param name="filterThreshold">Minimum interaction count for filtering</param>
    /// <returns>List of recommendations for the date range</returns>
    Task<Result<List<RecommendationResult<TItem>>>> GenerateRecommendationsForDateRangeAsync(
        string userId,
        DateTime from,
        DateTime to,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0);
}
