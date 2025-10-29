using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Lfm.Data.Direct;

/// <summary>
/// Data provider that merges Last.fm API data with local file data
/// Local files are source of truth for play counts, API supplements with additional data
/// </summary>
public class MergedDataProvider : IMusicDataProvider
{
    private readonly IMusicDataProvider _apiProvider;
    private readonly IMusicDataProvider _localProvider;
    private readonly ILogger<MergedDataProvider> _logger;

    public string ProviderName => "Merged (API + Local Files)";
    public bool SupportsDateRanges => true;
    public bool SupportsSimilarArtists => true;
    public bool SupportsLookup => true;

    public MergedDataProvider(
        IMusicDataProvider apiProvider,
        IMusicDataProvider localProvider,
        ILogger<MergedDataProvider> logger)
    {
        _apiProvider = apiProvider ?? throw new ArgumentNullException(nameof(apiProvider));
        _localProvider = localProvider ?? throw new ArgumentNullException(nameof(localProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TopArtists>> GetTopArtistsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1)
    {
        // Query both providers
        var apiResult = await _apiProvider.GetTopArtistsAsync(username, period, limit * 2, page);
        var localResult = await _localProvider.GetTopArtistsAsync(username, period, limit * 2, page);

        // Handle error cases
        if (!apiResult.IsSuccess && !localResult.IsSuccess)
        {
            _logger.LogWarning("Both providers failed. API: {ApiError}, Local: {LocalError}",
                apiResult.ErrorMessage, localResult.ErrorMessage);
            return Result<TopArtists>.DataError($"Both providers failed: API: {apiResult.ErrorMessage}, Local: {localResult.ErrorMessage}");
        }

        if (!apiResult.IsSuccess)
        {
            _logger.LogInformation("API provider failed, using local files only: {Error}", apiResult.ErrorMessage);
            return localResult;
        }

        if (!localResult.IsSuccess)
        {
            _logger.LogInformation("Local provider failed, using API only: {Error}", localResult.ErrorMessage);
            return apiResult;
        }

        // Merge results
        var merged = MergeArtists(apiResult.Data!.Artists, localResult.Data!.Artists);

        // Sort by play count descending and take top N
        var sortedMerged = merged
            .OrderByDescending(a => int.Parse(a.PlayCount))
            .Take(limit)
            .ToList();

        _logger.LogInformation("Merged {ApiCount} API artists with {LocalCount} local artists, result: {MergedCount} artists",
            apiResult.Data.Artists.Count, localResult.Data.Artists.Count, sortedMerged.Count);

        return Result<TopArtists>.Ok(new TopArtists
        {
            Artists = sortedMerged,
            Attributes = apiResult.Data.Attributes ?? localResult.Data.Attributes ?? new TopArtistsAttributes { User = username }
        });
    }

    public async Task<Result<TopTracks>> GetTopTracksAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1)
    {
        // Query both providers
        var apiResult = await _apiProvider.GetTopTracksAsync(username, period, limit * 2, page);
        var localResult = await _localProvider.GetTopTracksAsync(username, period, limit * 2, page);

        // Handle error cases
        if (!apiResult.IsSuccess && !localResult.IsSuccess)
        {
            _logger.LogWarning("Both providers failed. API: {ApiError}, Local: {LocalError}",
                apiResult.ErrorMessage, localResult.ErrorMessage);
            return Result<TopTracks>.DataError($"Both providers failed: API: {apiResult.ErrorMessage}, Local: {localResult.ErrorMessage}");
        }

        if (!apiResult.IsSuccess)
        {
            _logger.LogInformation("API provider failed, using local files only: {Error}", apiResult.ErrorMessage);
            return localResult;
        }

        if (!localResult.IsSuccess)
        {
            _logger.LogInformation("Local provider failed, using API only: {Error}", localResult.ErrorMessage);
            return apiResult;
        }

        // Merge results
        var merged = MergeTracks(apiResult.Data!.Tracks, localResult.Data!.Tracks);

        // Sort by play count descending and take top N
        var sortedMerged = merged
            .OrderByDescending(t => int.Parse(t.PlayCount))
            .Take(limit)
            .ToList();

        _logger.LogInformation("Merged {ApiCount} API tracks with {LocalCount} local tracks, result: {MergedCount} tracks",
            apiResult.Data.Tracks.Count, localResult.Data.Tracks.Count, sortedMerged.Count);

        return Result<TopTracks>.Ok(new TopTracks
        {
            Tracks = sortedMerged,
            Attributes = apiResult.Data.Attributes ?? localResult.Data.Attributes ?? new TopTracksAttributes { User = username }
        });
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1)
    {
        // Query both providers
        var apiResult = await _apiProvider.GetTopAlbumsAsync(username, period, limit * 2, page);
        var localResult = await _localProvider.GetTopAlbumsAsync(username, period, limit * 2, page);

        // Handle error cases
        if (!apiResult.IsSuccess && !localResult.IsSuccess)
        {
            _logger.LogWarning("Both providers failed. API: {ApiError}, Local: {LocalError}",
                apiResult.ErrorMessage, localResult.ErrorMessage);
            return Result<TopAlbums>.DataError($"Both providers failed: API: {apiResult.ErrorMessage}, Local: {localResult.ErrorMessage}");
        }

        if (!apiResult.IsSuccess)
        {
            _logger.LogInformation("API provider failed, using local files only: {Error}", apiResult.ErrorMessage);
            return localResult;
        }

        if (!localResult.IsSuccess)
        {
            _logger.LogInformation("Local provider failed, using API only: {Error}", localResult.ErrorMessage);
            return apiResult;
        }

        // Merge results
        var merged = MergeAlbums(apiResult.Data!.Albums, localResult.Data!.Albums);

        // Sort by play count descending and take top N
        var sortedMerged = merged
            .OrderByDescending(a => int.Parse(a.PlayCount))
            .Take(limit)
            .ToList();

        _logger.LogInformation("Merged {ApiCount} API albums with {LocalCount} local albums, result: {MergedCount} albums",
            apiResult.Data.Albums.Count, localResult.Data.Albums.Count, sortedMerged.Count);

        return Result<TopAlbums>.Ok(new TopAlbums
        {
            Albums = sortedMerged,
            Attributes = apiResult.Data.Attributes ?? localResult.Data.Attributes ?? new TopAlbumsAttributes { User = username }
        });
    }

    public async Task<Result<TopArtists>> GetTopArtistsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        // Query both providers
        var apiResult = await _apiProvider.GetTopArtistsForDateRangeAsync(username, from, to, limit * 2);
        var localResult = await _localProvider.GetTopArtistsForDateRangeAsync(username, from, to, limit * 2);

        // Handle error cases
        if (!apiResult.IsSuccess && !localResult.IsSuccess)
            return Result<TopArtists>.DataError($"Both providers failed: API: {apiResult.ErrorMessage}, Local: {localResult.ErrorMessage}");

        if (!apiResult.IsSuccess)
            return localResult;

        if (!localResult.IsSuccess)
            return apiResult;

        // Merge and return
        var merged = MergeArtists(apiResult.Data!.Artists, localResult.Data!.Artists);
        var sortedMerged = merged.OrderByDescending(a => int.Parse(a.PlayCount)).Take(limit).ToList();

        return Result<TopArtists>.Ok(new TopArtists
        {
            Artists = sortedMerged,
            Attributes = apiResult.Data.Attributes ?? localResult.Data.Attributes ?? new TopArtistsAttributes { User = username }
        });
    }

