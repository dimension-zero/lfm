using Lfm.Core.Services;
using Lfm.EfModels;
using Lfm.EfModels.Extensions;
using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Lfm.Tests.Mocks;

/// <summary>
/// Shared test fixture for comparing EF LINQ provider vs direct API client usage.
/// Provides mock setup for all query types and contexts for both approaches.
/// </summary>
public class OriginalVsEfTestFixture
{
    private const string TestUser = "testuser";

    public Mock<ILastFmApiClient> MockApiClient { get; }
    public ILastFmApiClient OriginalClient => MockApiClient.Object;
    public LfmDbContext EfContext { get; }

    public OriginalVsEfTestFixture()
    {
        MockApiClient = new Mock<ILastFmApiClient>();

        // Create EF context with mocked API client
        var options = new DbContextOptionsBuilder<LfmDbContext>()
            .UseLastFm(MockApiClient.Object, TestUser)
            .Options;

        EfContext = new LfmDbContext(options);
    }

    /// <summary>
    /// Setup mock data for top artists query.
    /// </summary>
    public TopArtists SetupTopArtists(string user, int count = 10)
    {
        var artists = Enumerable.Range(1, count)
            .Select(i => new Artist
            {
                Name = $"Artist {i}",
                PlayCount = (1000 - i * 10).ToString(),
                Attributes = new ArtistAttributes { Rank = i.ToString() },
                Url = $"https://last.fm/music/Artist+{i}",
                Mbid = $"mbid-{i}"
            })
            .ToList();

        var response = new TopArtists
        {
            Artists = artists,
            Attributes = new TopArtistsAttributes
            {
                User = user,
                Total = count.ToString(),
                Page = "1",
                PerPage = count.ToString(),
                TotalPages = "1"
            }
        };

        MockApiClient
            .Setup(x => x.GetTopArtistsAsync(user, It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(response);

        return response;
    }

    /// <summary>
    /// Setup mock data for top tracks query.
    /// </summary>
    public TopTracks SetupTopTracks(string user, int count = 10)
    {
        var tracks = Enumerable.Range(1, count)
            .Select(i => new Track
            {
                Name = $"Track {i}",
                PlayCount = (500 - i * 5).ToString(),
                Attributes = new TrackAttributes { Rank = i.ToString() },
                Url = $"https://last.fm/music/Track+{i}",
                Mbid = $"track-mbid-{i}",
                Artist = new ArtistInfo { Name = $"Artist {i % 3 + 1}" }
            })
            .ToList();

        var response = new TopTracks
        {
            Tracks = tracks,
            Attributes = new TopTracksAttributes
            {
                User = user,
                Total = count.ToString(),
                Page = "1",
                PerPage = count.ToString(),
                TotalPages = "1"
            }
        };

        MockApiClient
            .Setup(x => x.GetTopTracksAsync(user, It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(response);

        return response;
    }

    /// <summary>
    /// Setup mock data for artist top tracks query.
    /// </summary>
    public TopTracks SetupArtistTopTracks(string artist, int count = 5)
    {
        var tracks = Enumerable.Range(1, count)
            .Select(i => new Track
            {
                Name = $"{artist} - Track {i}",
                PlayCount = (300 - i * 3).ToString(),
                Attributes = new TrackAttributes { Rank = i.ToString() },
                Url = $"https://last.fm/music/{artist}/Track+{i}",
                Mbid = $"artist-track-{i}"
            })
            .ToList();

        var response = new TopTracks { Tracks = tracks };

        MockApiClient
            .Setup(x => x.GetArtistTopTracksAsync(artist, It.IsAny<int>()))
            .ReturnsAsync(response);

        return response;
    }

    /// <summary>
    /// Setup mock data for top albums query.
    /// </summary>
    public TopAlbums SetupTopAlbums(string user, int count = 10)
    {
        var albums = Enumerable.Range(1, count)
            .Select(i => new Album
            {
                Name = $"Album {i}",
                PlayCount = (400 - i * 4).ToString(),
                Attributes = new AlbumAttributes { Rank = i.ToString() },
                Url = $"https://last.fm/music/Album+{i}",
                Mbid = $"album-mbid-{i}",
                Artist = new ArtistInfo { Name = $"Artist {i % 3 + 1}" }
            })
            .ToList();

        var response = new TopAlbums
        {
            Albums = albums,
            Attributes = new TopAlbumsAttributes
            {
                User = user,
                Total = count.ToString(),
                Page = "1",
                PerPage = count.ToString(),
                TotalPages = "1"
            }
        };

        MockApiClient
            .Setup(x => x.GetTopAlbumsAsync(user, It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(response);

        return response;
    }

    /// <summary>
    /// Setup mock data for artist top albums query.
    /// </summary>
    public TopAlbums SetupArtistTopAlbums(string artist, int count = 5)
    {
        var albums = Enumerable.Range(1, count)
            .Select(i => new Album
            {
                Name = $"{artist} - Album {i}",
                PlayCount = (200 - i * 2).ToString(),
                Attributes = new AlbumAttributes { Rank = i.ToString() },
                Url = $"https://last.fm/music/{artist}/Album+{i}",
                Mbid = $"artist-album-{i}"
            })
            .ToList();

        var response = new TopAlbums { Albums = albums };

        MockApiClient
            .Setup(x => x.GetArtistTopAlbumsAsync(artist, It.IsAny<int>()))
            .ReturnsAsync(response);

        return response;
    }

    /// <summary>
    /// Setup mock data for recent tracks query.
    /// </summary>
    public RecentTracks SetupRecentTracks(string user, int count = 10)
    {
        var now = DateTime.UtcNow;
        var tracks = Enumerable.Range(1, count)
            .Select(i => new RecentTrack
            {
                Name = $"Recent Track {i}",
                Artist = new RecentTrackArtistInfo { Name = $"Artist {i % 3 + 1}" },
                Album = new AlbumInfo { Name = $"Album {i % 5 + 1}" },
                Url = $"https://last.fm/music/Recent+{i}",
                Mbid = $"recent-{i}",
                Date = new DateInfo
                {
                    UnixTimestamp = new DateTimeOffset(now.AddHours(-i)).ToUnixTimeSeconds().ToString()
                }
            })
            .ToList();

        var response = new RecentTracks
        {
            Tracks = tracks,
            Attributes = new RecentTracksAttributes
            {
                User = user,
                Total = count.ToString(),
                Page = "1",
                PerPage = count.ToString(),
                TotalPages = "1"
            }
        };

        MockApiClient
            .Setup(x => x.GetRecentTracksAsync(user, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(response);

        return response;
    }
}
