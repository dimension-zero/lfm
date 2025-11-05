using FluentAssertions;
using Lfm.Core.Utilities;
using Lfm.Shared.Models.Results;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for PaginationHelper
/// Tests pagination logic, parameter validation, and page calculation
/// </summary>
public class PaginationHelperTests
{
    // Test data model
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public TestItem(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    private class TestResponse
    {
        public List<TestItem> Items { get; set; }

        public TestResponse(List<TestItem> items)
        {
            Items = items;
        }
    }

    // ========== FetchPaginatedAsync Tests ==========

    [Fact]
    public async Task FetchPaginatedAsync_WithSinglePageResult_ReturnsList()
    {
        // Arrange
        var expectedItems = new List<TestItem>
        {
            new(1, "Item 1"),
            new(2, "Item 2"),
            new(3, "Item 3")
        };

        int callCount = 0;
        async Task<Result<TestResponse>> FetchPage(int limit, int page)
        {
            callCount++;
            // Return items on first call, empty on second call to stop iteration
            if (callCount == 1)
                return Result<TestResponse>.Ok(new TestResponse(expectedItems));
            return Result<TestResponse>.Ok(new TestResponse(new List<TestItem>()));
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 100,
            pageSize: 50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data[0].Id.Should().Be(1);
        result.Data[1].Id.Should().Be(2);
        result.Data[2].Id.Should().Be(3);
    }

    [Fact]
    public async Task FetchPaginatedAsync_WithMultiplePages_AggregatesAllItems()
    {
        // Arrange
        var page1Items = new List<TestItem> { new(1, "Item 1"), new(2, "Item 2") };
        var page2Items = new List<TestItem> { new(3, "Item 3"), new(4, "Item 4") };
        var page3Items = new List<TestItem> { new(5, "Item 5") };

        int callCount = 0;

        async Task<Result<TestResponse>> FetchPage(int limit, int page)
        {
            callCount++;
            return callCount switch
            {
                1 => Result<TestResponse>.Ok(new TestResponse(page1Items)),
                2 => Result<TestResponse>.Ok(new TestResponse(page2Items)),
                3 => Result<TestResponse>.Ok(new TestResponse(page3Items)),
                _ => Result<TestResponse>.Ok(new TestResponse(new List<TestItem>()))
            };
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 100,
            pageSize: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(5);
        result.Data.Should().SatisfyRespectively(
            item => item.Id.Should().Be(1),
            item => item.Id.Should().Be(2),
            item => item.Id.Should().Be(3),
            item => item.Id.Should().Be(4),
            item => item.Id.Should().Be(5));
    }

    [Fact]
    public async Task FetchPaginatedAsync_WithTotalLimit_StopsAfterLimit()
    {
        // Arrange
        var allItems = Enumerable.Range(1, 10)
            .Select(i => new TestItem(i, $"Item {i}"))
            .ToList();

        async Task<Result<TestResponse>> FetchPage(int limit, int page)
        {
            return Result<TestResponse>.Ok(new TestResponse(allItems));
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 5,
            pageSize: 50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(5);
    }

    [Fact]
    public async Task FetchPaginatedAsync_WithEmptyPage_StopsIteration()
    {
        // Arrange
        var page1Items = new List<TestItem> { new(1, "Item 1"), new(2, "Item 2") };
        int callCount = 0;

        async Task<Result<TestResponse>> FetchPage(int limit, int page)
        {
            callCount++;
            if (callCount == 1)
                return Result<TestResponse>.Ok(new TestResponse(page1Items));

            // Second page returns empty
            return Result<TestResponse>.Ok(new TestResponse(new List<TestItem>()));
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 100,
            pageSize: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2); // Only page 1 items
        callCount.Should().Be(2); // Called twice: page 1 with data, page 2 empty
    }

    [Fact]
    public async Task FetchPaginatedAsync_WithFetchError_ReturnsError()
    {
        // Arrange
        async Task<Result<TestResponse>> FetchPage(int limit, int page)
        {
            return Result<TestResponse>.Fail(
                new ErrorResult(ErrorType.ApiError, "API Error", "429 Too Many Requests"));
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 100,
            pageSize: 50);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ApiError);
    }

    // ========== FetchPaginatedLegacyAsync Tests ==========

    [Fact]
    public async Task FetchPaginatedLegacyAsync_WithValidPages_ReturnsList()
    {
        // Arrange
        var expectedItems = new List<TestItem>
        {
            new(1, "Item 1"),
            new(2, "Item 2"),
            new(3, "Item 3")
        };

        int callCount = 0;
        async Task<TestResponse?> FetchPage(int limit, int page)
        {
            callCount++;
            // Return items on first call, null on second to stop iteration
            if (callCount == 1)
                return new TestResponse(expectedItems);
            return new TestResponse(new List<TestItem>()); // Empty response stops iteration
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedLegacyAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 100,
            pageSize: 50);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result![0].Id.Should().Be(1);
    }

    [Fact]
    public async Task FetchPaginatedLegacyAsync_WithNullResponse_ReturnsNull()
    {
        // Arrange
        async Task<TestResponse?> FetchPage(int limit, int page)
        {
            return null;
        }

        // Act
        var result = await PaginationHelper.FetchPaginatedLegacyAsync<TestResponse, TestItem>(
            FetchPage,
            r => r.Items,
            totalLimit: 100,
            pageSize: 50);

        // Assert
        result.Should().BeNull();
    }

    // ========== CalculatePageCount Tests ==========

    [Fact]
    public void CalculatePageCount_WithEvenDivision_CalculatesCorrectly()
    {
        // Act
        var pages = PaginationHelper.CalculatePageCount(totalItems: 100, pageSize: 50);

        // Assert
        pages.Should().Be(2);
    }

    [Fact]
    public void CalculatePageCount_WithRemainderItems_RoundsUp()
    {
        // Act
        var pages = PaginationHelper.CalculatePageCount(totalItems: 105, pageSize: 50);

        // Assert
        pages.Should().Be(3); // 50 + 50 + 5
    }

    [Fact]
    public void CalculatePageCount_WithSinglePage_ReturnsOne()
    {
        // Act
        var pages = PaginationHelper.CalculatePageCount(totalItems: 25, pageSize: 50);

        // Assert
        pages.Should().Be(1);
    }

    [Fact]
    public void CalculatePageCount_WithZeroItems_ReturnsZero()
    {
        // Act
        var pages = PaginationHelper.CalculatePageCount(totalItems: 0, pageSize: 50);

        // Assert
        pages.Should().Be(0);
    }

    [Fact]
    public void CalculatePageCount_WithNegativeItems_ReturnsZero()
    {
        // Act
        var pages = PaginationHelper.CalculatePageCount(totalItems: -10, pageSize: 50);

        // Assert
        pages.Should().Be(0);
    }

    [Fact]
    public void CalculatePageCount_WithNegativePageSize_ReturnsZero()
    {
        // Act
        var pages = PaginationHelper.CalculatePageCount(totalItems: 100, pageSize: -10);

        // Assert
        pages.Should().Be(0);
    }

    // ========== ValidateParameters Tests ==========

    [Fact]
    public void ValidateParameters_WithValidParams_ReturnsSuccess()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 50, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Limit.Should().Be(50);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public void ValidateParameters_WithZeroLimit_ReturnsFail()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 0, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Message.Should().Contain("Limit must be greater than 0");
    }

    [Fact]
    public void ValidateParameters_WithNegativeLimit_ReturnsFail()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: -10, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Message.Should().Contain("Limit must be greater than 0");
    }

    [Fact]
    public void ValidateParameters_WithZeroPage_ReturnsFail()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 50, page: 0, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Message.Should().Contain("Page must be greater than 0");
    }

