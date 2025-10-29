using Lfm.Shared.Models.Results;

namespace Lfm.Data.Direct.Enrichment;

/// <summary>
/// Interface for album metadata enrichment services.
/// Implementations query external APIs to find album information for tracks.
/// </summary>
public interface IAlbumEnricher
{
    /// <summary>
    /// Name of this enricher (e.g., "LastFm", "Spotify", "MusicBrainz")
    /// Used for configuration and logging.
    /// </summary>
    string EnricherName { get; }

    /// <summary>
    /// Queries the enrichment source to find album information.
    /// </summary>
    /// <param name="artist">Artist name</param>
    /// <param name="track">Track name</param>
    /// <returns>Album name if found, null if not found, or error result</returns>
    Task<Result<string?>> GetAlbumAsync(string artist, string track);
}

/// <summary>
/// Result from album enrichment containing album name and metadata about the enrichment.
/// </summary>
public class EnrichmentResult
{
    public string? Album { get; init; }
    public string EnricherUsed { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public bool FromCache { get; init; }
}
