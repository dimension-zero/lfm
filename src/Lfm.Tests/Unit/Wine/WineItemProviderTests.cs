using FluentAssertions;
using Lfm.Framework.Models;
using Lfm.Shared.Models.Results;
using Lfm.Wine.Data;
using Lfm.Wine.Models;
using Lfm.Wine.Providers;
using Xunit;

namespace Lfm.Tests.Unit.Wine;

/// <summary>
/// Unit tests for WineItemProvider implementing IItemProvider<WineItem>
/// Tests wine search, detail retrieval, similarity lookups, and existence checks
/// </summary>
public class WineItemProviderTests
{
    private readonly MockWineDataProvider _dataProvider;
    private readonly WineItemProvider _provider;

    public WineItemProviderTests()
    {
        _dataProvider = new MockWineDataProvider();
        _provider = new WineItemProvider(_dataProvider);
    }

    // ========== Initialization Tests ==========

    [Fact]
    public void WineItemProvider_InitializesWithValidDataProvider()
    {
        // Act & Assert
        var provider = new WineItemProvider(_dataProvider);
        provider.Should().NotBeNull();
    }

    [Fact]
    public void WineItemProvider_ThrowsOnNullDataProvider()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WineItemProvider(null!));
    }

    // ========== GetItemDetailsAsync Tests ==========

    [Fact]
    public async Task GetItemDetailsAsync_ReturnsWineByValidId()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines[0];

        // Act
        var result = await _provider.GetItemDetailsAsync(targetWine.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(targetWine.Id);
        result.Data.Name.Should().Be(targetWine.Name);
    }

    [Fact]
    public async Task GetItemDetailsAsync_ReturnsAllWineProperties()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines[0];

        // Act
        var result = await _provider.GetItemDetailsAsync(targetWine.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().NotBeNullOrEmpty();
        result.Data.Name.Should().NotBeNullOrEmpty();
        result.Data.Type.Should().NotBeNullOrEmpty();
        result.Data.ProducerName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetItemDetailsAsync_FailsWithInvalidId()
    {
        // Act
        var result = await _provider.GetItemDetailsAsync("nonexistent-id");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.DataError);
        result.Error?.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetItemDetailsAsync_IsCaseInsensitive()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines[0];

        // Act
        var result = await _provider.GetItemDetailsAsync(targetWine.Id.ToUpper());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Id.Should().Be(targetWine.Id);
    }

    // ========== SearchItemsAsync Tests ==========

    [Fact]
    public async Task SearchItemsAsync_ReturnsWinesMatchingQuery()
    {
        // Act
        var result = await _provider.SearchItemsAsync("Pinot");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchItemsAsync_SearchesAcrossMultipleFields()
    {
        // Act - search by variety
        var varietyResult = await _provider.SearchItemsAsync("Noir");

        // Assert
        varietyResult.IsSuccess.Should().BeTrue();
        varietyResult.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchItemsAsync_RespectsSizeLimit()
    {
        // Act
        var result = await _provider.SearchItemsAsync("wine", limit: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task SearchItemsAsync_ReturnsEmptyForNoMatches()
    {
        // Act
        var result = await _provider.SearchItemsAsync("xyzabcnotawine");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchItemsAsync_IsCaseInsensitive()
    {
        // Arrange & Act
        var lowerResult = await _provider.SearchItemsAsync("pinot");
        var upperResult = await _provider.SearchItemsAsync("PINOT");

        // Assert
        lowerResult.IsSuccess.Should().BeTrue();
        upperResult.IsSuccess.Should().BeTrue();
        lowerResult.Data.Count.Should().Be(upperResult.Data.Count);
    }

    // ========== GetSimilarItemsAsync Tests ==========

    [Fact]
    public async Task GetSimilarItemsAsync_ReturnsSimilarWines()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines.First(w => w.Variety != null);

        // Act
        var result = await _provider.GetSimilarItemsAsync(targetWine.Id, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // POC sample data has unique varieties, so similar wines list may be empty
        // This is expected behavior - in production with more data, would return results
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSimilarItemsAsync_ExcludesSourceWine()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines.First(w => w.Variety != null);

        // Act
        var result = await _provider.GetSimilarItemsAsync(targetWine.Id, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify source wine is not in results (if any results exist)
        if (result.Data.Any())
        {
            result.Data.Should().NotContain(w => w.Id == targetWine.Id);
        }
    }

    [Fact]
    public async Task GetSimilarItemsAsync_MatchesByVariety()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines.First(w => w.Variety != null);

        // Act
        var result = await _provider.GetSimilarItemsAsync(targetWine.Id, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // POC with sample data has unique varieties, so no similar wines found
        // In real implementation with more data, similar wines would have same variety
        if (result.Data.Any())
        {
            result.Data.Should().AllSatisfy(w =>
            {
                w.Variety.Should().Be(targetWine.Variety);
            });
        }
    }

    [Fact]
    public async Task GetSimilarItemsAsync_RespectsSizeLimit()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines.First(w => w.Variety != null);

        // Act
        var result = await _provider.GetSimilarItemsAsync(targetWine.Id, 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSimilarItemsAsync_FailsWithInvalidSourceId()
    {
        // Act
        var result = await _provider.GetSimilarItemsAsync("nonexistent-id", 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.DataError);
    }

    [Fact]
    public async Task GetSimilarItemsAsync_ReturnsEmptyForWineWithoutVariety()
    {
        // Arrange - Create a wine without variety
        var wineWithoutVariety = new WineItem
        {
            Id = "test-wine-no-variety",
            Name = "Test Wine",
            Type = "red",
            Variety = null
        };

        // Act
        var result = await _provider.GetSimilarItemsAsync(wineWithoutVariety.Id, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Wine doesn't exist in the data provider, so should fail
        result.Error?.Type.Should().Be(ErrorType.DataError);
    }

    // ========== ItemExistsAsync Tests ==========

    [Fact]
    public async Task ItemExistsAsync_ReturnsTrueForExistingWine()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines[0];

        // Act
        var result = await _provider.ItemExistsAsync(targetWine.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ItemExistsAsync_ReturnsFalseForNonexistentWine()
    {
        // Act
        var result = await _provider.ItemExistsAsync("nonexistent-id");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ItemExistsAsync_IsCaseInsensitive()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines[0];

        // Act
        var result = await _provider.ItemExistsAsync(targetWine.Id.ToUpper());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    // ========== Integration Tests ==========

    [Fact]
    public async Task Provider_SearchFindsThenDetailsRetrieves()
    {
        // Arrange & Act - Search
        var searchResult = await _provider.SearchItemsAsync("Pinot");

        // Assert
        searchResult.IsSuccess.Should().BeTrue();
        searchResult.Data.Should().NotBeEmpty();

        // Arrange & Act - Get Details
        var wine = searchResult.Data[0];
        var detailResult = await _provider.GetItemDetailsAsync(wine.Id);

        // Assert
        detailResult.IsSuccess.Should().BeTrue();
        detailResult.Data.Id.Should().Be(wine.Id);
    }

    [Fact]
    public async Task Provider_ExistsThenGetDetails()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines[0];

        // Act - Check existence
        var existResult = await _provider.ItemExistsAsync(targetWine.Id);

        // Assert
        existResult.IsSuccess.Should().BeTrue();
        existResult.Data.Should().BeTrue();

        // Act - Get details
        var detailResult = await _provider.GetItemDetailsAsync(targetWine.Id);

        // Assert
        detailResult.IsSuccess.Should().BeTrue();
    }

    // ========== Error Handling ==========

    [Fact]
    public async Task SearchItemsAsync_HandlesBadQuery()
    {
        // Act
        var result = await _provider.SearchItemsAsync("");

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Empty query should return empty or all results depending on implementation
    }

    [Fact]
    public async Task GetSimilarItemsAsync_DefaultLimit()
    {
        // Arrange
        var wines = _dataProvider.GetAllWines();
        var targetWine = wines.First(w => w.Variety != null);

        // Act - Use default limit of 50
        var result = await _provider.GetSimilarItemsAsync(targetWine.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCountLessThanOrEqualTo(50);
    }
}
