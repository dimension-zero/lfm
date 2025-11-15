using Lfm.Framework.Abstractions;
using Lfm.Framework.Models;
using Lfm.Core.Music.Models;
using Lfm.Core.Services;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Configuration;

namespace Lfm.Core.Music.Algorithms;

/// <summary>
/// Music domain implementation of IRecommendationAlgorithm<MusicItem>
/// Adapter that wraps the existing RecommendationEngine to work with the framework
/// </summary>
public class MusicRecommendationAlgorithm : IRecommendationAlgorithm<MusicItem>
{
    private readonly IRecommendationEngine _recommendationEngine;

    public MusicRecommendationAlgorithm(IRecommendationEngine recommendationEngine)
    {
        _recommendationEngine = recommendationEngine ?? throw new ArgumentNullException(nameof(recommendationEngine));
    }

    /// <summary>
    /// Generate recommendations based on user's history in a time period
    /// </summary>
    public async Task<Result<List<RecommendationResult<MusicItem>>>> GenerateRecommendationsAsync(
        string userId,
        string period = "overall",
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0)
    {
        try
        {
            // Parse period string to LastFmPeriod enum
            var lastFmPeriod = period.ToLower() switch
            {
                "7days" or "7day" or "1week" or "week" => LastFmPeriod.SevenDay,
                "30days" or "30day" or "1month" or "month" => LastFmPeriod.OneMonth,
                "3months" or "3month" => LastFmPeriod.ThreeMonth,
                "6months" or "6month" => LastFmPeriod.SixMonth,
                "12months" or "12month" or "1year" or "year" => LastFmPeriod.TwelveMonth,
                "overall" or "alltime" or "all" => LastFmPeriod.Overall,
                _ => LastFmPeriod.Overall
            };

            // Get recommendations from the existing engine
            var recommendations = await _recommendationEngine.GetMusicRecommendationsWithResultAsync(
                userId,
                analysisLimit: analysisLimit,
                recommendationLimit: recommendationLimit,
                filterThreshold: filterThreshold,
                period: lastFmPeriod);

            if (!recommendations.IsSuccess)
                return Result<List<RecommendationResult<MusicItem>>>.Fail(recommendations.Error);

            // Convert recommendations to generic MusicItem format
            var musicRecommendations = recommendations.Value
                .Select(r => new RecommendationResult<MusicItem>
                {
                    Item = new MusicItem
                    {
                        Type = "artist",
                        Id = r.ArtistName,
                        Name = r.ArtistName,
                        ImageUrl = null,
                        ArtistName = null,
                        PlayCount = 0,
                        Url = null,
                        AlbumName = null,
                        Ranking = null
                    },
                    Score = r.Score,
                    AverageRelevance = r.AverageSimilarity,
                    OccurrenceCount = r.OccurrenceCount,
                    UserInteractionCount = r.UserPlayCount,
                    SourceItems = r.SourceArtists ?? new List<string>(),
                    Metadata = null
                })
                .ToList();

            return Result<List<RecommendationResult<MusicItem>>>.Ok(musicRecommendations);
        }
        catch (Exception ex)
        {
            return Result<List<RecommendationResult<MusicItem>>>.Fail(
                ErrorType.UnknownError,
                $"Failed to generate recommendations for period '{period}'",
                ex.Message);
        }
    }

    /// <summary>
    /// Generate recommendations based on user's history in a date range
    /// </summary>
    public async Task<Result<List<RecommendationResult<MusicItem>>>> GenerateRecommendationsForDateRangeAsync(
        string userId,
        DateTime from,
        DateTime to,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0)
    {
        try
        {
            // Get recommendations from the existing engine for date range
            var recommendations = await _recommendationEngine.GetMusicRecommendationsForDateRangeAsync(
                userId,
                from,
                to,
                analysisLimit: analysisLimit,
                recommendationLimit: recommendationLimit,
                filterThreshold: filterThreshold);

            // Convert recommendations to generic MusicItem format
            var musicRecommendations = recommendations
                .Select(r => new RecommendationResult<MusicItem>
                {
                    Item = new MusicItem
                    {
                        Type = "artist",
                        Id = r.ArtistName,
                        Name = r.ArtistName,
                        ImageUrl = null,
                        ArtistName = null,
                        PlayCount = 0,
                        Url = null,
                        AlbumName = null,
                        Ranking = null
                    },
                    Score = r.Score,
                    AverageRelevance = r.AverageSimilarity,
                    OccurrenceCount = r.OccurrenceCount,
                    UserInteractionCount = r.UserPlayCount,
                    SourceItems = r.SourceArtists ?? new List<string>(),
                    Metadata = null
                })
                .ToList();

            return Result<List<RecommendationResult<MusicItem>>>.Ok(musicRecommendations);
        }
        catch (Exception ex)
        {
            return Result<List<RecommendationResult<MusicItem>>>.Fail(
                ErrorType.UnknownError,
                $"Failed to generate recommendations for date range {from:O} to {to:O}",
                ex.Message);
        }
    }
}