    public async Task<Result<TopTracks>> GetTopTracksForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        // Query both providers
        var apiResult = await _apiProvider.GetTopTracksForDateRangeAsync(username, from, to, limit * 2);
        var localResult = await _localProvider.GetTopTracksForDateRangeAsync(username, from, to, limit * 2);

        // Handle error cases
        if (!apiResult.IsSuccess && !localResult.IsSuccess)
            return Result<TopTracks>.DataError($"Both providers failed: API: {apiResult.ErrorMessage}, Local: {localResult.ErrorMessage}");

        if (!apiResult.IsSuccess)
            return localResult;

        if (!localResult.IsSuccess)
            return apiResult;

        // Merge and return
        var merged = MergeTracks(apiResult.Data!.Tracks, localResult.Data!.Tracks);
        var sortedMerged = merged.OrderByDescending(t => int.Parse(t.PlayCount)).Take(limit).ToList();

        return Result<TopTracks>.Ok(new TopTracks
        {
            Tracks = sortedMerged,
            Attributes = apiResult.Data.Attributes ?? localResult.Data.Attributes ?? new TopTracksAttributes { User = username }
        });
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        // Query both providers
        var apiResult = await _apiProvider.GetTopAlbumsForDateRangeAsync(username, from, to, limit * 2);
        var localResult = await _localProvider.GetTopAlbumsForDateRangeAsync(username, from, to, limit * 2);

