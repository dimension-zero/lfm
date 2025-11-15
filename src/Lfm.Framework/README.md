# Lfm.Framework - Domain-Agnostic Recommendation Engine

## Overview

`Lfm.Framework` is a generic recommendation and item management framework that provides domain-agnostic abstractions for building recommendation systems, search capabilities, and action execution across any domain.

Originally extracted from the Last.fm CLI music recommendation system, this framework has been generalized to support multiple domains: music, wine, books, films, cars, retail products, and more.

## Architecture

### Core Design Pattern

The framework uses **adapter pattern** with **generic interfaces** to enable multi-domain support:

```
┌─────────────────────────────────────────────────────────────┐
│                    Lfm.Framework                            │
│  (Domain-Agnostic Interfaces & Shared Models)               │
└─────────────────────────────────────────────────────────────┘
          ▲                    ▲                    ▲
          │                    │                    │
    ┌─────┴─────┐      ┌─────┴─────┐      ┌──────┴──────┐
    │  Lfm.Core │      │ Lfm.Wine  │      │  Lfm.{X}    │
    │  (Music)  │      │ (Wine)    │      │  (Future)   │
    └───────────┘      └───────────┘      └─────────────┘

Each domain implements the same interfaces
with domain-specific logic
```

### Core Interfaces

#### 1. **IDomainItem** - Base Contract for Any Item
```csharp
public interface IDomainItem
{
    string Id { get; set; }
    string Name { get; set; }
    string? ImageUrl { get; set; }
}
```
Every domain item must have a unique ID, name, and optional image.

#### 2. **IUserHistoryProvider<TItem>** - User Activity Tracking
Provides access to user's interaction history with items:
- `GetUserHistoryAsync()` - Complete history
- `GetTopItemsAsync()` - User's top items
- `GetUserHistoryForDateRangeAsync()` - Time-bound history
- `GetUserInteractionCountsAsync()` - Engagement metrics

**Music Example**: Tasted wines, ratings, purchase history
**Wine Example**: User's favorite wines, most-played artists

#### 3. **IItemProvider<TItem>** - Search & Discovery
Handles finding items and related recommendations:
- `GetItemDetailsAsync()` - Item lookup
- `SearchItemsAsync()` - Full-text search
- `GetSimilarItemsAsync()` - Recommendations based on similarity
- `ItemExistsAsync()` - Existence validation

**Music Example**: Artist details, track search, similar artists
**Wine Example**: Wine details, vintage search, similar wines by variety

#### 4. **IRecommendationAlgorithm<TItem>** - Intelligent Recommendations
Generates personalized recommendations based on user preferences:
- `GenerateRecommendationsAsync()` - Overall recommendations
- `GenerateRecommendationsForDateRangeAsync()` - Time-scoped recommendations

Uses user's history and item similarity to produce ranked recommendations.

#### 5. **IActionExecutor<TItem>** - Domain Actions
Executes domain-specific actions on items:
- `ExecuteActionAsync()` - Perform action
- `GetAvailableActionsAsync()` - List possible actions
- `IsActionAvailableAsync()` - Check action availability

**Music Example**: Play now, queue, add to playlist, view artist
**Wine Example**: Add to cellar, rate, mark for purchase, view details

#### 6. **IItemFormatter<TItem>** - Presentation Layer
Formats items for display (future enhancement):
- `FormatItemForLine()` - Single-line display
- `FormatItemDetailed()` - Detailed view
- `FormatItemsAsTable()` - Table format
- `GetRecommendedColumnWidths()` - Layout optimization

### Supporting Models

#### RecommendationResult<TItem>
```csharp
public class RecommendationResult<TItem> where TItem : IDomainItem
{
    public required TItem Item { get; set; }                    // The recommended item
    public float Score { get; set; }                           // Relevance score
    public float AverageRelevance { get; set; }                // Average relevance
    public int OccurrenceCount { get; set; }                   // How many times recommended
    public int UserInteractionCount { get; set; }              // User engagement count
    public List<string> SourceItems { get; set; }              // What triggered recommendation
    public Dictionary<string, object>? Metadata { get; set; }  // Domain-specific data
}
```

#### ICache<TKey, TItem>
Generic caching interface for performance optimization:
```csharp
public interface ICache<TKey, TItem> where TItem : class
{
    Task<Result> StoreAsync(TKey key, TItem item, int expiryMinutes = 10);
    Task<Result<TItem>> RetrieveAsync(TKey key);
    Task<Result<bool>> ExistsAsync(TKey key);
    Task<Result> RemoveAsync(TKey key);
    Task<Result<int>> CleanupExpiredAsync();
    Task<Result<int>> CleanupAsync();
    Task<Result> ClearAllAsync();
    Task<Result<CacheInfo>> GetInfoAsync();
}
```

