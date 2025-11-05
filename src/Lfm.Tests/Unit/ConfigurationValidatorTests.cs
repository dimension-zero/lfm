using FluentAssertions;
using Lfm.Core.Configuration;
using Lfm.Integration.Sonos.Models;
using Lfm.Shared.Models.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for ConfigurationValidator
/// Tests configuration validation against all validation rules
/// </summary>
public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator;
    private readonly Mock<ILogger<ConfigurationValidator>> _loggerMock;

    public ConfigurationValidatorTests()
    {
        _loggerMock = new Mock<ILogger<ConfigurationValidator>>();
        _validator = new ConfigurationValidator(_loggerMock.Object);
    }

    // ========== Basic Validation Tests ==========

    [Fact]
    public void Validate_WithNullConfig_ReturnsFail()
    {
        // Act
        var result = _validator.Validate(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ConfigurationError);
    }

    [Fact]
    public void Validate_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            ApiThrottleMs = 200,
            ParallelApiCalls = 5,
            DefaultLimit = 10,
            CacheEnabled = false,
            CircuitBreakerEnabled = false
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ========== API Configuration Tests ==========

    [Fact]
    public void Validate_WithMissingApiKey_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig { ApiKey = "" };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("ApiKey is required");
    }

    [Fact]
    public void Validate_WithNegativeApiThrottle_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            ApiThrottleMs = -100
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("ApiThrottleMs must be non-negative");
    }

    [Fact]
    public void Validate_WithExcessiveApiThrottle_LogsWarning()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            ApiThrottleMs = 15000
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Warning doesn't fail validation
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ApiThrottleMs is very high")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WithInvalidParallelApiCalls_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            ParallelApiCalls = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("ParallelApiCalls must be at least 1");
    }

    // ========== Search Configuration Tests ==========

    [Fact]
    public void Validate_WithInvalidNormalSearchDepth_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            NormalSearchDepth = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("NormalSearchDepth must be at least 1");
    }

    [Fact]
    public void Validate_WithInvalidDeepSearchTimeout_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            DeepSearchTimeoutSeconds = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("DeepSearchTimeoutSeconds must be at least 1");
    }

    // ========== Cache Configuration Tests ==========

    [Fact]
    public void Validate_WithInvalidCacheExpiry_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            CacheEnabled = true,
            CacheExpiryMinutes = -1
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("CacheExpiryMinutes must be non-negative");
    }

    [Fact]
    public void Validate_WithInvalidMaxCacheSize_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            CacheEnabled = true,
            MaxCacheSizeMB = -1
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("MaxCacheSizeMB must be non-negative");
    }

    [Fact]
    public void Validate_WithInvalidCleanupInterval_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            CacheEnabled = true,
            CleanupIntervalHours = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("CleanupIntervalHours must be at least 1");
    }

    [Fact]
    public void Validate_WithCacheDisabled_SkipsCacheValidation()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            CacheEnabled = false,
            CacheExpiryMinutes = -1 // Invalid, but should be ignored
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Passes because cache disabled
    }

    // ========== Display Configuration Tests ==========

    [Fact]
    public void Validate_WithInvalidDefaultLimit_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            DefaultLimit = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("DefaultLimit must be at least 1");
    }

    // ========== Mixtape Configuration Tests ==========

    [Fact]
    public void Validate_WithInvalidMixtapeBias_ReturnsFail()
    {
        // Arrange - bias must be 0-1
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            DefaultMixtapeBias = 1.5f
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("DefaultMixtapeBias must be between 0 and 1");
    }

    [Fact]
    public void Validate_WithValidMixtapeBias_Succeeds()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            DefaultMixtapeBias = 0.5f
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidMaxMixtapeSampleSize_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            MaxMixtapeSampleSize = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("MaxMixtapeSampleSize must be at least 1");
    }

    // ========== Spotify Configuration Tests ==========

    [Fact]
    public void Validate_WithPartialSpotifyConfig_ReturnsFail()
    {
        // Arrange - if ClientId is set, ClientSecret must be set too
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            Spotify = new() { ClientId = "test-id", ClientSecret = "" }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("Spotify.ClientSecret is required");
    }

    [Fact]
    public void Validate_WithInvalidSpotifySearchTimeout_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            Spotify = new()
            {
                ClientId = "test-id",
                ClientSecret = "test-secret",
                SearchTimeoutMs = 50 // Too low
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("Spotify.SearchTimeoutMs must be at least 100ms");
    }

    // ========== Sonos Configuration Tests ==========

    [Fact]
    public void Validate_WithInvalidSonosUrl_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            Sonos = new() { HttpApiBaseUrl = "not-a-url" }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("Sonos.HttpApiBaseUrl is not a valid URL");
    }

    [Fact]
    public void Validate_WithValidSonosUrl_Succeeds()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            Sonos = new() { HttpApiBaseUrl = "http://192.168.1.24:5005" }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidSonosScheme_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            Sonos = new() { HttpApiBaseUrl = "ftp://192.168.1.24:5005" } // FTP not allowed
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("Sonos.HttpApiBaseUrl must use http or https scheme");
    }

    [Fact]
    public void Validate_WithInvalidSonosTimeout_ReturnsFail()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "test-key",
            Sonos = new()
            {
                HttpApiBaseUrl = "http://192.168.1.24:5005",
                TimeoutMs = 50 // Too low
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.TechnicalDetails.Should().Contain("Sonos.TimeoutMs must be at least 100ms");
    }

    // ========== Multiple Errors Tests ==========

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "", // Missing
            ApiThrottleMs = -100, // Negative
            ParallelApiCalls = 0, // Too low
            DefaultLimit = 0 // Too low
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeFalse();
        var details = result.Error?.TechnicalDetails ?? "";
        details.Should().Contain("ApiKey is required");
        details.Should().Contain("ApiThrottleMs must be non-negative");
        details.Should().Contain("ParallelApiCalls must be at least 1");
        details.Should().Contain("DefaultLimit must be at least 1");
    }

    [Fact]
    public void Validate_WithValidCompleteConfig_Succeeds()
    {
        // Arrange
        var config = new LfmConfig
        {
            ApiKey = "valid-api-key",
            DefaultUsername = "testuser",
            ApiThrottleMs = 200,
            ParallelApiCalls = 5,
            DefaultLimit = 50,
            NormalSearchDepth = 10000,
            DeepSearchTimeoutSeconds = 300,
            CacheEnabled = true,
            CacheExpiryMinutes = 60,
            MaxCacheSizeMB = 500,
            CircuitBreakerEnabled = true,
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerSuccessThreshold = 2,
            DefaultMixtapeBias = 0.3f,
            Spotify = new() { /* Empty Spotify config is valid if ClientId/ClientSecret not set */ }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
