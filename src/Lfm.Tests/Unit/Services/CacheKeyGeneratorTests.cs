using Lfm.Data.Direct;
using Lfm.Shared.Interfaces;
using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Lfm.Data.Direct.Cache;

namespace Lfm.Tests.Unit.Services;

/// <summary>
/// Unit tests for CacheKeyGenerator - critical for cache correctness (119x speedup depends on this)
/// </summary>
[Trait("Category", "Unit")]
public class CacheKeyGeneratorTests
{
    private readonly ICacheKeyGenerator _generator = new CacheKeyGenerator();

    #region Key Consistency Tests

    [Fact]
    public void GenerateKey_WithSameParameters_ReturnsSameKey()
    {
        // Arrange
        var method = "user.getTopTracks";
        var user = "testuser";
        var period = "overall";
        var limit = 10;
        var page = 1;

        // Act
        var key1 = _generator.GenerateKey(method, user, period, limit, page);
        var key2 = _generator.GenerateKey(method, user, period, limit, page);

        // Assert
        key1.Should().Be(key2, "same parameters should produce same cache key");
    }

    [Fact]
    public void GenerateKey_WithDifferentUser_ReturnsDifferentKey()
    {
        // Act
        var key1 = _generator.GenerateKey("user.getTopTracks", "user1", "overall", 10, 1);
        var key2 = _generator.GenerateKey("user.getTopTracks", "user2", "overall", 10, 1);

        // Assert
        key1.Should().NotBe(key2, "different users should produce different cache keys");
    }

    [Fact]
    public void GenerateKey_WithDifferentPeriod_ReturnsDifferentKey()
    {
        // Act
        var key1 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 10, 1);
        var key2 = _generator.GenerateKey("user.getTopTracks", "testuser", "7day", 10, 1);

        // Assert
        key1.Should().NotBe(key2, "different periods should produce different cache keys");
    }

    [Fact]
    public void GenerateKey_WithDifferentLimit_ReturnsDifferentKey()
    {
        // Act
        var key1 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 10, 1);
        var key2 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 20, 1);

        // Assert
        key1.Should().NotBe(key2, "different limits should produce different cache keys");
    }

    [Fact]
    public void GenerateKey_WithDifferentPage_ReturnsDifferentKey()
    {
        // Act
        var key1 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 10, 1);
        var key2 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 10, 2);

        // Assert
        key1.Should().NotBe(key2, "different pages should produce different cache keys");
    }

    #endregion

    #region Case Normalization Tests

    [Fact]
    public void GenerateKey_IsCaseInsensitiveForUser()
    {
        // Act
        var key1 = _generator.GenerateKey("user.getTopTracks", "TestUser", "overall", 10, 1);
        var key2 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 10, 1);

        // Assert
        key1.Should().Be(key2, "user should be case-insensitive");
    }

    [Fact]
    public void GenerateKey_IsCaseInsensitiveForMethod()
    {
        // Act
        var key1 = _generator.GenerateKey("User.GetTopTracks", "testuser", "overall", 10, 1);
        var key2 = _generator.GenerateKey("user.gettoptracks", "testuser", "overall", 10, 1);

        // Assert
        key1.Should().Be(key2, "method should be case-insensitive");
    }

    [Fact]
    public void GenerateKey_IsCaseInsensitiveForPeriod()
    {
        // Act
        var key1 = _generator.GenerateKey("user.getTopTracks", "testuser", "Overall", 10, 1);
        var key2 = _generator.GenerateKey("user.getTopTracks", "testuser", "overall", 10, 1);

        // Assert
        key1.Should().Be(key2, "period should be case-insensitive");
    }

    #endregion

    #region Extension Method Tests

    [Fact]
    public void ForTopTracks_GeneratesConsistentKey()
    {
        // Act
        var key1 = _generator.ForTopTracks("testuser", "overall", 10, 1);
        var key2 = _generator.ForTopTracks("testuser", "overall", 10, 1);

        // Assert
        key1.Should().Be(key2);
        key1.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ForTopArtists_GeneratesConsistentKey()
    {
        // Act
        var key1 = _generator.ForTopArtists("testuser", "overall", 10, 1);
        var key2 = _generator.ForTopArtists("testuser", "overall", 10, 1);

        // Assert
        key1.Should().Be(key2);
        key1.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ForTopAlbums_GeneratesConsistentKey()
    {
        // Act
        var key1 = _generator.ForTopAlbums("testuser", "overall", 10, 1);
        var key2 = _generator.ForTopAlbums("testuser", "overall", 10, 1);

        // Assert
        key1.Should().Be(key2);
        key1.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void GenerateKey_WithNullMethod_ThrowsArgumentException()
    {
        // Act
        Action act = () => _generator.GenerateKey(null!, "testuser", "overall", 10, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("method");
    }

    [Fact]
    public void GenerateKey_WithNullPeriod_ThrowsArgumentException()
    {
        // Act
        Action act = () => _generator.GenerateKey("user.getTopTracks", "testuser", null!, 10, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("period");
    }

    #endregion
}
