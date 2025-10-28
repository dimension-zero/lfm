using Lfm.Shared.Models;
using Lfm.Spotify.Models;

namespace Lfm.Spotify.Services;

/// <summary>
/// Service for searching Spotify catalog
/// </summary>
public interface ISpotifySearchService
{
    /// <summary>
    /// Search for a track on Spotify with detailed results
    /// </summary>
    Task<TrackSearchResult> SearchTrackWithDetailsAsync(Track track, string? albumName = null);

    /// <summary>
    /// Search for an album URI on Spotify
    /// </summary>
    Task<string?> SearchAlbumUriAsync(string artistName, string albumName);

    /// <summary>
    /// Search for all tracks in an album
    /// </summary>
    Task<List<string>> SearchAlbumTracksAsync(string artistName, string albumName);

    /// <summary>
    /// Get new album releases from Spotify
    /// </summary>
    Task<List<SpotifyNewReleaseAlbum>> GetNewReleasesAsync(int limit = 50);
}