## Implementation Examples

### Music Domain (Existing)
Located in: `src/Lfm.Core/Music/`

**Key Components**:
- `MusicItem` - Tracks, artists, albums
- `MusicItemConverter` - Converts Last.fm API responses to MusicItem
- `LastFmHistoryProvider` - Wraps Last.fm API for user history
- `LastFmItemProvider` - Search and similarity lookups
- `MusicRecommendationAlgorithm` - Music-specific recommendations

**Data Source**: Last.fm API

### Wine Domain (POC)
Located in: `src/Lfm.Wine/`

**Key Components**:
- `WineItem` - Wine with variety, vintage, region
- `MockWineDataProvider` - Sample data for demonstration
- `WineHistoryProvider` - User's tasting history
- `WineItemProvider` - Wine search and similarity
- `WineRecommendationAlgorithm` - Variety-based recommendations
- `WineActionExecutor` - Actions like "add to cellar", "rate"

**Data Source**: Mock data (extensible to real wine APIs)

## Result<T> Error Handling Pattern

All framework methods return `Result<T>` for explicit error handling:

```csharp
public async Task<Result<List<TItem>>> SearchItemsAsync(string query)
{
    try
    {
        var items = await _provider.SearchAsync(query);
        return Result<List<TItem>>.Ok(items);
    }
    catch (Exception ex)
    {
        return Result<List<TItem>>.Fail(
            ErrorType.ApiError,
            "Search failed",
            ex.Message);
    }
}
```

No exceptions thrown at framework level - errors are returned as values.

## Quick Start: Implementing a New Domain

### 1. Create Domain Model
```csharp
public class BookItem : IDomainItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? ImageUrl { get; set; }
    public string? AuthorName { get; set; }
    public int PublicationYear { get; set; }
    public string? Genre { get; set; }
    public float? UserRating { get; set; }
    public int UserReadCount { get; set; }
}
```

### 2. Create Data Provider
```csharp
public class BookDataProvider
{
    public List<BookItem> GetAllBooks() { }
    public List<BookItem> SearchBooks(string query) { }
    public List<BookItem> GetSimilarBooks(string genre) { }
}
```

### 3. Implement Adapter Interfaces
```csharp
public class BookHistoryProvider : IUserHistoryProvider<BookItem>
{
    private readonly BookDataProvider _provider;

    public async Task<Result<List<BookItem>>> GetUserHistoryAsync(string userId, int limit)
    {
        // Return user's read books
    }

    public async Task<Result<List<BookItem>>> GetTopItemsAsync(string userId, string period)
    {
        // Return user's top-rated books
    }

    // Implement other interface methods...
}
```

### 4. Implement IItemProvider
```csharp
public class BookItemProvider : IItemProvider<BookItem>
{
    public async Task<Result<BookItem>> GetItemDetailsAsync(string itemId) { }
    public async Task<Result<List<BookItem>>> SearchItemsAsync(string query) { }
    public async Task<Result<List<BookItem>>> GetSimilarItemsAsync(string sourceItemId) { }
    public async Task<Result<bool>> ItemExistsAsync(string itemId) { }
}
```

### 5. Implement IRecommendationAlgorithm
```csharp
public class BookRecommendationAlgorithm : IRecommendationAlgorithm<BookItem>
{
    public async Task<Result<List<RecommendationResult<BookItem>>>>
        GenerateRecommendationsAsync(string userId, string period = "overall")
    {
        // Recommend books based on user's favorite genres and authors
    }
}
```

### 6. Implement IActionExecutor
```csharp
public class BookActionExecutor : IActionExecutor<BookItem>
{
    public async Task<Result> ExecuteActionAsync(BookItem item, string actionType)
    {
        return actionType switch
        {
            "addshelf" => Result.Ok(),      // Add to reading shelf
            "rate" => Result.Ok(),          // Rate the book
            "purchase" => Result.Ok(),      // Mark for purchase
            "viewreviews" => Result.Ok(),   // View reviews
            _ => Result.Fail(ErrorType.ValidationError, "Unknown action")
        };
    }
}
```

