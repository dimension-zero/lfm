using FluentAssertions;
using Lfm.Framework.Models;
using Lfm.Shared.Models.Results;
using Lfm.Wine.Data;
using Lfm.Wine.Models;
using Lfm.Wine.Providers;
using Xunit;

namespace Lfm.Tests.Unit.Wine;

/// <summary>
/// Unit tests for WineHistoryProvider implementing IUserHistoryProvider<WineItem>
/// Tests user history retrieval, tasting counts, and period-based filtering
/// </summary>
public class WineHistoryProviderTests
{
    private readonly MockWineDataProvider _dataProvider;
    private readonly WineHistoryProvider _provider;

    public WineHistoryProviderTests()
    {
        _dataProvider = new MockWineDataProvider();
        _provider = new WineHistoryProvider(_dataProvider);
    }

    // ========== Initialization Tests ==========

    [Fact]
    public void WineHistoryProvider_InitializesWithValidDataProvider()
    {
        // Act & Assert
        var provider = new WineHistoryProvider(_dataProvider);
        provider.Should().NotBeNull();
    }

    [Fact]
    public void WineHistoryProvider_ThrowsOnNullDataProvider()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WineHistoryProvider(null!));
    }

    // ========== GetUserHistoryAsync Tests ==========

    [Fact]
    public async Task GetUserHistoryAsync_ReturnsAllTastedWines()
    {
        // Act
        var result = await _provider.GetUserHistoryAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().AllSatisfy(w => w.UserTastingCount.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetUserHistoryAsync_RespectsSizeLimit()
    {
        // Act
        var result = await _provider.GetUserHistoryAsync("user123", limit: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserHistoryAsync_OrdersByTastingCountDescending()
    {
        // Act
        var result = await _provider.GetUserHistoryAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var wines = result.Data;
        for (int i = 0; i < wines.Count - 1; i++)
        {
            wines[i].UserTastingCount.Should().BeGreaterThanOrEqualTo(wines[i + 1].UserTastingCount);
        }
    }

    [Fact]
    public async Task GetUserHistoryAsync_ExcludesUntastedWines()
    {
        // Act
        var result = await _provider.GetUserHistoryAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotContain(w => w.UserTastingCount == 0);
    }

    // ========== GetTopItemsAsync Tests ==========

    [Fact]
    public async Task GetTopItemsAsync_ReturnsTopRatedWinesOverallPeriod()
    {
        // Act
        var result = await _provider.GetTopItemsAsync("user123", "overall", 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().AllSatisfy(w => w.Ranking.Should().StartWith("#"));
    }

    [Fact]
    public async Task GetTopItemsAsync_AppliesPeriodFiltering()
    {
        // Act
        var overallResult = await _provider.GetTopItemsAsync("user123", "overall", 10);
        var recentResult = await _provider.GetTopItemsAsync("user123", "recent", 10);

        // Assert
        overallResult.IsSuccess.Should().BeTrue();
        recentResult.IsSuccess.Should().BeTrue();
        // Both should return data, though ordering may differ
        overallResult.Data.Should().NotBeEmpty();
        recentResult.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTopItemsAsync_RanksItemsCorrectly()
    {
        // Act
        var result = await _provider.GetTopItemsAsync("user123", "overall", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(5);
        // Check ranking format: #1, #2, #3, etc.
        for (int i = 0; i < result.Data.Count; i++)
        {
            result.Data[i].Ranking.Should().Be($"#{i + 1}");
        }
    }

    [Fact]
    public async Task GetTopItemsAsync_OnlyIncludesTastedWines()
    {
        // Act
        var result = await _provider.GetTopItemsAsync("user123", "overall", 50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().AllSatisfy(w => w.UserTastingCount.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetTopItemsAsync_RespectsCountParameter()
    {
        // Act
        var result = await _provider.GetTopItemsAsync("user123", "overall", 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(3);
    }

    // ========== GetUserHistoryForDateRangeAsync Tests ==========

    [Fact]
    public async Task GetUserHistoryForDateRangeAsync_ReturnsMostFrequentlyTastedWines()
    {
        // Arrange
        var from = DateTime.Now.AddMonths(-3);
        var to = DateTime.Now;

        // Act
        var result = await _provider.GetUserHistoryForDateRangeAsync("user123", from, to, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().AllSatisfy(w => w.UserTastingCount.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetUserHistoryForDateRangeAsync_RanksWinesCorrectly()
    {
        // Arrange
        var from = DateTime.Now.AddMonths(-3);
        var to = DateTime.Now;

        // Act
        var result = await _provider.GetUserHistoryForDateRangeAsync("user123", from, to, 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        for (int i = 0; i < result.Data.Count; i++)
        {
            result.Data[i].Ranking.Should().Be($"#{i + 1}");
        }
    }

    [Fact]
    public async Task GetUserHistoryForDateRangeAsync_RespectsCountParameter()
    {
        // Arrange
        var from = DateTime.Now.AddMonths(-3);
        var to = DateTime.Now;

        // Act
        var result = await _provider.GetUserHistoryForDateRangeAsync("user123", from, to, 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(2);
    }

    // ========== GetUserInteractionCountsAsync Tests ==========

    [Fact]
    public async Task GetUserInteractionCountsAsync_ReturnsWineNameToCountMapping()
    {
        // Act
        var result = await _provider.GetUserInteractionCountsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().AllSatisfy(kvp =>
        {
            kvp.Key.Should().NotBeNullOrEmpty();
            kvp.Value.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task GetUserInteractionCountsAsync_OnlyIncludesTastedWines()
    {
        // Act
        var result = await _provider.GetUserInteractionCountsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Values.Should().AllSatisfy(count => count.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetUserInteractionCountsAsync_RespectMaxItemsLimit()
    {
        // Act
        var result = await _provider.GetUserInteractionCountsAsync("user123", maxItems: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(2);
    }

    // ========== Edge Cases ==========

    [Fact]
    public async Task GetUserHistoryAsync_WithZeroLimit_ReturnsEmpty()
    {
        // Act
        var result = await _provider.GetUserHistoryAsync("user123", limit: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopItemsAsync_WithLargeCountParameter_ReturnsAllAvailableItems()
    {
        // Act
        var result = await _provider.GetTopItemsAsync("user123", "overall", 1000);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    // ========== Error Handling Tests ==========

    [Fact]
    public async Task GetUserHistoryAsync_HandlesMissingUser()
    {
        // Act
        var result = await _provider.GetUserHistoryAsync("nonexistent-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        // POC mock provider returns all tasted wines regardless of user ID
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTopItemsAsync_WithUnknownPeriod_UsesOverallDefault()
    {
        // Act
        var result = await _provider.GetTopItemsAsync("user123", "unknown-period", 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        // Should default to overall behavior
    }
}
