# Adapter Implementation Guide

## Introduction

This guide provides step-by-step instructions for implementing a new domain adapter for the Lfm.Framework. Follow these steps sequentially to build a complete, tested domain adapter.

**Time Estimate**: 2-3 hours for a complete implementation
**Target Audience**: Developers adding new domains (books, films, cars, retail, etc.)

## Prerequisites

- Understanding of the Lfm.Framework architecture (see `src/Lfm.Framework/README.md`)
- Familiarity with the `Result<T>` error handling pattern
- Knowledge of async/await patterns in C#
- Access to domain data source (API, database, mock provider)

## Overview of Steps

1. **Create Domain Project** - New .NET project for the domain
2. **Define Domain Model** - Implement `IDomainItem`
3. **Create Data Provider** - Access domain data source
4. **Implement History Provider** - User activity tracking
5. **Implement Item Provider** - Search and discovery
6. **Implement Recommendation Algorithm** - Smart recommendations
7. **Implement Action Executor** - Domain-specific actions
8. **Create Unit Tests** - Comprehensive test coverage
9. **Register in DI** - Integrate with application
10. **Validate Integration** - Verify all components work together

---

## Step 1: Create Domain Project

### 1.1 Create Project Structure

```bash
mkdir src/Lfm.{DomainName}
cd src/Lfm.{DomainName}
dotnet new classlib -f net8.0
```

Replace `{DomainName}` with your domain (e.g., `Books`, `Films`, `Cars`).

### 1.2 Create Project File

Edit `src/Lfm.{DomainName}/Lfm.{DomainName}.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Lfm.Shared\Lfm.Shared.csproj" />
    <ProjectReference Include="..\Lfm.Framework\Lfm.Framework.csproj" />
  </ItemGroup>

</Project>
```

### 1.3 Create Directory Structure

```
src/Lfm.{DomainName}/
├── Models/
│   └── {DomainName}Item.cs
├── Data/
│   └── {DataProvider}.cs
├── Providers/
│   ├── {DomainName}HistoryProvider.cs
│   └── {DomainName}ItemProvider.cs
├── Algorithms/
│   └── {DomainName}RecommendationAlgorithm.cs
├── Actions/
│   └── {DomainName}ActionExecutor.cs
└── Lfm.{DomainName}.csproj
```

### 1.4 Add Project to Solution

```bash
dotnet sln add src/Lfm.{DomainName}/Lfm.{DomainName}.csproj
```

---

## Step 2: Define Domain Model

### 2.1 Create Domain Item Class

File: `src/Lfm.{DomainName}/Models/{DomainName}Item.cs`

```csharp
using Lfm.Framework.Abstractions;

namespace Lfm.{DomainName}.Models;

/// <summary>
/// Domain item representing a {DomainName} entity
/// Implements IDomainItem for framework compatibility
/// </summary>
public class {DomainName}Item : IDomainItem
{
    // ========== Required Framework Properties ==========

    /// <summary>
    /// Unique identifier for this item
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Display name of the item
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// URL to item image for display
    /// </summary>
    public string? ImageUrl { get; set; }

    // ========== Domain-Specific Properties ==========

    /// <summary>
    /// Creator/producer of the item
    /// </summary>
    public string? ProducerName { get; set; }

    /// <summary>
    /// How many times user has interacted with this item
    /// </summary>
    public int UserInteractionCount { get; set; }

    /// <summary>
    /// User's rating of this item (1-5 scale)
    /// </summary>
    public float? UserRating { get; set; }

    /// <summary>
    /// Link to item details
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Primary category/genre for the item
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Average community rating
    /// </summary>
    public float? AverageRating { get; set; }

    /// <summary>
    /// Price or cost information
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Item description or summary
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ranking position (e.g., "#1", "#2")
    /// Used when returning ranked lists
    /// </summary>
    public string? Ranking { get; set; }

    // ========== Custom Domain Properties ==========
    // Add additional properties specific to your domain
    // Example for Books: ISBN, PublicationYear, PageCount, Genre, Author
    // Example for Films: ReleaseYear, Director, Duration, Genre
}
```

### 2.2 Validation Checklist

