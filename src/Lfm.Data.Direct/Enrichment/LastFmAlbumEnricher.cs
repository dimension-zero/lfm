using Microsoft.Extensions.Logging;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Interfaces;

namespace Lfm.Data.Direct.Enrichment;

/// <summary>
/// Album enrichment using Last.fm track.getInfo API.
/// Leverages existing Last.fm API client infrastructure.
/// </summary>
public class LastFmAlbumEnricher : IAlbumEnricher
{
    private readonly ILogger<LastFmAlbumEnricher> _logger;
    private readonly ILastFmApiClient _apiClient;

    public string EnricherName => "LastFm";

    public LastFmAlbumEnricher(
        ILogger<LastFmAlbumEnricher> logger,
        ILastFmApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    public async Task<Result<string?>> GetAlbumAsync(string artist, string track)
    {
        try
        {
            // track.getInfo doesn't require username for album lookup
            var result = await _apiClient.GetTrackInfoWithResultAsync(artist, track, string.Empty);

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Last.fm track.getInfo failed: {Error}", result.ErrorMessage);
                return Result<string?>.Ok(null);
            }

            var album = result.Data?.Track?.Album?.Title;

            if (string.IsNullOrWhiteSpace(album))
            {
                _logger.LogDebug("Last.fm returned no album for {Artist} - {Track}", artist, track);
                return Result<string?>.Ok(null);
            }

            _logger.LogDebug("Last.fm found album '{Album}' for {Artist} - {Track}", album, artist, track);
            return Result<string?>.Ok(album);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Last.fm enricher exception for {Artist} - {Track}", artist, track);
            return Result<string?>.DataError($"Last.fm API error: {ex.Message}");
        }
    }
}
