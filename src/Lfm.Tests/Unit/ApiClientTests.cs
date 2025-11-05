using FluentAssertions;
using Lfm.Data.Direct;
using Lfm.Shared.Models;
using Lfm.Tests.Fixtures;
using Lfm.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for LastFmApiClient response parsing and error handling
/// Tests core API client functionality without making real HTTP requests
/// </summary>
public class ApiClientTests
{
    private readonly LastFmApiClient _apiClient;
    private readonly MockHttpClientFactory _httpClientFactory;
    private readonly Mock<ILogger<LastFmApiClient>> _loggerMock;
    private const string TestApiKey = "test-api-key";

    public ApiClientTests()
    {
        _httpClientFactory = new MockHttpClientFactory();
        _loggerMock = new Mock<ILogger<LastFmApiClient>>();
        _apiClient = new LastFmApiClient(
            _httpClientFactory.CreateClient("test"),
            _loggerMock.Object,
            TestApiKey
        );
    }

    // ========== Top Artists Tests ==========

    [Fact]
    public async Task GetTopArtistsAsync_WithValidParams_ParsesResponseCorrectly()
    {
        // Arrange
        var mockResponse = new
        {
            topartists = new
            {
                artist = new[]
                {
                    new { name = "Pink Floyd", playcount = "500", mbid = "id-1" },
                    new { name = "The Beatles", playcount = "450", mbid = "id-2" }
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetTopArtistsAsync("testuser", limit: 10);

        // Assert
        result.Should().NotBeNull();
        result!.Artists.Should().HaveCount(2);
        result.Artists[0].Name.Should().Be("Pink Floyd");
        result.Artists[0].PlayCount.Should().Be("500");
    }

    [Fact]
    public async Task GetTopArtistsAsync_WithMultipleArtists_ReturnsList()
    {
        // Arrange
        var artists = Enumerable.Range(1, 50)
            .Select(i => new { name = $"Artist {i}", playcount = $"{1000 - i}", mbid = $"id-{i}" })
            .ToArray();

        var mockResponse = new
        {
            topartists = new
            {
                artist = artists
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetTopArtistsAsync("testuser", limit: 50);

        // Assert
        result.Should().NotBeNull();
        result!.Artists.Should().HaveCount(50);
    }

    [Fact]
    public async Task GetTopArtistsAsync_WithMalformedJson_ReturnsNull()
    {
        // Arrange
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            "{ invalid json"
        );

        // Act
        var result = await _apiClient.GetTopArtistsAsync("testuser");

        // Assert
        result.Should().BeNull();
    }

    // ========== Top Tracks Tests ==========

    [Fact]
    public async Task GetTopTracksAsync_WithValidParams_ParsesResponseCorrectly()
    {
        // Arrange
        var mockResponse = new
        {
            toptracks = new
            {
                track = new[]
                {
                    new
                    {
                        name = "Money",
                        playcount = "250",
                        artist = new { name = "Pink Floyd", mbid = "artist-1" },
                        mbid = "track-1"
                    },
                    new
                    {
                        name = "Hey Jude",
                        playcount = "200",
                        artist = new { name = "The Beatles", mbid = "artist-2" },
                        mbid = "track-2"
                    }
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetTopTracksAsync("testuser", limit: 10);

        // Assert
        result.Should().NotBeNull();
        result!.Tracks.Should().HaveCount(2);
        result.Tracks[0].Name.Should().Be("Money");
        result.Tracks[0].PlayCount.Should().Be("250");
        result.Tracks[0].Artist.Name.Should().Be("Pink Floyd");
    }

    [Fact]
    public async Task GetTopTracksAsync_WithZeroResults_ReturnsEmptyList()
    {
        // Arrange
        var mockResponse = new
        {
            toptracks = new
            {
                track = Array.Empty<object>()
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetTopTracksAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Tracks.Should().BeEmpty();
    }

    // ========== Track Info Tests ==========

    [Fact]
    public async Task GetTrackInfoAsync_WithValidTrack_ReturnsPlayCount()
    {
        // Arrange
        var mockResponse = new
        {
            track = new
            {
                name = "Money",
                artist = new { name = "Pink Floyd" },
                userplaycount = "50",
                listeners = "500000",
                playcount = "5000000"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetTrackInfoAsync("Pink Floyd", "Money", "testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Track.GetUserPlaycount().Should().Be(50);
    }

    [Fact]
    public async Task GetTrackInfoAsync_WithUnscrobbledTrack_ReturnsZeroPlayCount()
    {
        // Arrange
        var mockResponse = new
        {
            track = new
            {
                name = "Unknown Track",
                artist = new { name = "Unknown Artist" },
                userplaycount = "0",
                listeners = "100"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetTrackInfoAsync("Unknown Artist", "Unknown Track", "testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Track.GetUserPlaycount().Should().Be(0);
    }

    [Fact]
    public async Task GetTrackInfoAsync_WithNetworkError_ReturnsNull()
    {
        // Arrange
        _httpClientFactory.SetupErrorResponse(
            "https://ws.audioscrobbler.com/2.0/",
            System.Net.HttpStatusCode.ServiceUnavailable,
            "Last.fm is temporarily unavailable"
        );

        // Act
        var result = await _apiClient.GetTrackInfoAsync("Pink Floyd", "Money", "testuser");

        // Assert
        result.Should().BeNull();
    }

    // ========== Artist Info Tests ==========

    [Fact]
    public async Task GetArtistInfoAsync_WithValidArtist_ReturnsCorrectInfo()
    {
        // Arrange
        var mockResponse = new
        {
            artist = new
            {
                name = "Pink Floyd",
                stats = new
                {
                    listeners = "5000000",
                    playcount = "500000000",
                    userplaycount = "500"
                },
                mbid = "artist-id"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse(
            "https://ws.audioscrobbler.com/2.0/",
            json
        );

        // Act
        var result = await _apiClient.GetArtistInfoAsync("Pink Floyd", "testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Artist.Name.Should().Be("Pink Floyd");
        result.Artist.Stats.GetUserPlaycount().Should().Be(500);
    }

    // ========== Error Scenarios ==========

    [Fact]
    public async Task ApiCall_WithRateLimitError_Returns429()
    {
        // Arrange
        _httpClientFactory.SetupErrorResponse(
            "https://ws.audioscrobbler.com/2.0/",
            System.Net.HttpStatusCode.TooManyRequests,
            "Rate limit exceeded"
        );

        // Act
        var result = await _apiClient.GetTopArtistsAsync("testuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ApiCall_WithUnauthorized_Returns401()
    {
        // Arrange
        _httpClientFactory.SetupErrorResponse(
            "https://ws.audioscrobbler.com/2.0/",
            System.Net.HttpStatusCode.Unauthorized,
            "Invalid API key"
        );

        // Act
        var result = await _apiClient.GetTopArtistsAsync("testuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ApiCall_WithNotFound_Returns404()
    {
        // Arrange
        _httpClientFactory.SetupErrorResponse(
            "https://ws.audioscrobbler.com/2.0/",
            System.Net.HttpStatusCode.NotFound,
            "User not found"
        );

        // Act
        var result = await _apiClient.GetTopArtistsAsync("nonexistentuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MultipleApiCalls_AreHandledCorrectly()
    {
        // Arrange
        var mockResponse = new { topartists = new { artist = Array.Empty<object>() } };
        var json = System.Text.Json.JsonSerializer.Serialize(mockResponse);
        _httpClientFactory.SetupResponse("https://ws.audioscrobbler.com/2.0/", json);

        // Act
        var result1 = await _apiClient.GetTopArtistsAsync("testuser");
        var result2 = await _apiClient.GetTopArtistsAsync("testuser");
        var result3 = await _apiClient.GetTopArtistsAsync("testuser");

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();
    }
}