- [ ] Class inherits `IDomainItem`
- [ ] `Id` property is unique and required
- [ ] `Name` property describes the item
- [ ] `ImageUrl` is optional (nullable)
- [ ] Domain-specific properties are meaningful
- [ ] All properties have XML documentation
- [ ] Uses `required` keyword for mandatory properties
- [ ] Project compiles without errors

---

## Step 3: Create Data Provider

### 3.1 Create Data Provider Interface (Optional but Recommended)

File: `src/Lfm.{DomainName}/Data/I{DomainName}DataProvider.cs`

```csharp
using Lfm.{DomainName}.Models;

namespace Lfm.{DomainName}.Data;

/// <summary>
/// Interface for {DomainName} data access layer
/// Abstracts the actual data source (API, database, mock data)
/// </summary>
public interface I{DomainName}DataProvider
{
    /// <summary>
    /// Get all available items
    /// </summary>
    List<{DomainName}Item> GetAllItems();

    /// <summary>
    /// Get items matching search query
    /// </summary>
    List<{DomainName}Item> SearchItems(string query);

    /// <summary>
    /// Get items similar to the specified item
    /// </summary>
    List<{DomainName}Item> GetSimilarItems(string itemId);
}
```

### 3.2 Create Concrete Data Provider

File: `src/Lfm.{DomainName}/Data/{DataSourceProvider}.cs`

For **Mock/Sample Data** (POC):

```csharp
using Lfm.{DomainName}.Models;

namespace Lfm.{DomainName}.Data;

/// <summary>
/// Mock {DomainName} data provider for proof-of-concept
/// In production, this would integrate with real API or database
/// </summary>
public class Mock{DomainName}DataProvider : I{DomainName}DataProvider
{
    private static readonly List<{DomainName}Item> SampleItems = new()
    {
        new {DomainName}Item
        {
            Id = "item-001",
            Name = "Sample Item 1",
            ProducerName = "Producer A",
            UserInteractionCount = 5,
            UserRating = 4.5f,
            Category = "Category 1",
            Description = "First sample item",
            Url = "https://example.com/item-001"
        },
        // Add 4-5 more sample items for testing
    };

    public List<{DomainName}Item> GetAllItems()
    {
        return new List<{DomainName}Item>(SampleItems);
    }

    public List<{DomainName}Item> SearchItems(string query)
    {
        return SampleItems
            .Where(item =>
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
    }

    public List<{DomainName}Item> GetSimilarItems(string itemId)
    {
        var sourceItem = SampleItems.FirstOrDefault(i => i.Id == itemId);
        if (sourceItem == null)
            return new List<{DomainName}Item>();

        return SampleItems
            .Where(i =>
                i.Id != itemId &&
                i.Category == sourceItem.Category)
            .ToList();
    }
}
```

For **Real API** (Production):

```csharp
public class {ApiName}{DomainName}DataProvider : I{DomainName}DataProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public {ApiName}{DomainName}DataProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<{DomainName}Item>> GetAllItemsAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync("{API_ENDPOINT}/items");

        if (!response.IsSuccessStatusCode)
            return new List<{DomainName}Item>();

        var json = await response.Content.ReadAsStringAsync();
        // Parse JSON and convert to {DomainName}Item list
        return Parse(json);
    }

    // Implement other methods...
}
```

### 3.3 Validation Checklist

- [ ] Data provider implements `I{DomainName}DataProvider`
- [ ] All required methods are implemented
- [ ] Methods return `List<{DomainName}Item>` (not null)
- [ ] Search is case-insensitive
- [ ] Sample/mock data includes 5+ items
- [ ] Each item has all required properties set
- [ ] No exceptions thrown (use Result<T> pattern)
- [ ] Project compiles and builds successfully

---

## Step 4: Implement History Provider

File: `src/Lfm.{DomainName}/Providers/{DomainName}HistoryProvider.cs`

