using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using Lfm.Core.Attributes;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Sonos.Models;
using Lfm.Spotify.Models;

namespace Lfm.Spotify.Services;

/// <summary>
/// Handles Spotify playback control and playlist management
/// </summary>
public class SpotifyPlaybackService : ISpotifyPlaybackService
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyConfig _config;
    private readonly ISpotifyAuthService _authService;
    private readonly ISpotifySearchService _searchService;
    private bool _playbackWarningShown = false;

    public SpotifyPlaybackService(
        SpotifyConfig config,
        ISpotifyAuthService authService,
        ISpotifySearchService searchService,
        HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<PlaylistStreamResult> QueueTracksAsync(List<Track> tracks, string? device = null)
    {
        var result = new PlaylistStreamResult();

        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                result.Success = false;
                result.Message = "Authentication failed";
                return result;
            }

            Console.WriteLine($"🎵 Queueing {tracks.Count} tracks to Spotify...");

            // Check if playback is active, if not try to start it
            var hasActivePlayback = await EnsurePlaybackActiveAsync(tokenResult.Data!, device);

            var tracksToProcess = tracks.ToList();
            var startedPlayback = false;

            // If no active playback, start playing the first track
            if (!hasActivePlayback && tracksToProcess.Any())
            {
                var firstTrack = tracksToProcess.First();
                var firstSpotifyUri = await SearchSpotifyTrackAsync(firstTrack);

                if (firstSpotifyUri != null)
                {
                    var startSuccess = await StartPlaybackAsync(tokenResult.Data!, new List<string> { firstSpotifyUri }, device);
                    if (startSuccess)
                    {
                        Console.WriteLine($"🎵 Started playing: {firstTrack.Artist.Name} - {firstTrack.Name}");
                        result.TracksFound++;
                        result.TracksProcessed++;
                        startedPlayback = true;
                        tracksToProcess.RemoveAt(0); // Remove the first track since we started playing it
                    }
                }
            }

            if (!hasActivePlayback && !startedPlayback)
            {
                Console.WriteLine("⚠️  Could not start Spotify playback automatically.");
                Console.WriteLine("    Please start playing any song in Spotify and try again.");
                result.Success = false;
                result.Message = "No active Spotify playback device found";
                return result;
            }

            // Queue remaining tracks
            foreach (var track in tracksToProcess)
            {
                var spotifyUri = await SearchSpotifyTrackAsync(track);
                if (spotifyUri != null)
                {
                    var queueSuccess = await AddToQueueAsync(tokenResult.Data!, spotifyUri);
                    if (queueSuccess)
                    {
                        result.TracksFound++;
                        Console.WriteLine($"✅ Queued: {track.Artist.Name} - {track.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to queue: {track.Artist.Name} - {track.Name}");
                    }
                }
                else
                {
                    result.NotFoundTracks.Add($"{track.Artist.Name} - {track.Name}");
                    Console.WriteLine($"🔍 Not found: {track.Artist.Name} - {track.Name}");
                }

                result.TracksProcessed++;

                // Rate limiting
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }
            }

            result.Success = result.TracksFound > 0;
            result.Message = $"Queued {result.TracksFound}/{result.TracksProcessed} tracks";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error queueing tracks: {ex.Message}";
            return result;
        }
    }

    public async Task<PlaylistStreamResult> PlayNowAsync(List<Track> tracks, string? device = null)
    {
        var result = new PlaylistStreamResult();

        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                result.Success = false;
                result.Message = "Authentication failed";
                return result;
            }

            Console.WriteLine($"🎵 Playing {tracks.Count} track(s) on Spotify...");

            if (!tracks.Any())
            {
                result.Success = false;
                result.Message = "No tracks to play";
                return result;
            }

            // Search for first track
            var firstTrack = tracks.First();
            var firstSpotifyUri = await SearchSpotifyTrackAsync(firstTrack);

            if (firstSpotifyUri == null)
            {
                result.NotFoundTracks.Add($"{firstTrack.Artist.Name} - {firstTrack.Name}");
                result.Success = false;
                result.Message = $"Track not found on Spotify: {firstTrack.Artist.Name} - {firstTrack.Name}";
                result.TracksProcessed = 1;
                return result;
            }

            // Start playing first track immediately (interrupts current playback)
            var startSuccess = await StartPlaybackAsync(tokenResult.Data!, new List<string> { firstSpotifyUri }, device);
            if (!startSuccess)
            {
                result.Success = false;
                result.Message = "Failed to start playback";
                result.TracksProcessed = 1;
                return result;
            }

            Console.WriteLine($"▶️  Now playing: {firstTrack.Artist.Name} - {firstTrack.Name}");
            result.TracksFound++;
            result.TracksProcessed++;

            // Queue remaining tracks if there are any
            var remainingTracks = tracks.Skip(1).ToList();
            foreach (var track in remainingTracks)
            {
                var spotifyUri = await SearchSpotifyTrackAsync(track);
                if (spotifyUri != null)
                {
                    var queueSuccess = await AddToQueueAsync(tokenResult.Data!, spotifyUri);
                    if (queueSuccess)
                    {
                        result.TracksFound++;
                        Console.WriteLine($"✅ Queued: {track.Artist.Name} - {track.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to queue: {track.Artist.Name} - {track.Name}");
                    }
                }
                else
                {
                    result.NotFoundTracks.Add($"{track.Artist.Name} - {track.Name}");
                    Console.WriteLine($"🔍 Not found: {track.Artist.Name} - {track.Name}");
                }

                result.TracksProcessed++;

                // Rate limiting
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }
            }

            result.Success = result.TracksFound > 0;
            result.Message = result.TracksFound == 1
                ? $"Now playing: {firstTrack.Artist.Name} - {firstTrack.Name}"
                : $"Now playing {result.TracksFound} tracks";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error playing tracks: {ex.Message}";
            return result;
        }
    }

    public async Task<PlaylistStreamResult> PlayNowFromUrisAsync(List<string> spotifyUris, string? device = null)
    {
        var result = new PlaylistStreamResult();

        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                result.Success = false;
                result.Message = "Authentication failed";
                return result;
            }

            Console.WriteLine($"🎵 Playing {spotifyUris.Count} track(s) on Spotify...");

            if (!spotifyUris.Any())
            {
                result.Success = false;
                result.Message = "No tracks to play";
                return result;
            }

            // Play all tracks in one atomic API call (guaranteed order)
            var startSuccess = await StartPlaybackAsync(tokenResult.Data!, spotifyUris, device);
            if (!startSuccess)
            {
                result.Success = false;
                result.Message = "Failed to start playback";
                return result;
            }

            Console.WriteLine($"▶️  Now playing {spotifyUris.Count} tracks");
            result.TracksFound = spotifyUris.Count;
            result.TracksProcessed = spotifyUris.Count;
            result.Success = true;
            result.Message = $"Now playing {spotifyUris.Count} tracks from album";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error playing album: {ex.Message}";
            return result;
        }
    }

    public async Task<PlaylistStreamResult> QueueFromUrisAsync(List<string> spotifyUris, string? device = null)
    {
        var result = new PlaylistStreamResult();

        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                result.Success = false;
                result.Message = "Authentication failed";
                return result;
            }

            Console.WriteLine($"📋 Queueing {spotifyUris.Count} track(s) on Spotify...");

            if (!spotifyUris.Any())
            {
                result.Success = false;
                result.Message = "No tracks to queue";
                return result;
            }

            // Ensure device is active
            await EnsurePlaybackActiveAsync(tokenResult.Data!, device);

            // Queue all tracks
            foreach (var uri in spotifyUris)
            {
                var queueSuccess = await AddToQueueAsync(tokenResult.Data!, uri);
                if (queueSuccess)
                {
                    result.TracksFound++;
                    Console.WriteLine($"✅ Queued track");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to queue track");
                }

                result.TracksProcessed++;

                // Rate limiting
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }
            }

            result.Success = result.TracksFound > 0;
            result.Message = $"Queued {result.TracksFound} tracks from album";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error queueing album: {ex.Message}";
            return result;
        }
    }

    public async Task<List<PlaylistInfo>> GetUserPlaylistsAsync()
    {
        var playlists = new List<PlaylistInfo>();

        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return playlists;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            // Get current user ID once
            var currentUserId = await GetCurrentUserIdAsync(tokenResult.Data!);

            var url = "https://api.spotify.com/v1/me/playlists?limit=50";

            while (url != null)
            {
                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var playlistResponse = JsonSerializer.Deserialize<SpotifyPlaylistsResponse>(json);
                    if (playlistResponse?.Items != null)
                    {
                        foreach (var playlist in playlistResponse.Items)
                        {
                            playlists.Add(new PlaylistInfo
                            {
                                Id = playlist.Id,
                                Name = playlist.Name,
                                TracksCount = playlist.Tracks.Total,
                                IsOwned = playlist.Owner.Id == currentUserId
                            });
                        }
                    }

                    url = playlistResponse?.Next; // Next page URL
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting playlists: {ex.Message}");
        }

        return playlists;
    }

    public async Task<bool> DeletePlaylistAsync(string playlistId)
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            var response = await _httpClient.DeleteAsync($"https://api.spotify.com/v1/playlists/{playlistId}/followers");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<SpotifyDevice>> GetDevicesAsync()
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return new List<SpotifyDevice>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me/player/devices");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var devices = JsonSerializer.Deserialize<SpotifyDevicesResponse>(json);
                return devices?.Devices ?? new List<SpotifyDevice>();
            }

            return new List<SpotifyDevice>();
        }
        catch
        {
            return new List<SpotifyDevice>();
        }
    }

    public async Task<PlaylistStreamResult> SavePlaylistAsync(List<Track> tracks, string playlistName, string? device = null)
    {
        var result = new PlaylistStreamResult();

        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                result.Success = false;
                result.Message = "Authentication failed";
                return result;
            }

            Console.WriteLine($"🎵 Creating Spotify playlist '{playlistName}' with {tracks.Count} tracks...");

            // Get user ID
            var userId = await GetCurrentUserIdAsync(tokenResult.Data!);
            if (string.IsNullOrEmpty(userId))
            {
                result.Message = "Failed to get Spotify user ID";
                return result;
            }

            // Create playlist
            var playlist = await CreatePlaylistAsync(tokenResult.Data!, userId, playlistName);
            if (playlist == null)
            {
                result.Message = "Failed to create Spotify playlist";
                return result;
            }

            // Search for tracks and collect URIs
            var spotifyUris = new List<string>();
            foreach (var track in tracks)
            {
                var spotifyUri = await SearchSpotifyTrackAsync(track);
                if (spotifyUri != null)
                {
                    spotifyUris.Add(spotifyUri);
                    result.TracksFound++;
                    Console.WriteLine($"✅ Found: {track.Artist.Name} - {track.Name}");
                }
                else
                {
                    result.NotFoundTracks.Add($"{track.Artist.Name} - {track.Name}");
                    Console.WriteLine($"🔍 Not found: {track.Artist.Name} - {track.Name}");
                }

                result.TracksProcessed++;

                // Rate limiting
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }
            }

            // Add tracks to playlist
            if (spotifyUris.Any())
            {
                await AddTracksToPlaylistAsync(tokenResult.Data!, playlist.Id, spotifyUris);
            }

            result.Success = result.TracksFound > 0;
            result.Message = $"Created playlist '{playlistName}' with {result.TracksFound}/{result.TracksProcessed} tracks";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error creating playlist: {ex.Message}";
            return result;
        }
    }

    public async Task<bool> ActivateDeviceAsync(string? deviceName = null)
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            // Get available devices
            var devices = await GetDevicesAsync();
            if (!devices.Any())
            {
                Console.WriteLine("⚠️  No Spotify devices found. Please open Spotify on a device first.");
                return false;
            }

            // Device selection priority: CLI parameter > config default > active device > smart prioritization
            SpotifyDevice? targetDevice = null;

            // 1. Check if specific device was requested
            if (!string.IsNullOrEmpty(deviceName))
            {
                targetDevice = devices.FirstOrDefault(d =>
                    d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));

                if (targetDevice == null)
                {
                    Console.WriteLine($"⚠️  Device '{deviceName}' not found.");
                    Console.WriteLine($"Available devices: {string.Join(", ", devices.Select(d => d.Name))}");
                    return false;
                }
            }

            // 2. Check config default device
            if (targetDevice == null && !string.IsNullOrEmpty(_config.DefaultDevice))
            {
                targetDevice = devices.FirstOrDefault(d =>
                    d.Name.Equals(_config.DefaultDevice, StringComparison.OrdinalIgnoreCase));

                if (targetDevice != null)
                {
                    Console.WriteLine($"📱 Using config default device: {targetDevice.Name}");
                }
            }

            // 3. Check for currently active device
            if (targetDevice == null)
            {
                targetDevice = devices.FirstOrDefault(d => d.IsActive);
            }

            // 4. Smart prioritization: Computer > Smartphone > Speaker > other
            if (targetDevice == null)
            {
                targetDevice = devices.FirstOrDefault(d => d.Type.Equals("Computer", StringComparison.OrdinalIgnoreCase))
                            ?? devices.FirstOrDefault(d => d.Type.Equals("Smartphone", StringComparison.OrdinalIgnoreCase))
                            ?? devices.FirstOrDefault(d => d.Type.Equals("Speaker", StringComparison.OrdinalIgnoreCase))
                            ?? devices.First();
            }

            // If device is already active, no need to transfer
            if (targetDevice.IsActive)
            {
                Console.WriteLine($"✅ Device '{targetDevice.Name}' is already active");
                return true;
            }

            // Transfer playback to the device (activates it without playing)
            var transferRequest = new
            {
                device_ids = new[] { targetDevice.Id },
                play = false  // Don't start playing, just activate
            };

            var json = JsonSerializer.Serialize(transferRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("https://api.spotify.com/v1/me/player", content);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine($"✅ Activated device '{targetDevice.Name}'");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"⚠️  Failed to activate device: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error activating device: {ex.Message}");
            return false;
        }
    }

    [SuppressMessage("SilentFailure", "SF001", Justification = "External Spotify API integration - null indicates no track currently playing or API unavailable")]
    public async Task<CurrentTrackInfo?> GetCurrentlyPlayingAsync()
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me/player");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(json))
                {
                    // No playback currently active
                    return null;
                }

                var playbackState = JsonSerializer.Deserialize<SpotifyPlaybackState>(json);

                if (playbackState?.Item == null)
                {
                    return null;
                }

                return new CurrentTrackInfo
                {
                    TrackName = playbackState.Item.Name,
                    ArtistName = playbackState.Item.Artists?.FirstOrDefault()?.Name ?? "Unknown Artist",
                    AlbumName = playbackState.Item.Album?.Name ?? "Unknown Album",
                    ProgressMs = playbackState.ProgressMs,
                    DurationMs = playbackState.Item.DurationMs,
                    IsPlaying = playbackState.IsPlaying,
                    DeviceName = playbackState.Device?.Name ?? "Unknown Device",
                    DeviceType = playbackState.Device?.Type ?? "Unknown"
                };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> PauseAsync()
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            var response = await _httpClient.PutAsync("https://api.spotify.com/v1/me/player/pause", null);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ResumeAsync()
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            var response = await _httpClient.PutAsync("https://api.spotify.com/v1/me/player/play", null);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SkipAsync(SkipDirection direction = SkipDirection.Next)
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Data);

            var endpoint = direction == SkipDirection.Next
                ? "https://api.spotify.com/v1/me/player/next"
                : "https://api.spotify.com/v1/me/player/previous";

            var response = await _httpClient.PostAsync(endpoint, null);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch
        {
            return false;
        }
    }

    // Private helper methods

    [SuppressMessage("SilentFailure", "SF001", Justification = "External Spotify API integration - null indicates track not found in Spotify catalog")]
    private async Task<string?> SearchSpotifyTrackAsync(Track track, string? albumName = null)
    {
        var result = await _searchService.SearchTrackWithDetailsAsync(track, albumName);
        return result.SpotifyUri;
    }

    private async Task<bool> AddToQueueAsync(string accessToken, string trackUri)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var queueUrl = $"https://api.spotify.com/v1/me/player/queue?uri={HttpUtility.UrlEncode(trackUri)}";
        var response = await _httpClient.PostAsync(queueUrl, null);

        if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // First track, provide helpful message
            if (!_playbackWarningShown)
            {
                Console.WriteLine("⚠️  No active Spotify playback detected. Please:");
                Console.WriteLine("    1. Open Spotify on your device");
                Console.WriteLine("    2. Start playing any song");
                Console.WriteLine("    3. Try the command again");
                _playbackWarningShown = true;
            }
        }

        return response.IsSuccessStatusCode;
    }

    private async Task<bool> StartPlaybackAsync(string accessToken, List<string> trackUris, string? device = null)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            // Prepare the play request with multiple URIs (supports up to 100 tracks)
            var playRequest = new
            {
                uris = trackUris
            };

            // If a specific device is requested, include it
            string playUrl = "https://api.spotify.com/v1/me/player/play";
            if (!string.IsNullOrEmpty(device))
            {
                // First try to get the device ID
                var devices = await GetDevicesAsync();
                var targetDevice = devices.FirstOrDefault(d =>
                    d.Name.Equals(device, StringComparison.OrdinalIgnoreCase));

                if (targetDevice != null)
                {
                    playUrl += $"?device_id={targetDevice.Id}";
                }
            }

            var json = JsonSerializer.Serialize(playRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(playUrl, content);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"⚠️  Failed to start playback: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error starting playback: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> EnsurePlaybackActiveAsync(string accessToken, string? device = null)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // First check if playback is already active
        var playbackResponse = await _httpClient.GetAsync("https://api.spotify.com/v1/me/player");

        if (playbackResponse.IsSuccessStatusCode)
        {
            var json = await playbackResponse.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(json))
            {
                // Playback is active
                return true;
            }
        }

        // No active playback, try to get available devices
        var devicesResponse = await _httpClient.GetAsync("https://api.spotify.com/v1/me/player/devices");
        if (!devicesResponse.IsSuccessStatusCode)
        {
            return false;
        }

        var devicesJson = await devicesResponse.Content.ReadAsStringAsync();
        var devices = JsonSerializer.Deserialize<SpotifyDevicesResponse>(devicesJson);

        if (devices?.Devices == null || !devices.Devices.Any())
        {
            Console.WriteLine("ℹ️  No Spotify devices found. Please open Spotify on any device.");
            return false;
        }

        // Show all available devices
        if (devices.Devices.Count > 1)
        {
            Console.WriteLine($"📱 Found {devices.Devices.Count} Spotify devices:");
            foreach (var dev in devices.Devices)
            {
                var status = dev.IsActive ? " (active)" : "";
                Console.WriteLine($"    • {dev.Name} ({dev.Type}){status}");
            }
        }

        // Device selection priority: CLI parameter > config default > active device > smart prioritization
        SpotifyDevice? selectedDevice = null;

        // 1. Check if specific device was requested via CLI parameter
        if (!string.IsNullOrEmpty(device))
        {
            selectedDevice = devices.Devices.FirstOrDefault(d =>
                d.Name.Equals(device, StringComparison.OrdinalIgnoreCase));

            if (selectedDevice == null)
            {
                Console.WriteLine($"⚠️  Requested device '{device}' not found. Available devices:");
                foreach (var dev in devices.Devices)
                {
                    Console.WriteLine($"    • {dev.Name}");
                }
                // Continue with fallback logic
            }
        }

        // 2. Check config default device if no CLI device or CLI device not found
        if (selectedDevice == null && !string.IsNullOrEmpty(_config.DefaultDevice))
        {
            selectedDevice = devices.Devices.FirstOrDefault(d =>
                d.Name.Equals(_config.DefaultDevice, StringComparison.OrdinalIgnoreCase));

            if (selectedDevice != null)
            {
                Console.WriteLine($"📱 Using config default device: {selectedDevice.Name}");
            }
        }

        // 3. Check for currently active device
        if (selectedDevice == null)
        {
            selectedDevice = devices.Devices.FirstOrDefault(d => d.IsActive);
        }

        // 4. Smart prioritization: Computer > Smartphone > Speaker > other
        if (selectedDevice == null)
        {
            selectedDevice = devices.Devices.FirstOrDefault(d => d.Type.Equals("Computer", StringComparison.OrdinalIgnoreCase))
                        ?? devices.Devices.FirstOrDefault(d => d.Type.Equals("Smartphone", StringComparison.OrdinalIgnoreCase))
                        ?? devices.Devices.FirstOrDefault(d => d.Type.Equals("Speaker", StringComparison.OrdinalIgnoreCase))
                        ?? devices.Devices.First();
        }

        var activeDevice = selectedDevice;

        Console.WriteLine($"🎵 Using device: {activeDevice.Name} ({activeDevice.Type})");

        // Try to start playback on the device
        if (!activeDevice.IsActive)
        {
            // Transfer playback to this device
            var transferRequest = new
            {
                device_ids = new[] { activeDevice.Id },
                play = false  // Don't auto-play
            };

            var transferJson = JsonSerializer.Serialize(transferRequest);
            var transferContent = new StringContent(transferJson, Encoding.UTF8, "application/json");

            var transferResponse = await _httpClient.PutAsync("https://api.spotify.com/v1/me/player", transferContent);

            if (transferResponse.IsSuccessStatusCode || transferResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine($"✅ Activated device: {activeDevice.Name}");
                await Task.Delay(1000); // Give Spotify a moment to activate the device
                return true;
            }
        }

        return activeDevice.IsActive;
    }

    [SuppressMessage("SilentFailure", "SF001", Justification = "External Spotify API integration - null indicates user profile unavailable")]
    private async Task<string?> GetCurrentUserIdAsync(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me");
        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var user = JsonSerializer.Deserialize<SpotifyUser>(json);
            return user?.Id;
        }

        return null;
    }

    [SuppressMessage("SilentFailure", "SF001", Justification = "External Spotify API integration - null indicates playlist creation failed")]
    private async Task<SpotifyPlaylist?> CreatePlaylistAsync(string accessToken, string userId, string playlistName)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreatePlaylistRequest
        {
            Name = playlistName,
            Description = "Created by lfm CLI tool",
            Public = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<SpotifyPlaylist>(responseJson);
        }

        return null;
    }

    private async Task<bool> AddTracksToPlaylistAsync(string accessToken, string playlistId, List<string> trackUris)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new AddTracksRequest { Uris = trackUris };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", content);
        return response.IsSuccessStatusCode;
    }
}
