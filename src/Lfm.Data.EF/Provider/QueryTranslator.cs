using Lfm.Shared.Configuration;

namespace Lfm.Data.EF.Provider;

/// <summary>
/// Translates QueryDescriptor (parsed LINQ expression) to Last.fm API method call.
/// Determines which API endpoint to call and what parameters to pass.
///
/// Translation Rules:
/// - Artists.Where(a => a.User == "X") → user.getTopArtists(user: X)
/// - Tracks.Where(t => t.User == "X") → user.getTopTracks(user: X)
/// - Tracks.Where(t => t.ArtistName == "X") → artist.getTopTracks(artist: X)
/// - Albums.Where(a => a.ArtistName == "X") → artist.getTopAlbums(artist: X)
/// - RecentTracks.Where(r => r.User == "X") → user.getRecentTracks(user: X)
/// </summary>
public class QueryTranslator
{
    private readonly string _defaultUser;

    public QueryTranslator(string defaultUser)
    {
        _defaultUser = defaultUser ?? throw new ArgumentNullException(nameof(defaultUser));
    }

    /// <summary>
    /// Translate query descriptor to API call parameters.
    /// </summary>
    public ApiCallInfo Translate(QueryDescriptor descriptor)
    {
        System.Diagnostics.Debug.WriteLine($"[QueryTranslator] QueryType={descriptor.QueryType}, Artist={descriptor.Artist}, User={descriptor.User}, Limit={descriptor.Limit}");

        var user = descriptor.User ?? _defaultUser;
        var limit = descriptor.Limit ?? 10;
        var page = CalculatePage(descriptor.Skip, limit);

        return descriptor.QueryType switch
        {
            QueryType.TopArtists => new ApiCallInfo
            {
                QueryType = QueryType.TopArtists,
                User = user,
                Period = descriptor.Period ?? LastFmPeriod.Overall,
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.TopTracks when string.IsNullOrEmpty(descriptor.Artist) => new ApiCallInfo
            {
                QueryType = QueryType.TopTracks,
                User = user,
                Period = descriptor.Period ?? LastFmPeriod.Overall,
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.TopTracks when !string.IsNullOrEmpty(descriptor.Artist) => new ApiCallInfo
            {
                QueryType = QueryType.ArtistTracks,
                Artist = descriptor.Artist,
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.TopAlbums when string.IsNullOrEmpty(descriptor.Artist) => new ApiCallInfo
            {
                QueryType = QueryType.TopAlbums,
                User = user,
                Period = descriptor.Period ?? LastFmPeriod.Overall,
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.TopAlbums when !string.IsNullOrEmpty(descriptor.Artist) => new ApiCallInfo
            {
                QueryType = QueryType.ArtistAlbums,
                Artist = descriptor.Artist,
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.ArtistTracks => new ApiCallInfo
            {
                QueryType = QueryType.ArtistTracks,
                Artist = descriptor.Artist ?? throw new InvalidOperationException("Artist name required for artist tracks query"),
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.ArtistAlbums => new ApiCallInfo
            {
                QueryType = QueryType.ArtistAlbums,
                Artist = descriptor.Artist ?? throw new InvalidOperationException("Artist name required for artist albums query"),
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.RecentTracks => new ApiCallInfo
            {
                QueryType = QueryType.RecentTracks,
                User = user,
                FromDate = descriptor.FromDate,
                ToDate = descriptor.ToDate,
                Limit = limit,
                Page = page,
                IncludeNavigations = descriptor.Includes
            },

            QueryType.SimilarArtists => new ApiCallInfo
            {
                QueryType = QueryType.SimilarArtists,
                Artist = descriptor.Artist ?? throw new InvalidOperationException("Artist name required for similar artists query"),
                Limit = limit,
                IncludeNavigations = descriptor.Includes
            },

            _ => throw new NotSupportedException($"Query type '{descriptor.QueryType}' not supported or could not be determined from LINQ query")
        };
    }

    /// <summary>
    /// Calculate page number from skip and limit.
    /// Last.fm uses 1-based page numbers.
    /// </summary>
    private static int CalculatePage(int? skip, int limit)
    {
        if (!skip.HasValue || skip.Value == 0)
            return 1;

        // Validate that skip is a multiple of limit
        if (skip.Value % limit != 0)
        {
            throw new InvalidOperationException(
                $"Skip ({skip.Value}) must be a multiple of Take ({limit}) for Last.fm API pagination. " +
                $"Use Skip({skip.Value / limit * limit}) instead.");
        }

        return (skip.Value / limit) + 1;
    }
}

/// <summary>
/// Information about an API call to be executed.
/// Output of QueryTranslator, input to ResultMapper.
/// </summary>
public class ApiCallInfo
{
    /// <summary>
    /// Type of query (determines which API method to call).
    /// </summary>
    public QueryType QueryType { get; set; }

    /// <summary>
    /// Username for user-based queries.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Artist name for artist-based queries.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Time period for top queries.
    /// </summary>
    public LastFmPeriod? Period { get; set; }

    /// <summary>
    /// Start date for date range queries.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for date range queries.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Navigation properties to load (Include).
    /// </summary>
    public HashSet<string> IncludeNavigations { get; set; } = new();
}
