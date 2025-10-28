using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Core.Services.LocalFiles;

/// <summary>
/// Interface for parsing local music history files from various sources.
/// </summary>
public interface ILocalFileParser
{
    /// <summary>
    /// Determines if this parser can handle the given file.
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if this parser supports the file format</returns>
    bool CanParse(string filePath);

    /// <summary>
    /// Parses the file and returns normalized play events.
    /// </summary>
    /// <param name="filePath">Path to the file to parse</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>Result containing list of play events</returns>
    Task<Result<List<PlayEvent>>> ParseAsync(string filePath, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets the data source name for this parser.
    /// </summary>
    string DataSource { get; }
}

/// <summary>
/// Normalized play event from any data source.
/// </summary>
public class PlayEvent
{
    public required string Artist { get; init; }
    public required string Track { get; init; }
    public string? Album { get; init; }
    public required DateTime PlayedAt { get; init; }
    public required string DataSource { get; init; }

    /// <summary>
    /// Duration in milliseconds (if available).
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// Indicates if this was a full play or partial (e.g., skipped).
    /// For Spotify Extended History, this is based on ms_played >= 30000.
    /// </summary>
    public bool IsFullPlay { get; init; }
}

/// <summary>
/// Simple artist model for aggregation results.
/// </summary>
public class LocalArtistInfo
{
    public required string Name { get; init; }
    public int PlayCount { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Mbid { get; init; } = string.Empty;
    public bool Streamable { get; init; }
}

/// <summary>
/// Simple track model for aggregation results.
/// </summary>
public class LocalTrackInfo
{
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public int PlayCount { get; init; }
    public string? AlbumName { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Mbid { get; init; } = string.Empty;
    public bool Streamable { get; init; }
}

/// <summary>
/// Simple album model for aggregation results.
/// </summary>
public class LocalAlbumInfo
{
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public int PlayCount { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Mbid { get; init; } = string.Empty;
}
