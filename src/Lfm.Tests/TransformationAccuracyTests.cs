using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using Lfm.Shared.Services;
using Lfm.McpServer.Services;
using Lfm.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lfm.Tests;

/// <summary>
/// Tests that verify transformation rules are applied correctly:
/// - Token optimization (Url, Mbid excluded)
/// - Property flattening (Artist.Name → artist)
/// - Data preservation (Name, PlayCount, Rank)
/// </summary>
public class TransformationAccuracyTests
{
    private readonly ILastFmApiClient _mockClient;
    private readonly LastFmMcpClient _mcpClient;

    public TransformationAccuracyTests()
    {
        // Get path to test-data directory (3 levels up from bin/Debug/net9.0)
        var testDataPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "test-data", "lastfm-responses"
        );
        testDataPath = Path.GetFullPath(testDataPath);

        _mockClient = new MockLastFmApiClient(testDataPath);
        _mcpClient = new LastFmMcpClient(_mockClient, NullLogger<LastFmMcpClient>.Instance);
    }

    [Fact]
    public async Task CompactArtist_PreservesEssentialData()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopArtistsWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopArtistsAsync("testuser");

        // Assert
        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var original = originalResult.Data!.Artists.First();
        var compact = compactResult.Data!.First();

        // Essential data preserved
        Assert.Equal(original.Name, compact.Name);
        Assert.Equal(original.PlayCount, compact.PlayCount);
        Assert.Equal(original.Attributes?.Rank, compact.Rank);
    }

    [Fact]
    public async Task CompactArtist_ExcludesUrlAndMbid()
    {
        // Arrange & Act
        var compactResult = await _mcpClient.GetTopArtistsAsync("testuser");

        // Assert
        Assert.True(compactResult.Success);
        var compact = compactResult.Data!.First();

        // CompactArtist should not have Url or Mbid properties
        var compactType = compact.GetType();
        Assert.Null(compactType.GetProperty("Url"));
        Assert.Null(compactType.GetProperty("Mbid"));

        // Verify type is actually CompactArtist
        Assert.Equal("CompactArtist", compactType.Name);
    }

    [Fact]
    public async Task CompactTrack_FlattenedArtistIsCorrect()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopTracksWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopTracksAsync("testuser");

        // Assert
        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var original = originalResult.Data!.Tracks.First();
        var compact = compactResult.Data!.First();

        // Flattening: Track.Artist.Name → artist (string)
        Assert.Equal(original.Artist.Name, compact.Artist);

        // Essential data preserved
        Assert.Equal(original.Name, compact.Name);
        Assert.Equal(original.PlayCount, compact.PlayCount);
        Assert.Equal(original.Attributes?.Rank, compact.Rank);
    }

    [Fact]
    public async Task CompactTrack_ExcludesUrlAndMbid()
    {
        // Arrange & Act
        var compactResult = await _mcpClient.GetTopTracksAsync("testuser");

        // Assert
        Assert.True(compactResult.Success);
        var compact = compactResult.Data!.First();

        // CompactTrack should not have Url or Mbid properties
        var compactType = compact.GetType();
        Assert.Null(compactType.GetProperty("Url"));
        Assert.Null(compactType.GetProperty("Mbid"));

        // Verify type is actually CompactTrack
        Assert.Equal("CompactTrack", compactType.Name);
    }

    [Fact]
    public async Task CompactAlbum_FlattenedArtistIsCorrect()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopAlbumsWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopAlbumsAsync("testuser");

        // Assert
        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var original = originalResult.Data!.Albums.First();
        var compact = compactResult.Data!.First();

        // Flattening: Album.Artist.Name → artist (string)
        Assert.Equal(original.Artist.Name, compact.Artist);

        // Essential data preserved
        Assert.Equal(original.Name, compact.Name);
        Assert.Equal(original.PlayCount, compact.PlayCount);
        Assert.Equal(original.Attributes?.Rank, compact.Rank);
    }

    [Fact]
    public async Task CompactAlbum_ExcludesUrlAndMbid()
    {
        // Arrange & Act
        var compactResult = await _mcpClient.GetTopAlbumsAsync("testuser");

        // Assert
        Assert.True(compactResult.Success);
        var compact = compactResult.Data!.First();

        // CompactAlbum should not have Url or Mbid properties
        var compactType = compact.GetType();
        Assert.Null(compactType.GetProperty("Url"));
        Assert.Null(compactType.GetProperty("Mbid"));

        // Verify type is actually CompactAlbum
        Assert.Equal("CompactAlbum", compactType.Name);
    }

    [Fact]
    public async Task AllThreeArtists_AreTransformed()
    {
        // Arrange & Act
        var compactResult = await _mcpClient.GetTopArtistsAsync("testuser");

        // Assert
        Assert.True(compactResult.Success);
        Assert.Equal(3, compactResult.Data!.Count);

        // Verify all three artists from fixture
        var artists = compactResult.Data;
        Assert.Contains(artists, a => a.Name == "Pink Floyd" && a.PlayCount == "1234" && a.Rank == "1");
        Assert.Contains(artists, a => a.Name == "The Beatles" && a.PlayCount == "987" && a.Rank == "2");
        Assert.Contains(artists, a => a.Name == "Radiohead" && a.PlayCount == "765" && a.Rank == "3");
    }

    [Fact]
    public async Task AllThreeTracks_AreTransformed()
    {
        // Arrange & Act
        var compactResult = await _mcpClient.GetTopTracksAsync("testuser");

        // Assert
        Assert.True(compactResult.Success);
        Assert.Equal(3, compactResult.Data!.Count);

        // Verify all three tracks from fixture
        var tracks = compactResult.Data;
        Assert.Contains(tracks, t => t.Name == "Comfortably Numb" && t.Artist == "Pink Floyd" && t.Rank == "1");
        Assert.Contains(tracks, t => t.Name == "Hey Jude" && t.Artist == "The Beatles" && t.Rank == "2");
        Assert.Contains(tracks, t => t.Name == "Creep" && t.Artist == "Radiohead" && t.Rank == "3");
    }

    [Fact]
    public async Task AllThreeAlbums_AreTransformed()
    {
        // Arrange & Act
        var compactResult = await _mcpClient.GetTopAlbumsAsync("testuser");

        // Assert
        Assert.True(compactResult.Success);
        Assert.Equal(3, compactResult.Data!.Count);

        // Verify all three albums from fixture
        var albums = compactResult.Data;
        Assert.Contains(albums, a => a.Name == "The Dark Side of the Moon" && a.Artist == "Pink Floyd" && a.Rank == "1");
        Assert.Contains(albums, a => a.Name == "Abbey Road" && a.Artist == "The Beatles" && a.Rank == "2");
        Assert.Contains(albums, a => a.Name == "OK Computer" && a.Artist == "Radiohead" && a.Rank == "3");
    }
}
