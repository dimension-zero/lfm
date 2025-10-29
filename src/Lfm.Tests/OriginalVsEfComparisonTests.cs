using Lfm.Data.Direct;
using Lfm.Shared.Interfaces;
using FluentAssertions;
using Lfm.Core.Services;
using Lfm.Data.EF;
using Lfm.Data.EF.Extensions;
using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Lfm.Tests;

/// <summary>
/// Comparison tests between original API implementation and EF Core LINQ provider.
/// Verifies functional equivalence between:
/// - Original: ILastFmApiClient direct calls
/// - EF: LfmDbContext LINQ queries
/// </summary>
public class OriginalVsEfComparisonTests
{
    private const string TestUser = "testuser";

    /// <summary>
    /// Test fixture for shared test data.
    /// </summary>
    private class TestFixture
    {
        public Mock<ILastFmApiClient> MockApiClient { get; }
        public LfmDbContext EfContext { get; }

        public TestFixture()
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
        public TopArtists SetupTopArtists(int count = 10)
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
                    User = TestUser,
                    Total = count.ToString(),
                    Page = "1",
                    PerPage = count.ToString(),
                    TotalPages = "1"
                }
            };

            MockApiClient
                .Setup(x => x.GetTopArtistsAsync(TestUser, It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(response);

            return response;
        }

        /// <summary>
        /// Setup mock data for top tracks query.
        /// </summary>
        public TopTracks SetupTopTracks(int count = 10)
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
                    User = TestUser,
                    Total = count.ToString(),
                    Page = "1",
                    PerPage = count.ToString(),
                    TotalPages = "1"
                }
            };

            MockApiClient
                .Setup(x => x.GetTopTracksAsync(TestUser, It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
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
        public TopAlbums SetupTopAlbums(int count = 10)
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
                    User = TestUser,
                    Total = count.ToString(),
                    Page = "1",
                    PerPage = count.ToString(),
                    TotalPages = "1"
                }
            };

            MockApiClient
                .Setup(x => x.GetTopAlbumsAsync(TestUser, It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
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
        public RecentTracks SetupRecentTracks(int count = 10)
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
                    User = TestUser,
                    Total = count.ToString(),
                    Page = "1",
                    PerPage = count.ToString(),
                    TotalPages = "1"
                }
            };

            MockApiClient
                .Setup(x => x.GetRecentTracksAsync(TestUser, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(response);

            return response;
        }
    }

    [Fact]
    public async Task TopArtists_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupTopArtists(10);

        // Act - Original
        var originalResults = originalResponse.Artists;

        // Act - EF
        var efResults = await fixture.EfContext.Artists
            .Where(a => a.User == TestUser)
            .Take(10)
            .ToListAsync();

        // Assert
        efResults.Should().HaveCount(originalResults.Count);

        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);
            efResults[i].PlayCount.Should().Be(originalResults[i].PlayCount);
            efResults[i].Rank.Should().Be(originalResults[i].Attributes?.Rank ?? "");
            efResults[i].Url.Should().Be(originalResults[i].Url);
            efResults[i].Mbid.Should().Be(originalResults[i].Mbid);
            efResults[i].User.Should().Be(TestUser);
        }
    }

    [Fact]
    public async Task TopTracks_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupTopTracks(10);

        // Act - Original
        var originalResults = originalResponse.Tracks;

        // Act - EF
        var efResults = await fixture.EfContext.Tracks
            .Where(t => t.User == TestUser)
            .Take(10)
            .ToListAsync();

        // Assert
        efResults.Should().HaveCount(originalResults.Count);

        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);
            efResults[i].PlayCount.Should().Be(originalResults[i].PlayCount);
            efResults[i].Rank.Should().Be(originalResults[i].Attributes?.Rank ?? "");
            efResults[i].Url.Should().Be(originalResults[i].Url);
            efResults[i].ArtistName.Should().Be(originalResults[i].Artist.Name);
            efResults[i].User.Should().Be(TestUser);
        }
    }

    [Fact]
    public async Task ArtistTopTracks_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        const string artist = "Pink Floyd";
        var originalResponse = fixture.SetupArtistTopTracks(artist, 5);

        // Act - Original
        var originalResults = originalResponse.Tracks;

        // Act - EF (without any direct calls first)
        var efResults = await fixture.EfContext.Tracks
            .Where(t => t.ArtistName == artist)
            .Take(5)
            .ToListAsync();

        // Check if mock was called and how many times
        fixture.MockApiClient.Verify(
            x => x.GetArtistTopTracksAsync(artist, It.IsAny<int>()),
            Times.AtLeastOnce(),
            "Mock GetArtistTopTracksAsync should have been called by EF query");

        // Debug: Show all invocations
        var invocations = fixture.MockApiClient.Invocations
            .Where(i => i.Method.Name == "GetArtistTopTracksAsync")
            .ToList();

        invocations.Should().NotBeEmpty("Should have invocations");

        foreach (var inv in invocations)
        {
            var args = inv.Arguments;
            Console.WriteLine($"Invocation: artist={args[0]}, limit={args[1]}");
        }

        // Assert
        efResults.Should().HaveCount(originalResults.Count,
            $"EF query should return {originalResults.Count} results (found {efResults.Count})");

        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);
            efResults[i].PlayCount.Should().Be(originalResults[i].PlayCount);
            efResults[i].ArtistName.Should().Be(artist);
        }
    }

    [Fact]
    public async Task TopAlbums_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupTopAlbums(10);

        // Act - Original
        var originalResults = originalResponse.Albums;

        // Act - EF
        var efResults = await fixture.EfContext.Albums
            .Where(a => a.User == TestUser)
            .Take(10)
            .ToListAsync();

        // Assert
        efResults.Should().HaveCount(originalResults.Count);

        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);
            efResults[i].PlayCount.Should().Be(originalResults[i].PlayCount);
            efResults[i].Rank.Should().Be(originalResults[i].Attributes?.Rank ?? "");
            efResults[i].ArtistName.Should().Be(originalResults[i].Artist.Name);
            efResults[i].User.Should().Be(TestUser);
        }
    }

    [Fact]
    public async Task ArtistTopAlbums_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        const string artist = "The Beatles";
        var originalResponse = fixture.SetupArtistTopAlbums(artist, 5);

        // Act - Original
        var originalResults = originalResponse.Albums;

        // Act - EF
        var efResults = await fixture.EfContext.Albums
            .Where(a => a.ArtistName == artist)
            .Take(5)
            .ToListAsync();

        // Assert
        efResults.Should().HaveCount(originalResults.Count);

        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);
            efResults[i].PlayCount.Should().Be(originalResults[i].PlayCount);
            efResults[i].ArtistName.Should().Be(artist);
        }
    }

    [Fact]
    public async Task RecentTracks_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupRecentTracks(10);

        // Act - Original
        var originalResults = originalResponse.Tracks;

        // Act - EF
        var efResults = await fixture.EfContext.RecentTracks
            .Where(r => r.User == TestUser)
            .ToListAsync();

        // Assert
        efResults.Should().HaveCount(originalResults.Count);

        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);
            efResults[i].ArtistName.Should().Be(originalResults[i].Artist.Name);
            efResults[i].AlbumName.Should().Be(originalResults[i].Album.Name);
            efResults[i].Url.Should().Be(originalResults[i].Url);
            efResults[i].User.Should().Be(TestUser);

            // Verify date parsing works correctly
            var expectedTimestamp = long.Parse(originalResults[i].Date!.UnixTimestamp);
            var expectedDate = DateTimeOffset.FromUnixTimeSeconds(expectedTimestamp).UtcDateTime;
            efResults[i].PlayedAt.Should().BeCloseTo(expectedDate, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task Pagination_OriginalVsEf_ReturnsSameResults()
    {
        // Arrange
        var fixture = new TestFixture();
        fixture.SetupTopArtists(100);

        // Mock page 2 response
        var page2Artists = Enumerable.Range(51, 50)
            .Select(i => new Artist
            {
                Name = $"Artist {i}",
                PlayCount = (1000 - i * 10).ToString(),
                Attributes = new ArtistAttributes { Rank = i.ToString() }
            })
            .ToList();

        var page2Response = new TopArtists
        {
            Artists = page2Artists,
            Attributes = new TopArtistsAttributes
            {
                User = TestUser,
                Total = "100",
                Page = "2",
                PerPage = "50",
                TotalPages = "2"
            }
        };

        fixture.MockApiClient
            .Setup(x => x.GetTopArtistsAsync(TestUser, It.IsAny<LastFmPeriod>(), 50, 2))
            .ReturnsAsync(page2Response);

        // Act - EF with pagination (Skip/Take)
        var efResults = await fixture.EfContext.Artists
            .Where(a => a.User == TestUser)
            .Skip(50)  // Skip first 50 (page 1)
            .Take(50)  // Take next 50 (page 2)
            .ToListAsync();

        // Assert
        efResults.Should().HaveCount(page2Artists.Count);

        for (int i = 0; i < page2Artists.Count; i++)
        {
            efResults[i].Name.Should().Be(page2Artists[i].Name);
            efResults[i].PlayCount.Should().Be(page2Artists[i].PlayCount);
        }
    }

    [Fact]
    public async Task Pagination_InvalidSkip_ThrowsException()
    {
        // Arrange
        var fixture = new TestFixture();
        fixture.SetupTopArtists(100);

        // Act & Assert - Skip not multiple of Take
        var act = async () => await fixture.EfContext.Artists
            .Where(a => a.User == TestUser)
            .Skip(25)  // Not a multiple of Take(50)
            .Take(50)
            .ToListAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Skip (25) must be a multiple of Take (50)*");
    }

    [Fact]
    public async Task Count_OriginalVsEf_ReturnsSameValue()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupTopArtists(100);

        // Act - Original
        var originalCount = int.Parse(originalResponse.Attributes.Total);

        // Act - EF
        var efCount = await fixture.EfContext.Artists
            .Where(a => a.User == TestUser)
            .CountAsync();

        // Assert
        efCount.Should().Be(originalCount);
    }

    [Fact]
    public async Task First_OriginalVsEf_ReturnsSameResult()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupTopArtists(10);

        // Act - Original
        var originalFirst = originalResponse.Artists.First();

        // Act - EF
        var efFirst = await fixture.EfContext.Artists
            .Where(a => a.User == TestUser)
            .FirstAsync();

        // Assert
        efFirst.Name.Should().Be(originalFirst.Name);
        efFirst.PlayCount.Should().Be(originalFirst.PlayCount);
        efFirst.Rank.Should().Be(originalFirst.Attributes?.Rank ?? "");
    }

    [Fact]
    public async Task OrderByDescending_OriginalVsEf_ReturnsSameOrder()
    {
        // Arrange
        var fixture = new TestFixture();
        var originalResponse = fixture.SetupTopArtists(10);

        // Act - Original (already sorted by playcount descending from API)
        var originalResults = originalResponse.Artists;

        // Act - EF with explicit OrderByDescending
        var efResults = await fixture.EfContext.Artists
            .Where(a => a.User == TestUser)
            .OrderByDescending(a => a.PlayCount)
            .Take(10)
            .ToListAsync();

        // Assert - Order should match
        for (int i = 0; i < originalResults.Count; i++)
        {
            efResults[i].Name.Should().Be(originalResults[i].Name);

            // Verify descending order
            if (i > 0)
            {
                int currentPlayCount = int.Parse(efResults[i].PlayCount);
                int previousPlayCount = int.Parse(efResults[i - 1].PlayCount);
                currentPlayCount.Should().BeLessOrEqualTo(previousPlayCount);
            }
        }
    }
}
