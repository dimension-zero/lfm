using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Shared.Services;

/// <summary>
/// Abstraction for music listening data sources (Last.fm API, local files, etc.)
/// Provides unified interface for querying top artists, tracks, albums, and recent listening history.
/// </summary>
public interface IMusicDataProvider
{
    /// <summary>
    /// Provider name for logging and display (e.g., "Last.fm API", "Local Files", "Merged")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider supports date range queries
    /// </summary>
    bool SupportsDateRanges { get; }

    /// <summary>
    /// Whether this provider supports similar artist recommendations
    /// </summary>
    bool SupportsSimilarArtists { get; }

    /// <summary>
    /// Whether this provider supports artist/track/album lookup by name
    /// </summary>
    bool SupportsLookup { get; }

    // Core query methods (Result-based for consistent error handling)

    Task<Result<TopArtists>> GetTopArtistsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<Result<TopTracks>> GetTopTracksAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<Result<TopAlbums>> GetTopAlbumsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);

    // Date range methods
    Task<Result<TopArtists>> GetTopArtistsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<Result<TopTracks>> GetTopTracksForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<Result<TopAlbums>> GetTopAlbumsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<Result<RecentTracks>> GetRecentTracksAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1);

    // Artist-specific methods
    Task<Result<TopTracks>> GetArtistTopTracksAsync(string artist, int limit = 10);
    Task<Result<TopAlbums>> GetArtistTopAlbumsAsync(string artist, int limit = 10);
    Task<Result<SimilarArtists>> GetSimilarArtistsAsync(string artist, int limit = 50);
    Task<Result<TopTags>> GetArtistTopTagsAsync(string artist, bool autocorrect = true);

    // Lookup methods (check if user has listened to specific artist/track/album)
    Task<Result<ArtistLookupInfo>> GetArtistInfoAsync(string artist, string username);
    Task<Result<TrackLookupInfo>> GetTrackInfoAsync(string artist, string track, string username);
    Task<Result<AlbumLookupInfo>> GetAlbumInfoAsync(string artist, string album, string username);
}
