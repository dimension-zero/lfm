namespace Lfm.Data.EF.Provider;

/// <summary>
/// Types of queries that can be executed against Last.fm API via LINQ.
/// Each maps to specific Last.fm API endpoint(s).
/// </summary>
public enum QueryType
{
    /// <summary>
    /// Unknown or unsupported query pattern.
    /// </summary>
    Unknown,

    /// <summary>
    /// Top artists for a user.
    /// Maps to: user.getTopArtists
    /// </summary>
    TopArtists,

    /// <summary>
    /// Top tracks for a user.
    /// Maps to: user.getTopTracks
    /// </summary>
    TopTracks,

    /// <summary>
    /// Top albums for a user.
    /// Maps to: user.getTopAlbums
    /// </summary>
    TopAlbums,

    /// <summary>
    /// Top tracks for a specific artist.
    /// Maps to: artist.getTopTracks
    /// </summary>
    ArtistTracks,

    /// <summary>
    /// Top albums for a specific artist.
    /// Maps to: artist.getTopAlbums
    /// </summary>
    ArtistAlbums,

    /// <summary>
    /// Recent tracks for a user (chronological).
    /// Maps to: user.getRecentTracks
    /// </summary>
    RecentTracks,

    /// <summary>
    /// Similar artists to a given artist.
    /// Maps to: artist.getSimilar
    /// </summary>
    SimilarArtists
}