```csharp
using Lfm.Framework.Abstractions;
using Lfm.{DomainName}.Data;
using Lfm.{DomainName}.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.{DomainName}.Providers;

/// <summary>
/// {DomainName} history provider implementing IUserHistoryProvider<{DomainName}Item>
/// Provides user's interaction history, top items, and engagement metrics
/// </summary>
public class {DomainName}HistoryProvider : IUserHistoryProvider<{DomainName}Item>
{
    private readonly I{DomainName}DataProvider _dataProvider;

    public {DomainName}HistoryProvider(I{DomainName}DataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<Result<List<{DomainName}Item>>> GetUserHistoryAsync(string userId, int limit = int.MaxValue)
    {
        try
        {
            var items = _dataProvider.GetAllItems()
                .Where(i => i.UserInteractionCount > 0)
                .OrderByDescending(i => i.UserInteractionCount)
                .Take(limit)
                .ToList();

            return await Task.FromResult(Result<List<{DomainName}Item>>.Ok(items));
        }
        catch (Exception ex)
        {
            return Result<List<{DomainName}Item>>.Fail(
                ErrorType.UnknownError,
                "Failed to get user history",
                ex.Message);
        }
    }

    public async Task<Result<List<{DomainName}Item>>> GetTopItemsAsync(
        string userId, string period = "overall", int count = 50)
    {
        try
        {
            var items = period.ToLower() switch
            {
                "recent" => _dataProvider.GetAllItems()
                    .Where(i => i.UserInteractionCount > 0)
                    .OrderByDescending(i => i.UserRating ?? 0)
                    .Take(count)
                    .Select((i, idx) => CloneWithRanking(i, $"#{idx + 1}"))
                    .ToList(),

                _ => _dataProvider.GetAllItems()
                    .Where(i => i.UserInteractionCount > 0)
                    .OrderByDescending(i => i.UserRating ?? 0)
                    .ThenByDescending(i => i.UserInteractionCount)
                    .Take(count)
                    .Select((i, idx) => CloneWithRanking(i, $"#{idx + 1}"))
                    .ToList()
            };

            return await Task.FromResult(Result<List<{DomainName}Item>>.Ok(items));
        }
        catch (Exception ex)
        {
            return Result<List<{DomainName}Item>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get top items for period '{period}'",
                ex.Message);
        }
    }

    public async Task<Result<List<{DomainName}Item>>> GetUserHistoryForDateRangeAsync(
        string userId, DateTime from, DateTime to, int count = 50)
    {
        try
        {
            // For POC, return top items by interaction count
            // In production, use actual date tracking from data source
            var items = _dataProvider.GetAllItems()
                .Where(i => i.UserInteractionCount > 0)
                .OrderByDescending(i => i.UserInteractionCount)
                .Take(count)
                .ToList();

            return await Task.FromResult(Result<List<{DomainName}Item>>.Ok(items));
        }
        catch (Exception ex)
        {
            return Result<List<{DomainName}Item>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get history for date range",
                ex.Message);
        }
    }

    public async Task<Result<Dictionary<string, int>>> GetUserInteractionCountsAsync(
        string userId, int maxItems = int.MaxValue)
    {
        try
        {
            var counts = _dataProvider.GetAllItems()
                .Where(i => i.UserInteractionCount > 0)
                .Take(maxItems)
                .ToDictionary(
                    i => i.Name,
                    i => i.UserInteractionCount);

            return await Task.FromResult(Result<Dictionary<string, int>>.Ok(counts));
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, int>>.Fail(
                ErrorType.UnknownError,
                "Failed to get interaction counts",
                ex.Message);
        }
    }

    private static {DomainName}Item CloneWithRanking({DomainName}Item item, string ranking)
    {
        return new {DomainName}Item
        {
            Id = item.Id,
            Name = item.Name,
            ImageUrl = item.ImageUrl,
            ProducerName = item.ProducerName,
            UserInteractionCount = item.UserInteractionCount,
            UserRating = item.UserRating,
            Category = item.Category,
            AverageRating = item.AverageRating,
            Price = item.Price,
            Description = item.Description,
            Url = item.Url,
            Ranking = ranking
        };
    }
}
```

### Validation Checklist

