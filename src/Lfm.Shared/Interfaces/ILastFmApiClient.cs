using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Shared.Interfaces;

/// <summary>
/// Interface for Last.fm API client operations.
/// Defines the contract for accessing Last.fm data either directly via API or through other providers.
/// </summary>
public interface ILastFmApiClient
{
    // Legacy nullable methods (maintained for compatibility)
    Task<TopArtists?> GetTopArtistsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<TopTracks?> GetTopTracksAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<TopAlbums?> GetTopAlbumsAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<TopTracks?> GetArtistTopTracksAsync(string artist, int limit = 10);
    Task<TopAlbums?> GetArtistTopAlbumsAsync(string artist, int limit = 10);
    Task<SimilarArtists?> GetSimilarArtistsAsync(string artist, int limit = 50);
    Task<TopTags?> GetArtistTopTagsAsync(string artist, bool autocorrect = true);

    // Date range methods
    Task<RecentTracks?> GetRecentTracksAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1);
    Task<TopArtists?> GetTopArtistsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<TopTracks?> GetTopTracksForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<TopAlbums?> GetTopAlbumsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10);

    // New Result-based methods for better error handling
    Task<Result<TopArtists>> GetTopArtistsWithResultAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<Result<TopTracks>> GetTopTracksWithResultAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<Result<TopAlbums>> GetTopAlbumsWithResultAsync(string username, LastFmPeriod period = LastFmPeriod.Overall, int limit = 10, int page = 1);
    Task<Result<TopTracks>> GetArtistTopTracksWithResultAsync(string artist, int limit = 10);
    Task<Result<TopAlbums>> GetArtistTopAlbumsWithResultAsync(string artist, int limit = 10);
    Task<Result<SimilarArtists>> GetSimilarArtistsWithResultAsync(string artist, int limit = 50);
    Task<Result<TopTags>> GetArtistTopTagsWithResultAsync(string artist, bool autocorrect = true);

    // Result-based date range methods
    Task<Result<RecentTracks>> GetRecentTracksWithResultAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1);
    Task<Result<TopArtists>> GetTopArtistsForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<Result<TopTracks>> GetTopTracksForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10);
    Task<Result<TopAlbums>> GetTopAlbumsForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10);

    // Lookup methods for checking user's listening history
    Task<ArtistLookupInfo?> GetArtistInfoAsync(string artist, string username);
    Task<TrackLookupInfo?> GetTrackInfoAsync(string artist, string track, string username);
    Task<AlbumLookupInfo?> GetAlbumInfoAsync(string artist, string album, string username);
    Task<Result<ArtistLookupInfo>> GetArtistInfoWithResultAsync(string artist, string username);
    Task<Result<TrackLookupInfo>> GetTrackInfoWithResultAsync(string artist, string track, string username);
    Task<Result<AlbumLookupInfo>> GetAlbumInfoWithResultAsync(string artist, string album, string username);
}