        // Handle error cases
        if (!apiResult.IsSuccess && !localResult.IsSuccess)
            return Result<TopAlbums>.DataError($"Both providers failed: API: {apiResult.ErrorMessage}, Local: {localResult.ErrorMessage}");

        if (!apiResult.IsSuccess)
            return localResult;

        if (!localResult.IsSuccess)
            return apiResult;

        // Merge and return
        var merged = MergeAlbums(apiResult.Data!.Albums, localResult.Data!.Albums);
        var sortedMerged = merged.OrderByDescending(a => int.Parse(a.PlayCount)).Take(limit).ToList();

        return Result<TopAlbums>.Ok(new TopAlbums
        {
            Albums = sortedMerged,
            Attributes = apiResult.Data.Attributes ?? localResult.Data.Attributes ?? new TopAlbumsAttributes { User = username }
        });
    }

    public async Task<Result<RecentTracks>> GetRecentTracksAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1)
    {
        // Recent tracks: Try API first (more accurate timestamps), fallback to local
        var apiResult = await _apiProvider.GetRecentTracksAsync(username, from, to, limit, page);

        if (apiResult.IsSuccess)
            return apiResult;

        _logger.LogInformation("API provider failed for recent tracks, trying local files: {Error}", apiResult.ErrorMessage);
        return await _localProvider.GetRecentTracksAsync(username, from, to, limit, page);
    }

    public async Task<Result<TopTracks>> GetArtistTopTracksAsync(string artist, int limit = 10)
    {
        // Artist top tracks: Try API first (has artist track rankings), fallback to local
        var apiResult = await _apiProvider.GetArtistTopTracksAsync(artist, limit);

        if (apiResult.IsSuccess)
            return apiResult;

        _logger.LogInformation("API provider failed for artist top tracks, trying local files: {Error}", apiResult.ErrorMessage);
        return await _localProvider.GetArtistTopTracksAsync(artist, limit);
    }

    public async Task<Result<TopAlbums>> GetArtistTopAlbumsAsync(string artist, int limit = 10)
    {
        // Artist top albums: Try API first (has artist album rankings), fallback to local
        var apiResult = await _apiProvider.GetArtistTopAlbumsAsync(artist, limit);

        if (apiResult.IsSuccess)
            return apiResult;

        _logger.LogInformation("API provider failed for artist top albums, trying local files: {Error}", apiResult.ErrorMessage);
        return await _localProvider.GetArtistTopAlbumsAsync(artist, limit);
    }

    public Task<Result<SimilarArtists>> GetSimilarArtistsAsync(string artist, int limit = 50)
    {
        // Delegate to API only - local files don't support similar artists
        return _apiProvider.GetSimilarArtistsAsync(artist, limit);
    }

    public Task<Result<TopTags>> GetArtistTopTagsAsync(string artist, bool autocorrect = true)
    {
        // Delegate to API only - local files don't support tags
        return _apiProvider.GetArtistTopTagsAsync(artist, autocorrect);
    }

    public async Task<Result<ArtistLookupInfo>> GetArtistInfoAsync(string artist, string username)
    {
        // Lookup: Try API first (has more metadata), fallback to local
        var apiResult = await _apiProvider.GetArtistInfoAsync(artist, username);

        if (apiResult.IsSuccess)
            return apiResult;

        _logger.LogInformation("API provider failed for artist lookup, trying local files: {Error}", apiResult.ErrorMessage);
        return await _localProvider.GetArtistInfoAsync(artist, username);
    }

    public async Task<Result<TrackLookupInfo>> GetTrackInfoAsync(string artist, string track, string username)
    {
        // Lookup: Try API first (has more metadata), fallback to local
        var apiResult = await _apiProvider.GetTrackInfoAsync(artist, track, username);

        if (apiResult.IsSuccess)
            return apiResult;

        _logger.LogInformation("API provider failed for track lookup, trying local files: {Error}", apiResult.ErrorMessage);
        return await _localProvider.GetTrackInfoAsync(artist, track, username);
    }

    public async Task<Result<AlbumLookupInfo>> GetAlbumInfoAsync(string artist, string album, string username)
    {
        // Lookup: Try API first (has more metadata), fallback to local
        var apiResult = await _apiProvider.GetAlbumInfoAsync(artist, album, username);

        if (apiResult.IsSuccess)
            return apiResult;

        _logger.LogInformation("API provider failed for album lookup, trying local files: {Error}", apiResult.ErrorMessage);
        return await _localProvider.GetAlbumInfoAsync(artist, album, username);
    }

    // Helper methods for merging results

    private List<Artist> MergeArtists(List<Artist> apiArtists, List<Artist> localArtists)
    {
        var mergedDict = new Dictionary<string, Artist>(StringComparer.OrdinalIgnoreCase);

        // Add API artists
        foreach (var artist in apiArtists)
        {
            mergedDict[artist.Name] = artist;
        }

        // Merge or add local artists
        foreach (var local in localArtists)
        {
            if (mergedDict.TryGetValue(local.Name, out var existing))
            {
                // Sum play counts
                var apiCount = int.Parse(existing.PlayCount);
                var localCount = int.Parse(local.PlayCount);
                existing.PlayCount = (apiCount + localCount).ToString();
            }
            else
            {
                // Add local-only artist
                mergedDict[local.Name] = local;
            }
        }

        return mergedDict.Values.ToList();
    }

    private List<Track> MergeTracks(List<Track> apiTracks, List<Track> localTracks)
    {
        var mergedDict = new Dictionary<string, Track>(StringComparer.OrdinalIgnoreCase);

        // Add API tracks (use artist+track as key)
        foreach (var track in apiTracks)
        {
            var key = $"{track.Artist.Name}|{track.Name}";
            mergedDict[key] = track;
        }

        // Merge or add local tracks
        foreach (var local in localTracks)
        {
            var key = $"{local.Artist.Name}|{local.Name}";
            if (mergedDict.TryGetValue(key, out var existing))
            {
                // Sum play counts
                var apiCount = int.Parse(existing.PlayCount);
                var localCount = int.Parse(local.PlayCount);
                existing.PlayCount = (apiCount + localCount).ToString();
            }
            else
            {
                // Add local-only track
                mergedDict[key] = local;
            }
        }

        return mergedDict.Values.ToList();
    }

    private List<Album> MergeAlbums(List<Album> apiAlbums, List<Album> localAlbums)
    {
        var mergedDict = new Dictionary<string, Album>(StringComparer.OrdinalIgnoreCase);

        // Add API albums (use artist+album as key)
        foreach (var album in apiAlbums)
        {
            var key = $"{album.Artist.Name}|{album.Name}";
            mergedDict[key] = album;
        }

        // Merge or add local albums
        foreach (var local in localAlbums)
        {
            var key = $"{local.Artist.Name}|{local.Name}";
            if (mergedDict.TryGetValue(key, out var existing))
            {
                // Sum play counts
                var apiCount = int.Parse(existing.PlayCount);
                var localCount = int.Parse(local.PlayCount);
                existing.PlayCount = (apiCount + localCount).ToString();
            }
            else
            {
                // Add local-only album
                mergedDict[key] = local;
            }
        }

        return mergedDict.Values.ToList();
    }
}