- [ ] Class implements `IUserHistoryProvider<{DomainName}Item>`
- [ ] Constructor validates data provider is not null
- [ ] `GetUserHistoryAsync` filters by `UserInteractionCount > 0`
- [ ] `GetTopItemsAsync` supports "overall" and "recent" periods
- [ ] All methods return `Result<T>` with proper error handling
- [ ] No exceptions thrown at method level
- [ ] Methods are properly marked `async`
- [ ] Helper method `CloneWithRanking` copies all properties

---

## Step 5: Implement Item Provider

File: `src/Lfm.{DomainName}/Providers/{DomainName}ItemProvider.cs`

```csharp
using Lfm.Framework.Abstractions;
using Lfm.{DomainName}.Data;
using Lfm.{DomainName}.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.{DomainName}.Providers;

/// <summary>
/// {DomainName} item provider implementing IItemProvider<{DomainName}Item>
/// Handles item lookups, search, and similarity discovery
/// </summary>
public class {DomainName}ItemProvider : IItemProvider<{DomainName}Item>
{
    private readonly I{DomainName}DataProvider _dataProvider;

    public {DomainName}ItemProvider(I{DomainName}DataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<Result<{DomainName}Item>> GetItemDetailsAsync(string itemId)
    {
        try
        {
            var item = _dataProvider.GetAllItems()
                .FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));

            if (item == null)
                return Result<{DomainName}Item>.Fail(
                    ErrorType.DataError,
                    $"Item '{itemId}' not found");

            return await Task.FromResult(Result<{DomainName}Item>.Ok(item));
        }
        catch (Exception ex)
        {
            return Result<{DomainName}Item>.Fail(
                ErrorType.UnknownError,
                "Failed to get item details",
                ex.Message);
        }
    }

    public async Task<Result<List<{DomainName}Item>>> SearchItemsAsync(string query, int limit = 50)
    {
        try
        {
            var results = _dataProvider.SearchItems(query)
                .Take(limit)
                .ToList();

            return await Task.FromResult(Result<List<{DomainName}Item>>.Ok(results));
        }
        catch (Exception ex)
        {
            return Result<List<{DomainName}Item>>.Fail(
                ErrorType.UnknownError,
                "Search failed",
                ex.Message);
        }
    }

    public async Task<Result<List<{DomainName}Item>>> GetSimilarItemsAsync(string sourceItemId, int limit = 50)
    {
        try
        {
            var item = _dataProvider.GetAllItems()
                .FirstOrDefault(i => i.Id.Equals(sourceItemId, StringComparison.OrdinalIgnoreCase));

            if (item == null)
                return Result<List<{DomainName}Item>>.Fail(
                    ErrorType.DataError,
                    "Source item not found");

            var similar = _dataProvider.GetSimilarItems(sourceItemId)
                .Take(limit)
                .ToList();

            return await Task.FromResult(Result<List<{DomainName}Item>>.Ok(similar));
        }
        catch (Exception ex)
        {
            return Result<List<{DomainName}Item>>.Fail(
                ErrorType.UnknownError,
                "Failed to get similar items",
                ex.Message);
        }
    }

    public async Task<Result<bool>> ItemExistsAsync(string itemId)
    {
        try
        {
            var exists = _dataProvider.GetAllItems()
                .Any(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));

            return await Task.FromResult(Result<bool>.Ok(exists));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(
                ErrorType.UnknownError,
                "Failed to check item existence",
                ex.Message);
        }
    }
}
```

### Validation Checklist

- [ ] Class implements `IItemProvider<{DomainName}Item>`
- [ ] `GetItemDetailsAsync` returns correct item or DataError
- [ ] `SearchItemsAsync` is case-insensitive
- [ ] `GetSimilarItemsAsync` respects limit parameter
- [ ] `ItemExistsAsync` returns bool result
- [ ] All methods use case-insensitive ID comparison
- [ ] Null checks prevent NullReferenceExceptions

---

## Step 6: Implement Recommendation Algorithm

File: `src/Lfm.{DomainName}/Algorithms/{DomainName}RecommendationAlgorithm.cs`

