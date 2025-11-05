using Lfm.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lfm.Tests.Fixtures;

/// <summary>
/// Generates test data for unit and integration tests
/// Provides consistent, reproducible test fixtures across the test suite
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random Random = new(42); // Fixed seed for reproducibility

    /// <summary>
    /// Generates a list of top artists with play counts
    /// </summary>
    public static List<Artist> GenerateTopArtists(int count = 10)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Artist
            {
                Name = $"Artist {i}",
                PlayCount = Random.Next(1, 1000).ToString(),
                Mbid = $"mbid-{i}",
                Url = $"https://www.last.fm/music/Artist+{i}"
            })
            .ToList();
    }

    /// <summary>
    /// Generates a list of top tracks with play counts
    /// </summary>
    public static List<Track> GenerateTopTracks(int count = 10)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Track
            {
                Name = $"Track {i}",
                Artist = new ArtistInfo
                {
                    Name = $"Artist {i}",
                    Mbid = $"artist-mbid-{i}",
                    Url = $"https://www.last.fm/music/Artist+{i}"
                },
                PlayCount = Random.Next(1, 500).ToString(),
                Mbid = $"track-mbid-{i}",
                Url = $"https://www.last.fm/music/Artist+{i}/_/Track+{i}"
            })
            .ToList();
    }

    /// <summary>
    /// Generates a list of top albums with track counts
    /// </summary>
    public static List<Album> GenerateTopAlbums(int count = 10)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Album
            {
                Name = $"Album {i}",
                Artist = new ArtistInfo
                {
                    Name = $"Artist {i}",
                    Mbid = $"artist-mbid-{i}",
                    Url = $"https://www.last.fm/music/Artist+{i}"
                },
                PlayCount = Random.Next(1, 300).ToString(),
                Mbid = $"album-mbid-{i}",
                Url = $"https://www.last.fm/music/Artist+{i}/Album+{i}"
            })
            .ToList();
    }

    /// <summary>
    /// Generates a list of recent tracks (for feed/history)
    /// </summary>
    public static List<Track> GenerateRecentTracks(int count = 20)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Track
            {
                Name = $"Recent Track {i}",
                Artist = new ArtistInfo
                {
                    Name = $"Artist {i % 5}",
                    Mbid = $"artist-mbid-{i % 5}",
                    Url = $"https://www.last.fm/music/Artist+{i % 5}"
                },
                PlayCount = "1",
                Mbid = $"track-mbid-{i}",
                Url = $"https://www.last.fm/music/Artist+{i % 5}/_/Track+{i}"
            })
            .ToList();
    }

    /// <summary>
    /// Generates random play counts with varied distribution
    /// </summary>
    public static int GenerateRandomPlayCount()
    {
        return Random.Next(1, 1000);
    }

    /// <summary>
    /// Generates a valid Last.fm username
    /// </summary>
    public static string GenerateUsername(int variant = 1)
    {
        var names = new[] { "testuser", "musiclover", "lastfmfan", "audiophile", "trackgeek" };
        return $"{names[variant % names.Length]}{variant}";
    }

    /// <summary>
    /// Generates a valid artist name with proper formatting
    /// </summary>
    public static string GenerateArtistName(int variant = 1)
    {
        var artists = new[] { "Pink Floyd", "The Beatles", "David Bowie", "Queen", "Led Zeppelin" };
        return artists[variant % artists.Length];
    }

    /// <summary>
    /// Generates a valid track name
    /// </summary>
    public static string GenerateTrackName(int variant = 1)
    {
        var tracks = new[] { "Money", "Hey Jude", "Space Oddity", "Bohemian Rhapsody", "Whole Lotta Love" };
        return tracks[variant % tracks.Length];
    }

    /// <summary>
    /// Generates a valid album name
    /// </summary>
    public static string GenerateAlbumName(int variant = 1)
    {
        var albums = new[] { "The Dark Side of the Moon", "Abbey Road", "Hunky Dory", "A Night at the Opera", "IV" };
        return albums[variant % albums.Length];
    }

    /// <summary>
    /// Generates a Spotify track URI
    /// </summary>
    public static string GenerateSpotifyTrackUri(int variant = 1)
    {
        // Format: spotify:track:{40-character hex ID}
        return $"spotify:track:{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 32)}{variant:00000000}".Substring(0, 40);
    }

    /// <summary>
    /// Generates a Spotify album URI
    /// </summary>
    public static string GenerateSpotifyAlbumUri(int variant = 1)
    {
        // Format: spotify:album:{22-character ID}
        return $"spotify:album:{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 22)}{variant:0000}".Substring(0, 32);
    }

    /// <summary>
    /// Generates a Spotify device ID
    /// </summary>
    public static string GenerateSpotifyDeviceId(int variant = 1)
    {
        return $"device-{Guid.NewGuid().ToString().Substring(0, 8)}-{variant}";
    }

    /// <summary>
    /// Generates a Sonos room UUID
    /// </summary>
    public static string GenerateSonosRoomUuid(int variant = 1)
    {
        return $"RINCON_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16)}_0";
    }
}
