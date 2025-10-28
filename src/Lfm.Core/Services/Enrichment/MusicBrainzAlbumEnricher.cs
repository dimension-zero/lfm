using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Lfm.Shared.Models.Results;

namespace Lfm.Core.Services.Enrichment;

/// <summary>
/// Album enrichment using MusicBrainz API.
/// Uses the free, open MusicBrainz music database.
/// Rate limited to 1 request per second as per MusicBrainz guidelines.
/// </summary>
public class MusicBrainzAlbumEnricher : IAlbumEnricher
{
    private readonly ILogger<MusicBrainzAlbumEnricher> _logger;
    private readonly HttpClient _httpClient;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);

    public string EnricherName => "MusicBrainz";

    public MusicBrainzAlbumEnricher(ILogger<MusicBrainzAlbumEnricher> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        // MusicBrainz requires a User-Agent header
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LfmCli/1.0 (https://github.com/steven-marshall/lfm)");
    }

    public async Task<Result<string?>> GetAlbumAsync(string artist, string track)
    {
        try
        {
            // Apply rate limiting (1 request per second)
            await ApplyRateLimitAsync();

            // Build search query
            var query = HttpUtility.UrlEncode($"artist:\"{artist}\" AND recording:\"{track}\"");
            var searchUrl = $"https://musicbrainz.org/ws/2/recording/?query={query}&fmt=json&limit=1";

            var response = await _httpClient.GetAsync(searchUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("MusicBrainz search failed with status {Status}", response.StatusCode);
                return Result<string?>.Ok(null);
            }

            var json = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<MusicBrainzSearchResponse>(json);

            var firstRecording = searchResponse?.Recordings?.FirstOrDefault();
            var firstRelease = firstRecording?.Releases?.FirstOrDefault();
            var album = firstRelease?.Title;

            if (!string.IsNullOrWhiteSpace(album))
            {
                _logger.LogDebug("MusicBrainz found album '{Album}' for {Artist} - {Track}", album, artist, track);
                return Result<string?>.Ok(album);
            }

            _logger.LogDebug("MusicBrainz: no album found for {Artist} - {Track}", artist, track);
            return Result<string?>.Ok(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MusicBrainz enricher exception for {Artist} - {Track}", artist, track);
            return Result<string?>.DataError($"MusicBrainz API error: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies MusicBrainz rate limiting: 1 request per second.
    /// Uses semaphore to ensure thread-safe rate limiting.
    /// </summary>
    private async Task ApplyRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minimumDelay = TimeSpan.FromMilliseconds(1000);

            if (timeSinceLastRequest < minimumDelay)
            {
                var delayNeeded = minimumDelay - timeSinceLastRequest;
                _logger.LogDebug("MusicBrainz rate limit: waiting {Ms}ms", delayNeeded.TotalMilliseconds);
                await Task.Delay(delayNeeded);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // Minimal MusicBrainz API models for enrichment
    private class MusicBrainzSearchResponse
    {
        [JsonPropertyName("recordings")]
        public List<MusicBrainzRecording> Recordings { get; set; } = new();
    }

    private class MusicBrainzRecording
    {
        [JsonPropertyName("releases")]
        public List<MusicBrainzRelease> Releases { get; set; } = new();
    }

    private class MusicBrainzRelease
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
