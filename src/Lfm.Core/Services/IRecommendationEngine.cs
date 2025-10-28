using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Core.Services;

/// <summary>
/// Service for generating music recommendations based on user listening history
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Generate music recommendations for a user based on their top artists in a period
    /// </summary>
    Task<List<RecommendationResult>> GetMusicRecommendationsAsync(
        string username,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0,
        int tracksPerArtist = 0,
        LastFmPeriod period = LastFmPeriod.Overall,
        bool excludeTags = false);

    /// <summary>
    /// Generate music recommendations for a user based on their top artists in a date range
    /// </summary>
    Task<List<RecommendationResult>> GetMusicRecommendationsForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0,
        int tracksPerArtist = 0,
        bool excludeTags = false);

    /// <summary>
    /// Generate music recommendations with Result&lt;T&gt; error handling
    /// </summary>
    Task<Result<List<RecommendationResult>>> GetMusicRecommendationsWithResultAsync(
        string username,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0,
        int tracksPerArtist = 0,
        LastFmPeriod period = LastFmPeriod.Overall,
        bool excludeTags = false);

    /// <summary>
    /// Get user's artist play counts (helper for filtering recommendations)
    /// </summary>
    Task<Dictionary<string, int>> GetUserArtistPlayCountsAsync(string username, int maxArtists = int.MaxValue);
}
