using Lfm.Shared.Interfaces;
using Lfm.Core.Configuration;
using Lfm.Data.Direct;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using System.Text.Json;
using Lfm.McpServer.Services;
using Lfm.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lfm.Tests;

/// <summary>
/// Tests comparing JSON output size between original (full) and compact models.
/// Measures actual token reduction achieved by transformation rules.
/// </summary>
public class OutputComparisonTests
{
    private readonly ILastFmApiClient _mockClient;
    private readonly LastFmMcpClient _mcpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public OutputComparisonTests()
    {
        // Get path to test-data directory
        var testDataPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "test-data", "lastfm-responses"
        );
        testDataPath = Path.GetFullPath(testDataPath);

        _mockClient = new MockLastFmApiClient(testDataPath);
        _mcpClient = new LastFmMcpClient(_mockClient, NullLogger<LastFmMcpClient>.Instance);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false  // Compact JSON for accurate size comparison
        };
    }

    [Fact]
    public async Task TopArtists_CompactIsSmaller()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopArtistsWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopArtistsAsync("testuser");

        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var originalJson = JsonSerializer.Serialize(originalResult.Data!.Artists, _jsonOptions);
        var compactJson = JsonSerializer.Serialize(compactResult.Data, _jsonOptions);

        var originalSize = originalJson.Length;
        var compactSize = compactJson.Length;

        // Assert: Compact should be smaller
        Assert.True(compactSize < originalSize,
            $"Compact JSON ({compactSize} chars) should be smaller than original ({originalSize} chars)");

        // Calculate reduction percentage
        var reduction = (originalSize - compactSize) / (double)originalSize * 100;

        // Output for documentation
        Console.WriteLine($"Original size: {originalSize} characters");
        Console.WriteLine($"Compact size: {compactSize} characters");
        Console.WriteLine($"Reduction: {reduction:F1}%");
    }

    [Fact]
    public async Task TopTracks_CompactIsSmaller()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopTracksWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopTracksAsync("testuser");

        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var originalJson = JsonSerializer.Serialize(originalResult.Data!.Tracks, _jsonOptions);
        var compactJson = JsonSerializer.Serialize(compactResult.Data, _jsonOptions);

        var originalSize = originalJson.Length;
        var compactSize = compactJson.Length;

        // Assert: Compact should be smaller
        Assert.True(compactSize < originalSize,
            $"Compact JSON ({compactSize} chars) should be smaller than original ({originalSize} chars)");

        // Calculate reduction percentage
        var reduction = (originalSize - compactSize) / (double)originalSize * 100;

        // Output for documentation
        Console.WriteLine($"Original size: {originalSize} characters");
        Console.WriteLine($"Compact size: {compactSize} characters");
        Console.WriteLine($"Reduction: {reduction:F1}%");
    }

    [Fact]
    public async Task TopAlbums_CompactIsSmaller()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopAlbumsWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopAlbumsAsync("testuser");

        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var originalJson = JsonSerializer.Serialize(originalResult.Data!.Albums, _jsonOptions);
        var compactJson = JsonSerializer.Serialize(compactResult.Data, _jsonOptions);

        var originalSize = originalJson.Length;
        var compactSize = compactJson.Length;

        // Assert: Compact should be smaller
        Assert.True(compactSize < originalSize,
            $"Compact JSON ({compactSize} chars) should be smaller than original ({originalSize} chars)");

        // Calculate reduction percentage
        var reduction = (originalSize - compactSize) / (double)originalSize * 100;

        // Output for documentation
        Console.WriteLine($"Original size: {originalSize} characters");
        Console.WriteLine($"Compact size: {compactSize} characters");
        Console.WriteLine($"Reduction: {reduction:F1}%");
    }

    [Fact]
    public async Task TopArtists_DataMatches()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopArtistsWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopArtistsAsync("testuser");

        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var original = originalResult.Data!.Artists;
        var compact = compactResult.Data!;

        // Assert: Same count
        Assert.Equal(original.Count, compact.Count);

        // Assert: Each artist matches
        for (int i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Name, compact[i].Name);
            Assert.Equal(original[i].PlayCount, compact[i].PlayCount);
            Assert.Equal(original[i].Attributes?.Rank, compact[i].Rank);
        }
    }

    [Fact]
    public async Task TopTracks_DataMatches()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopTracksWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopTracksAsync("testuser");

        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var original = originalResult.Data!.Tracks;
        var compact = compactResult.Data!;

        // Assert: Same count
        Assert.Equal(original.Count, compact.Count);

        // Assert: Each track matches (including flattened artist)
        for (int i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Name, compact[i].Name);
            Assert.Equal(original[i].PlayCount, compact[i].PlayCount);
            Assert.Equal(original[i].Artist.Name, compact[i].Artist);  // Flattened
            Assert.Equal(original[i].Attributes?.Rank, compact[i].Rank);
        }
    }

    [Fact]
    public async Task TopAlbums_DataMatches()
    {
        // Arrange & Act
        var originalResult = await _mockClient.GetTopAlbumsWithResultAsync("testuser");
        var compactResult = await _mcpClient.GetTopAlbumsAsync("testuser");

        Assert.True(originalResult.Success);
        Assert.True(compactResult.Success);

        var original = originalResult.Data!.Albums;
        var compact = compactResult.Data!;

        // Assert: Same count
        Assert.Equal(original.Count, compact.Count);

        // Assert: Each album matches (including flattened artist)
        for (int i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Name, compact[i].Name);
            Assert.Equal(original[i].PlayCount, compact[i].PlayCount);
            Assert.Equal(original[i].Artist.Name, compact[i].Artist);  // Flattened
            Assert.Equal(original[i].Attributes?.Rank, compact[i].Rank);
        }
    }
}
