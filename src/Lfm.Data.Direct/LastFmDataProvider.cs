using Lfm.Shared.Interfaces;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Lfm.Data.Direct;

/// <summary>
/// Adapter for Last.fm API as a data provider.
/// Wraps ILastFmApiClient (CachedLastFmApiClient) to implement IMusicDataProvider interface.
/// Maintains full compatibility with existing Last.fm API functionality.
/// </summary>
public class LastFmDataProvider : IMusicDataProvider
{
    private readonly ILastFmApiClient _apiClient;
    private readonly ILogger<LastFmDataProvider> _logger;

    public LastFmDataProvider(ILastFmApiClient apiClient, ILogger<LastFmDataProvider> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProviderName => "Last.fm API";
    public bool SupportsDateRanges => true;
    public bool SupportsSimilarArtists => true;
    public bool SupportsLookup => true;

    /// <summary>
    /// Exposes the underlying API client for advanced scenarios (caching configuration, timing)
    /// </summary>
    public ILastFmApiClient ApiClient => _apiClient;

    // Core query methods - delegate directly to API client

    public Task<Result<TopArtists>> GetTopArtistsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1)
    {
        _logger.LogDebug("GetTopArtistsAsync via Last.fm API: user={Username}, period={Period}, limit={Limit}, page={Page}",
            username, period, limit, page);
        return _apiClient.GetTopArtistsWithResultAsync(username, period, limit, page);
    }

    public Task<Result<TopTracks>> GetTopTracksAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1)
    {
        _logger.LogDebug("GetTopTracksAsync via Last.fm API: user={Username}, period={Period}, limit={Limit}, page={Page}",
            username, period, limit, page);
        return _apiClient.GetTopTracksWithResultAsync(username, period, limit, page);
    }

    public Task<Result<TopAlbums>> GetTopAlbumsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1)
    {
        _logger.LogDebug("GetTopAlbumsAsync via Last.fm API: user={Username}, period={Period}, limit={Limit}, page={Page}",
            username, period, limit, page);
        return _apiClient.GetTopAlbumsWithResultAsync(username, period, limit, page);
    }

    // Date range methods

    public Task<Result<TopArtists>> GetTopArtistsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        _logger.LogDebug("GetTopArtistsForDateRangeAsync via Last.fm API: user={Username}, from={From}, to={To}, limit={Limit}",
            username, from, to, limit);
        return _apiClient.GetTopArtistsForDateRangeWithResultAsync(username, from, to, limit);
    }

    public Task<Result<TopTracks>> GetTopTracksForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        _logger.LogDebug("GetTopTracksForDateRangeAsync via Last.fm API: user={Username}, from={From}, to={To}, limit={Limit}",
            username, from, to, limit);
        return _apiClient.GetTopTracksForDateRangeWithResultAsync(username, from, to, limit);
    }

    public Task<Result<TopAlbums>> GetTopAlbumsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        _logger.LogDebug("GetTopAlbumsForDateRangeAsync via Last.fm API: user={Username}, from={From}, to={To}, limit={Limit}",
            username, from, to, limit);
        return _apiClient.GetTopAlbumsForDateRangeWithResultAsync(username, from, to, limit);
    }

    public Task<Result<RecentTracks>> GetRecentTracksAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1)
    {
        _logger.LogDebug("GetRecentTracksAsync via Last.fm API: user={Username}, from={From}, to={To}, limit={Limit}, page={Page}",
            username, from, to, limit, page);
        return _apiClient.GetRecentTracksWithResultAsync(username, from, to, limit, page);
    }

    // Artist-specific methods

    public Task<Result<TopTracks>> GetArtistTopTracksAsync(string artist, int limit = 10)
    {
        _logger.LogDebug("GetArtistTopTracksAsync via Last.fm API: artist={Artist}, limit={Limit}", artist, limit);
        return _apiClient.GetArtistTopTracksWithResultAsync(artist, limit);
    }

    public Task<Result<TopAlbums>> GetArtistTopAlbumsAsync(string artist, int limit = 10)
    {
        _logger.LogDebug("GetArtistTopAlbumsAsync via Last.fm API: artist={Artist}, limit={Limit}", artist, limit);
        return _apiClient.GetArtistTopAlbumsWithResultAsync(artist, limit);
    }

    public Task<Result<SimilarArtists>> GetSimilarArtistsAsync(string artist, int limit = 50)
    {
        _logger.LogDebug("GetSimilarArtistsAsync via Last.fm API: artist={Artist}, limit={Limit}", artist, limit);
        return _apiClient.GetSimilarArtistsWithResultAsync(artist, limit);
    }

    public Task<Result<TopTags>> GetArtistTopTagsAsync(string artist, bool autocorrect = true)
    {
        _logger.LogDebug("GetArtistTopTagsAsync via Last.fm API: artist={Artist}, autocorrect={Autocorrect}", artist, autocorrect);
        return _apiClient.GetArtistTopTagsWithResultAsync(artist, autocorrect);
    }

    // Lookup methods

    public Task<Result<ArtistLookupInfo>> GetArtistInfoAsync(string artist, string username)
    {
        _logger.LogDebug("GetArtistInfoAsync via Last.fm API: artist={Artist}, user={Username}", artist, username);
        return _apiClient.GetArtistInfoWithResultAsync(artist, username);
    }

    public Task<Result<TrackLookupInfo>> GetTrackInfoAsync(string artist, string track, string username)
    {
        _logger.LogDebug("GetTrackInfoAsync via Last.fm API: artist={Artist}, track={Track}, user={Username}", artist, track, username);
        return _apiClient.GetTrackInfoWithResultAsync(artist, track, username);
    }

    public Task<Result<AlbumLookupInfo>> GetAlbumInfoAsync(string artist, string album, string username)
    {
        _logger.LogDebug("GetAlbumInfoAsync via Last.fm API: artist={Artist}, album={Album}, user={Username}", artist, album, username);
        return _apiClient.GetAlbumInfoWithResultAsync(artist, album, username);
    }
}