```csharp
using Lfm.Framework.Abstractions;
using Lfm.Framework.Models;
using Lfm.{DomainName}.Data;
using Lfm.{DomainName}.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.{DomainName}.Algorithms;

/// <summary>
/// {DomainName} recommendation algorithm
/// Generates personalized recommendations based on user preferences
/// </summary>
public class {DomainName}RecommendationAlgorithm : IRecommendationAlgorithm<{DomainName}Item>
{
    private readonly I{DomainName}DataProvider _dataProvider;

    public {DomainName}RecommendationAlgorithm(I{DomainName}DataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<Result<List<RecommendationResult<{DomainName}Item>>>> GenerateRecommendationsAsync(
        string userId, string period = "overall", int analysisLimit = 20,
        int recommendationLimit = 20, int filterThreshold = 0)
    {
        try
        {
            // Get user's top items
            var userItems = _dataProvider.GetAllItems()
                .Where(i => i.UserInteractionCount > filterThreshold)
                .OrderByDescending(i => i.UserRating ?? 0)
                .Take(analysisLimit)
                .ToList();

            if (!userItems.Any())
                return Result<List<RecommendationResult<{DomainName}Item>>>.Ok(
                    new List<RecommendationResult<{DomainName}Item>>());

            // Find similar items not yet interacted with
            var recommendations = new Dictionary<string, RecommendationResult<{DomainName}Item>>();

            foreach (var userItem in userItems)
            {
                var similar = _dataProvider.GetSimilarItems(userItem.Id)
                    .Where(i => i.UserInteractionCount == 0)
                    .Take(5)
                    .ToList();

                foreach (var rec in similar)
                {
                    // Calculate score: user rating * item rating / 5
                    var score = (userItem.UserRating ?? 3.5f) * (rec.AverageRating ?? 3.5f) / 5f;

                    if (!recommendations.ContainsKey(rec.Id))
                    {
                        recommendations[rec.Id] = new RecommendationResult<{DomainName}Item>
                        {
                            Item = rec,
                            Score = score,
                            AverageRelevance = score,
                            OccurrenceCount = 1,
                            UserInteractionCount = 0,
                            SourceItems = new List<string> { userItem.Name }
                        };
                    }
                    else
                    {
                        var existing = recommendations[rec.Id];
                        existing.Score += score;
                        existing.OccurrenceCount++;
                        existing.SourceItems.Add(userItem.Name);
                        existing.AverageRelevance = existing.Score / existing.OccurrenceCount;
                    }
                }
            }

            var results = recommendations.Values
                .OrderByDescending(r => r.Score)
                .Take(recommendationLimit)
                .ToList();

            return await Task.FromResult(Result<List<RecommendationResult<{DomainName}Item>>>.Ok(results));
        }
        catch (Exception ex)
        {
            return Result<List<RecommendationResult<{DomainName}Item>>>.Fail(
                ErrorType.UnknownError,
                "Failed to generate recommendations",
                ex.Message);
        }
    }

    public async Task<Result<List<RecommendationResult<{DomainName}Item>>>>
        GenerateRecommendationsForDateRangeAsync(
            string userId, DateTime from, DateTime to, int analysisLimit = 20,
            int recommendationLimit = 20, int filterThreshold = 0)
    {
        // For POC, same as overall recommendations
        return await GenerateRecommendationsAsync(userId, "overall", analysisLimit, recommendationLimit, filterThreshold);
    }
}
```

### Validation Checklist

- [ ] Class implements `IRecommendationAlgorithm<{DomainName}Item>`
- [ ] Analyzes user's top items by rating
- [ ] Filters by `UserInteractionCount == 0` (uninteracted items)
- [ ] Calculates relevance score based on similarity + ratings
- [ ] Returns results sorted by score (descending)
- [ ] Respects both `analysisLimit` and `recommendationLimit`
- [ ] Aggregates multiple source items for same recommendation

---

## Step 7: Implement Action Executor

File: `src/Lfm.{DomainName}/Actions/{DomainName}ActionExecutor.cs`

