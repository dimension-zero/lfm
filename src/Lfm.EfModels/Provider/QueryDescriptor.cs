using Lfm.Shared.Configuration;

namespace Lfm.EfModels.Provider;

/// <summary>
/// Describes a parsed LINQ query ready for translation to Last.fm API call.
/// Extracted from expression tree by LastFmExpressionVisitor.
/// </summary>
public class QueryDescriptor
{
    /// <summary>
    /// Type of query (determines API endpoint).
    /// </summary>
    public QueryType QueryType { get; set; } = QueryType.Unknown;

    /// <summary>
    /// Last.fm username (required for user-based queries).
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Artist name (required for artist-based queries).
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Time period for top queries (overall, 7day, 1month, etc.).
    /// </summary>
    public LastFmPeriod? Period { get; set; }

    /// <summary>
    /// Start date for date range queries (recent tracks).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for date range queries (recent tracks).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Maximum number of results to return (LINQ Take).
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of results to skip (LINQ Skip).
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Field to sort by (e.g., "PlayCount", "Rank").
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction ("Ascending" or "Descending").
    /// </summary>
    public string? SortDirection { get; set; }

    /// <summary>
    /// Navigation properties to include (LINQ Include).
    /// E.g., "Artist" for Track.Artist, "Tracks" for Artist.Tracks.
    /// </summary>
    public HashSet<string> Includes { get; set; } = new();

    /// <summary>
    /// Whether to execute and return first result only (LINQ First/FirstOrDefault).
    /// </summary>
    public bool IsFirstQuery { get; set; }

    /// <summary>
    /// Whether to execute and return single result (LINQ Single/SingleOrDefault).
    /// </summary>
    public bool IsSingleQuery { get; set; }

    /// <summary>
    /// Whether to count results instead of returning data (LINQ Count).
    /// </summary>
    public bool IsCountQuery { get; set; }

    /// <summary>
    /// Additional filters that couldn't be translated to API parameters.
    /// These will be applied client-side after fetching data.
    /// </summary>
    public List<string> ClientSideFilters { get; set; } = new();
}
