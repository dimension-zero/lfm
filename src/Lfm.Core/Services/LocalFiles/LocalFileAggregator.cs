using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Core.Services.LocalFiles;

/// <summary>
/// Aggregates play events into Top Artists, Tracks, and Albums lists.
/// Converts event-based data into the same format as Last.fm API.
/// </summary>
public class LocalFileAggregator
{
    /// <summary>
    /// Aggregates play events into top artists.
    /// </summary>
    public List<LocalArtistInfo> AggregateTopArtists(List<PlayEvent> events, int? limit = null)
    {
        var artistGroups = events
            .GroupBy(e => e.Artist, StringComparer.OrdinalIgnoreCase)
            .Select(g => new LocalArtistInfo
            {
                Name = g.First().Artist, // Use original casing from first occurrence
                PlayCount = g.Count(),
                Url = string.Empty, // Local files don't have URLs
                Mbid = string.Empty,
                Streamable = false
            })
            .OrderByDescending(a => a.PlayCount)
            .ThenBy(a => a.Name);

        return limit.HasValue
            ? artistGroups.Take(limit.Value).ToList()
            : artistGroups.ToList();
    }

    /// <summary>
    /// Aggregates play events into top tracks.
    /// </summary>
    public List<LocalTrackInfo> AggregateTopTracks(List<PlayEvent> events, int? limit = null)
    {
        var trackGroups = events
            .GroupBy(e => new { Artist = e.Artist.ToLowerInvariant(), Track = e.Track.ToLowerInvariant() })
            .Select(g =>
            {
                var firstEvent = g.First();
                return new LocalTrackInfo
                {
                    Name = firstEvent.Track,
                    Artist = firstEvent.Artist,
                    PlayCount = g.Count(),
                    Url = string.Empty,
                    Mbid = string.Empty,
                    Streamable = false,
                    AlbumName = g.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Album))?.Album
                };
            })
            .OrderByDescending(t => t.PlayCount)
            .ThenBy(t => t.Artist)
            .ThenBy(t => t.Name);

        return limit.HasValue
            ? trackGroups.Take(limit.Value).ToList()
            : trackGroups.ToList();
    }

    /// <summary>
    /// Aggregates play events into top albums.
    /// </summary>
    public List<LocalAlbumInfo> AggregateTopAlbums(List<PlayEvent> events, int? limit = null)
    {
        var albumGroups = events
            .Where(e => !string.IsNullOrWhiteSpace(e.Album)) // Only events with album info
            .GroupBy(e => new { Artist = e.Artist.ToLowerInvariant(), Album = e.Album!.ToLowerInvariant() })
            .Select(g =>
            {
                var firstEvent = g.First();
                return new LocalAlbumInfo
                {
                    Name = firstEvent.Album!,
                    Artist = firstEvent.Artist,
                    PlayCount = g.Count(),
                    Url = string.Empty,
                    Mbid = string.Empty
                };
            })
            .OrderByDescending(a => a.PlayCount)
            .ThenBy(a => a.Artist)
            .ThenBy(a => a.Name);

        return limit.HasValue
            ? albumGroups.Take(limit.Value).ToList()
            : albumGroups.ToList();
    }

    /// <summary>
    /// Gets all tracks for a specific artist.
    /// </summary>
    public List<LocalTrackInfo> GetArtistTracks(List<PlayEvent> events, string artistName)
    {
        var artistEvents = events
            .Where(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return AggregateTopTracks(artistEvents);
    }

    /// <summary>
    /// Gets all albums for a specific artist.
    /// </summary>
    public List<LocalAlbumInfo> GetArtistAlbums(List<PlayEvent> events, string artistName)
    {
        var artistEvents = events
            .Where(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return AggregateTopAlbums(artistEvents);
    }

    /// <summary>
    /// Gets album tracks with play counts.
    /// </summary>
    public List<LocalTrackInfo> GetAlbumTracks(List<PlayEvent> events, string artistName, string albumName)
    {
        var albumEvents = events
            .Where(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase))
            .Where(e => e.Album != null && e.Album.Equals(albumName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return AggregateTopTracks(albumEvents);
    }

    /// <summary>
    /// Gets track play count for a specific artist/track combination.
    /// </summary>
    public int GetTrackPlayCount(List<PlayEvent> events, string artistName, string trackName)
    {
        return events
            .Count(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase) &&
                        e.Track.Equals(trackName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets album play count (sum of all track plays).
    /// </summary>
    public int GetAlbumPlayCount(List<PlayEvent> events, string artistName, string albumName)
    {
        return events
            .Count(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase) &&
                        e.Album != null &&
                        e.Album.Equals(albumName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets artist play count (total plays across all tracks).
    /// </summary>
    public int GetArtistPlayCount(List<PlayEvent> events, string artistName)
    {
        return events
            .Count(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets recent tracks (chronologically ordered).
    /// </summary>
    public List<LocalTrackInfo> GetRecentTracks(List<PlayEvent> events, int limit = 50)
    {
        return events
            .OrderByDescending(e => e.PlayedAt)
            .Take(limit)
            .Select(e => new LocalTrackInfo
            {
                Name = e.Track,
                Artist = e.Artist,
                PlayCount = 1, // Each event is one play
                Url = string.Empty,
                Mbid = string.Empty,
                Streamable = false,
                AlbumName = e.Album
            })
            .ToList();
    }

    /// <summary>
    /// Gets total play count across all events.
    /// </summary>
    public int GetTotalPlayCount(List<PlayEvent> events)
    {
        return events.Count;
    }

    /// <summary>
    /// Gets date range of play events.
    /// </summary>
    public (DateTime? Start, DateTime? End) GetDateRange(List<PlayEvent> events)
    {
        if (!events.Any())
            return (null, null);

        return (events.Min(e => e.PlayedAt), events.Max(e => e.PlayedAt));
    }
}