```csharp
using Lfm.Framework.Abstractions;
using Lfm.{DomainName}.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.{DomainName}.Actions;

/// <summary>
/// {DomainName} action executor
/// Executes domain-specific actions on items
/// </summary>
public class {DomainName}ActionExecutor : IActionExecutor<{DomainName}Item>
{
    private static readonly List<string> AvailableActions = new()
    {
        "add",          // Add to collection
        "rate",         // Rate the item
        "purchase",     // Mark for purchase
        "viewdetails",  // View full details
        "viewsimilar",  // View similar items
        "export"        // Export collection
    };

    public async Task<Result> ExecuteActionAsync(
        {DomainName}Item item, string actionType, Dictionary<string, object>? parameters = null)
    {
        if (!AvailableActions.Contains(actionType.ToLowerInvariant()))
            return Result.Fail(ErrorType.ValidationError, $"Unknown action '{actionType}'");

        if (item == null)
            return Result.Fail(ErrorType.ValidationError, "Item cannot be null");

        try
        {
            return actionType.ToLowerInvariant() switch
            {
                "add" => Result.Ok(),
                "rate" => ExecuteRateAction(item, parameters),
                "purchase" => Result.Ok(),
                "viewdetails" => Result.Ok(),
                "viewsimilar" => Result.Ok(),
                "export" => Result.Ok(),
                _ => Result.Fail(ErrorType.ValidationError, $"Unsupported action: {actionType}")
            };
        }
        catch (Exception ex)
        {
            return Result.Fail(ErrorType.UnknownError, $"Failed to execute action '{actionType}'", ex.Message);
        }
    }

    public async Task<Result<List<string>>> GetAvailableActionsAsync()
    {
        return await Task.FromResult(Result<List<string>>.Ok(new List<string>(AvailableActions)));
    }

    public async Task<Result<bool>> IsActionAvailableAsync(string actionType)
    {
        var available = AvailableActions.Contains(actionType.ToLowerInvariant());
        return await Task.FromResult(Result<bool>.Ok(available));
    }

    private Result ExecuteRateAction({DomainName}Item item, Dictionary<string, object>? parameters)
    {
        if (parameters?.TryGetValue("rating", out var ratingObj) != true)
            return Result.Fail(ErrorType.ValidationError, "Rating parameter required");

        if (!float.TryParse(ratingObj?.ToString(), out var rating) || rating < 1 || rating > 5)
            return Result.Fail(ErrorType.ValidationError, "Rating must be between 1 and 5");

        return Result.Ok();
    }
}
```

### Validation Checklist

- [ ] Class implements `IActionExecutor<{DomainName}Item>`
- [ ] `AvailableActions` list includes 4-6 domain-relevant actions
- [ ] `ExecuteActionAsync` validates action type and item
- [ ] Parameter validation for actions that require it (e.g., "rate")
- [ ] All methods properly handle null parameters
- [ ] Error handling returns appropriate `ErrorType` values
- [ ] Methods use `Result<T>` pattern (no exceptions)

---

## Step 8: Create Unit Tests

Create test directory structure:

```
src/Lfm.Tests/Unit/{DomainName}/
├── {DomainName}HistoryProviderTests.cs
├── {DomainName}ItemProviderTests.cs
├── {DomainName}RecommendationAlgorithmTests.cs
└── {DomainName}ActionExecutorTests.cs
```

Create comprehensive tests following the Wine adapter pattern (see `src/Lfm.Tests/Unit/Wine/`).

**Target**: Minimum 80+ tests across 4 test classes

### Validation Checklist

- [ ] 25+ history provider tests
- [ ] 30+ item provider tests
- [ ] 25+ recommendation algorithm tests
- [ ] 10+ action executor tests
- [ ] All tests passing (100% pass rate)
- [ ] Tests cover both happy path and error cases
- [ ] Tests verify Result<T> error handling
- [ ] Build completes with 0 errors

---

## Step 9: Register in Dependency Injection

File: `src/Lfm.Cli/Program.cs` (or your main application startup)

```csharp
// Add to service registration
services.AddSingleton<I{DomainName}DataProvider, Mock{DomainName}DataProvider>();
// OR for real API:
// services.AddSingleton<I{DomainName}DataProvider>(_ =>
//     new {ApiName}{DomainName}DataProvider(httpClientFactory));

services.AddScoped<IUserHistoryProvider<{DomainName}Item>, {DomainName}HistoryProvider>();
services.AddScoped<IItemProvider<{DomainName}Item>, {DomainName}ItemProvider>();
services.AddScoped<IRecommendationAlgorithm<{DomainName}Item>, {DomainName}RecommendationAlgorithm>();
services.AddScoped<IActionExecutor<{DomainName}Item>, {DomainName}ActionExecutor>();
```

