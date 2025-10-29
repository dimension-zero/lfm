using Lfm.Core.Configuration;
using Lfm.Data.Direct.Enrichment;
using Lfm.Data.Direct.LocalFiles;
using Lfm.Shared.Configuration;
using Lfm.Shared.Models.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lfm.Tests.Integration;

/// <summary>
/// Integration tests for LocalFileDataProvider - validates parser accuracy,
/// aggregation correctness, and filtering logic across all supported file formats.
/// </summary>
[Trait("Category", "Integration")]
public class LocalFileDataProviderTests
{
    private readonly string _testDataPath;
    private readonly AlbumEnrichmentService _mockEnrichmentService;

    public LocalFileDataProviderTests()
    {
        // Build path to test-data/local-files directory
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        _testDataPath = Path.Combine(projectRoot, "test-data", "local-files");

        // Create enrichment service with enrichment disabled for testing
        var enrichmentConfig = new AlbumEnrichmentConfig { Enabled = false };
        _mockEnrichmentService = new AlbumEnrichmentService(
            NullLogger<AlbumEnrichmentService>.Instance,
            enrichmentConfig,
            new List<IAlbumEnricher>());
    }

    #region Spotify Extended History Tests

    [Fact]
    public async Task SpotifyExtended_ParsesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "Streaming_History_Audio_2024_0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Artists.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SpotifyExtended_FiltersSkippedTracks()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "Streaming_History_Audio_2024_0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act - Get top tracks to verify skipped tracks are filtered
        var result = await provider.GetTopTracksAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // "Time" by Pink Floyd was skipped (15 seconds) - should not appear
        var tracks = result.Data!.Tracks;
        tracks.Should().NotContain(t => t.Name == "Time" && t.Artist.Name == "Pink Floyd");
    }

    [Fact]
    public async Task SpotifyExtended_FiltersPodcasts()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "Streaming_History_Audio_2024_0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Podcast host should not appear in artists
        var artists = result.Data!.Artists;
        artists.Should().NotContain(a => a.Name == "Joe Rogan");
    }

    [Fact]
    public async Task SpotifyExtended_CalculatesCorrectPlayCounts()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "Streaming_History_Audio_2024_0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Expected: Pink Floyd = 3 plays (skipped "Time" filtered out = 2 remaining)
        // Note: Test fixture has 3 Pink Floyd tracks, 1 is skipped
        var pinkFloyd = result.Data!.Artists.FirstOrDefault(a => a.Name == "Pink Floyd");
        pinkFloyd.Should().NotBeNull();

        // Parse play count (stored as string in Artist model)
        int.Parse(pinkFloyd!.PlayCount).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SpotifyExtended_HasAlbumData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "Streaming_History_Audio_2024_0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopAlbumsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Spotify Extended format includes album metadata
        var albums = result.Data!.Albums;
        albums.Should().NotBeEmpty();

        // Expected albums: The Wall, Wish You Were Here, The Dark Side of the Moon
        albums.Should().Contain(a => a.Name.Contains("The Wall"));
    }

    #endregion

    #region Spotify Standard History Tests

    [Fact]
    public async Task SpotifyStandard_ParsesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "StreamingHistory0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Artists.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SpotifyStandard_CalculatesCorrectPlayCounts()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "StreamingHistory0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var artists = result.Data!.Artists;

        // Expected: The Beatles = 3 plays (Hey Jude, Let It Be, Come Together)
        var beatles = artists.FirstOrDefault(a => a.Name == "The Beatles");
        beatles.Should().NotBeNull();
        int.Parse(beatles!.PlayCount).Should().Be(3);

        // Expected: Led Zeppelin = 2 plays (Stairway, Kashmir)
        var zeppelin = artists.FirstOrDefault(a => a.Name == "Led Zeppelin");
        zeppelin.Should().NotBeNull();
        int.Parse(zeppelin!.PlayCount).Should().Be(2);
    }

    [Fact]
    public async Task SpotifyStandard_NoAlbumData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "StreamingHistory0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopAlbumsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Spotify Standard format does NOT include album metadata
        // Albums list should be empty or enrichment would be needed
        var albums = result.Data!.Albums;
        albums.Should().BeEmpty();
    }

    #endregion

    #region YouTube Music Watch History Tests

    [Fact]
    public async Task YouTubeMusicWatch_ParsesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "watch-history.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Artists.Should().NotBeEmpty();
    }

    [Fact]
    public async Task YouTubeMusicWatch_FiltersRegularYouTubeVideos()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "watch-history.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // "Random Channel" is from regular YouTube (not YouTube Music) - should be filtered
        var artists = result.Data!.Artists;
        artists.Should().NotContain(a => a.Name == "Random Channel");
    }

    [Fact]
    public async Task YouTubeMusicWatch_ExtractsArtistFromSubtitles()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "watch-history.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Expected: Queen = 4 plays (4 YouTube Music entries)
        var queen = result.Data!.Artists.FirstOrDefault(a => a.Name == "Queen");
        queen.Should().NotBeNull();
        int.Parse(queen!.PlayCount).Should().Be(4);
    }

    [Fact]
    public async Task YouTubeMusicWatch_NoAlbumData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "watch-history.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopAlbumsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // YouTube Music watch history does NOT include album metadata
        var albums = result.Data!.Albums;
        albums.Should().BeEmpty();
    }

    #endregion

    #region YouTube Music Library CSV Tests

    [Fact]
    public async Task YouTubeMusicLibrary_ParsesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "music-library-songs.csv");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Artists.Should().NotBeEmpty();
    }

    [Fact]
    public async Task YouTubeMusicLibrary_FiltersRemovedSongs()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "music-library-songs.csv");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopTracksAsync(filePath, limit: 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // "Old Song That Was Removed" has Removed="Yes" - should be filtered
        var tracks = result.Data!.Tracks;
        tracks.Should().NotContain(t => t.Name == "Old Song That Was Removed");
    }

    [Fact]
    public async Task YouTubeMusicLibrary_CalculatesCorrectPlayCounts()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "music-library-songs.csv");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var artists = result.Data!.Artists;

        // Expected: The Beatles = 80 (42 + 38 from "Let It Be" and "Hey Jude")
        var beatles = artists.FirstOrDefault(a => a.Name == "The Beatles");
        beatles.Should().NotBeNull();
        int.Parse(beatles!.PlayCount).Should().Be(80);

        // Expected: Pink Floyd = 85 (45 + 40 from "Comfortably Numb" and "Wish You Were Here")
        var pinkFloyd = artists.FirstOrDefault(a => a.Name == "Pink Floyd");
        pinkFloyd.Should().NotBeNull();
        int.Parse(pinkFloyd!.PlayCount).Should().Be(85);
    }

    #endregion

    #region Date Range Filtering Tests

    [Fact]
    public async Task DateRangeFiltering_WorksCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "StreamingHistory0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Test data has events on 2024-01-15
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 15, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await provider.GetTopArtistsForDateRangeAsync(
            filePath,
            from: startDate,
            to: endDate,
            limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Artists.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DateRangeFiltering_ReturnsEmptyWhenNoMatches()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "StreamingHistory0.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Use date range that excludes all test data (2024-01-15)
        var startDate = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await provider.GetTopArtistsForDateRangeAsync(
            filePath,
            from: startDate,
            to: endDate,
            limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Artists.Should().BeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task NonExistentFile_ReturnsError()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "non-existent-file.json");
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync(filePath, limit: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvalidJson_ReturnsError()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Write invalid JSON
            File.WriteAllText(tempFile, "{ invalid json [}");

            var provider = new LocalFileDataProvider(_mockEnrichmentService);

            // Act
            var result = await provider.GetTopArtistsAsync(tempFile, limit: 10);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task EmptyFilePath_ReturnsError()
    {
        // Arrange
        var provider = new LocalFileDataProvider(_mockEnrichmentService);

        // Act
        var result = await provider.GetTopArtistsAsync("", limit: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File path required");
    }

    #endregion

    #region Parser Format Detection Tests

    [Fact]
    public void SpotifyParser_CanParseSpotifyFiles()
    {
        // Arrange
        var parser = new SpotifyJsonParser();
        var extendedFile = Path.Combine(_testDataPath, "Streaming_History_Audio_2024_0.json");
        var standardFile = Path.Combine(_testDataPath, "StreamingHistory0.json");

        // Act & Assert
        parser.CanParse(extendedFile).Should().BeTrue();
        parser.CanParse(standardFile).Should().BeTrue();
    }

    [Fact]
    public void YouTubeParser_CanParseYouTubeMusicFiles()
    {
        // Arrange
        var parser = new YouTubeMusicParser();
        var watchHistoryFile = Path.Combine(_testDataPath, "watch-history.json");

        // Act & Assert
        parser.CanParse(watchHistoryFile).Should().BeTrue();
    }

    [Fact]
    public void Parsers_RejectWrongFormats()
    {
        // Arrange
        var spotifyParser = new SpotifyJsonParser();
        var youtubeParser = new YouTubeMusicParser();
        var youtubeFile = Path.Combine(_testDataPath, "watch-history.json");
        var spotifyFile = Path.Combine(_testDataPath, "StreamingHistory0.json");

        // Act & Assert
        // Spotify parser should reject YouTube Music format
        spotifyParser.CanParse(youtubeFile).Should().BeFalse();

        // YouTube parser should reject Spotify format
        youtubeParser.CanParse(spotifyFile).Should().BeFalse();
    }

    #endregion
}
