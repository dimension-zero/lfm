using Lfm.Shared.Models;

namespace Lfm.Core.Music.Models;

/// <summary>
/// Converts Last.fm domain models (Track, Artist, Album) to MusicItem
/// Enables reuse of existing API response models with framework
/// </summary>
public static class MusicItemConverter
{
    /// <summary>
    /// Convert a Track to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(Track track, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "track",
            Id = track.Mbid ?? track.Name,
            Name = track.Name,
            ImageUrl = null, // Tracks don't have images in Last.fm API
            ArtistName = track.Artist?.Name,
            PlayCount = int.TryParse(track.PlayCount, out var count) ? count : 0,
            Url = track.Url,
            AlbumName = null,
            Ranking = ranking
        };
    }

    /// <summary>
    /// Convert an Artist to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(Artist artist, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "artist",
            Id = artist.Mbid ?? artist.Name,
            Name = artist.Name,
            ImageUrl = null, // Images not in standard response
            ArtistName = null,
            PlayCount = int.TryParse(artist.PlayCount, out var count) ? count : 0,
            Url = artist.Url,
            AlbumName = null,
            Ranking = ranking
        };
    }

    /// <summary>
    /// Convert an Album to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(Album album, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "album",
            Id = album.Mbid ?? album.Name,
            Name = album.Name,
            ImageUrl = null,
            ArtistName = album.Artist?.Name,
            PlayCount = int.TryParse(album.PlayCount, out var count) ? count : 0,
            Url = album.Url,
            AlbumName = album.Name,
            Ranking = ranking
        };
    }

    /// <summary>
    /// Convert an ArtistAggregation to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(ArtistAggregation artist, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "artist",
            Id = artist.Mbid ?? artist.Name,
            Name = artist.Name,
            ImageUrl = null,
            ArtistName = null,
            PlayCount = artist.PlayCount,
            Url = artist.Url,
            AlbumName = null,
            Ranking = ranking
        };
    }

    /// <summary>
    /// Convert a TrackAggregation to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(TrackAggregation track, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "track",
            Id = track.Mbid ?? track.Name,
            Name = track.Name,
            ImageUrl = null,
            ArtistName = track.Artist?.Name,
            PlayCount = track.PlayCount,
            Url = track.Url,
            AlbumName = null,
            Ranking = ranking
        };
    }

    /// <summary>
    /// Convert an AlbumAggregation to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(AlbumAggregation album, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "album",
            Id = album.Mbid ?? album.Name,
            Name = album.Name,
            ImageUrl = null,
            ArtistName = album.Artist?.Name,
            PlayCount = album.PlayCount,
            Url = null,
            AlbumName = album.Name,
            Ranking = ranking
        };
    }

    /// <summary>
    /// Convert a SimilarArtist to MusicItem
    /// </summary>
    public static MusicItem ToMusicItem(SimilarArtist artist, string? ranking = null)
    {
        return new MusicItem
        {
            Type = "artist",
            Id = artist.Mbid ?? artist.Name,
            Name = artist.Name,
            ImageUrl = null,
            ArtistName = null,
            PlayCount = 0,
            Url = artist.Url,
            AlbumName = null,
            Ranking = ranking
        };
    }
}
