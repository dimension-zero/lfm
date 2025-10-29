using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lfm.Data.EF.Entities;

/// <summary>
/// EF Core entity representing a Last.fm artist.
/// Maps to user.getTopArtists API endpoint.
/// </summary>
[Table("Artists")]
public class Artist : LastFmEntity
{
    /// <summary>
    /// Artist name - serves as the primary key since Last.fm API doesn't provide IDs.
    /// Composite key with User to support multi-user scenarios.
    /// </summary>
    [Key]
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Play count as string to match Last.fm API fidelity (avoids int overflow, preserves API format).
    /// Can be parsed to int for sorting/filtering.
    /// </summary>
    [Required]
    public string PlayCount { get; set; } = "0";

    /// <summary>
    /// Rank in top artists list (e.g., "1", "2", "3").
    /// String-based to match API response format.
    /// </summary>
    public string Rank { get; set; } = string.Empty;

    /// <summary>
    /// Last.fm URL for this artist.
    /// Excluded in token optimization but kept in entity for completeness.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// MusicBrainz ID for this artist (if available).
    /// Excluded in token optimization but kept in entity for completeness.
    /// </summary>
    public string Mbid { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property: Tracks by this artist.
    /// Populated via Track.Artist foreign key relationship.
    /// </summary>
    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();

    /// <summary>
    /// Navigation property: Albums by this artist.
    /// Populated via Album.Artist foreign key relationship.
    /// </summary>
    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
}
