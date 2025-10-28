namespace Lfm.Shared.Configuration;

/// <summary>
/// Last.fm time period values - provides compiler safety via enum
/// </summary>
public enum LastFmPeriod
{
    Overall,
    SevenDay,
    OneMonth,
    ThreeMonth,
    SixMonth,
    TwelveMonth
}

/// <summary>
/// Extension methods for LastFmPeriod enum
/// </summary>
public static class LastFmPeriodExtensions
{
    /// <summary>
    /// Converts LastFmPeriod enum to Last.fm API string format
    /// </summary>
    public static string ToApiString(this LastFmPeriod period) => period switch
    {
        LastFmPeriod.Overall => "overall",
        LastFmPeriod.SevenDay => "7day",
        LastFmPeriod.OneMonth => "1month",
        LastFmPeriod.ThreeMonth => "3month",
        LastFmPeriod.SixMonth => "6month",
        LastFmPeriod.TwelveMonth => "12month",
        _ => "overall"
    };

    /// <summary>
    /// Parses a period string (from CLI or user input) to LastFmPeriod enum
    /// </summary>
    /// <param name="periodString">Period string (e.g., "overall", "7day", "12month")</param>
    /// <returns>Corresponding LastFmPeriod enum value</returns>
    /// <exception cref="ArgumentException">Thrown when period string is invalid</exception>
    public static LastFmPeriod ParsePeriod(string? periodString)
    {
        if (string.IsNullOrWhiteSpace(periodString))
            return LastFmPeriod.Overall;

        return periodString.ToLowerInvariant() switch
        {
            "overall" => LastFmPeriod.Overall,
            "7day" => LastFmPeriod.SevenDay,
            "1month" => LastFmPeriod.OneMonth,
            "3month" => LastFmPeriod.ThreeMonth,
            "6month" => LastFmPeriod.SixMonth,
            "12month" => LastFmPeriod.TwelveMonth,
            _ => throw new ArgumentException(
                $"Invalid period '{periodString}'. Valid values: overall, 7day, 1month, 3month, 6month, 12month",
                nameof(periodString))
        };
    }
}

/// <summary>
/// Centralized constants for search operations and pagination
/// </summary>
public static class SearchConstants
{
    /// <summary>
    /// Last.fm API pagination settings
    /// </summary>
    public static class Api
    {
        /// <summary>
        /// Maximum items per page supported by Last.fm API
        /// </summary>
        public const int MaxItemsPerPage = 50;
        
        /// <summary>
        /// Recommended page size for efficient API calls
        /// </summary>
        public const int RecommendedPageSize = 50;
    }
    
    /// <summary>
    /// Search configuration for artist filtering
    /// </summary>
    public static class ArtistSearch
    {
        /// <summary>
        /// Progress update interval (show progress every N pages)
        /// </summary>
        public const int ProgressUpdateInterval = 10;
        
        /// <summary>
        /// Early termination multiplier for normal search
        /// (stop when found results >= limit * this value)
        /// </summary>
        public const int EarlyTerminationMultiplier = 3;
    }
    
    /// <summary>
    /// Range query pagination settings
    /// </summary>
    public static class RangeQuery
    {
        /// <summary>
        /// Maximum items per page for range queries
        /// </summary>
        public const int MaxItemsPerPage = 50;
    }
    
    /// <summary>
    /// Default values for commands
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Default number of items to display
        /// </summary>
        public const int ItemLimit = 10;
        
        /// <summary>
        /// Default time period for user stats
        /// </summary>
        public const string TimePeriod = "overall";
    }
    
    /// <summary>
    /// Display formatting constants
    /// </summary>
    public static class Display
    {
        /// <summary>
        /// Maximum length for artist names before truncation
        /// </summary>
        public const int ArtistNameMaxLength = 40;

        /// <summary>
        /// Maximum length for track names before truncation
        /// </summary>
        public const int TrackNameMaxLength = 40;

        /// <summary>
        /// Maximum length for album names before truncation
        /// </summary>
        public const int AlbumNameMaxLength = 40;

        /// <summary>
        /// Maximum length for secondary artist names before truncation
        /// </summary>
        public const int SecondaryArtistNameMaxLength = 30;

        /// <summary>
        /// Characters to append when truncating strings
        /// </summary>
        public const string TruncationSuffix = "...";

        /// <summary>
        /// Length of truncation suffix
        /// </summary>
        public const int TruncationSuffixLength = 3;
    }

    /// <summary>
    /// Last.fm API method names - centralized to prevent typos and provide compile-time safety
    /// </summary>
    public static class LastFmApiMethods
    {
        /// <summary>
        /// Get top artists for a user
        /// </summary>
        public const string UserGetTopArtists = "user.getTopArtists";

        /// <summary>
        /// Get top tracks for a user
        /// </summary>
        public const string UserGetTopTracks = "user.getTopTracks";

        /// <summary>
        /// Get top albums for a user
        /// </summary>
        public const string UserGetTopAlbums = "user.getTopAlbums";

        /// <summary>
        /// Get recent tracks for a user
        /// </summary>
        public const string UserGetRecentTracks = "user.getRecentTracks";

        /// <summary>
        /// Get top tracks for an artist
        /// </summary>
        public const string ArtistGetTopTracks = "artist.getTopTracks";

        /// <summary>
        /// Get top albums for an artist
        /// </summary>
        public const string ArtistGetTopAlbums = "artist.getTopAlbums";

        /// <summary>
        /// Get similar artists
        /// </summary>
        public const string ArtistGetSimilar = "artist.getSimilar";

        /// <summary>
        /// Get top tags for an artist
        /// </summary>
        public const string ArtistGetTopTags = "artist.getTopTags";

        /// <summary>
        /// Get artist information
        /// </summary>
        public const string ArtistGetInfo = "artist.getInfo";

        /// <summary>
        /// Get track information
        /// </summary>
        public const string TrackGetInfo = "track.getInfo";

        /// <summary>
        /// Get album information
        /// </summary>
        public const string AlbumGetInfo = "album.getInfo";
    }
}