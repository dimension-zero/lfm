using System.ComponentModel;
using System.Text.Json;
using Lfm.McpServer.Services;
using Lfm.Shared.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Lfm.McpServer.Tools;

/// <summary>
/// MCP tools for Last.fm API.
/// Implements transformation rules from lastfm-rules.json:
/// - Property flattening (Track.Artist.Name → artist)
/// - Token optimization (strips Url, Mbid for 50% reduction)
/// - Equivalence handling (case-insensitive via underlying client)
/// </summary>
public static class LastFmTools
{
    private static LastFmMcpClient? _client;
    private static ILogger? _logger;
    private static string? _username;

    public static void Initialize(LastFmMcpClient client, ILogger logger, string username)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _username = username ?? throw new ArgumentNullException(nameof(username));
    }

    [Description("Get user's top artists for a time period")]
    public static async Task<string> lfm_artists(
        [Description("Time period: overall, 7day, 1month, 3month, 6month, 12month")] string? period = null,
        [Description("Number of artists to return (1-50)")] int? limit = null)
    {
        if (_client == null || _logger == null || _username == null)
            throw new InvalidOperationException("Tools not initialized");

        var actualPeriod = LastFmPeriodExtensions.ParsePeriod(period ?? "overall");
        var actualLimit = Math.Clamp(limit ?? 10, 1, 50);

        var result = await _client.GetTopArtistsAsync(_username, actualPeriod, actualLimit);

        if (result.IsFailure)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = result.Error!.Message
            });
        }

        // Return compact artists (transformation rules applied in client)
        return JsonSerializer.Serialize(new
        {
            artists = result.Data,
            period = actualPeriod.ToApiString(),
            count = result.Data!.Count
        });
    }

    [Description("Get user's top tracks for a time period")]
    public static async Task<string> lfm_tracks(
        [Description("Time period: overall, 7day, 1month, 3month, 6month, 12month")] string? period = null,
        [Description("Number of tracks to return (1-50)")] int? limit = null)
    {
        if (_client == null || _logger == null || _username == null)
            throw new InvalidOperationException("Tools not initialized");

        var actualPeriod = LastFmPeriodExtensions.ParsePeriod(period ?? "overall");
        var actualLimit = Math.Clamp(limit ?? 10, 1, 50);

        var result = await _client.GetTopTracksAsync(_username, actualPeriod, actualLimit);

        if (result.IsFailure)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = result.Error!.Message
            });
        }

        // Return compact tracks with flattened artist names
        return JsonSerializer.Serialize(new
        {
            tracks = result.Data,
            period = actualPeriod.ToApiString(),
            count = result.Data!.Count
        });
    }

    [Description("Get user's top albums for a time period")]
    public static async Task<string> lfm_albums(
        [Description("Time period: overall, 7day, 1month, 3month, 6month, 12month")] string? period = null,
        [Description("Number of albums to return (1-50)")] int? limit = null)
    {
        if (_client == null || _logger == null || _username == null)
            throw new InvalidOperationException("Tools not initialized");

        var actualPeriod = LastFmPeriodExtensions.ParsePeriod(period ?? "overall");
        var actualLimit = Math.Clamp(limit ?? 10, 1, 50);

        var result = await _client.GetTopAlbumsAsync(_username, actualPeriod, actualLimit);

        if (result.IsFailure)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = result.Error!.Message
            });
        }

        // Return compact albums with flattened artist names
        return JsonSerializer.Serialize(new
        {
            albums = result.Data,
            period = actualPeriod.ToApiString(),
            count = result.Data!.Count
        });
    }
}
