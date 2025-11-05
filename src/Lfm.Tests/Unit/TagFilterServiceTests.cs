using FluentAssertions;
using Lfm.Core.Services;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for TagFilterService
/// Tests tag-based artist filtering logic
/// </summary>
public class TagFilterServiceTests
{
    private readonly Mock<ILastFmApiClient> _apiClientMock;
    private readonly Mock<ILogger<TagFilterService>> _loggerMock;
    private readonly TagFilterService _service;

    public TagFilterServiceTests()
    {
        _apiClientMock = new Mock<ILastFmApiClient>();
        _loggerMock = new Mock<ILogger<TagFilterService>>();
        _service = new TagFilterService(_apiClientMock.Object, _loggerMock.Object);
    }

    private LfmConfig CreateConfig(bool enableFiltering = true, int threshold = 10, params string[] excludedTags)
    {
        return new LfmConfig
        {
            ApiKey = "test-key",
            EnableTagFiltering = enableFiltering,
            TagFilterThreshold = threshold,
            ExcludedTags = excludedTags.ToList()
        };
    }

    private TopTags CreateArtistTags(params (string name, int count)[] tags)
    {
        return new TopTags
        {
            Tags = tags.Select(t => new Tag { Name = t.name, Count = t.count }).ToList()
        };
    }

    // ========== ShouldExcludeArtist Tests ==========

    [Fact]
    public void ShouldExcludeArtist_WithFilteringDisabled_ReturnsFalse()
    {
        // Arrange
        var config = CreateConfig(enableFiltering: false, excludedTags: "rock");
        var tags = CreateArtistTags(("rock", 100));

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldExcludeArtist_WithNoExcludedTags_ReturnsFalse()
    {
        // Arrange
        var config = CreateConfig(enableFiltering: true); // No excluded tags
        var tags = CreateArtistTags(("rock", 100));

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldExcludeArtist_WithNullTags_ReturnsFalse()
    {
        // Arrange
        var config = CreateConfig(enableFiltering: true, excludedTags: "rock");

        // Act
        var result = _service.ShouldExcludeArtist(null, config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldExcludeArtist_WithEmptyTags_ReturnsFalse()
    {
        // Arrange
        var config = CreateConfig(enableFiltering: true, excludedTags: "rock");
        var tags = CreateArtistTags(); // Empty tags

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldExcludeArtist_WithMatchingExcludedTag_ReturnsTrue()
    {
        // Arrange
        var config = CreateConfig(threshold: 10, excludedTags: "spam");
        var tags = CreateArtistTags(("spam", 50));

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldExcludeArtist_WithCaseInsensitiveMatch_ReturnsTrue()
    {
        // Arrange
        var config = CreateConfig(threshold: 10, excludedTags: "Rock");
        var tags = CreateArtistTags(("ROCK", 50));

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldExcludeArtist_WithTagBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var config = CreateConfig(threshold: 50, excludedTags: "spam");
        var tags = CreateArtistTags(("spam", 30)); // Below threshold

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeFalse(); // Tag exists but count is too low
    }

    [Fact]
    public void ShouldExcludeArtist_WithTagAtThreshold_ReturnsTrue()
    {
        // Arrange
        var config = CreateConfig(threshold: 50, excludedTags: "spam");
        var tags = CreateArtistTags(("spam", 50)); // At threshold

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldExcludeArtist_WithMultipleTags_ExcludesIfAnyMatch()
    {
        // Arrange
        var config = CreateConfig(true, 10, "spam", "cover");
        var tags = CreateArtistTags(
            ("rock", 100),
            ("cover", 50),
            ("alternative", 30));

        // Act
        var result = _service.ShouldExcludeArtist(tags, config);

        // Assert
        result.Should().BeTrue(); // "cover" matches
    }

    // ========== FilterArtistsAsync Tests ==========

    [Fact]
    public async Task FilterArtistsAsync_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        var config = CreateConfig();
        var artistNames = new List<string>();

        // Act
        var (filtered, excluded) = await _service.FilterArtistsAsync(artistNames, config);

        // Assert
        filtered.Should().BeEmpty();
        excluded.Should().Be(0);
    }

    [Fact]
    public async Task FilterArtistsAsync_WithAllValidArtists_ReturnsAll()
    {
        // Arrange
        var config = CreateConfig(threshold: 10, excludedTags: "spam");
        var artistNames = new List<string> { "Artist A", "Artist B" };

        _apiClientMock
            .Setup(x => x.GetArtistTopTagsAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string artist, bool autocorrect) =>
                CreateArtistTags(("rock", 100)));

        // Act
        var (filtered, excluded) = await _service.FilterArtistsAsync(artistNames, config);

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Should().Contain(new[] { "Artist A", "Artist B" });
        excluded.Should().Be(0);
    }

    [Fact]
    public async Task FilterArtistsAsync_WithExcludedArtist_FiltersIt()
    {
        // Arrange
        var config = CreateConfig(threshold: 10, excludedTags: "spam");
        var artistNames = new List<string> { "Good Artist", "Bad Artist" };

        _apiClientMock
            .Setup(x => x.GetArtistTopTagsAsync("Good Artist", It.IsAny<bool>()))
            .ReturnsAsync(CreateArtistTags(("rock", 100)));
        _apiClientMock
            .Setup(x => x.GetArtistTopTagsAsync("Bad Artist", It.IsAny<bool>()))
            .ReturnsAsync(CreateArtistTags(("spam", 100)));

        // Act
        var (filtered, excluded) = await _service.FilterArtistsAsync(artistNames, config);

        // Assert
        filtered.Should().HaveCount(1);
        filtered.Should().Contain("Good Artist");
        filtered.Should().NotContain("Bad Artist");
        excluded.Should().Be(1);
    }

    [Fact]
    public async Task FilterArtistsAsync_WithApiException_IncludesArtist()
    {
        // Arrange
        var config = CreateConfig();
        var artistNames = new List<string> { "Artist A", "Artist B" };

        _apiClientMock
            .Setup(x => x.GetArtistTopTagsAsync("Artist A", It.IsAny<bool>()))
            .ThrowsAsync(new Exception("API Error"));
        _apiClientMock
            .Setup(x => x.GetArtistTopTagsAsync("Artist B", It.IsAny<bool>()))
            .ReturnsAsync(CreateArtistTags(("rock", 100)));

        // Act
        var (filtered, excluded) = await _service.FilterArtistsAsync(artistNames, config);

        // Assert
        filtered.Should().HaveCount(2); // Both included (benefit of the doubt on error)
        excluded.Should().Be(0);
    }

    [Fact]
    public async Task FilterArtistsAsync_CallsApiForEachArtist()
    {
        // Arrange
        var config = CreateConfig();
        var artistNames = new List<string> { "Artist A", "Artist B", "Artist C" };

        _apiClientMock
            .Setup(x => x.GetArtistTopTagsAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(CreateArtistTags());

        // Act
        await _service.FilterArtistsAsync(artistNames, config);

        // Assert
        _apiClientMock.Verify(
            x => x.GetArtistTopTagsAsync(It.IsAny<string>(), It.IsAny<bool>()),
            Times.Exactly(3));
    }
}