Add project reference to test project:

```xml
<!-- In src/Lfm.Tests/Lfm.Tests.csproj -->
<ProjectReference Include="..\Lfm.{DomainName}\Lfm.{DomainName}.csproj" />
```

### Validation Checklist

- [ ] Project added to solution file
- [ ] Project references added to test project
- [ ] DI registrations added and organized
- [ ] All referenced namespaces imported
- [ ] Application builds successfully with new domain

---

## Step 10: Validate Integration

### 10.1 Run Full Build

```bash
dotnet build -c Release
```

**Expected Result**: 0 errors, 0 warnings (or pre-existing warnings only)

### 10.2 Run Domain Tests

```bash
dotnet test --filter "FullyQualifiedName~Lfm.Tests.Unit.{DomainName}"
```

**Expected Result**: All tests passing

### 10.3 Integration Test

Create a simple test demonstrating all components working together:

```csharp
[Fact]
public async Task IntegrationTest_AllComponentsWorkTogether()
{
    // Arrange
    var dataProvider = new Mock{DomainName}DataProvider();
    var historyProvider = new {DomainName}HistoryProvider(dataProvider);
    var itemProvider = new {DomainName}ItemProvider(dataProvider);
    var recommendationAlgo = new {DomainName}RecommendationAlgorithm(dataProvider);
    var actionExecutor = new {DomainName}ActionExecutor();

    // Act - Get user history
    var historyResult = await historyProvider.GetUserHistoryAsync("user1");

    // Act - Search for item
    var searchResult = await itemProvider.SearchItemsAsync("sample");

    // Act - Get recommendations
    var recsResult = await recommendationAlgo.GenerateRecommendationsAsync("user1");

    // Act - Execute action
    var actionResult = await actionExecutor.ExecuteActionAsync(
        searchResult.Data.First(), "add");

    // Assert
    historyResult.Success.Should().BeTrue();
    searchResult.Success.Should().BeTrue();
    recsResult.Success.Should().BeTrue();
    actionResult.Success.Should().BeTrue();
}
```

### 10.4 Checklist

- [ ] Build completes: `0 Errors, 0 Warnings`
- [ ] All domain tests pass: `100% pass rate`
- [ ] Integration test passes
- [ ] Solution file includes new project
- [ ] No broken references

---

## Summary

Congratulations! You have successfully implemented a complete domain adapter for the Lfm.Framework. Your new domain now:

✅ **Integrates with the framework** - Implements all required interfaces
✅ **Has comprehensive tests** - 80+ unit tests with full coverage
✅ **Is production-ready** - Error handling, validation, and Result<T> pattern
✅ **Is maintainable** - Clear structure, documented code, validated design
✅ **Is reusable** - Same pattern for next domain (books, films, cars, etc.)

### Files Created

- [ ] `Lfm.{DomainName}.csproj`
- [ ] `Models/{DomainName}Item.cs`
- [ ] `Data/I{DomainName}DataProvider.cs`
- [ ] `Data/Mock{DomainName}DataProvider.cs`
- [ ] `Providers/{DomainName}HistoryProvider.cs`
- [ ] `Providers/{DomainName}ItemProvider.cs`
- [ ] `Algorithms/{DomainName}RecommendationAlgorithm.cs`
- [ ] `Actions/{DomainName}ActionExecutor.cs`
- [ ] `Unit/{DomainName}/[4 test files]`

### Next Steps

1. Refine recommendations algorithm for your specific domain
2. Connect to real data source (API/database) if desired
3. Add domain-specific formatters (`IItemFormatter<TItem>`)
4. Implement caching using `ICache<TKey, TItem>`
5. Write documentation for your domain

---

**Questions?** Refer back to Music adapter (`src/Lfm.Core/Music/`) or Wine adapter (`src/Lfm.Wine/`) for implementation examples.
