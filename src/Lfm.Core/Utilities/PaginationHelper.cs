using Lfm.Shared.Models.Results;

namespace Lfm.Core.Utilities;

/// <summary>
/// Helper for handling paginated API requests
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Fetches all pages of data up to the specified limit using Result<T> pattern
    /// </summary>
    /// <typeparam name="TResponse">The response type from the API</typeparam>
    /// <typeparam name="TItem">The item type being collected</typeparam>
    /// <param name="fetchPage">Function to fetch a single page - returns Result</param>
    /// <param name="extractItems">Function to extract items from response</param>
    /// <param name="totalLimit">Total number of items to fetch</param>
    /// <param name="pageSize">Items per page (default: 50)</param>
    /// <returns>Result containing list of all fetched items, or error</returns>
    public static async Task<Result<List<TItem>>> FetchPaginatedAsync<TResponse, TItem>(
        Func<int, int, Task<Result<TResponse>>> fetchPage,
        Func<TResponse, List<TItem>> extractItems,
        int totalLimit,
        int pageSize = 50)
    {
        var allItems = new List<TItem>(capacity: Math.Min(totalLimit, 1000));
        int currentPage = 1;

        while (allItems.Count < totalLimit)
        {
            var itemsNeeded = Math.Min(pageSize, totalLimit - allItems.Count);

            // Fetch page using Result pattern
            var pageResult = await fetchPage(itemsNeeded, currentPage);

            // Handle errors
            if (!pageResult.IsSuccess)
                return Result<List<TItem>>.Fail(pageResult.Error!);

            // Extract items from successful response
            var items = extractItems(pageResult.Data!);

            // No more items available
            if (items.Count == 0)
                break;

            // Add items up to the limit
            var itemsToAdd = items.Take(totalLimit - allItems.Count);
            allItems.AddRange(itemsToAdd);

            currentPage++;
        }

        return Result<List<TItem>>.Ok(allItems);
    }

    /// <summary>
    /// Legacy version for nullable-based APIs (maintains backward compatibility)
    /// </summary>
    public static async Task<List<TItem>?> FetchPaginatedLegacyAsync<TResponse, TItem>(
        Func<int, int, Task<TResponse?>> fetchPage,
        Func<TResponse, List<TItem>> extractItems,
        int totalLimit,
        int pageSize = 50) where TResponse : class
    {
        var allItems = new List<TItem>(capacity: Math.Min(totalLimit, 1000));
        int currentPage = 1;

        while (allItems.Count < totalLimit)
        {
            var itemsNeeded = Math.Min(pageSize, totalLimit - allItems.Count);

            var response = await fetchPage(itemsNeeded, currentPage);
            if (response == null)
                return null; // Error occurred

            var items = extractItems(response);
            if (items.Count == 0)
                break; // No more items

            var itemsToAdd = items.Take(totalLimit - allItems.Count);
            allItems.AddRange(itemsToAdd);

            currentPage++;
        }

        return allItems;
    }

    /// <summary>
    /// Calculates the number of pages needed for a given total and page size
    /// </summary>
    public static int CalculatePageCount(int totalItems, int pageSize)
    {
        if (totalItems <= 0 || pageSize <= 0)
            return 0;

        return (int)Math.Ceiling((double)totalItems / pageSize);
    }

    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    public static Result<PaginationParams> ValidateParameters(int limit, int page, int pageSize)
    {
        if (limit <= 0)
            return Result<PaginationParams>.ValidationError(
                "Limit must be greater than 0",
                $"Provided limit: {limit}");

        if (page <= 0)
            return Result<PaginationParams>.ValidationError(
                "Page must be greater than 0",
                $"Provided page: {page}");

        if (pageSize <= 0 || pageSize > 1000)
            return Result<PaginationParams>.ValidationError(
                "Page size must be between 1 and 1000",
                $"Provided page size: {pageSize}");

        return Result<PaginationParams>.Ok(new PaginationParams(limit, page, pageSize));
    }
}

/// <summary>
/// Represents validated pagination parameters
/// </summary>
public record PaginationParams(int Limit, int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;
}
