using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Services;
using Lfm.McpServer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lfm.Tests.Integration;

/// <summary>
/// Integration tests for LastFmMcpClient - validates MCP transformation layer
/// </summary>
[Trait("Category", "Integration")]
public class LastFmMcpClientTests
{
    private readonly Mock<ILastFmApiClient> _mockApiClient;
    private readonly LastFmMcpClient _mcpClient;

    public LastFmMcpClientTests()
    {
        _mockApiClient = new Mock<ILastFmApiClient>();
        _mcpClient = new LastFmMcpClient(_mockApiClient.Object, NullLogger<LastFmMcpClient>.Instance);
    }

    [Fact]
    public async Task GetTopArtistsAsync_WithSuccessfulApiResponse_ReturnsCompactArtists()
    {
        // Arrange
        var apiResponse = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist
                {
                    Name = "Pink Floyd",
                    PlayCount = "1234",
                    Url = "https://www.last.fm/music/Pink+Floyd",  // Should be stripped
                    Mbid = "83d91898-7763-47d7-b03b-b92132375c47",  // Should be stripped
                    Attributes = new ArtistAttributes { Rank = "1" }
                }
            }
        };

        _mockApiClient
            .Setup(c => c.GetTopArtistsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopArtists>.Ok(apiResponse));

        // Act
        var result = await _mcpClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var artist = result.Data![0];
        artist.Name.Should().Be("Pink Floyd");
        artist.PlayCount.Should().Be("1234");
        artist.Rank.Should().Be("1");

        // Verify token optimization: Url and Mbid should not exist in CompactArtist
        typeof(CompactArtist).GetProperty("Url").Should().BeNull();
        typeof(CompactArtist).GetProperty("Mbid").Should().BeNull();
    }

    [Fact]
    public async Task GetTopTracksAsync_WithSuccessfulApiResponse_ReturnsCompactTracksWithFlattenedArtist()
    {
        // Arrange
        var apiResponse = new TopTracks
        {
            Tracks = new List<Track>
            {
                new Track
                {
                    Name = "Money",
                    PlayCount = "567",
                    Artist = new ArtistInfo { Name = "Pink Floyd" },  // Nested artist object
                    Url = "https://www.last.fm/music/Pink+Floyd/_/Money",  // Should be stripped
                    Mbid = "track-mbid-123",  // Should be stripped
                    Attributes = new TrackAttributes { Rank = "2" }
                }
            }
        };

        _mockApiClient
            .Setup(c => c.GetTopTracksWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopTracks>.Ok(apiResponse));

        // Act
        var result = await _mcpClient.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var track = result.Data![0];
        track.Name.Should().Be("Money");
        track.PlayCount.Should().Be("567");
        track.Artist.Should().Be("Pink Floyd", "artist name should be flattened from nested object");
        track.Rank.Should().Be("2");

        // Verify property flattening: Artist should be string, not nested object
        track.Artist.Should().BeOfType<string>();
    }

    [Fact]
    public async Task GetTopAlbumsAsync_WithSuccessfulApiResponse_ReturnsCompactAlbumsWithFlattenedArtist()
    {
        // Arrange
        var apiResponse = new TopAlbums
        {
            Albums = new List<Album>
            {
                new Album
                {
                    Name = "The Dark Side of the Moon",
                    PlayCount = "890",
                    Artist = new ArtistInfo { Name = "Pink Floyd" },  // Nested artist object
                    Url = "https://www.last.fm/music/Pink+Floyd/The+Dark+Side+of+the+Moon",  // Should be stripped
                    Mbid = "album-mbid-456",  // Should be stripped
                    Attributes = new AlbumAttributes { Rank = "1" }
                }
            }
        };

        _mockApiClient
            .Setup(c => c.GetTopAlbumsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopAlbums>.Ok(apiResponse));

        // Act
        var result = await _mcpClient.GetTopAlbumsAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var album = result.Data![0];
        album.Name.Should().Be("The Dark Side of the Moon");
        album.PlayCount.Should().Be("890");
        album.Artist.Should().Be("Pink Floyd", "artist name should be flattened from nested object");
        album.Rank.Should().Be("1");

        // Verify property flattening
        album.Artist.Should().BeOfType<string>();
    }

    [Fact]
    public async Task GetTopArtistsAsync_WithApiFailure_ReturnsFailureResult()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ApiError, "API timeout");

        _mockApiClient
            .Setup(c => c.GetTopArtistsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopArtists>.Fail(error));

        // Act
        var result = await _mcpClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("API timeout");
        result.Error.Type.Should().Be(ErrorType.ApiError);
    }

    [Fact]
    public async Task GetTopTracksAsync_WithApiFailure_ReturnsFailureResult()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.DataError, "User not found");

        _mockApiClient
            .Setup(c => c.GetTopTracksWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopTracks>.Fail(error));

        // Act
        var result = await _mcpClient.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("User not found");
        result.Error.Type.Should().Be(ErrorType.DataError);
    }

    [Fact]
    public async Task GetTopAlbumsAsync_WithApiFailure_ReturnsFailureResult()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ConfigurationError, "Configuration error");

        _mockApiClient
            .Setup(c => c.GetTopAlbumsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopAlbums>.Fail(error));

        // Act
        var result = await _mcpClient.GetTopAlbumsAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("Configuration error");
        result.Error.Type.Should().Be(ErrorType.ConfigurationError);
    }

    [Fact]
    public async Task GetTopArtistsAsync_WithMultipleArtists_PreservesRankOrder()
    {
        // Arrange
        var apiResponse = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "Artist1", PlayCount = "100", Attributes = new ArtistAttributes { Rank = "1" } },
                new Artist { Name = "Artist2", PlayCount = "90", Attributes = new ArtistAttributes { Rank = "2" } },
                new Artist { Name = "Artist3", PlayCount = "80", Attributes = new ArtistAttributes { Rank = "3" } }
            }
        };

        _mockApiClient
            .Setup(c => c.GetTopArtistsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopArtists>.Ok(apiResponse));

        // Act
        var result = await _mcpClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 3);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data![0].Rank.Should().Be("1");
        result.Data[1].Rank.Should().Be("2");
        result.Data[2].Rank.Should().Be("3");
    }

    [Fact]
    public async Task GetTopTracksAsync_WithMissingRank_HandlesGracefully()
    {
        // Arrange
        var apiResponse = new TopTracks
        {
            Tracks = new List<Track>
            {
                new Track
                {
                    Name = "Track1",
                    PlayCount = "50",
                    Artist = new ArtistInfo { Name = "Artist1" },
                    Attributes = null  // Missing rank
                }
            }
        };

        _mockApiClient
            .Setup(c => c.GetTopTracksWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopTracks>.Ok(apiResponse));

        // Act
        var result = await _mcpClient.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data![0].Rank.Should().BeNull("rank should be null when attributes are missing");
    }

    [Fact]
    public async Task GetTopAlbumsAsync_WithEmptyList_ReturnsEmptyCompactList()
    {
        // Arrange
        var apiResponse = new TopAlbums
        {
            Albums = new List<Album>()  // Empty list
        };

        _mockApiClient
            .Setup(c => c.GetTopAlbumsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<TopAlbums>.Ok(apiResponse));

        // Act
        var result = await _mcpClient.GetTopAlbumsAsync("testuser", LastFmPeriod.Overall, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
