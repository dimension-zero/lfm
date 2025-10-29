using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models.Results;

namespace Lfm.Data.Direct.Enrichment;

/// <summary>
/// Album enrichment using Spotify Web API.
/// Performs OAuth authentication and track search to retrieve album information.
/// </summary>
public class SpotifyAlbumEnricher : IAlbumEnricher
{
    private readonly ILogger<SpotifyAlbumEnricher> _logger;
    private readonly HttpClient _httpClient;
    private readonly SpotifyConfig _config;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public string EnricherName => "Spotify";

    public SpotifyAlbumEnricher(
        ILogger<SpotifyAlbumEnricher> logger,
        LfmConfig lfmConfig)
    {
        _logger = logger;
        _config = lfmConfig.Spotify;
        _httpClient = new HttpClient();
        _tokenExpiry = DateTime.MinValue;
    }

    public async Task<Result<string?>> GetAlbumAsync(string artist, string track)
    {
        try
        {
            // Check if Spotify is configured
            if (string.IsNullOrWhiteSpace(_config.ClientId) || string.IsNullOrWhiteSpace(_config.ClientSecret))
            {
                _logger.LogDebug("Spotify API not configured (missing ClientId or ClientSecret)");
                return Result<string?>.Ok(null);
            }

            // Ensure we have a valid access token
            var tokenResult = await EnsureValidAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                _logger.LogDebug("Failed to get Spotify access token: {Error}", tokenResult.ErrorMessage);
                return Result<string?>.Ok(null);
            }

            // Search for track
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var query = HttpUtility.UrlEncode($"artist:\"{artist}\" track:\"{track}\"");
            var searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=1";

            var response = await _httpClient.GetAsync(searchUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Spotify search failed with status {Status}", response.StatusCode);
                return Result<string?>.Ok(null);
            }

            var json = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(json);

            var firstTrack = searchResponse?.Tracks?.Items?.FirstOrDefault();
            var album = firstTrack?.Album?.Name;

            if (!string.IsNullOrWhiteSpace(album))
            {
                _logger.LogDebug("Spotify found album '{Album}' for {Artist} - {Track}", album, artist, track);
                return Result<string?>.Ok(album);
            }

            _logger.LogDebug("Spotify: no album found for {Artist} - {Track}", artist, track);
            return Result<string?>.Ok(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spotify enricher exception for {Artist} - {Track}", artist, track);
            return Result<string?>.DataError($"Spotify API error: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures we have a valid access token, refreshing if necessary.
    /// </summary>
    private async Task<Result<bool>> EnsureValidAccessTokenAsync()
    {
        try
        {
            // Check if token is still valid
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return Result<bool>.Ok(true);
            }

            // Get new access token using client credentials flow
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var requestData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", requestData);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result<bool>.DataError($"Spotify auth failed: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(json);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return Result<bool>.DataError("Failed to parse Spotify token response");
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // 60 second buffer

            _logger.LogDebug("Spotify access token refreshed, expires in {Seconds} seconds", tokenResponse.ExpiresIn);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Spotify access token");
            return Result<bool>.DataError($"Token refresh error: {ex.Message}");
        }
    }

    // Minimal Spotify API models for enrichment (to avoid dependency on Lfm.Spotify)
    private class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class SpotifySearchResponse
    {
        [JsonPropertyName("tracks")]
        public SpotifyTracksResult? Tracks { get; set; }
    }

    private class SpotifyTracksResult
    {
        [JsonPropertyName("items")]
        public List<SpotifyTrack> Items { get; set; } = new();
    }

    private class SpotifyTrack
    {
        [JsonPropertyName("album")]
        public SpotifyAlbum? Album { get; set; }
    }

    private class SpotifyAlbum
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