### 7. Add to DI Container
```csharp
// In Program.cs or startup
services.AddSingleton<BookDataProvider>();
services.AddScoped<IUserHistoryProvider<BookItem>, BookHistoryProvider>();
services.AddScoped<IItemProvider<BookItem>, BookItemProvider>();
services.AddScoped<IRecommendationAlgorithm<BookItem>, BookRecommendationAlgorithm>();
services.AddScoped<IActionExecutor<BookItem>, BookActionExecutor>();
```

## Testing

Each domain adapter includes comprehensive unit tests:

```
src/Lfm.Tests/Unit/Wine/
├── WineHistoryProviderTests.cs       (25 tests)
├── WineItemProviderTests.cs          (35 tests)
├── WineRecommendationAlgorithmTests.cs (28 tests)
└── WineActionExecutorTests.cs        (10 tests)
```

**Total Wine Tests**: 98 passing
**Pattern**: All domain adapters should follow the same test structure

## Key Design Principles

### 1. **Generic Reusability**
- Single set of interfaces works for any domain
- No music-specific or wine-specific code in framework
- Types parameterized with `<TItem>` where `TItem : IDomainItem`

### 2. **Explicit Error Handling**
- `Result<T>` pattern instead of exceptions
- Errors are values, not control flow
- Always check `result.Success` or `result.IsSuccess` before using data

### 3. **Data Source Independence**
- Framework doesn't care about data source (API, database, mock, file)
- Each domain adapter integrates its own data provider
- `IUserHistoryProvider` abstracts the source completely

### 4. **Composition Over Inheritance**
- Adapters compose framework interfaces
- No deep inheritance hierarchies
- Easy to extend or replace individual components

### 5. **Minimal Framework Footprint**
- Framework only defines contracts (interfaces + models)
- No business logic in framework
- Each domain adapter contains domain-specific logic

## Directory Structure

```
src/Lfm.Framework/
├── Abstractions/
│   ├── IDomainItem.cs
│   ├── IUserHistoryProvider.cs
│   ├── IItemProvider.cs
│   ├── IRecommendationAlgorithm.cs
│   ├── IActionExecutor.cs
│   ├── IItemFormatter.cs
│   └── ICache.cs
├── Models/
│   ├── RecommendationResult.cs
│   └── CacheInfo.cs
└── README.md (this file)

src/Lfm.Core/Music/
├── Models/MusicItem.cs
├── Converters/MusicItemConverter.cs
├── Providers/
│   ├── LastFmHistoryProvider.cs
│   └── LastFmItemProvider.cs
├── Algorithms/MusicRecommendationAlgorithm.cs
└── Actions/MusicActionExecutor.cs

src/Lfm.Wine/
├── Models/WineItem.cs
├── Data/MockWineDataProvider.cs
├── Providers/
│   ├── WineHistoryProvider.cs
│   └── WineItemProvider.cs
├── Algorithms/WineRecommendationAlgorithm.cs
└── Actions/WineActionExecutor.cs
```

## Performance Considerations

### Caching
- All data access should be cached using `ICache<TKey, TItem>`
- Cache key strategy: `{Domain}:{Operation}:{Parameters}`
- Default expiry: 10 minutes (configurable)

### Pagination
- Long result sets should use offset/limit
- Recommendation algorithms should respect `recommendationLimit`
- User histories should support `limit` parameter

### Lazy Loading
- Don't load full item details unless requested
- Use minimal data for list views (ID, name, image only)
- Load full details in `GetItemDetailsAsync()`

## Extensibility Points

### Future Enhancements
1. **IItemFormatter<TItem>** - Display customization
2. **ICache<TKey, TItem>** - Performance optimization
3. **Batch Operations** - Multiple items at once
4. **Filtering & Sorting** - Advanced search capabilities
5. **User Preferences** - Personalization tuning
6. **Analytics** - Usage tracking

### Adding New Domains
Each new domain (books, films, cars, retail) follows the same pattern:
1. Create domain project (`Lfm.{DomainName}`)
2. Define domain model implementing `IDomainItem`
3. Create data provider for domain
4. Implement 4+ core interfaces
5. Add unit tests (mirror Wine test structure)
6. Register in DI container

## Version History

- **v1.0.0** - Framework extracted from Lfm.Core (Music)
- **v1.1.0** - Wine adapter POC (demonstrates multi-domain support)
- **v1.2.0** - Planned: Books adapter
- **v1.3.0** - Planned: Films adapter

## Related Projects

- **Lfm.Core** - Music domain implementation
- **Lfm.Wine** - Wine domain POC
- **Lfm.Shared** - Shared Result<T> pattern and ErrorType enums
- **Lfm.Tests** - Comprehensive test suites for all adapters
