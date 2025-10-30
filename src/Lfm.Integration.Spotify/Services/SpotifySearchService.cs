using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Lfm.Core.Attributes;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Integration.Spotify.Models;

namespace Lfm.Integration.Spotify.Services;

/// <summary>
/// Handles Spotify catalog search operations
/// </summary>
public class SpotifySearchService : ISpotifySearchService
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyConfig _config;
    private readonly ISpotifyAuthService _authService;

    public SpotifySearchService(SpotifyConfig config, ISpotifyAuthService authService, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Search for track with multiple version detection
    /// </summary>
    public async Task<TrackSearchResult> SearchTrackWithDetailsAsync(Track track, string? albumName = null)
    {
        var tokenResult = await _authService.GetAccessTokenAsync();
        if (!tokenResult.IsSuccess)
        {
            return new TrackSearchResult(); // Empty result on auth failure
        }

        var result = new TrackSearchResult();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

        // Try precise search first, with album context if provided
        var queryBuilder = $"artist:\"{track.Artist.Name}\" track:\"{track.Name}\"";
        if (!string.IsNullOrWhiteSpace(albumName))
        {
            queryBuilder += $" album:\"{albumName}\"";
        }
        var query = HttpUtility.UrlEncode(queryBuilder);

        // Get multiple results to detect versions (limit=5 instead of limit=1)
        var limit = string.IsNullOrWhiteSpace(albumName) ? 5 : 1;
        var searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=track&limit={limit}";

        var response = await _httpClient.GetAsync(searchUrl);
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(json);
            var tracks = searchResponse?.Tracks?.Items;

            if (tracks != null && tracks.Any())
            {
                // Return first track URI
                result.SpotifyUri = tracks.First().Uri;

                // Check for multiple album versions (only if no album was specified)
                if (string.IsNullOrWhiteSpace(albumName) && tracks.Count > 1)
                {
                    var uniqueAlbums = tracks
                        .Where(t => t.Album != null)
                        .Select(t => t.Album!.Name)
                        .Distinct()
                        .ToList();

                    if (uniqueAlbums.Count > 1)
                    {
                        result.HasMultipleVersions = true;
                        result.AlbumVersions = uniqueAlbums;
                    }
                }

                return result;
            }
        }

        // Try loose search if precise search failed and fallback is enabled
        if (_config.FallbackToLooseSearch)
        {
            query = HttpUtility.UrlEncode($"{track.Artist.Name} {track.Name}");
            searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=track&limit={limit}";

            response = await _httpClient.GetAsync(searchUrl);
            json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(json);
                var tracks = searchResponse?.Tracks?.Items;

                if (tracks != null && tracks.Any())
                {
                    result.SpotifyUri = tracks.First().Uri;

                    // Check for multiple album versions
                    if (string.IsNullOrWhiteSpace(albumName) && tracks.Count > 1)
                    {
                        var uniqueAlbums = tracks
                            .Where(t => t.Album != null)
                            .Select(t => t.Album!.Name)
                            .Distinct()
                            .ToList();

                        if (uniqueAlbums.Count > 1)
                        {
                            result.HasMultipleVersions = true;
                            result.AlbumVersions = uniqueAlbums;
                        }
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Search for an album on Spotify and return the album URI
    /// </summary>
    [SuppressMessage("SilentFailure", "SF001", Justification = "External Spotify API integration - null indicates album not found in Spotify catalog")]
    public async Task<string?> SearchAlbumUriAsync(string artistName, string albumName)
    {
        var tokenResult = await _authService.GetAccessTokenAsync();
        if (!tokenResult.IsSuccess)
        {
            return null;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

        // Search for album on Spotify
        var query = HttpUtility.UrlEncode($"artist:\"{artistName}\" album:\"{albumName}\"");
        var searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=album&limit=1";

        try
        {
            var response = await _httpClient.GetAsync(searchUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var searchResponse = JsonSerializer.Deserialize<SpotifyAlbumSearchResponse>(json);
                var firstAlbum = searchResponse?.Albums?.Items.FirstOrDefault();

                if (firstAlbum != null)
                {
                    return firstAlbum.Uri; // Returns spotify:album:XXXXX
                }
            }

            // Try loose search if precise search failed and fallback is enabled
            if (_config.FallbackToLooseSearch)
            {
                query = HttpUtility.UrlEncode($"{artistName} {albumName}");
                searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=album&limit=1";

                response = await _httpClient.GetAsync(searchUrl);
                json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var searchResponse = JsonSerializer.Deserialize<SpotifyAlbumSearchResponse>(json);
                    var firstAlbum = searchResponse?.Albums?.Items.FirstOrDefault();

                    if (firstAlbum != null)
                    {
                        return firstAlbum.Uri;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error searching for album: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Search for an album on Spotify and return all track URIs
    /// </summary>
    public async Task<List<string>> SearchAlbumTracksAsync(string artistName, string albumName)
    {
        var tokenResult = await _authService.GetAccessTokenAsync();
        if (!tokenResult.IsSuccess)
        {
            return new List<string>();
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

        // Search for album on Spotify
        var query = HttpUtility.UrlEncode($"artist:\"{artistName}\" album:\"{albumName}\"");
        var searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=album&limit=1";

        try
        {
            var response = await _httpClient.GetAsync(searchUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var searchResponse = JsonSerializer.Deserialize<SpotifyAlbumSearchResponse>(json);
                var firstAlbum = searchResponse?.Albums?.Items.FirstOrDefault();

                if (firstAlbum != null)
                {
                    // Get album tracks
                    var tracksUrl = $"https://api.spotify.com/v1/albums/{firstAlbum.Id}/tracks";
                    var tracksResponse = await _httpClient.GetAsync(tracksUrl);
                    var tracksJson = await tracksResponse.Content.ReadAsStringAsync();

                    if (tracksResponse.IsSuccessStatusCode)
                    {
                        var albumTracks = JsonSerializer.Deserialize<SpotifyAlbumTracksResponse>(tracksJson);
                        return albumTracks?.Items.Select(t => t.Uri).ToList() ?? new List<string>();
                    }
                }
            }

            // Try loose search if precise search failed and fallback is enabled
            if (_config.FallbackToLooseSearch)
            {
                query = HttpUtility.UrlEncode($"{artistName} {albumName}");
                searchUrl = $"https://api.spotify.com/v1/search?q={query}&type=album&limit=1";

                response = await _httpClient.GetAsync(searchUrl);
                json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var searchResponse = JsonSerializer.Deserialize<SpotifyAlbumSearchResponse>(json);
                    var firstAlbum = searchResponse?.Albums?.Items.FirstOrDefault();

                    if (firstAlbum != null)
                    {
                        // Get album tracks
                        var tracksUrl = $"https://api.spotify.com/v1/albums/{firstAlbum.Id}/tracks";
                        var tracksResponse = await _httpClient.GetAsync(tracksUrl);
                        var tracksJson = await tracksResponse.Content.ReadAsStringAsync();

                        if (tracksResponse.IsSuccessStatusCode)
                        {
                            var albumTracks = JsonSerializer.Deserialize<SpotifyAlbumTracksResponse>(tracksJson);
                            return albumTracks?.Items.Select(t => t.Uri).ToList() ?? new List<string>();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error searching for album: {ex.Message}");
        }

        return new List<string>();
    }

    /// <summary>
    /// Get new album releases from Spotify
    /// NOTE: Spotify's /browse/new-releases API is broken (returns months-old albums)
    /// See: https://community.spotify.com/t5/Spotify-for-Developers/Web-API-Get-New-Releases-API-Returning-Old-Items/m-p/6069709
    /// Keeping implementation for future alternative data source (AOTY, Discogs, etc.)
    /// </summary>
    public async Task<List<SpotifyNewReleaseAlbum>> GetNewReleasesAsync(int limit = 50)
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return new List<SpotifyNewReleaseAlbum>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            // Limit to 1-50 as per Spotify API
            var effectiveLimit = Math.Max(1, Math.Min(50, limit));
            var url = $"https://api.spotify.com/v1/browse/new-releases?limit={effectiveLimit}";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var newReleasesResponse = JsonSerializer.Deserialize<SpotifyNewReleasesResponse>(json);
                return newReleasesResponse?.Albums?.Items ?? new List<SpotifyNewReleaseAlbum>();
            }

            return new List<SpotifyNewReleaseAlbum>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error getting new releases: {ex.Message}");
            return new List<SpotifyNewReleaseAlbum>();
        }
    }
}
