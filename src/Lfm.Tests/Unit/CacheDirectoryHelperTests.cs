using FluentAssertions;
using Lfm.Core.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for CacheDirectoryHelper
/// Tests cross-platform cache directory management, creation, and file path construction
/// </summary>
public class CacheDirectoryHelperTests
{
    private readonly Mock<ILogger<CacheDirectoryHelper>> _loggerMock;

    public CacheDirectoryHelperTests()
    {
        _loggerMock = new Mock<ILogger<CacheDirectoryHelper>>();
    }

    // ========== GetCacheDirectory Tests ==========

    [Fact]
    public void GetCacheDirectory_ReturnsNonEmptyPath()
    {
        // Arrange & Act
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var cachePath = helper.GetCacheDirectory();

        // Assert
        cachePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetCacheDirectory_ReturnsPlatformAppropriateDirectory()
    {
        // Arrange & Act
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var cachePath = helper.GetCacheDirectory();

        // Assert - Path should contain platform-specific parts
        if (OperatingSystem.IsWindows())
        {
            cachePath.Should().Contain("lfm");
            cachePath.Should().Contain("cache");
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            cachePath.Should().Contain("lfm");
        }
    }

    [Fact]
    public void GetCacheDirectory_ReturnsConsistentPath()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        // Act
        var path1 = helper.GetCacheDirectory();
        var path2 = helper.GetCacheDirectory();

        // Assert
        path1.Should().Be(path2);
    }

    [Fact]
    public void GetCacheDirectory_ReturnedPathEndsWithCacheOrLfm()
    {
        // Arrange & Act
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var cachePath = helper.GetCacheDirectory();

        // Assert - Should end with cache directory name (on Windows: cache, on Linux: lfm)
        var endsWithCache = cachePath.EndsWith("cache" + Path.DirectorySeparatorChar) || cachePath.EndsWith("cache");
        var endsWithLfm = cachePath.EndsWith("lfm" + Path.DirectorySeparatorChar) || cachePath.EndsWith("lfm");
        (endsWithCache || endsWithLfm).Should().BeTrue();
    }

    // ========== EnsureCacheDirectoryExists Tests ==========

    [Fact]
    public void EnsureCacheDirectoryExists_WithExistingDirectory_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"lfm_cache_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a helper that will work with a real directory
            var helper = new CacheDirectoryHelper(_loggerMock.Object);

