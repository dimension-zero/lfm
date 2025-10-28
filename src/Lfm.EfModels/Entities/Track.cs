using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lfm.EfModels.Entities;

/// <summary>
/// EF Core entity representing a Last.fm track.
/// Maps to user.getTopTracks and artist.getTopTracks API endpoints.
/// </summary>
[Table("Tracks")]
public class Track : LastFmEntity
{
    /// <summary>
    /// Track name - serves as part of composite primary key (Name + ArtistName + User).
    /// </summary>
    [Key]
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Artist name - foreign key to Artist entity.
    /// Part of composite key to support tracks with same name by different artists.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ArtistName { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property: Artist who performed this track.
    /// </summary>
    [ForeignKey(nameof(ArtistName))]
    public virtual Artist? Artist { get; set; }

    /// <summary>
    /// Album name (optional - not all tracks have album data).
    /// </summary>
    [MaxLength(500)]
    public string? AlbumName { get; set; }

    /// <summary>
    /// Navigation property: Album this track belongs to (if available).
    /// </summary>
    [ForeignKey(nameof(AlbumName))]
    public virtual Album? Album { get; set; }

    /// <summary>
    /// Play count as string to match Last.fm API format.
    /// </summary>
    [Required]
    public string PlayCount { get; set; } = "0";

    /// <summary>
    /// Rank in top tracks list (e.g., "1", "2", "3").
    /// </summary>
    public string Rank { get; set; } = string.Empty;

    /// <summary>
    /// Last.fm URL for this track.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// MusicBrainz ID for this track (if available).
    /// </summary>
    public string Mbid { get; set; } = string.Empty;
}
