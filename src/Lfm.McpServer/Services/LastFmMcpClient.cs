using Lfm.Core.Models;
using Lfm.Core.Models.Results;
using Lfm.Core.Services;
using Microsoft.Extensions.Logging;

namespace Lfm.McpServer.Services;

/// <summary>
/// MCP-friendly wrapper around LastFmApiClient.
/// Applies transformation rules to produce conversational output.
/// </summary>
public class LastFmMcpClient
{
    private readonly ILastFmApiClient _apiClient;
    private readonly ILogger<LastFmMcpClient> _logger;

    public LastFmMcpClient(ILastFmApiClient apiClient, ILogger<LastFmMcpClient> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get user's top artists with transformation rules applied.
    /// Token optimization: Strips Url, Mbid (50% reduction target)
    /// </summary>
    public async Task<Result<List<CompactArtist>>> GetTopArtistsAsync(string username, string period = "overall", int limit = 10)
    {
        var result = await _apiClient.GetTopArtistsWithResultAsync(username, period, limit);

        if (result.IsFailure)
            return Result<List<CompactArtist>>.Fail(result.Error!);

        var compactArtists = result.Data!.Artists
            .Select(a => new CompactArtist
            {
                Name = a.Name,
                PlayCount = a.PlayCount,
                Rank = a.Attributes?.Rank
            })
            .ToList();

        return Result<List<CompactArtist>>.Ok(compactArtists);
    }

    /// <summary>
    /// Get user's top tracks with transformation rules applied.
    /// Property flattening: Track.Artist.Name → artist
    /// Token optimization: Strips Url, Mbid
    /// </summary>
    public async Task<Result<List<CompactTrack>>> GetTopTracksAsync(string username, string period = "overall", int limit = 10)
    {
        var result = await _apiClient.GetTopTracksWithResultAsync(username, period, limit);

        if (result.IsFailure)
            return Result<List<CompactTrack>>.Fail(result.Error!);

        var compactTracks = result.Data!.Tracks
            .Select(t => new CompactTrack
            {
                Name = t.Name,
                PlayCount = t.PlayCount,
                Artist = t.Artist.Name,  // Flattened from Track.Artist.Name
                Rank = t.Attributes?.Rank
            })
            .ToList();

        return Result<List<CompactTrack>>.Ok(compactTracks);
    }

    /// <summary>
    /// Get user's top albums with transformation rules applied.
    /// Property flattening: Album.Artist.Name → artist
    /// Token optimization: Strips Url, Mbid
    /// </summary>
    public async Task<Result<List<CompactAlbum>>> GetTopAlbumsAsync(string username, string period = "overall", int limit = 10)
    {
        var result = await _apiClient.GetTopAlbumsWithResultAsync(username, period, limit);

        if (result.IsFailure)
            return Result<List<CompactAlbum>>.Fail(result.Error!);

        var compactAlbums = result.Data!.Albums
            .Select(a => new CompactAlbum
            {
                Name = a.Name,
                PlayCount = a.PlayCount,
                Artist = a.Artist.Name,  // Flattened from Album.Artist.Name
                Rank = a.Attributes?.Rank
            })
            .ToList();

        return Result<List<CompactAlbum>>.Ok(compactAlbums);
    }
}

/// <summary>
/// Compact artist model with transformation rules applied.
/// Excludes: Url, Mbid (token optimization)
/// </summary>
public class CompactArtist
{
    public string Name { get; set; } = string.Empty;
    public string PlayCount { get; set; } = "0";
    public string? Rank { get; set; }
}

/// <summary>
/// Compact track model with transformation rules applied.
/// Excludes: Url, Mbid (token optimization)
/// Flattens: Artist.Name → artist (property flattening)
/// </summary>
public class CompactTrack
{
    public string Name { get; set; } = string.Empty;
    public string PlayCount { get; set; } = "0";
    public string Artist { get; set; } = string.Empty;  // Flattened
    public string? Rank { get; set; }
}

/// <summary>
/// Compact album model with transformation rules applied.
/// Excludes: Url, Mbid (token optimization)
/// Flattens: Artist.Name → artist (property flattening)
/// </summary>
public class CompactAlbum
{
    public string Name { get; set; } = string.Empty;
    public string PlayCount { get; set; } = "0";
    public string Artist { get; set; } = string.Empty;  // Flattened
    public string? Rank { get; set; }
}