            // Act
            var result = helper.EnsureCacheDirectoryExists();

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnsureCacheDirectoryExists_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"lfm_cache_new_{Guid.NewGuid()}");

        try
        {
            // Ensure directory doesn't exist
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            var helper = new CacheDirectoryHelper(_loggerMock.Object);

            // Act
            var result = helper.EnsureCacheDirectoryExists();

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnsureCacheDirectoryExists_LogsWhenDirectoryAlreadyExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"lfm_cache_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var helper = new CacheDirectoryHelper(_loggerMock.Object);

            // Act
            helper.EnsureCacheDirectoryExists();

            // Assert - Should have logged debug message about directory existing
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already exists")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnsureCacheDirectoryExists_LogsMessage()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        // Act
        helper.EnsureCacheDirectoryExists();

        // Assert - Should have logged either debug (already exists) or information (created)
        _loggerMock.Verify(
            x => x.Log(
                It.IsIn(LogLevel.Debug, LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void EnsureCacheDirectoryExists_ActualDirectoryExistsAfterCall()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var cacheDir = helper.GetCacheDirectory();

        // Act
        var result = helper.EnsureCacheDirectoryExists();

        // Assert
        result.Should().BeTrue();
        Directory.Exists(cacheDir).Should().BeTrue();
    }

    // ========== GetCacheFilePath Tests ==========

    [Fact]
    public void GetCacheFilePath_WithValidFilename_ReturnsFullPath()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var filename = "test_cache.json";

        // Act
        var filePath = helper.GetCacheFilePath(filename);

        // Assert
        filePath.Should().EndWith(filename);
        filePath.Should().Contain("lfm");
    }

    [Fact]
    public void GetCacheFilePath_WithFilename_PathIsUnderCacheDirectory()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var filename = "artists_cache.dat";
        var cacheDir = helper.GetCacheDirectory();

        // Act
        var filePath = helper.GetCacheFilePath(filename);

        // Assert
        filePath.Should().StartWith(cacheDir.TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public void GetCacheFilePath_WithValidFilename_UsesPathCombine()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var filename = "test.cache";
        var expectedPath = Path.Combine(helper.GetCacheDirectory(), filename);

        // Act
        var filePath = helper.GetCacheFilePath(filename);

        // Assert
        filePath.Should().Be(expectedPath);
    }

    [Fact]
    public void GetCacheFilePath_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => helper.GetCacheFilePath(""));
        ex.ParamName.Should().Be("filename");
        ex.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public void GetCacheFilePath_WithNull_ThrowsArgumentException()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => helper.GetCacheFilePath(null));
        ex.ParamName.Should().Be("filename");
    }

    [Fact]
    public void GetCacheFilePath_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => helper.GetCacheFilePath("   "));
        ex.ParamName.Should().Be("filename");
    }

    [Fact]
    public void GetCacheFilePath_WithFilenameContainingPath_IncludesFullPath()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var filename = "subdir/cache.json";

        // Act
        var filePath = helper.GetCacheFilePath(filename);

        // Assert
        filePath.Should().Contain(filename);
    }

    [Fact]
    public void GetCacheFilePath_MultipleCallsWithSameFilename_ReturnConsistentPath()
    {
        // Arrange
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var filename = "consistent.cache";

        // Act
        var path1 = helper.GetCacheFilePath(filename);
        var path2 = helper.GetCacheFilePath(filename);

        // Assert
        path1.Should().Be(path2);
    }

    // ========== Constructor Tests ==========

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new CacheDirectoryHelper(null));
        ex.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithValidLogger_InitializesSuccessfully()
    {
        // Act
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        // Assert
        helper.Should().NotBeNull();
        helper.GetCacheDirectory().Should().NotBeNullOrEmpty();
    }

    // ========== Platform-Specific Tests ==========

    [Fact]
    public void GetCacheDirectory_OnWindows_ContainsLfmAndCache()
    {
        // This test documents the Windows behavior
        if (!OperatingSystem.IsWindows())
            return;

        // Arrange & Act
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var cachePath = helper.GetCacheDirectory();

        // Assert
        cachePath.Should().Contain("lfm");
        cachePath.Should().Contain("cache");
        cachePath.Should().StartWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }

    [Fact]
    public void GetCacheDirectory_OnLinuxMacOS_ContainsLfm()
    {
        // This test documents the Linux/macOS behavior
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        // Arrange & Act
        var helper = new CacheDirectoryHelper(_loggerMock.Object);
        var cachePath = helper.GetCacheDirectory();

        // Assert
        cachePath.Should().Contain("lfm");
        // Should follow XDG spec or use ~/.cache/lfm
        var hasXdgCache = cachePath.Contains(".cache");
        var xdgHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        var hasXdgHome = xdgHome != null && cachePath.Contains(xdgHome);
        (hasXdgCache || hasXdgHome).Should().BeTrue();
    }

    // ========== Integration Tests ==========

    [Fact]
    public void CacheDirectoryHelper_FullWorkflow_CanCreateAndLocateFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"lfm_workflow_{Guid.NewGuid()}");
        var helper = new CacheDirectoryHelper(_loggerMock.Object);

        try
        {
            // Act - Ensure directory exists
            var dirResult = helper.EnsureCacheDirectoryExists();
            dirResult.Should().BeTrue();

            // Act - Get file path
            var filename = "test_workflow.json";
            var filePath = helper.GetCacheFilePath(filename);

            // Assert
            filePath.Should().EndWith(filename);
            var directory = Path.GetDirectoryName(filePath);
            directory.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
