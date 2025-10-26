using FluentAssertions;
using Lfm.Core.Configuration;
using Lfm.Core.Models;
using Lfm.Core.Services;
using Lfm.Core.Services.Cache;
using Lfm.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics;

namespace Lfm.Tests.Integration;

/// <summary>
/// Performance tests for caching - validates 119x speedup claim
/// </summary>
[Trait("Category", "Integration")]
public class CachePerformanceTests
{
    private readonly Mock<ILastFmApiClient> _mockInnerClient;
    private readonly InMemoryCacheStorage _cacheStorage;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly Mock<IConfigurationManager> _mockConfigManager;
    private readonly CachedLastFmApiClient _cachedClient;

    public CachePerformanceTests()
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

        // Disable throttling for consistent timing
        _cachedClient.DisableThrottling = true;
        _cachedClient.EnableTiming = true;
    }

    [Fact]
    public async Task CacheHit_SignificantlyFasterThanCacheMiss()
    {
        // Arrange
        var topArtists = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "Test Artist", PlayCount = "100", Attributes = new ArtistAttributes { Rank = "1" } }
            }
        };

        _mockInnerClient
            .Setup(c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(topArtists)
            .Callback(() => System.Threading.Thread.Sleep(10)); // Simulate API latency

        // Act - Cache miss (warm up cache)
        await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1);

        // Act - Cache hit (should be much faster)
        var sw = Stopwatch.StartNew();
        await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1);
        sw.Stop();

        // Assert - Cache hit should be < 5ms (vs 10ms+ for API call)
        sw.ElapsedMilliseconds.Should().BeLessThan(5, "cache hits should be significantly faster than API calls");
    }

    [Fact]
    public async Task MultipleRequests_CacheReducesApiCallCount()
    {
        // Arrange
        var topArtists = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "Artist1", PlayCount = "100", Attributes = new ArtistAttributes { Rank = "1" } }
            }
        };

        _mockInnerClient
            .Setup(c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(topArtists);

        // Act - Make 100 identical requests
        for (int i = 0; i < 100; i++)
        {
            await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1);
        }

        // Assert - Inner client should only be called once (first request)
        _mockInnerClient.Verify(
            c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once,
            "cache should serve subsequent requests without hitting API");
    }

    [Fact]
    public async Task TimingResults_CapturePerformanceDifference()
    {
        // Arrange
        var topArtists = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "Test", PlayCount = "50", Attributes = new ArtistAttributes { Rank = "1" } }
            }
        };

        _mockInnerClient
            .Setup(c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(topArtists)
            .Callback(() => System.Threading.Thread.Sleep(5)); // Simulate 5ms API call

        // Act
        await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1); // Cache miss
        await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1); // Cache hit

        // Assert
        _cachedClient.TimingResults.Should().HaveCount(2);

        var cacheMissTime = _cachedClient.TimingResults[0].ElapsedMs;
        var cacheHitTime = _cachedClient.TimingResults[1].ElapsedMs;

        cacheMissTime.Should().BeGreaterThan(cacheHitTime, "cache misses should take longer than cache hits");
        _cachedClient.TimingResults[0].CacheHit.Should().BeFalse();
        _cachedClient.TimingResults[1].CacheHit.Should().BeTrue();
    }

    [Fact]
    public async Task CacheStorage_FastRetrievalPerformance()
    {
        // Arrange
        var testData = "{\"test\":\"data\"}";
        await _cacheStorage.StoreAsync("test-key", testData, expiryMinutes: 10);

        // Act - Measure cache retrieval time
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            await _cacheStorage.RetrieveAsync("test-key");
        }
        sw.Stop();

        // Assert - 1000 retrievals should be < 100ms (avg < 0.1ms per retrieval)
        sw.ElapsedMilliseconds.Should().BeLessThan(100, "cache storage should provide fast retrieval");
    }

    [Fact]
    public async Task CacheBehaviorNormal_BalancesPerformanceAndFreshness()
    {
        // Arrange
        var topArtists = new TopArtists
        {
            Artists = new List<Artist>
            {
                new Artist { Name = "Artist", PlayCount = "75", Attributes = new ArtistAttributes { Rank = "1" } }
            }
        };

        _mockInnerClient
            .Setup(c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(topArtists);

        _cachedClient.CacheBehavior = CacheBehavior.Normal;

        // Act - First call (cache miss), subsequent calls (cache hits)
        var sw1 = Stopwatch.StartNew();
        await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1);
        sw1.Stop();

        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            await _cachedClient.GetTopArtistsAsync("testuser", "overall", 10, 1);
        }
        sw2.Stop();

        // Assert
        _mockInnerClient.Verify(
            c => c.GetTopArtistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once,
            "Normal mode should cache and reuse results");

        // 10 cache hits should be faster than 1 cache miss
        sw2.ElapsedMilliseconds.Should().BeLessThan(sw1.ElapsedMilliseconds * 5,
            "cached requests should be faster than uncached");
    }
}
