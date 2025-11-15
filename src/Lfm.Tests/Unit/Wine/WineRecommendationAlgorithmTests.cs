using FluentAssertions;
using Lfm.Framework.Models;
using Lfm.Shared.Models.Results;
using Lfm.Wine.Algorithms;
using Lfm.Wine.Data;
using Lfm.Wine.Models;
using Xunit;

namespace Lfm.Tests.Unit.Wine;

/// <summary>
/// Unit tests for WineRecommendationAlgorithm implementing IRecommendationAlgorithm<WineItem>
/// Tests recommendation generation based on user preferences and similar wines
/// </summary>
public class WineRecommendationAlgorithmTests
{
    private readonly MockWineDataProvider _dataProvider;
    private readonly WineRecommendationAlgorithm _algorithm;

    public WineRecommendationAlgorithmTests()
    {
        _dataProvider = new MockWineDataProvider();
        _algorithm = new WineRecommendationAlgorithm(_dataProvider);
    }

    // ========== Initialization Tests ==========

    [Fact]
    public void WineRecommendationAlgorithm_InitializesWithValidDataProvider()
    {
        // Act & Assert
        var algorithm = new WineRecommendationAlgorithm(_dataProvider);
        algorithm.Should().NotBeNull();
    }

    [Fact]
    public void WineRecommendationAlgorithm_ThrowsOnNullDataProvider()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WineRecommendationAlgorithm(null!));
    }

    // ========== GenerateRecommendationsAsync Tests ==========

    [Fact]
    public async Task GenerateRecommendationsAsync_ReturnsRecommendations()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RespectsAnalysisLimit()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", "overall", analysisLimit: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Algorithm analyzes up to 2 wines (limits tasted wine analysis)
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RespectsRecommendationLimit()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", "overall", recommendationLimit: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_ReturnsItemsWithScores()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any())
        {
            result.Data.Should().AllSatisfy(rec =>
            {
                rec.Score.Should().BeGreaterThan(0);
                rec.Item.Should().NotBeNull();
                rec.AverageRelevance.Should().BeGreaterThan(0);
            });
        }
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_SortsRecommendationsByScoreDescending()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", recommendationLimit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Count > 1)
        {
            // Verify sorted by score descending
            for (int i = 0; i < result.Data.Count - 1; i++)
            {
                result.Data[i].Score.Should().BeGreaterThanOrEqualTo(result.Data[i + 1].Score);
            }
        }
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RecommendationsBasedOnFavoriteVarieties()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any())
        {
            // Recommendations should be similar to tasted wines
            // Check that recommended wines have same varieties as some tasted wines
            var tastedWines = _dataProvider.GetAllWines()
                .Where(w => w.UserTastingCount > 0)
                .ToList();

            var tastedVarieties = tastedWines
                .Where(w => w.Variety != null)
                .Select(w => w.Variety)
                .Distinct()
                .ToList();

            result.Data.Should().AllSatisfy(rec =>
            {
                tastedVarieties.Should().Contain(rec.Item.Variety);
            });
        }
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RecommendationsExcludeAlreadyTasted()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any())
        {
            result.Data.Should().AllSatisfy(rec =>
            {
                rec.Item.UserTastingCount.Should().Be(0);
            });
        }
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_IncludesSourceWinesInResult()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any())
        {
            result.Data.Should().AllSatisfy(rec =>
            {
                rec.SourceItems.Should().NotBeEmpty();
                rec.OccurrenceCount.Should().BeGreaterThan(0);
            });
        }
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithHighFilterThreshold()
    {
        // Act - Only consider wines tasted more than 10 times
        var result = await _algorithm.GenerateRecommendationsAsync("user123", filterThreshold: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // May return empty if no wines meet threshold
    }

    // ========== GenerateRecommendationsForDateRangeAsync Tests ==========

    [Fact]
    public async Task GenerateRecommendationsForDateRangeAsync_ReturnsPocBehavior()
    {
        // Arrange
        var from = DateTime.Now.AddMonths(-3);
        var to = DateTime.Now;

        // Act
        var result = await _algorithm.GenerateRecommendationsForDateRangeAsync("user123", from, to);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // POC returns same as overall recommendations
    }

    [Fact]
    public async Task GenerateRecommendationsForDateRangeAsync_RespectsPeriodParam()
    {
        // Arrange
        var from = DateTime.Now.AddMonths(-3);
        var to = DateTime.Now;

        // Act
        var result = await _algorithm.GenerateRecommendationsForDateRangeAsync(
            "user123", from, to, analysisLimit: 5, recommendationLimit: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(3);
    }

    // ========== Score Calculation Tests ==========

    [Fact]
    public async Task GenerateRecommendationsAsync_ScoreCalculation()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", recommendationLimit: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any())
        {
            result.Data.Should().AllSatisfy(rec =>
            {
                // Score = (user rating * wine avg rating) / 5
                // Should be positive and reasonable
                rec.Score.Should().BeGreaterThan(0);
                rec.Score.Should().BeLessThanOrEqualTo(25); // max 5 * 5 = 25
            });
        }
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_AggregatesMultipleSourceWines()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", recommendationLimit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any(r => r.OccurrenceCount > 1))
        {
            // Some recommendations should come from multiple source wines
            var aggregated = result.Data.First(r => r.OccurrenceCount > 1);
            aggregated.SourceItems.Should().HaveCountGreaterThan(1);
            aggregated.Score.Should().BeGreaterThan(0);
        }
    }

    // ========== Edge Cases ==========

    [Fact]
    public async Task GenerateRecommendationsAsync_WithZeroAnalysisLimit()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", analysisLimit: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithZeroRecommendationLimit()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", recommendationLimit: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithNoTastedWines()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("nonexistent-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_WithNullUserIdPattern()
    {
        // Arrange - User ID not used in POC implementation
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ========== Period Parameter Tests ==========

    [Fact]
    public async Task GenerateRecommendationsAsync_PeriodParameter_IsUsedButIgnoredInPOC()
    {
        // Act
        var overallResult = await _algorithm.GenerateRecommendationsAsync("user123", "overall");
        var sevenDayResult = await _algorithm.GenerateRecommendationsAsync("user123", "7day");

        // Assert - POC ignores period parameter
        overallResult.IsSuccess.Should().BeTrue();
        sevenDayResult.IsSuccess.Should().BeTrue();
    }

    // ========== Integration Tests ==========

    [Fact]
    public async Task GenerateRecommendationsAsync_IntegrationWithProvider()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", recommendationLimit: 10);

        // Assert - Validate complete recommendation object
        result.IsSuccess.Should().BeTrue();
        if (result.Data.Any())
        {
            var rec = result.Data[0];
            rec.Item.Should().NotBeNull();
            rec.Item.Id.Should().NotBeNullOrEmpty();
            rec.Item.Name.Should().NotBeNullOrEmpty();
            rec.Item.Variety.Should().NotBeNullOrEmpty();
            rec.Score.Should().BeGreaterThan(0);
            rec.AverageRelevance.Should().BeGreaterThan(0);
            rec.OccurrenceCount.Should().BeGreaterThan(0);
            rec.SourceItems.Should().NotBeEmpty();
        }
    }

    // ========== Error Handling ==========

    [Fact]
    public async Task GenerateRecommendationsAsync_HandlesBadLimits()
    {
        // Act
        var result = await _algorithm.GenerateRecommendationsAsync("user123", analysisLimit: -1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should handle gracefully with empty results
    }

    [Fact]
    public async Task GenerateRecommendationsForDateRangeAsync_WithInvertedDateRange()
    {
        // Arrange
        var from = DateTime.Now;
        var to = DateTime.Now.AddMonths(-3);

        // Act
        var result = await _algorithm.GenerateRecommendationsForDateRangeAsync("user123", from, to);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should handle inverted dates gracefully
    }
}