    [Fact]
    public void ValidateParameters_WithNegativePage_ReturnsFail()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 50, page: -1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Message.Should().Contain("Page must be greater than 0");
    }

    [Fact]
    public void ValidateParameters_WithZeroPageSize_ReturnsFail()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 50, page: 1, pageSize: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Message.Should().Contain("Page size must be between 1 and 1000");
    }

    [Fact]
    public void ValidateParameters_WithExcessivePageSize_ReturnsFail()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 50, page: 1, pageSize: 1001);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Message.Should().Contain("Page size must be between 1 and 1000");
    }

    [Fact]
    public void ValidateParameters_WithMaxPageSize_Succeeds()
    {
        // Act
        var result = PaginationHelper.ValidateParameters(limit: 50, page: 1, pageSize: 1000);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.PageSize.Should().Be(1000);
    }

    // ========== PaginationParams Tests ==========

    [Fact]
    public void PaginationParams_CalculatesSkipCorrectly()
    {
        // Act
        var params1 = new PaginationParams(Limit: 50, Page: 1, PageSize: 10);
        var params2 = new PaginationParams(Limit: 50, Page: 2, PageSize: 10);
        var params3 = new PaginationParams(Limit: 50, Page: 5, PageSize: 20);

        // Assert
        params1.Skip.Should().Be(0); // (1-1) * 10 = 0
        params2.Skip.Should().Be(10); // (2-1) * 10 = 10
        params3.Skip.Should().Be(80); // (5-1) * 20 = 80
    }

    [Fact]
    public void PaginationParams_WithPage1_SkipIsZero()
    {
        // Act
        var @params = new PaginationParams(Limit: 50, Page: 1, PageSize: 25);

        // Assert
        @params.Skip.Should().Be(0);
    }
}
