# Lfm.Framework API Reference

Complete reference documentation for all framework interfaces, models, and usage patterns.

---

## Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [Models](#models)
3. [Error Handling](#error-handling)
4. [Usage Examples](#usage-examples)
5. [Common Patterns](#common-patterns)

---

## Core Interfaces

### IDomainItem

Base interface that all domain items must implement.

```csharp
namespace Lfm.Framework.Abstractions;

public interface IDomainItem
{
    string Id { get; set; }
    string Name { get; set; }
    string? ImageUrl { get; set; }
}
```

**Implementation Notes:**
- `Id` must be unique and never null
- `Name` must be user-friendly and never empty
- `ImageUrl` can be null for items without images

---

## IUserHistoryProvider<TItem>

Provides access to user interaction history with items.

```csharp
namespace Lfm.Framework.Abstractions;

public interface IUserHistoryProvider<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// Get user's complete interaction history
    /// </summary>
    Task<Result<List<TItem>>> GetUserHistoryAsync(string userId, int limit = int.MaxValue);

    /// <summary>
    /// Get user's top-rated items for a time period
    /// </summary>
    Task<Result<List<TItem>>> GetTopItemsAsync(
        string userId, string period = "overall", int count = 50);

    /// <summary>
    /// Get user's interactions within a date range
    /// </summary>
    Task<Result<List<TItem>>> GetUserHistoryForDateRangeAsync(
        string userId, DateTime from, DateTime to, int count = 50);

    /// <summary>
    /// Get user's interaction counts for items
    /// </summary>
    Task<Result<Dictionary<string, int>>> GetUserInteractionCountsAsync(
        string userId, int maxItems = int.MaxValue);
}
```

---

## IItemProvider<TItem>

Handles item discovery, search, and lookup operations.

```csharp
public interface IItemProvider<TItem> where TItem : IDomainItem
{
    Task<Result<TItem>> GetItemDetailsAsync(string itemId);
    Task<Result<List<TItem>>> SearchItemsAsync(string query, int limit = 50);
    Task<Result<List<TItem>>> GetSimilarItemsAsync(string sourceItemId, int limit = 50);
    Task<Result<bool>> ItemExistsAsync(string itemId);
}
```

---

## IRecommendationAlgorithm<TItem>

Generates personalized recommendations based on user preferences.

```csharp
public interface IRecommendationAlgorithm<TItem> where TItem : IDomainItem
{
    Task<Result<List<RecommendationResult<TItem>>>> GenerateRecommendationsAsync(
        string userId,
        string period = "overall",
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0);

    Task<Result<List<RecommendationResult<TItem>>>> GenerateRecommendationsForDateRangeAsync(
        string userId,
        DateTime from,
        DateTime to,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0);
}
```

---

## IActionExecutor<TItem>

Executes domain-specific operations on items.

```csharp
public interface IActionExecutor<TItem> where TItem : IDomainItem
{
    Task<Result> ExecuteActionAsync(
        TItem item,
        string actionType,
        Dictionary<string, object>? parameters = null);

    Task<Result<List<string>>> GetAvailableActionsAsync();
    Task<Result<bool>> IsActionAvailableAsync(string actionType);
}
```

---

## Models

### RecommendationResult<TItem>

Represents a single recommendation with metadata.

```csharp
public class RecommendationResult<TItem> where TItem : IDomainItem
{
    public required TItem Item { get; set; }
    public float Score { get; set; }
    public float AverageRelevance { get; set; }
    public int OccurrenceCount { get; set; }
    public int UserInteractionCount { get; set; }
    public List<string> SourceItems { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}
```

---

## Error Handling

### Result<T> Pattern

All framework methods return `Result<T>` for explicit error handling.

```csharp
public class Result<T>
{
    public bool Success { get; }
    public bool IsSuccess => Success;
    public T? Data { get; }
    public ErrorResult? Error { get; }

    public static Result<T> Ok(T data) => new(data, null, true);
    public static Result<T> Fail(ErrorType type, string message) =>
        new(default, new ErrorResult(type, message, null), false);
}
```

### ErrorType Enumeration

```csharp
public enum ErrorType
{
    ValidationError,
    DataError,
    ApiError,
    ConfigurationError,
    CircuitBreakerOpen,
    UnknownError
}
```

### Error Handling Pattern

```csharp
var result = await provider.GetItemDetailsAsync("item-123");

if (result.Success)
{
    var item = result.Data;
    // Use item
}
else
{
    var error = result.Error;
    Console.WriteLine($"Error: {error.Message}");
}
```

---

## Complete Workflow Example

```csharp
public class RecommendationService
{
    private readonly IUserHistoryProvider<BookItem> _historyProvider;
    private readonly IItemProvider<BookItem> _itemProvider;
    private readonly IRecommendationAlgorithm<BookItem> _algorithm;
    private readonly IActionExecutor<BookItem> _executor;

    public async Task<List<BookItem>> GetRecommendationsForUserAsync(string userId)
    {
        // Step 1: Get user's top books
        var topBooksResult = await _historyProvider.GetTopItemsAsync(userId, "overall", 10);
        if (!topBooksResult.Success)
            return new List<BookItem>();

        // Step 2: Generate recommendations
        var recsResult = await _algorithm.GenerateRecommendationsAsync(
            userId,
            analysisLimit: 10,
            recommendationLimit: 10
        );

        if (!recsResult.Success)
            return new List<BookItem>();

        var recommendations = recsResult.Data;

        // Step 3: Get details for top recommendation
        if (recommendations.Any())
        {
            var topRec = recommendations.First();
            Console.WriteLine($"Top recommendation: {topRec.Item.Name} (Score: {topRec.Score:F2})");

            // Step 4: Add to wishlist
            var addResult = await _executor.ExecuteActionAsync(
                topRec.Item,
                "addwishlist"
            );
        }

        return recommendations.Select(r => r.Item).ToList();
    }
}
```

---

**For additional examples, see:**
- `src/Lfm.Core/Music/` - Music domain implementation
- `src/Lfm.Wine/` - Wine domain POC
- `docs/ADAPTER_IMPLEMENTATION_GUIDE.md` - Step-by-step implementation
