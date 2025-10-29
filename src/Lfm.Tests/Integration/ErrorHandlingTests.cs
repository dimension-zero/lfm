using Lfm.Data.Direct;
using Lfm.Data.Direct.Cache;
using Lfm.Shared.Interfaces;
using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Lfm.Shared.Models.Results;
using Lfm.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lfm.Tests.Integration;

/// <summary>
/// Integration tests for error handling - validates graceful degradation
/// </summary>
[Trait("Category", "Integration")]
public class ErrorHandlingTests
{
    private readonly Mock<ILastFmApiClient> _mockInnerClient;
    private readonly InMemoryCacheStorage _cacheStorage;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly Mock<IConfigurationManager> _mockConfigManager;
    private readonly CachedLastFmApiClient _cachedClient;

    public ErrorHandlingTests()
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

        _cachedClient.DisableThrottling = true;
    }

    [Fact]
    public async Task ApiError_PropagatedThroughCache()
    {
        // Arrange
        _mockInnerClient
            .Setup(c => c.GetTopArtistsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopArtists>.ApiError("API timeout"));

        // Act
        var result = await _cachedClient.GetTopArtistsWithResultAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.ApiError);
        result.Error.Message.Should().Be("API timeout");

        // Error results should not be cached
        _cacheStorage.StoreCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidationError_NotCached()
    {
        // Arrange
        _mockInnerClient
            .Setup(c => c.GetTopTracksWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopTracks>.ValidationError("Invalid username"));

        // Act
        await _cachedClient.GetTopTracksWithResultAsync("invaliduser", LastFmPeriod.Overall, 10, 1);
        await _cachedClient.GetTopTracksWithResultAsync("invaliduser", LastFmPeriod.Overall, 10, 1);

        // Assert - Errors should never be cached
        _cacheStorage.StoreCount.Should().Be(0, "validation errors should not be cached");
    }

    [Fact]
    public async Task DataError_GracefullyHandled()
    {
        // Arrange
        _mockInnerClient
            .Setup(c => c.GetTopAlbumsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopAlbums>.DataError("No albums found"));

        // Act
        var result = await _cachedClient.GetTopAlbumsWithResultAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.DataError);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ConfigurationError_StopsExecution()
    {
        // Arrange
        _mockInnerClient
            .Setup(c => c.GetTopArtistsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopArtists>.ConfigurationError("Missing API key"));

        // Act
        var result = await _cachedClient.GetTopArtistsWithResultAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.ConfigurationError);
        result.Error.RequiresUserAction.Should().BeTrue("configuration errors require user intervention");
    }

    [Fact]
    public async Task NetworkError_IsRetryable()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.NetworkError, "Connection timeout");

        _mockInnerClient
            .Setup(c => c.GetTopTracksWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopTracks>.Fail(error));

        // Act
        var result = await _cachedClient.GetTopTracksWithResultAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NetworkError);
        result.Error.IsRetryable.Should().BeTrue("network errors should be retryable");
    }

    [Fact]
    public async Task RateLimitError_ProperlyCategorized()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.RateLimitError, "Rate limit exceeded");

        _mockInnerClient
            .Setup(c => c.GetTopArtistsWithResultAsync(It.IsAny<string>(), It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopArtists>.Fail(error));

        // Act
        var result = await _cachedClient.GetTopArtistsWithResultAsync("testuser", LastFmPeriod.Overall, 10, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.RateLimitError);
    }

    [Fact]
    public async Task MultipleErrorTypes_HandledIndependently()
    {
        // Arrange
        _mockInnerClient
            .Setup(c => c.GetTopArtistsWithResultAsync("user1", It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopArtists>.ApiError("API error"));

        _mockInnerClient
            .Setup(c => c.GetTopArtistsWithResultAsync("user2", It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopArtists>.ValidationError("Validation error"));

        _mockInnerClient
            .Setup(c => c.GetTopArtistsWithResultAsync("user3", It.IsAny<LastFmPeriod>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<Lfm.Shared.Models.TopArtists>.DataError("Data error"));

        // Act
        var result1 = await _cachedClient.GetTopArtistsWithResultAsync("user1", LastFmPeriod.Overall, 10, 1);
        var result2 = await _cachedClient.GetTopArtistsWithResultAsync("user2", LastFmPeriod.Overall, 10, 1);
        var result3 = await _cachedClient.GetTopArtistsWithResultAsync("user3", LastFmPeriod.Overall, 10, 1);

        // Assert
        result1.Error!.Type.Should().Be(ErrorType.ApiError);
        result2.Error!.Type.Should().Be(ErrorType.ValidationError);
        result3.Error!.Type.Should().Be(ErrorType.DataError);

        // All should be failures
        result1.IsFailure.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
        result3.IsFailure.Should().BeTrue();

        // None should be cached
        _cacheStorage.StoreCount.Should().Be(0);
    }

    [Fact]
    public void ErrorResult_DisplaySymbolsWork()
    {
        // Arrange
        var apiError = new ErrorResult(ErrorType.ApiError, "API failed");
        var validationError = new ErrorResult(ErrorType.ValidationError, "Invalid input");
        var configError = new ErrorResult(ErrorType.ConfigurationError, "Missing config");

        // Act & Assert - Unicode symbols
        apiError.GetDisplaySymbol(useUnicode: true).Should().Be("🌐");
        validationError.GetDisplaySymbol(useUnicode: true).Should().Be("⚠️");
        configError.GetDisplaySymbol(useUnicode: true).Should().Be("⚙️");

        // Act & Assert - ASCII symbols
        apiError.GetDisplaySymbol(useUnicode: false).Should().Be("[API]");
        validationError.GetDisplaySymbol(useUnicode: false).Should().Be("[WARN]");
        configError.GetDisplaySymbol(useUnicode: false).Should().Be("[CONFIG]");
    }
}
