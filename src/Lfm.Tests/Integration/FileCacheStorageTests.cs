using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Lfm.Shared.Models.Results;
using Lfm.Data.Direct.Cache;
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
    private readonly TestConfigurationManager _configManager;

    public FileCacheStorageTests()
    {
        // Create unique test directory for each test run
        _testCacheDirectory = Path.Combine(Path.GetTempPath(), $"lfm-test-cache-{Guid.NewGuid()}");
        _cacheHelper = new TestCacheDirectoryHelper(_testCacheDirectory);
        _configManager = new TestConfigurationManager();
        _storage = new FileCacheStorage(_cacheHelper, NullLogger<FileCacheStorage>.Instance, _configManager);
    }

    public void Dispose()
    {
        // Cleanup test directory after each test
        if (Directory.Exists(_testCacheDirectory))
        {
            // Retry directory deletion to handle async file operations from cache cleanup
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(_testCacheDirectory, recursive: true);
                    break;
                }
                catch (UnauthorizedAccessException) when (i < 2)
                {
                    // Cache's fire-and-forget cleanup may still be running
                    Thread.Sleep(100);
                }
            }
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

    [Fact]
    public async Task EnforceCacheSizeLimit_EvictsOldestEntriesWhenOverLimit()
    {
        // Arrange - Create a custom config manager with a small cache size limit (1 KB)
        var smallCacheConfig = new TestConfigurationManager(maxCacheSizeMB: 0.001); // 1 KB limit
        var storage = new FileCacheStorage(_cacheHelper, NullLogger<FileCacheStorage>.Instance, smallCacheConfig);

        // Store 3 entries (each ~50 bytes, total ~150 bytes > 1 KB limit)
        await storage.StoreAsync("entry1", "{\"data\":\"" + new string('a', 500) + "\"}", expiryMinutes: 60);
        await Task.Delay(100); // Ensure different LastAccessedAt times

        await storage.StoreAsync("entry2", "{\"data\":\"" + new string('b', 500) + "\"}", expiryMinutes: 60);
        await Task.Delay(100);

        await storage.StoreAsync("entry3", "{\"data\":\"" + new string('c', 500) + "\"}", expiryMinutes: 60);

        // Act - Run cleanup which should trigger LRU eviction
        var removedCount = await storage.CleanupAsync();

        // Assert - Oldest entries (entry1, entry2) should be evicted, entry3 should remain
        removedCount.Should().BeGreaterThan(0, "should have evicted entries to enforce size limit");

        var stats = await storage.GetStatisticsAsync();
        stats.TotalSizeBytes.Should().BeLessThanOrEqualTo(1024, "cache size should be under 1 KB limit");
    }

    [Fact]
    public async Task RetrieveAsync_UpdatesLastAccessedTime()
    {
        // Arrange
        await _storage.StoreAsync("test-key", "{\"test\":\"data\"}", expiryMinutes: 60);
        var originalTime = DateTime.UtcNow;

        await Task.Delay(200); // Wait to ensure LastAccessedAt will be different

        // Act - Retrieve the entry (should update LastAccessedAt)
        var result = await _storage.RetrieveAsync("test-key");

        // Assert
        result.Should().NotBeNull();

        // Read the metadata directly to verify LastAccessedAt was updated
        var metaPath = Path.Combine(_testCacheDirectory, "test-key.meta");
        var metaJson = await File.ReadAllTextAsync(metaPath);
        metaJson.Should().Contain("lastAccessedAt", "metadata should include lastAccessedAt field");
    }

    [Fact]
    public async Task LRUEviction_EnforcesCacheSizeLimitCorrectly()
    {
        // Arrange - Create storage with very small cache limit
        var tinyConfig = new TestConfigurationManager(maxCacheSizeMB: 0.001); // 1 KB
        var storage = new FileCacheStorage(_cacheHelper, NullLogger<FileCacheStorage>.Instance, tinyConfig);

        // Create several small entries that collectively exceed the limit
        for (int i = 1; i <= 10; i++)
        {
            await storage.StoreAsync($"entry{i}", $"{{\"data\":{i}}}", expiryMinutes: 60);
            await Task.Delay(10); // Ensure different LastAccessedAt times
        }

        // Act - Trigger cleanup which should evict oldest entries
        var removedCount = await storage.CleanupAsync();

        // Assert - Should have evicted some entries and now be under the limit
        removedCount.Should().BeGreaterThan(0, "should have evicted entries to enforce size limit");

        var stats = await storage.GetStatisticsAsync();
        stats.TotalSizeBytes.Should().BeLessThanOrEqualTo(1024, "cache size should be under 1KB limit after cleanup");
        stats.TotalEntries.Should().BeLessThan(10, "some entries should have been evicted");
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

/// <summary>
/// Test helper for configuration manager
/// </summary>
internal class TestConfigurationManager : IConfigurationManager
{
    private readonly LfmConfig _config;

    public TestConfigurationManager(double maxCacheSizeMB = 100)
    {
        _config = new LfmConfig
        {
            MaxCacheSizeMB = (int)maxCacheSizeMB,
            CacheExpiryMinutes = 10,
            ApiKey = "test-key",
            DefaultUsername = "test-user"
        };
    }

    public Task<LfmConfig> LoadAsync() => Task.FromResult(_config);

    public Task<Result<LfmConfig>> LoadWithValidationAsync() => Task.FromResult(Result<LfmConfig>.Ok(_config));

    public Task SaveAsync(LfmConfig config) => Task.CompletedTask;

    public string GetConfigPath() => Path.Combine(Path.GetTempPath(), "test-config.json");
}
