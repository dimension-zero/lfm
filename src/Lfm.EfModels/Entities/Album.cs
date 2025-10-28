using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lfm.EfModels.Entities;

/// <summary>
/// EF Core entity representing a Last.fm album.
/// Maps to user.getTopAlbums and artist.getTopAlbums API endpoints.
/// </summary>
[Table("Albums")]
public class Album : LastFmEntity
{
    /// <summary>
    /// Album name - serves as part of composite primary key (Name + ArtistName + User).
    /// </summary>
    [Key]
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Artist name - foreign key to Artist entity.
    /// Part of composite key to support albums with same name by different artists.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ArtistName { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property: Artist who created this album.
    /// </summary>
    [ForeignKey(nameof(ArtistName))]
    public virtual Artist? Artist { get; set; }

    /// <summary>
    /// Play count as string to match Last.fm API format.
    /// </summary>
    [Required]
    public string PlayCount { get; set; } = "0";

    /// <summary>
    /// Rank in top albums list (e.g., "1", "2", "3").
    /// </summary>
    public string Rank { get; set; } = string.Empty;

    /// <summary>
    /// Last.fm URL for this album.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// MusicBrainz ID for this album (if available).
    /// </summary>
    public string Mbid { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property: Tracks on this album.
    /// </summary>
    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
}
