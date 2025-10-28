using Lfm.EfModels.Entities;
using Lfm.EfModels.Utilities;
using ApiModels = Lfm.Shared.Models;
using EfEntities = Lfm.EfModels.Entities;

namespace Lfm.EfModels.Provider;

/// <summary>
/// Maps Last.fm API responses to EF entities.
/// Converts API models (TopArtists, TopTracks, etc.) to EF entities (Artist, Track, etc.).
///
/// Responsibilities:
/// 1. Deserialize API responses to LastFmModels (handled by API client)
/// 2. Map LastFmModels to EF entities
/// 3. Populate navigation properties if Include() was used
/// 4. Apply any client-side filters from QueryDescriptor
/// </summary>
public class ResultMapper
{
    /// <summary>
    /// Map TopArtists API response to Artist entities.
    /// </summary>
    public List<EfEntities.Artist> MapArtists(ApiModels.TopArtists response, string user)
    {
        if (response?.Artists == null)
            return new List<Artist>();

        return response.Artists.Select(apiArtist => new EfEntities.Artist
        {
            Name = StringNormalizer.NormalizeArtistName(apiArtist.Name),
            PlayCount = apiArtist.PlayCount,
            Rank = apiArtist.Attributes?.Rank ?? "",
            Url = apiArtist.Url,
            Mbid = apiArtist.Mbid,
            User = user,
            QueryTimestamp = DateTime.UtcNow
        }).ToList();
    }

    /// <summary>
    /// Map TopTracks API response to Track entities.
    /// </summary>
    public List<EfEntities.Track> MapTracks(ApiModels.TopTracks response, string user)
    {
        if (response?.Tracks == null)
            return new List<Track>();

        return response.Tracks.Select(apiTrack => new Track
        {
            Name = StringNormalizer.NormalizeTrackName(apiTrack.Name),
            ArtistName = StringNormalizer.NormalizeArtistName(apiTrack.Artist?.Name ?? ""),
            AlbumName = null, // Top tracks don't include album info
            PlayCount = apiTrack.PlayCount,
            Rank = apiTrack.Attributes?.Rank ?? "",
            Url = apiTrack.Url,
            Mbid = apiTrack.Mbid,
            User = user,
            QueryTimestamp = DateTime.UtcNow
        }).ToList();
    }

    /// <summary>
    /// Map artist's top tracks to Track entities (artist.getTopTracks).
    /// </summary>
    public List<EfEntities.Track> MapArtistTracks(ApiModels.TopTracks response, string artist, string user)
    {
        System.Diagnostics.Debug.WriteLine($"[MapArtistTracks] response={response}, Tracks={response?.Tracks}, Count={response?.Tracks?.Count ?? -1}");

        if (response?.Tracks == null)
            return new List<Track>();

        return response.Tracks.Select(apiTrack => new Track
        {
            Name = StringNormalizer.NormalizeTrackName(apiTrack.Name),
            ArtistName = artist, // Use queried artist name (already normalized in query provider)
            AlbumName = null,
            PlayCount = apiTrack.PlayCount,
            Rank = apiTrack.Attributes?.Rank ?? "",
            Url = apiTrack.Url,
            Mbid = apiTrack.Mbid,
            User = user,
            QueryTimestamp = DateTime.UtcNow
        }).ToList();
    }

    /// <summary>
    /// Map TopAlbums API response to Album entities.
    /// </summary>
    public List<EfEntities.Album> MapAlbums(ApiModels.TopAlbums response, string user)
    {
        if (response?.Albums == null)
            return new List<Album>();

        return response.Albums.Select(apiAlbum => new Album
        {
            Name = StringNormalizer.NormalizeAlbumName(apiAlbum.Name),
            ArtistName = StringNormalizer.NormalizeArtistName(apiAlbum.Artist?.Name ?? ""),
            PlayCount = apiAlbum.PlayCount,
            Rank = apiAlbum.Attributes?.Rank ?? "",
            Url = apiAlbum.Url,
            Mbid = apiAlbum.Mbid,
            User = user,
            QueryTimestamp = DateTime.UtcNow
        }).ToList();
    }

