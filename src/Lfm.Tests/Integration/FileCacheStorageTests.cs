using FluentAssertions;
using Lfm.Core.Configuration;
using Lfm.Core.Services.Cache;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lfm.Tests.Integration;

/// <summary>
/// Integration tests for FileCacheStorage - validates actual file I/O operations
/// </summary>
[Trait("Category", "Integration")]
public class FileCacheStorageTests : IDisposable
{
    private readonly string _testCacheDirectory;
    private readonly FileCacheStorage _storage;
    private readonly TestCacheDirectoryHelper _cacheHelper;

    public FileCacheStorageTests()
    {
        // Create unique test directory for each test run
        _testCacheDirectory = Path.Combine(Path.GetTempPath(), $"lfm-test-cache-{Guid.NewGuid()}");
        _cacheHelper = new TestCacheDirectoryHelper(_testCacheDirectory);
        _storage = new FileCacheStorage(_cacheHelper, NullLogger<FileCacheStorage>.Instance);
    }

    public void Dispose()
    {
        // Cleanup test directory after each test
        if (Directory.Exists(_testCacheDirectory))
        {
            Directory.Delete(_testCacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task StoreAsync_CreatesDataAndMetadataFiles()
    {
        // Arrange
        var key = "test-key";
        var jsonData = "{\"test\":\"data\"}";

        // Act
        var result = await _storage.StoreAsync(key, jsonData, expiryMinutes: 10);

        // Assert
        result.Should().BeTrue();
        File.Exists(Path.Combine(_testCacheDirectory, $"{key}.json")).Should().BeTrue();
        File.Exists(Path.Combine(_testCacheDirectory, $"{key}.meta")).Should().BeTrue();
    }

    [Fact]
    public async Task StoreAndRetrieve_RoundTripPreservesData()
    {
        // Arrange
        var key = "roundtrip-key";
        var jsonData = "{\"artist\":\"Pink Floyd\",\"playcount\":1234}";

        // Act
        await _storage.StoreAsync(key, jsonData, expiryMinutes: 10);
        var retrieved = await _storage.RetrieveAsync(key);

        // Assert
        retrieved.Should().Be(jsonData);
    }

    [Fact]
    public async Task RetrieveAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _storage.RetrieveAsync(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_WithExpiredEntry_ReturnsNull()
    {
        // Arrange
        var key = "expired-key";
        var jsonData = "{\"test\":\"data\"}";

        // Store with very short expiry
        await _storage.StoreAsync(key, jsonData, expiryMinutes: 0);

        // Wait for expiration (plus small buffer)
        await Task.Delay(100);

        // Act
        var result = await _storage.RetrieveAsync(key);

        // Assert
        result.Should().BeNull("expired cache entries should return null");
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "exists-key";
        var jsonData = "{\"test\":\"data\"}";
        await _storage.StoreAsync(key, jsonData, expiryMinutes: 10);

        // Act
        var exists = await _storage.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var exists = await _storage.ExistsAsync(key);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_DeletesDataAndMetadataFiles()
    {
        // Arrange
        var key = "remove-key";
        var jsonData = "{\"test\":\"data\"}";
        await _storage.StoreAsync(key, jsonData, expiryMinutes: 10);

        // Act
        var result = await _storage.RemoveAsync(key);

        // Assert
        result.Should().BeTrue();
        File.Exists(Path.Combine(_testCacheDirectory, $"{key}.json")).Should().BeFalse();
        File.Exists(Path.Combine(_testCacheDirectory, $"{key}.meta")).Should().BeFalse();
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllCacheFiles()
    {
        // Arrange
        await _storage.StoreAsync("key1", "{\"data\":1}", expiryMinutes: 10);
        await _storage.StoreAsync("key2", "{\"data\":2}", expiryMinutes: 10);
        await _storage.StoreAsync("key3", "{\"data\":3}", expiryMinutes: 10);

        // Act
        var result = await _storage.ClearAllAsync();

        // Assert
        result.Should().BeTrue();
        Directory.GetFiles(_testCacheDirectory).Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        await _storage.StoreAsync("key1", "{\"data\":1}", expiryMinutes: 10);
        await _storage.StoreAsync("key2", "{\"data\":2}", expiryMinutes: 10);
        await _storage.StoreAsync("key3", "{\"data\":3}", expiryMinutes: 10);

        // Act
        var stats = await _storage.GetStatisticsAsync();

        // Assert
        stats.TotalEntries.Should().Be(3);
        stats.TotalFiles.Should().Be(6); // 3 .json + 3 .meta
        stats.TotalSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CleanupExpiredAsync_RemovesOnlyExpiredEntries()
    {
        // Arrange
        await _storage.StoreAsync("valid-key", "{\"data\":1}", expiryMinutes: 10);
        await _storage.StoreAsync("expired-key", "{\"data\":2}", expiryMinutes: 0);

        await Task.Delay(100); // Wait for expiration

        // Act
        var removedCount = await _storage.CleanupExpiredAsync();

        // Assert
        removedCount.Should().Be(1);
        var validExists = await _storage.ExistsAsync("valid-key");
        var expiredExists = await _storage.ExistsAsync("expired-key");

        validExists.Should().BeTrue("valid entry should remain");
        expiredExists.Should().BeFalse("expired entry should be removed");
    }
}

/// <summary>
/// Test helper for cache directory operations
/// </summary>
internal class TestCacheDirectoryHelper : ICacheDirectoryHelper
{
    private readonly string _cacheDirectory;

    public TestCacheDirectoryHelper(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
    }

    public string GetCacheDirectory() => _cacheDirectory;

    public string GetCacheFilePath(string fileName) => Path.Combine(_cacheDirectory, fileName);

    public bool EnsureCacheDirectoryExists()
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
        return true;
    }

    public bool CacheDirectoryExists() => Directory.Exists(_cacheDirectory);
}
