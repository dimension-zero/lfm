using Lfm.Framework.Abstractions;

namespace Lfm.Core.Music.Models;

/// <summary>
/// Represents a music domain item (track, artist, or album)
/// Implements IDomainItem for use with the generic framework
/// </summary>
public class MusicItem : IDomainItem
{
    /// <summary>
    /// Type of music item: "track", "artist", or "album"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier (MBID or Last.fm ID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name (track name, artist name, or album name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional image URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// For tracks: artist name
    /// For albums: artist name
    /// For artists: null
    /// </summary>
    public string? ArtistName { get; set; }

    /// <summary>
    /// User's play count for this item
    /// </summary>
    public int PlayCount { get; set; }

    /// <summary>
    /// URL to the item on Last.fm
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// For tracks: album name
    /// For albums: album name
    /// For artists: null
    /// </summary>
    public string? AlbumName { get; set; }

    /// <summary>
    /// Optional ranking (e.g., "#1" in user's top tracks)
    /// </summary>
    public string? Ranking { get; set; }
}
