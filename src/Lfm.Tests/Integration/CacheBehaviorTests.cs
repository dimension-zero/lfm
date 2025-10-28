using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Services;
using Lfm.Core.Services.Cache;
using Lfm.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lfm.Tests.Integration;

/// <summary>
/// Integration tests for CachedLastFmApiClient - validates cache decorator behavior
/// </summary>
[Trait("Category", "Integration")]
public class CacheBehaviorTests
{
    private readonly Mock<ILastFmApiClient> _mockInnerClient;
    private readonly InMemoryCacheStorage _cacheStorage;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly Mock<IConfigurationManager> _mockConfigManager;
    private readonly CachedLastFmApiClient _cachedClient;

    public CacheBehaviorTests()
    {
        _mockInnerClient = new Mock<ILastFmApiClient>();
        _cacheStorage = new InMemoryCacheStorage();
        _keyGenerator = new CacheKeyGenerator();
        _mockConfigManager = new Mock<IConfigurationManager>();

        var config = new LfmConfig();
        _mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(config);

        _cachedClient = new CachedLastFmApiClient(
            _mockInnerClient.Object,
            _cacheStorage,
            _keyGenerator,
            NullLogger<CachedLastFmApiClient>.Instance,
            _mockConfigManager.Object,
            defaultCacheExpiryMinutes: 10
        );

        // Disable throttling for faster tests
        _cachedClient.DisableThrottling = true;
    }

