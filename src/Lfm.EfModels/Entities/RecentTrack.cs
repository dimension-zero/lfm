using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lfm.EfModels.Entities;

/// <summary>
/// EF Core entity representing a recently played track.
/// Maps to user.getRecentTracks API endpoint.
/// Different from Track entity because it includes timestamp and is chronological.
/// </summary>
[Table("RecentTracks")]
public class RecentTrack : LastFmEntity
{
    /// <summary>
    /// Auto-generated ID for recent tracks (since same track can be played multiple times).
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Track name.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Artist name.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ArtistName { get; set; } = string.Empty;

    /// <summary>
    /// Album name (optional).
    /// </summary>
    [MaxLength(500)]
    public string? AlbumName { get; set; }

    /// <summary>
    /// Timestamp when this track was played.
    /// Core property for recent tracks - enables chronological queries.
    /// </summary>
    [Required]
    public DateTime PlayedAt { get; set; }

    /// <summary>
    /// Whether this track is currently playing (nowplaying flag from API).
    /// </summary>
    public bool IsNowPlaying { get; set; }

    /// <summary>
    /// Last.fm URL for this track.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// MusicBrainz ID for this track (if available).
    /// </summary>
    public string Mbid { get; set; } = string.Empty;
}
