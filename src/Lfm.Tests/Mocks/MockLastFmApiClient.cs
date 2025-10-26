using System.Text.Json;
using Lfm.Core.Models;
using Lfm.Core.Models.Results;
using Lfm.Core.Services;

namespace Lfm.Tests.Mocks;

/// <summary>
/// Mock Last.fm API client that loads responses from JSON fixture files.
/// Used for testing without requiring actual API credentials.
/// </summary>
public class MockLastFmApiClient : ILastFmApiClient
{
    private readonly string _testDataPath;

    public MockLastFmApiClient(string testDataPath)
    {
        _testDataPath = testDataPath ?? throw new ArgumentNullException(nameof(testDataPath));
    }

    public async Task<Result<TopArtists>> GetTopArtistsWithResultAsync(string username, string period = "overall", int limit = 10, int page = 1)
    {
        var filePath = Path.Combine(_testDataPath, "top-artists-response.json");
        var json = await File.ReadAllTextAsync(filePath);
        var response = JsonSerializer.Deserialize<TopArtistsResponse>(json);

        if (response?.TopArtists == null)
            return Result<TopArtists>.ApiError("Failed to deserialize top artists response");

        return Result<TopArtists>.Ok(response.TopArtists);
    }

    public async Task<Result<TopTracks>> GetTopTracksWithResultAsync(string username, string period = "overall", int limit = 10, int page = 1)
    {
        var filePath = Path.Combine(_testDataPath, "top-tracks-response.json");
        var json = await File.ReadAllTextAsync(filePath);
        var response = JsonSerializer.Deserialize<TopTracksResponse>(json);

        if (response?.TopTracks == null)
            return Result<TopTracks>.ApiError("Failed to deserialize top tracks response");

        return Result<TopTracks>.Ok(response.TopTracks);
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsWithResultAsync(string username, string period = "overall", int limit = 10, int page = 1)
    {
        var filePath = Path.Combine(_testDataPath, "top-albums-response.json");
        var json = await File.ReadAllTextAsync(filePath);
        var response = JsonSerializer.Deserialize<TopAlbumsResponse>(json);

        if (response?.TopAlbums == null)
            return Result<TopAlbums>.ApiError("Failed to deserialize top albums response");

        return Result<TopAlbums>.Ok(response.TopAlbums);
    }

    // Legacy nullable methods - delegate to Result-based methods
    public async Task<TopArtists?> GetTopArtistsAsync(string username, string period = "overall", int limit = 10, int page = 1)
    {
        var result = await GetTopArtistsWithResultAsync(username, period, limit, page);
        return result.Success ? result.Data : null;
    }

    public async Task<TopTracks?> GetTopTracksAsync(string username, string period = "overall", int limit = 10, int page = 1)
    {
        var result = await GetTopTracksWithResultAsync(username, period, limit, page);
        return result.Success ? result.Data : null;
    }

    public async Task<TopAlbums?> GetTopAlbumsAsync(string username, string period = "overall", int limit = 10, int page = 1)
    {
        var result = await GetTopAlbumsWithResultAsync(username, period, limit, page);
        return result.Success ? result.Data : null;
    }

    // Unimplemented methods - throw NotImplementedException for now
    public Task<TopTracks?> GetArtistTopTracksAsync(string artist, int limit = 10)
        => throw new NotImplementedException();

    public Task<TopAlbums?> GetArtistTopAlbumsAsync(string artist, int limit = 10)
        => throw new NotImplementedException();

    public Task<SimilarArtists?> GetSimilarArtistsAsync(string artist, int limit = 50)
        => throw new NotImplementedException();

    public Task<TopTags?> GetArtistTopTagsAsync(string artist, bool autocorrect = true)
        => throw new NotImplementedException();

    public Task<RecentTracks?> GetRecentTracksAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1)
        => throw new NotImplementedException();

    public Task<TopArtists?> GetTopArtistsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
        => throw new NotImplementedException();

    public Task<TopTracks?> GetTopTracksForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
        => throw new NotImplementedException();

    public Task<TopAlbums?> GetTopAlbumsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
        => throw new NotImplementedException();

    public Task<Result<TopTracks>> GetArtistTopTracksWithResultAsync(string artist, int limit = 10)
        => throw new NotImplementedException();

    public Task<Result<TopAlbums>> GetArtistTopAlbumsWithResultAsync(string artist, int limit = 10)
        => throw new NotImplementedException();

    public Task<Result<SimilarArtists>> GetSimilarArtistsWithResultAsync(string artist, int limit = 50)
        => throw new NotImplementedException();

    public Task<Result<TopTags>> GetArtistTopTagsWithResultAsync(string artist, bool autocorrect = true)
        => throw new NotImplementedException();

    public Task<Result<RecentTracks>> GetRecentTracksWithResultAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1)
        => throw new NotImplementedException();

    public Task<Result<TopArtists>> GetTopArtistsForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10)
        => throw new NotImplementedException();

    public Task<Result<TopTracks>> GetTopTracksForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10)
        => throw new NotImplementedException();

    public Task<Result<TopAlbums>> GetTopAlbumsForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10)
        => throw new NotImplementedException();

    public Task<ArtistLookupInfo?> GetArtistInfoAsync(string artist, string username)
        => throw new NotImplementedException();

    public Task<TrackLookupInfo?> GetTrackInfoAsync(string artist, string track, string username)
        => throw new NotImplementedException();

    public Task<AlbumLookupInfo?> GetAlbumInfoAsync(string artist, string album, string username)
        => throw new NotImplementedException();

    public Task<Result<ArtistLookupInfo>> GetArtistInfoWithResultAsync(string artist, string username)
        => throw new NotImplementedException();

    public Task<Result<TrackLookupInfo>> GetTrackInfoWithResultAsync(string artist, string track, string username)
        => throw new NotImplementedException();

    public Task<Result<AlbumLookupInfo>> GetAlbumInfoWithResultAsync(string artist, string album, string username)
        => throw new NotImplementedException();
}

// JSON response wrapper classes for deserialization
internal class TopArtistsResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("topartists")]
    public TopArtists? TopArtists { get; set; }
}

internal class TopTracksResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("toptracks")]
    public TopTracks? TopTracks { get; set; }
}

internal class TopAlbumsResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("topalbums")]
    public TopAlbums? TopAlbums { get; set; }
}
