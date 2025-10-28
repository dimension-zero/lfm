using Lfm.Shared.Models;
using Lfm.Sonos.Models;
using Lfm.Spotify.Models;

namespace Lfm.Spotify.Services;

/// <summary>
/// Service for Spotify playback control and playlist management
/// </summary>
public interface ISpotifyPlaybackService
{
    /// <summary>
    /// Queue tracks to Spotify
    /// </summary>
    Task<PlaylistStreamResult> QueueTracksAsync(List<Track> tracks, string? device = null);

    /// <summary>
    /// Play tracks immediately (interrupts current playback)
    /// </summary>
    Task<PlaylistStreamResult> PlayNowAsync(List<Track> tracks, string? device = null);

    /// <summary>
    /// Play Spotify URIs immediately (for album playback)
    /// </summary>
    Task<PlaylistStreamResult> PlayNowFromUrisAsync(List<string> spotifyUris, string? device = null);

    /// <summary>
    /// Queue Spotify URIs (for album queueing)
    /// </summary>
    Task<PlaylistStreamResult> QueueFromUrisAsync(List<string> spotifyUris, string? device = null);

    /// <summary>
    /// Get user's playlists
    /// </summary>
    Task<List<PlaylistInfo>> GetUserPlaylistsAsync();

    /// <summary>
    /// Delete a playlist
    /// </summary>
    Task<bool> DeletePlaylistAsync(string playlistId);

    /// <summary>
    /// Get available Spotify devices
    /// </summary>
    Task<List<SpotifyDevice>> GetDevicesAsync();

    /// <summary>
    /// Create a new playlist with tracks
    /// </summary>
    Task<PlaylistStreamResult> SavePlaylistAsync(List<Track> tracks, string playlistName, string? device = null);

    /// <summary>
    /// Activate a Spotify device for playback
    /// </summary>
    Task<bool> ActivateDeviceAsync(string? deviceName = null);

    /// <summary>
    /// Get currently playing track information
    /// </summary>
    Task<CurrentTrackInfo?> GetCurrentlyPlayingAsync();

    /// <summary>
    /// Pause current playback
    /// </summary>
    Task<bool> PauseAsync();

    /// <summary>
    /// Resume paused playback
    /// </summary>
    Task<bool> ResumeAsync();

    /// <summary>
    /// Skip to next or previous track
    /// </summary>
    Task<bool> SkipAsync(SkipDirection direction = SkipDirection.Next);
}