    /// <summary>
    /// Map artist's top albums to Album entities (artist.getTopAlbums).
    /// </summary>
    public List<EfEntities.Album> MapArtistAlbums(ApiModels.TopAlbums response, string artist, string user)
    {
        if (response?.Albums == null)
            return new List<Album>();

        return response.Albums.Select(apiAlbum => new Album
        {
            Name = StringNormalizer.NormalizeAlbumName(apiAlbum.Name),
            ArtistName = artist, // Use queried artist name (already normalized in query provider)
            PlayCount = apiAlbum.PlayCount,
            Rank = apiAlbum.Attributes?.Rank ?? "",
            Url = apiAlbum.Url,
            Mbid = apiAlbum.Mbid,
            User = user,
            QueryTimestamp = DateTime.UtcNow
        }).ToList();
    }

    /// <summary>
    /// Map RecentTracks API response to RecentTrack entities.
    /// </summary>
    public List<EfEntities.RecentTrack> MapRecentTracks(ApiModels.RecentTracks response, string user)
    {
        if (response?.Tracks == null)
            return new List<RecentTrack>();

        return response.Tracks.Select(apiTrack => new EfEntities.RecentTrack
        {
            Name = StringNormalizer.NormalizeTrackName(apiTrack.Name ?? ""),
            ArtistName = StringNormalizer.NormalizeArtistName(apiTrack.Artist?.Name ?? ""),
            AlbumName = StringNormalizer.NormalizeAlbumName(apiTrack.Album?.Name),
            PlayedAt = apiTrack.Date?.UnixTimestamp != null
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(apiTrack.Date.UnixTimestamp)).UtcDateTime
                : DateTime.UtcNow,
            IsNowPlaying = apiTrack.Attributes?.NowPlaying == "true",
            Url = apiTrack.Url ?? "",
            Mbid = apiTrack.Mbid ?? "",
            User = user,
            QueryTimestamp = DateTime.UtcNow
        }).ToList();
    }

    /// <summary>
    /// Populate navigation properties for Track entities (Include support).
    /// </summary>
    public async Task PopulateTrackNavigationsAsync(
        List<EfEntities.Track> tracks,
        HashSet<string> includes,
        Func<string, Task<EfEntities.Artist?>> artistLookup)
    {
        if (!includes.Contains("Artist") && !includes.Contains("Album"))
            return;

        // Group tracks by artist to minimize lookups
        var artistNames = tracks.Select(t => t.ArtistName).Distinct().ToList();

        if (includes.Contains("Artist"))
        {
            foreach (var artistName in artistNames)
            {
                var artist = await artistLookup(artistName);
                if (artist != null)
                {
                    foreach (var track in tracks.Where(t => t.ArtistName == artistName))
                    {
                        track.Artist = artist;
                    }
                }
            }
        }

        // Album population would require additional API calls (artist.getTopAlbums)
        // For now, Album navigation is not populated
    }

    /// <summary>
    /// Populate navigation properties for Album entities (Include support).
    /// </summary>
    public async Task PopulateAlbumNavigationsAsync(
        List<EfEntities.Album> albums,
        HashSet<string> includes,
        Func<string, Task<EfEntities.Artist?>> artistLookup)
    {
        if (!includes.Contains("Artist"))
            return;

        // Group albums by artist to minimize lookups
        var artistNames = albums.Select(a => a.ArtistName).Distinct().ToList();

        foreach (var artistName in artistNames)
        {
            var artist = await artistLookup(artistName);
            if (artist != null)
            {
                foreach (var album in albums.Where(a => a.ArtistName == artistName))
                {
                    album.Artist = artist;
                }
            }
        }
    }

    /// <summary>
    /// Apply client-side filters to results.
    /// Used for LINQ operations that couldn't be translated to API calls.
    /// </summary>
    public List<T> ApplyClientSideFilters<T>(List<T> results, List<string> filters)
    {
        // For now, just log warnings about client-side filters
        // Full implementation would parse filter strings and apply them
        if (filters.Any())
        {
            Console.WriteLine($"Warning: {filters.Count} client-side filters applied. " +
                            $"This may impact performance. Filters: {string.Join(", ", filters)}");
        }

        return results;
    }
}
