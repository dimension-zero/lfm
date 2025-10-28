using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Services;

namespace Lfm.Core.Services;

/// <summary>
/// IMusicDataProvider implementation that wraps ILastFmApiClient.
/// Allows Last.fm API to be used as a data source for EF Core LINQ provider.
/// </summary>
public class LastFmApiProvider : IMusicDataProvider
{
    private readonly ILastFmApiClient _apiClient;

    public LastFmApiProvider(ILastFmApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    // Interface properties
    public string ProviderName => "Last.fm API";
    public bool SupportsDateRanges => true;
    public bool SupportsSimilarArtists => true;
    public bool SupportsLookup => true;

    // Core query methods - direct passthrough to API client
    public async Task<Result<TopArtists>> GetTopArtistsAsync(
        string username,
        LastFmPeriod period = LastFmPeriod.Overall,
        int limit = 10,
        int page = 1)
    {
        var response = await _apiClient.GetTopArtistsAsync(username, period, limit, page);
        return response != null
            ? Result<TopArtists>.Ok(response)
            : Result<TopArtists>.DataError("No data returned from Last.fm API");
    }

    public async Task<Result<TopTracks>> GetTopTracksAsync(
        string username,
        LastFmPeriod period = LastFmPeriod.Overall,
        int limit = 10,
        int page = 1)
    {
        var response = await _apiClient.GetTopTracksAsync(username, period, limit, page);
        return response != null
            ? Result<TopTracks>.Ok(response)
            : Result<TopTracks>.DataError("No data returned from Last.fm API");
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsAsync(
        string username,
        LastFmPeriod period = LastFmPeriod.Overall,
        int limit = 10,
        int page = 1)
    {
        var response = await _apiClient.GetTopAlbumsAsync(username, period, limit, page);
        return response != null
            ? Result<TopAlbums>.Ok(response)
            : Result<TopAlbums>.DataError("No data returned from Last.fm API");
    }

    // Date range methods - direct passthrough
    public async Task<Result<TopArtists>> GetTopArtistsForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 10)
    {
        var response = await _apiClient.GetTopArtistsForDateRangeAsync(username, from, to, limit);
        return response != null
            ? Result<TopArtists>.Ok(response)
            : Result<TopArtists>.DataError("No data returned from Last.fm API");
    }

    public async Task<Result<TopTracks>> GetTopTracksForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 10)
    {
        var response = await _apiClient.GetTopTracksForDateRangeAsync(username, from, to, limit);
        return response != null
            ? Result<TopTracks>.Ok(response)
            : Result<TopTracks>.DataError("No data returned from Last.fm API");
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 10)
    {
        var response = await _apiClient.GetTopAlbumsForDateRangeAsync(username, from, to, limit);
        return response != null
            ? Result<TopAlbums>.Ok(response)
            : Result<TopAlbums>.DataError("No data returned from Last.fm API");
    }

    public async Task<Result<RecentTracks>> GetRecentTracksAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 200,
        int page = 1)
    {
        var response = await _apiClient.GetRecentTracksAsync(username, from, to, limit, page);
        return response != null
            ? Result<RecentTracks>.Ok(response)
            : Result<RecentTracks>.DataError("No data returned from Last.fm API");
    }

    // Artist-specific methods - direct passthrough
    public async Task<Result<TopTracks>> GetArtistTopTracksAsync(string artist, int limit = 10)
    {
        var response = await _apiClient.GetArtistTopTracksAsync(artist, limit);
        return response != null
            ? Result<TopTracks>.Ok(response)
            : Result<TopTracks>.DataError($"No tracks found for artist: {artist}");
    }

    public async Task<Result<TopAlbums>> GetArtistTopAlbumsAsync(string artist, int limit = 10)
    {
        var response = await _apiClient.GetArtistTopAlbumsAsync(artist, limit);
        return response != null
            ? Result<TopAlbums>.Ok(response)
            : Result<TopAlbums>.DataError($"No albums found for artist: {artist}");
    }

    public async Task<Result<SimilarArtists>> GetSimilarArtistsAsync(string artist, int limit = 50)
    {
        var response = await _apiClient.GetSimilarArtistsAsync(artist, limit);
        return response != null
            ? Result<SimilarArtists>.Ok(response)
            : Result<SimilarArtists>.DataError($"No similar artists found for: {artist}");
    }

    public async Task<Result<TopTags>> GetArtistTopTagsAsync(string artist, bool autocorrect = true)
    {
        var response = await _apiClient.GetArtistTopTagsAsync(artist, autocorrect);
        return response != null
            ? Result<TopTags>.Ok(response)
            : Result<TopTags>.DataError($"No tags found for artist: {artist}");
    }

    // Lookup methods - direct passthrough
    public async Task<Result<ArtistLookupInfo>> GetArtistInfoAsync(string artist, string username)
    {
        var response = await _apiClient.GetArtistInfoAsync(artist, username);
        return response != null
            ? Result<ArtistLookupInfo>.Ok(response)
            : Result<ArtistLookupInfo>.DataError($"Artist not found: {artist}");
    }

    public async Task<Result<TrackLookupInfo>> GetTrackInfoAsync(
        string artist,
        string track,
        string username)
    {
        var response = await _apiClient.GetTrackInfoAsync(artist, track, username);
        return response != null
            ? Result<TrackLookupInfo>.Ok(response)
            : Result<TrackLookupInfo>.DataError($"Track not found: {artist} - {track}");
    }

    public async Task<Result<AlbumLookupInfo>> GetAlbumInfoAsync(
        string artist,
        string album,
        string username)
    {
        var response = await _apiClient.GetAlbumInfoAsync(artist, album, username);
        return response != null
            ? Result<AlbumLookupInfo>.Ok(response)
            : Result<AlbumLookupInfo>.DataError($"Album not found: {artist} - {album}");
    }
}
