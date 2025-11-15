using Lfm.Framework.Abstractions;
using Lfm.Core.Music.Models;
using Lfm.Shared.Interfaces;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Configuration;

namespace Lfm.Core.Music.Providers;

/// <summary>
/// Provides Last.fm user history data implementing IUserHistoryProvider<MusicItem>
/// Adapter that wraps existing LastFmApiClient to work with the framework
/// </summary>
public class LastFmHistoryProvider : IUserHistoryProvider<MusicItem>
{
    private readonly ILastFmApiClient _apiClient;

    public LastFmHistoryProvider(ILastFmApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// Get user's complete history (all items they've interacted with)
    /// Currently returns top artists as approximation of history
    /// </summary>
    public async Task<Result<List<MusicItem>>> GetUserHistoryAsync(string userId, int limit = int.MaxValue)
    {
        try
        {
            // Get top artists as primary history representation
            var artists = await _apiClient.GetTopArtistsWithResultAsync(userId, limit: Math.Min(limit, 500));

            if (!artists.IsSuccess)
                return Result<List<MusicItem>>.Fail(artists.Error);

            var musicItems = artists.Value.Artists
                .Select(a => MusicItemConverter.ToMusicItem(a))
                .ToList();

            return Result<List<MusicItem>>.Ok(musicItems);
        }
        catch (Exception ex)
        {
            return Result<List<MusicItem>>.Fail(
                ErrorType.UnknownError,
                "Failed to get user history",
                ex.Message);
        }
    }

    /// <summary>
    /// Get user's top items in a specific period
    /// </summary>
    public async Task<Result<List<MusicItem>>> GetTopItemsAsync(string userId, string period = "overall", int count = 50)
    {
        try
        {
            // Map period string to LastFmPeriod enum
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

            var artists = await _apiClient.GetTopArtistsWithResultAsync(userId, period: lastFmPeriod, limit: count);

            if (!artists.IsSuccess)
                return Result<List<MusicItem>>.Fail(artists.Error);

            var musicItems = artists.Value.Artists
                .Select((a, i) => MusicItemConverter.ToMusicItem(a, ranking: $"#{i + 1}"))
                .ToList();

            return Result<List<MusicItem>>.Ok(musicItems);
        }
        catch (Exception ex)
        {
            return Result<List<MusicItem>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get top items for period {period}",
                ex.Message);
        }
    }

    /// <summary>
    /// Get user's items in a specific date range
    /// Uses Last.fm API's date range parameters
    /// </summary>
    public async Task<Result<List<MusicItem>>> GetUserHistoryForDateRangeAsync(
        string userId,
        DateTime from,
        DateTime to,
        int count = 50)
    {
        try
        {
            var artists = await _apiClient.GetTopArtistsForDateRangeWithResultAsync(
                userId,
                from,
                to,
                limit: count);

            if (!artists.IsSuccess)
                return Result<List<MusicItem>>.Fail(artists.Error);

            var musicItems = artists.Value.Artists
                .Select((a, i) => MusicItemConverter.ToMusicItem(a, ranking: $"#{i + 1}"))
                .ToList();

            return Result<List<MusicItem>>.Ok(musicItems);
        }
        catch (Exception ex)
        {
            return Result<List<MusicItem>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get history for date range {from:O} to {to:O}",
                ex.Message);
        }
    }

    /// <summary>
    /// Get user's top items ranked by play count
    /// Helper method for filtering and analysis
    /// </summary>
    public async Task<Result<Dictionary<string, int>>> GetUserInteractionCountsAsync(
        string userId,
        int maxItems = int.MaxValue)
    {
        try
        {
            var artists = await _apiClient.GetTopArtistsWithResultAsync(userId, limit: Math.Min(maxItems, 500));

            if (!artists.IsSuccess)
                return Result<Dictionary<string, int>>.Fail(artists.Error);

            var counts = artists.Value.Artists
                .Where(a => int.TryParse(a.PlayCount, out _))
                .ToDictionary(
                    a => a.Name,
                    a => int.Parse(a.PlayCount));

            return Result<Dictionary<string, int>>.Ok(counts);
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, int>>.Fail(
                ErrorType.UnknownError,
                "Failed to get user interaction counts",
                ex.Message);
        }
    }
}