    [Fact]
    public async Task CacheMiss_CallsInnerClientAndCachesResult()
    {
        // Arrange
        var topArtists = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "Pink Floyd", PlayCount = "1234", Attributes = new ArtistAttributes { Rank = "1" } }
            }
        };

        _mockInnerClient
            .Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1))
            .ReturnsAsync(topArtists);

        // Act
        var result = await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.Should().NotBeNull();
        result!.Artists.Should().HaveCount(1);
        result.Artists[0].Name.Should().Be("Pink Floyd");

        // Verify inner client was called
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1), Times.Once);

        // Verify data was stored in cache
        _cacheStorage.StoreCount.Should().Be(1);
    }

    [Fact]
    public async Task CacheHit_DoesNotCallInnerClient()
    {
        // Arrange
        var topArtists = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "The Beatles", PlayCount = "987", Attributes = new ArtistAttributes { Rank = "1" } }
            }
        };

        _mockInnerClient
            .Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1))
            .ReturnsAsync(topArtists);

        // First call - cache miss
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        _mockInnerClient.Invocations.Clear(); // Reset call count

        // Act - Second call should hit cache
        var result = await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.Should().NotBeNull();
        result!.Artists[0].Name.Should().Be("The Beatles");

        // Verify inner client was NOT called on second request
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);

        // Verify cache was read
        _cacheStorage.RetrieveCount.Should().Be(2); // Once for miss, once for hit
    }

    [Fact]
    public async Task DifferentParameters_ProduceDifferentCacheKeys()
    {
        // Arrange
        var artists1 = new TopArtists { Artists = new List<Artist> { new Artist { Name = "Artist1" } } };
        var artists2 = new TopArtists { Artists = new List<Artist> { new Artist { Name = "Artist2" } } };

        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("user1", LastFmPeriod.Overall, 10, 1)).ReturnsAsync(artists1);
        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("user2", LastFmPeriod.Overall, 10, 1)).ReturnsAsync(artists2);

        // Act
        var result1 = await _cachedClient.GetTopArtistsAsync("user1", LastFmPeriod.Overall, 10, 1);
        var result2 = await _cachedClient.GetTopArtistsAsync("user2", LastFmPeriod.Overall, 10, 1);

        // Assert
        result1!.Artists[0].Name.Should().Be("Artist1");
        result2!.Artists[0].Name.Should().Be("Artist2");

        // Both should be cache misses (different users = different keys)
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync("user1", LastFmPeriod.Overall, 10, 1), Times.Once);
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync("user2", LastFmPeriod.Overall, 10, 1), Times.Once);

        // Should have 2 cache entries
        _cacheStorage.StoreCount.Should().Be(2);
    }

    [Fact]
    public async Task CacheBehaviorNoCache_AlwaysCallsInnerClient()
    {
        // Arrange
        var topArtists = new TopArtists { Artists = new List<Artist> { new Artist { Name = "Test" } } };
        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1)).ReturnsAsync(topArtists);

        _cachedClient.CacheBehavior = CacheBehavior.NoCache;

        // Act
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert - Inner client should be called twice (cache disabled)
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1), Times.Exactly(2));

        // Cache should not be used
        _cacheStorage.StoreCount.Should().Be(0);
    }

    [Fact]
    public async Task CacheBehaviorForceApi_SkipsCacheAndUpdates()
    {
        // Arrange
        var oldArtists = new TopArtists { Artists = new List<Artist> { new Artist { Name = "Old" } } };
        var newArtists = new TopArtists { Artists = new List<Artist> { new Artist { Name = "New" } } };

        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1))
            .ReturnsAsync(oldArtists);

        // First call - populate cache
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Change return value
        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1))
            .ReturnsAsync(newArtists);

        _cachedClient.CacheBehavior = CacheBehavior.ForceApi;

        // Act - Should force API call and update cache
        var result = await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result!.Artists[0].Name.Should().Be("New", "cache should be updated with new data");

        // Inner client should be called twice (initial + force API)
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1), Times.Exactly(2));
    }

    [Fact]
    public async Task NullResultFromInnerClient_NotCached()
    {
        // Arrange
        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1))
            .ReturnsAsync((TopArtists?)null);

        // Act
        var result = await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.Should().BeNull();

        // Null results should not be cached
        _cacheStorage.StoreCount.Should().Be(0);
    }

    [Fact]
    public async Task EnableTiming_CapturesPerformanceMetrics()
    {
        // Arrange
        var topArtists = new TopArtists { Artists = new List<Artist> { new Artist { Name = "Test" } } };
        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1)).ReturnsAsync(topArtists);

        _cachedClient.EnableTiming = true;

        // Act
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1); // Cache miss
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1); // Cache hit

        // Assert
        _cachedClient.TimingResults.Should().HaveCount(2);
        _cachedClient.TimingResults[0].CacheHit.Should().BeFalse("first call is cache miss");
        _cachedClient.TimingResults[1].CacheHit.Should().BeTrue("second call is cache hit");
        _cachedClient.TimingResults[0].ElapsedMs.Should().BeGreaterThanOrEqualTo(0, "timing should be captured");
        _cachedClient.TimingResults[1].ElapsedMs.Should().BeGreaterThanOrEqualTo(0, "timing should be captured");
    }

    [Fact]
    public async Task MultipleEntityTypes_CachedIndependently()
    {
        // Arrange
        var artists = new TopArtists { Artists = new List<Artist> { new Artist { Name = "Artist" } } };
        var tracks = new TopTracks { Tracks = new List<Track> { new Track { Name = "Track", Artist = new ArtistInfo { Name = "Artist" } } } };

        _mockInnerClient.Setup(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1)).ReturnsAsync(artists);
        _mockInnerClient.Setup(c => c.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10, 1)).ReturnsAsync(tracks);

        // Act
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);
        await _cachedClient.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Second calls should hit cache
        await _cachedClient.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1);
        await _cachedClient.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        _mockInnerClient.Verify(c => c.GetTopArtistsAsync("testuser", LastFmPeriod.Overall, 10, 1), Times.Once);
        _mockInnerClient.Verify(c => c.GetTopTracksAsync("testuser", LastFmPeriod.Overall, 10, 1), Times.Once);

        // Should have 2 separate cache entries
        _cacheStorage.StoreCount.Should().Be(2);
    }
}
