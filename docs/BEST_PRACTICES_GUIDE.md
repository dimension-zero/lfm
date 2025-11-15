# Lfm.Framework - Domain Adapter Best Practices Guide

Complete best practices and patterns for implementing high-quality, production-ready domain adapters.

---

## Table of Contents

1. [Data Provider Optimization](#data-provider-optimization)
2. [Recommendation Algorithm Design](#recommendation-algorithm-design)
3. [Caching Strategy](#caching-strategy)
4. [Error Handling Patterns](#error-handling-patterns)
5. [Testing Best Practices](#testing-best-practices)
6. [Common Pitfalls](#common-pitfalls)
7. [Performance Optimization](#performance-optimization)
8. [Production Readiness](#production-readiness)
9. [Scaling Considerations](#scaling-considerations)

---

## Data Provider Optimization

### 1. Provider Lifecycle Management

**Good Practice: Minimize External API Calls**

```csharp
// ✅ GOOD - Cache results at provider level
public class BookDataProvider
{
    private readonly ICache<string, BookItem> _cache;
    private List<BookItem>? _allBooksCache;
    private DateTime _allBooksCacheTime;
    private const int CACHE_DURATION_MINUTES = 60;

    public async Task<List<BookItem>> GetAllBooksAsync()
    {
        // Return cached if still fresh
        if (_allBooksCache != null &&
            (DateTime.UtcNow - _allBooksCacheTime).TotalMinutes < CACHE_DURATION_MINUTES)
        {
            return _allBooksCache;
        }

        // Fetch and cache
        var books = await _externalApi.GetBooksAsync();
        _allBooksCache = books;
        _allBooksCacheTime = DateTime.UtcNow;
        return books;
    }
}

// ❌ BAD - Uncached repeated calls
public class BookDataProvider
{
    public async Task<List<BookItem>> GetAllBooksAsync()
    {
        // Makes API call every time
        return await _externalApi.GetBooksAsync();
    }
}
```

### 2. Pagination and Batch Operations

**Good Practice: Implement Intelligent Pagination**

```csharp
// ✅ GOOD - Respects API limits and tracks progress
public async Task<List<BookItem>> GetAuthorBooksAsync(string authorId, int pageSize = 50)
{
    var allBooks = new List<BookItem>();
    int page = 1;

    while (true)
    {
        var result = await _externalApi.GetAuthorBooksAsync(authorId, page, pageSize);
        if (result.Count == 0) break;

        allBooks.AddRange(result);

        // Respect API throttling
        await Task.Delay(200);

        page++;
    }

    return allBooks;
}

// ❌ BAD - No pagination, tries to load everything at once
public async Task<List<BookItem>> GetAuthorBooksAsync(string authorId)
{
    return await _externalApi.GetAllAuthorBooksAsync(authorId); // Potential OOM error
}
```

### 3. Null-Safe Property Access

**Good Practice: Defensive null checking**

```csharp
// ✅ GOOD - Handles missing data gracefully
public BookItem MapFromApiResponse(ApiBook apiBook)
{
    return new BookItem
    {
        Id = apiBook.Id ?? throw new ArgumentNullException(nameof(apiBook.Id)),
        Name = apiBook.Title ?? "Unknown Title",
        ImageUrl = apiBook.CoverUrl?.IsValidUri() ? apiBook.CoverUrl : null,
        AuthorName = apiBook.Author?.Name ?? "Unknown Author",
        PublicationYear = apiBook.Published?.Year ?? 0,
        Genre = apiBook.PrimaryGenre?.ToLower() ?? "uncategorized"
    };
}

// ❌ BAD - Crashes on missing properties
public BookItem MapFromApiResponse(ApiBook apiBook)
{
    return new BookItem
    {
        Id = apiBook.Id,
        Name = apiBook.Title,  // NullReferenceException if null
        AuthorName = apiBook.Author.Name,  // NullReferenceException
        Genre = apiBook.Genre.ToLower()  // NullReferenceException
    };
}
```

### 4. API Rate Limiting Awareness

**Good Practice: Respect provider rate limits**

```csharp
// ✅ GOOD - Implements rate limit detection
public class BookDataProvider
{
    private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1);
    private const int MIN_REQUEST_DELAY_MS = 100;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public async Task<List<BookItem>> SearchAsync(string query)
    {
        await _rateLimiter.WaitAsync();
        try
        {
            // Enforce minimum delay between requests
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest.TotalMilliseconds < MIN_REQUEST_DELAY_MS)
            {
                await Task.Delay((int)(MIN_REQUEST_DELAY_MS - timeSinceLastRequest.TotalMilliseconds));
            }

            _lastRequestTime = DateTime.UtcNow;
            return await _externalApi.SearchAsync(query);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}
```

---

## Recommendation Algorithm Design

### 1. Score Calculation Strategy

**Good Practice: Transparent, tunable scoring**

```csharp
// ✅ GOOD - Parameterized algorithm with clear logic
public class BookRecommendationAlgorithm : IRecommendationAlgorithm<BookItem>
{
    private const float BASE_SCORE = 1.0f;
    private const float GENRE_WEIGHT = 0.3f;
    private const float AUTHOR_WEIGHT = 0.2f;
    private const float RATING_WEIGHT = 0.5f;

    private float CalculateScore(BookItem candidate, List<BookItem> userFavorites)
    {
        float score = BASE_SCORE;

        // Genre matching
        var genreMatches = userFavorites.Where(b => b.Genre == candidate.Genre).Count();
        score += (genreMatches / (float)userFavorites.Count) * GENRE_WEIGHT;

        // Author familiarity
        var authorMatches = userFavorites.Where(b => b.AuthorName == candidate.AuthorName).Count();
        score += (authorMatches / (float)userFavorites.Count) * AUTHOR_WEIGHT;

        // Rating alignment
        var avgUserRating = userFavorites.Average(b => b.UserRating ?? 3);
        if (candidate.AvgRating.HasValue)
        {
            var ratingDiff = Math.Abs(candidate.AvgRating.Value - avgUserRating);
            score += Math.Max(0, 1 - (ratingDiff / 5)) * RATING_WEIGHT;
        }

        return score;
    }
}

// ❌ BAD - Black-box algorithm, hard to tune
private float CalculateScore(BookItem candidate, List<BookItem> userFavorites)
{
    // Magic numbers, unclear logic
    return userFavorites.Where(b => b.Genre == candidate.Genre).Count() * 3.14159f +
           candidate.AvgRating.GetValueOrDefault() * 2.71828f;
}
```

### 2. Candidate Pool Strategy

**Good Practice: Manage candidate expansion dynamically**

```csharp
// ✅ GOOD - Expands candidate pool when needed
public async Task<Result<List<RecommendationResult<BookItem>>>>
    GenerateRecommendationsAsync(string userId, string period = "overall")
{
    var userHistory = await _historyProvider.GetUserHistoryAsync(userId);
    if (!userHistory.Success) return Result<List<RecommendationResult<BookItem>>>.Fail(userHistory.Error);

    var candidates = new HashSet<BookItem>();
    var expandedAuthors = new HashSet<string>();

    // Start with user's favorite genres
    foreach (var favoriteBook in userHistory.Data.Take(10))
    {
        if (!string.IsNullOrEmpty(favoriteBook.Genre))
        {
            var genreCandidates = await _itemProvider.SearchItemsAsync(favoriteBook.Genre);
            if (genreCandidates.Success)
            {
                foreach (var item in genreCandidates.Data.Take(20))
                {
                    candidates.Add(item);
                }
            }
        }

        // Expand to similar authors
        if (!string.IsNullOrEmpty(favoriteBook.AuthorName) && !expandedAuthors.Contains(favoriteBook.AuthorName))
        {
            var authorBooks = await _itemProvider.SearchItemsAsync(favoriteBook.AuthorName);
            if (authorBooks.Success)
            {
                foreach (var item in authorBooks.Data.Take(30))
                {
                    candidates.Add(item);
                }
                expandedAuthors.Add(favoriteBook.AuthorName);
            }
        }
    }

    // Score and return
    var scored = candidates
        .Select(c => new RecommendationResult<BookItem> { Item = c, Score = CalculateScore(c, userHistory.Data) })
        .OrderByDescending(r => r.Score)
        .Take(20)
        .ToList();

    return Result<List<RecommendationResult<BookItem>>>.Ok(scored);
}

// ❌ BAD - Fixed candidate pool, misses opportunities
private async Task<List<RecommendationResult<BookItem>>> GenerateRecommendationsAsync(string userId)
{
    var favorites = await _historyProvider.GetTopItemsAsync(userId, "overall", 5);

    // Only looks at these 5 books, never expands
    var candidates = await _itemProvider.GetSimilarItemsAsync(favorites[0].Id, 20);

    return candidates.Select(c => new RecommendationResult<BookItem> { Item = c }).ToList();
}
```

### 3. Period Parameter Handling

**Good Practice: Support meaningful time periods**

```csharp
// ✅ GOOD - Maps periods to actual date ranges
private (DateTime from, DateTime to) GetDateRangeForPeriod(string period)
{
    var now = DateTime.UtcNow;
    return period switch
    {
        "today" => (now.Date, now),
        "thisweek" => (now.AddDays(-(int)now.DayOfWeek), now),
        "thismonth" => (new DateTime(now.Year, now.Month, 1), now),
        "thisyear" => (new DateTime(now.Year, 1, 1), now),
        "last30days" => (now.AddDays(-30), now),
        "last90days" => (now.AddDays(-90), now),
        "last365days" => (now.AddDays(-365), now),
        "overall" => (DateTime.MinValue, now),
        _ => throw new ArgumentException($"Unknown period: {period}")
    };
}

// ❌ BAD - No period support or hardcoded behavior
public async Task<List<BookItem>> GetTopItemsAsync(string userId, string period = "overall")
{
    // Ignores period parameter, always returns overall
    return await _externalApi.GetTopBooksAllTimeAsync(userId);
}
```

---

## Caching Strategy

### 1. Cache Key Design

**Good Practice: Structured, collision-free keys**

```csharp
// ✅ GOOD - Clear cache key pattern
private string CreateCacheKey(string operation, string userId, string? parameter = null)
{
    return parameter == null
        ? $"book:{operation}:{userId}"
        : $"book:{operation}:{userId}:{parameter}";
}

// Usage:
await _cache.StoreAsync(CreateCacheKey("history", userId), results, expiryMinutes: 30);
var cached = await _cache.RetrieveAsync(CreateCacheKey("topitems", userId, "overall"));

// ❌ BAD - Ambiguous keys prone to collisions
await _cache.StoreAsync($"{userId}_results", results);  // What results?
var cached = await _cache.RetrieveAsync($"user_{userId}");  // Which user data?
```

### 2. Cache Invalidation Strategy

**Good Practice: Explicit invalidation on mutations**

```csharp
// ✅ GOOD - Invalidates related cache entries
public async Task<Result> AddBookToWishlistAsync(string userId, string bookId)
{
    var result = await _externalApi.AddToWishlistAsync(userId, bookId);
    if (!result.Success) return result;

    // Invalidate user-specific caches
    await _cache.RemoveAsync($"book:topitems:{userId}");
    await _cache.RemoveAsync($"book:history:{userId}");
    await _cache.RemoveAsync($"book:wishlist:{userId}");

    return Result.Ok();
}

// ❌ BAD - Doesn't invalidate cache, data becomes stale
public async Task<Result> AddBookToWishlistAsync(string userId, string bookId)
{
    return await _externalApi.AddToWishlistAsync(userId, bookId);
    // Cache never updated, wishlist now stale
}
```

### 3. Cache Expiration Tuning

**Good Practice: Balance freshness and performance**

```csharp
// ✅ GOOD - Differentiated expiration by data type
private const int USER_HISTORY_EXPIRY_MINUTES = 60;    // Stable data
private const int SEARCH_RESULTS_EXPIRY_MINUTES = 30;   // Medium volatility
private const int RECOMMENDATIONS_EXPIRY_MINUTES = 15;  // High variance
private const int ITEM_DETAILS_EXPIRY_MINUTES = 240;    // Very stable

await _cache.StoreAsync(
    CreateCacheKey("history", userId),
    results,
    expiryMinutes: USER_HISTORY_EXPIRY_MINUTES);
```

---

## Error Handling Patterns

### 1. Result<T> Composition

**Good Practice: Chain operations with error propagation**

```csharp
// ✅ GOOD - Errors propagate automatically
public async Task<Result<List<RecommendationResult<BookItem>>>>
    GenerateRecommendationsAsync(string userId)
{
    // Step 1: Get history
    var historyResult = await _historyProvider.GetUserHistoryAsync(userId);
    if (!historyResult.Success) return Result<List<RecommendationResult<BookItem>>>.Fail(historyResult.Error);

    // Step 2: Get top items
    var topResult = await _historyProvider.GetTopItemsAsync(userId, "overall", 10);
    if (!topResult.Success) return Result<List<RecommendationResult<BookItem>>>.Fail(topResult.Error);

    // Step 3: Generate scores
    var recommendations = topResult.Data
        .SelectMany(fav => GetCandidateBooks(fav))
        .DistinctBy(b => b.Id)
        .Select(b => new RecommendationResult<BookItem> { Item = b, Score = CalculateScore(b, historyResult.Data) })
        .OrderByDescending(r => r.Score)
        .Take(20)
        .ToList();

    return Result<List<RecommendationResult<BookItem>>>.Ok(recommendations);
}

// ❌ BAD - Doesn't check errors, crashes on null
public async Task<List<RecommendationResult<BookItem>>>
    GenerateRecommendationsAsync(string userId)
{
    var history = await _historyProvider.GetUserHistoryAsync(userId);
    var top = await _historyProvider.GetTopItemsAsync(userId, "overall", 10);

    // If either result fails, this will crash with NullReferenceException
    return top.Data
        .SelectMany(fav => GetCandidateBooks(fav))
        .ToList();
}
```

### 2. Validation Before Processing

**Good Practice: Validate early**

```csharp
// ✅ GOOD - Validates input before processing
public async Task<Result<List<BookItem>>> SearchItemsAsync(string query, int limit = 50)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(query))
        return Result<List<BookItem>>.Fail(ErrorType.ValidationError, "Search query cannot be empty");

    if (limit < 1 || limit > 1000)
        return Result<List<BookItem>>.Fail(ErrorType.ValidationError, "Limit must be between 1 and 1000");

    try
    {
        var results = await _externalApi.SearchAsync(query, limit);
        return Result<List<BookItem>>.Ok(results);
    }
    catch (HttpRequestException ex)
    {
        return Result<List<BookItem>>.Fail(
            ErrorType.ApiError,
            $"Failed to search: {ex.Message}",
            ex.StackTrace);
    }
}

// ❌ BAD - No validation
public async Task<List<BookItem>> SearchItemsAsync(string query, int limit)
{
    return await _externalApi.SearchAsync(query, limit);  // Crashes if query is null
}
```

### 3. Graceful Degradation

**Good Practice: Provide partial results when possible**

```csharp
// ✅ GOOD - Returns what succeeded
public async Task<List<BookItem>> GetAuthorBooksWithCoverUrlsAsync(string authorId)
{
    var books = await _externalApi.GetBooksAsync(authorId);

    var enrichedBooks = new List<BookItem>();
    foreach (var book in books)
    {
        var result = await GetBookCoverAsync(book.Id);

        // Add book even if cover fetch fails
        book.ImageUrl = result.Success ? result.Data : null;
        enrichedBooks.Add(book);
    }

    return enrichedBooks;
}

// ❌ BAD - Fails completely if any part fails
public async Task<List<BookItem>> GetAuthorBooksWithCoverUrlsAsync(string authorId)
{
    var books = await _externalApi.GetBooksAsync(authorId);

    // If any single cover fetch fails, entire operation fails
    foreach (var book in books)
    {
        book.ImageUrl = await _externalApi.GetCoverUrlAsync(book.Id);  // Throws exception
    }

    return books;
}
```

---

## Testing Best Practices

### 1. Test Structure and Organization

**Good Practice: Organized test classes with clear sections**

```csharp
// ✅ GOOD - Clear test organization
public class BookItemProviderTests
{
    private readonly MockBookDataProvider _dataProvider;
    private readonly BookItemProvider _provider;

    public BookItemProviderTests()
    {
        _dataProvider = new MockBookDataProvider();
        _provider = new BookItemProvider(_dataProvider);
    }

    // ========== GetItemDetailsAsync Tests ==========

    [Fact]
    public async Task GetItemDetailsAsync_ReturnsBookByValidId()
    {
        // Arrange
        var books = _dataProvider.GetAllBooks();
        var targetBook = books[0];

        // Act
        var result = await _provider.GetItemDetailsAsync(targetBook.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(targetBook.Id);
    }

    [Fact]
    public async Task GetItemDetailsAsync_FailsWithInvalidId()
    {
        // Act
        var result = await _provider.GetItemDetailsAsync("nonexistent-id");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.DataError);
    }
}

// ❌ BAD - Unorganized, unclear test purpose
public class BookItemProviderTests
{
    [Fact]
    public void Test1() { }

    [Fact]
    public void Test2() { }

    // No clear what is being tested
}
```

### 2. Mock Data Design

**Good Practice: Representative mock data**

```csharp
// ✅ GOOD - Mock data covers realistic scenarios
public class MockBookDataProvider
{
    public List<BookItem> GetAllBooks()
    {
        return new List<BookItem>
        {
            new BookItem { Id = "b1", Name = "The Great Gatsby", Genre = "Fiction", AuthorName = "F. Scott Fitzgerald" },
            new BookItem { Id = "b2", Name = "1984", Genre = "Dystopian", AuthorName = "George Orwell" },
            new BookItem { Id = "b3", Name = "To Kill a Mockingbird", Genre = "Fiction", AuthorName = "Harper Lee" },
            new BookItem { Id = "b4", Name = "Animal Farm", Genre = "Dystopian", AuthorName = "George Orwell" },
        };
    }
}

// ❌ BAD - Insufficient mock data for testing
public class MockBookDataProvider
{
    public List<BookItem> GetAllBooks()
    {
        return new List<BookItem>
        {
            new BookItem { Id = "b1", Name = "Book 1" },
        };
    }
}
```

### 3. Test Coverage Targets

**Minimum test counts by component:**
- **History Provider**: 25+ tests (initialization, success, failures, null handling, edge cases)
- **Item Provider**: 35+ tests (search, details, similarity, existence checks)
- **Recommendation Algorithm**: 30+ tests (scoring, candidate pools, periods)
- **Action Executor**: 15+ tests (actions, validation, parameter handling)

**Total per domain: 100+ tests**

---

## Common Pitfalls

### Pitfall 1: Unbounded Data Loading

**Problem**: Loading all user data without pagination limits

```csharp
// ❌ PITFALL - Loads unlimited data
public async Task<List<BookItem>> GetUserHistoryAsync(string userId)
{
    var allHistory = new List<BookItem>();
    int page = 1;

    while (true)  // No exit condition!
    {
        var batch = await _externalApi.GetUserHistoryAsync(userId, page++);
        allHistory.AddRange(batch);
        // Will loop infinitely if there's a data loop or massive dataset
    }

    return allHistory;
}

// ✅ FIX - Respects limits
public async Task<List<BookItem>> GetUserHistoryAsync(string userId, int limit = int.MaxValue)
{
    var allHistory = new List<BookItem>();
    int page = 1;
    const int MAX_PAGES = 10000;  // Sanity limit

    while (allHistory.Count < limit && page < MAX_PAGES)
    {
        var batch = await _externalApi.GetUserHistoryAsync(userId, page);
        if (batch.Count == 0) break;

        allHistory.AddRange(batch.Take(limit - allHistory.Count));
        page++;
    }

    return allHistory;
}
```

### Pitfall 2: Silent Failures in Aggregation

**Problem**: Errors in one item crash entire operation

```csharp
// ❌ PITFALL - One failed enrichment fails all
var enrichedBooks = books
    .Select(b => new { Book = b, Cover = _externalApi.GetCoverAsync(b.Id).Result })  // Throws!
    .ToList();

// ✅ FIX - Handles errors gracefully
var enrichedBooks = new List<object>();
foreach (var book in books)
{
    var coverResult = await _externalApi.GetCoverAsync(b.Id);
    enrichedBooks.Add(new { Book = book, Cover = coverResult.Data });
}
```

### Pitfall 3: N+1 Query Problem

**Problem**: One query per item instead of batch operations

```csharp
// ❌ PITFALL - 101 API calls for 100 books
var books = await _externalApi.GetTopBooksAsync(userId, 100);
foreach (var book in books)
{
    var details = await _externalApi.GetBookDetailsAsync(book.Id);  // 100 separate calls!
    enrichedBooks.Add(details);
}

// ✅ FIX - Single batch call or smart caching
var books = await _externalApi.GetTopBooksAsync(userId, 100);
var detailedBooks = await _externalApi.GetBookDetailsAsync(books.Select(b => b.Id).ToList());
```

### Pitfall 4: Incorrect Period Handling

**Problem**: Ignoring or misunderstanding period parameters

```csharp
// ❌ PITFALL - Treats all periods the same
public async Task<List<BookItem>> GetTopItemsAsync(string userId, string period = "overall")
{
    return await _externalApi.GetTopBooksAllTimeAsync(userId);  // Period ignored
}

// ✅ FIX - Maps periods correctly
public async Task<List<BookItem>> GetTopItemsAsync(string userId, string period = "overall")
{
    var (from, to) = GetDateRangeForPeriod(period);
    return await _externalApi.GetTopBooksAsync(userId, from, to);
}
```

---

## Performance Optimization

### 1. Algorithm Efficiency

**Good Practice: O(n) or O(n log n) operations**

```csharp
// ✅ GOOD - O(n) complexity
private float CalculateScore(BookItem candidate, List<BookItem> userHistory)
{
    var historicalIds = new HashSet<string>(userHistory.Select(b => b.Id));
    return historicalIds.Contains(candidate.Id) ? 0 : 1;  // O(1) lookup
}

// ❌ BAD - O(n²) complexity for 1000 items = 1M comparisons
private float CalculateScore(BookItem candidate, List<BookItem> userHistory)
{
    return userHistory.Count(b => b.Genre == candidate.Genre) * 100;  // O(n) per call
}
```

### 2. Lazy Loading

**Good Practice: Only load what you need**

```csharp
// ✅ GOOD - Minimal data for list views
public async Task<Result<List<BookItem>>> SearchItemsAsync(string query, int limit = 50)
{
    var results = await _externalApi.SearchAsync(query, limit);
    return Result<List<BookItem>>.Ok(
        results.Select(r => new BookItem { Id = r.Id, Name = r.Name, ImageUrl = r.CoverUrl }).ToList()
    );
}

// Detailed loads only requested
public async Task<Result<BookItem>> GetItemDetailsAsync(string bookId)
{
    return await _externalApi.GetFullDetailsAsync(bookId);
}
```

### 3. Parallel Operations

**Good Practice: Batch operations safely**

```csharp
// ✅ GOOD - Limited parallelism with throttling
public async Task<List<BookItem>> GetAuthorBooksInParallelAsync(List<string> authorIds)
{
    var results = new List<BookItem>();
    var semaphore = new SemaphoreSlim(5);  // Max 5 concurrent requests

    var tasks = authorIds.Select(async authorId =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await _externalApi.GetAuthorBooksAsync(authorId);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var batchResults = await Task.WhenAll(tasks);
    return batchResults.SelectMany(r => r).ToList();
}

// ❌ BAD - Unbounded parallelism
var tasks = authorIds.Select(id => _externalApi.GetAuthorBooksAsync(id));
var results = await Task.WhenAll(tasks);  // 1000 concurrent requests = API ban
```

---

## Production Readiness

### 1. Logging and Diagnostics

**Good Practice: Comprehensive logging**

```csharp
// ✅ GOOD - Detailed logging at each step
public async Task<Result<List<RecommendationResult<BookItem>>>>
    GenerateRecommendationsAsync(string userId)
{
    _logger.LogInformation("Generating recommendations for user {UserId}", userId);

    var historyResult = await _historyProvider.GetUserHistoryAsync(userId);
    if (!historyResult.Success)
    {
        _logger.LogError("Failed to get user history: {Error}", historyResult.Error?.Message);
        return Result<List<RecommendationResult<BookItem>>>.Fail(historyResult.Error);
    }

    _logger.LogDebug("Retrieved {Count} historical items for user {UserId}", historyResult.Data.Count, userId);

    var recommendations = GenerateScores(historyResult.Data);

    _logger.LogInformation("Generated {Count} recommendations for user {UserId}", recommendations.Count, userId);
    return Result<List<RecommendationResult<BookItem>>>.Ok(recommendations);
}
```

### 2. Configuration Validation

**Good Practice: Validate all settings at startup**

```csharp
// ✅ GOOD - Validates configuration immediately
public async Task<Result> ValidateConfigurationAsync()
{
    var errors = new List<string>();

    if (string.IsNullOrEmpty(_config.ExternalApiKey))
        errors.Add("ExternalApiKey is not configured");

    if (_config.CacheExpiryMinutes < 1)
        errors.Add("CacheExpiryMinutes must be at least 1");

    if (_config.MaxConcurrentRequests < 1 || _config.MaxConcurrentRequests > 100)
        errors.Add("MaxConcurrentRequests must be between 1 and 100");

    if (errors.Any())
    {
        var message = string.Join("; ", errors);
        return Result.Fail(ErrorType.ConfigurationError, message);
    }

    return Result.Ok();
}
```

### 3. Health Checks

**Good Practice: Monitor provider health**

```csharp
// ✅ GOOD - Periodic health verification
public async Task<Result<HealthStatus>> GetHealthAsync()
{
    try
    {
        var testResult = await _externalApi.GetBookAsync("test-id");
        if (testResult == null)
            return Result<HealthStatus>.Ok(new HealthStatus { IsHealthy = false, Message = "API returned null" });

        return Result<HealthStatus>.Ok(new HealthStatus { IsHealthy = true, Message = "All systems operational" });
    }
    catch (Exception ex)
    {
        return Result<HealthStatus>.Fail(
            ErrorType.ApiError,
            $"Health check failed: {ex.Message}");
    }
}
```

---

## Scaling Considerations

### 1. Database vs API Scaling

**Decision Tree**:
- **API-backed domain**: When external API can handle volume
  - Use aggressive caching (60+ minutes)
  - Batch operations where supported
  - Implement request deduplication

- **Database-backed domain**: When controlling data source
  - Use indices on frequently queried columns
  - Implement pagination for all queries
  - Cache strategically (user-specific data 60min, global data 120min+)

### 2. User Scale Growth

**10 to 100 users**:
- Single cache instance (in-memory)
- API calls with minimal throttling

**100 to 10,000 users**:
- Distributed cache (Redis/Memcached)
- Aggressive throttling (200ms+ between requests)
- Request deduplication service

**10,000+ users**:
- Database for frequently accessed data
- CDN for image URLs
- Dedicated cache cluster
- Request queue system with prioritization

### 3. Data Volume Growth

**Small (<100K items)**:
- In-memory caching sufficient
- Full item search effective

**Medium (100K-1M items)**:
- Database with indices required
- Pagination mandatory
- Search optimization (trigram indices, full-text search)

**Large (1M+ items)**:
- Elasticsearch or similar for search
- Database sharding for item storage
- Recommendation algorithm must be approximate, not exhaustive

---

## Checklist Before Production

- [ ] 100+ unit tests written and passing
- [ ] All error paths covered by tests
- [ ] Configuration validation in place
- [ ] Logging at INFO level for major operations
- [ ] Rate limiting or throttling implemented
- [ ] Cache strategy documented
- [ ] Error messages are user-friendly
- [ ] Documentation complete (README, implementation guide, API reference)
- [ ] Example usage provided in README
- [ ] Code review completed
- [ ] Performance tested with realistic data volumes
- [ ] Graceful degradation tested (partial failures handled)
- [ ] Security review completed (no secrets in logs, input validation)

---

## Related Documentation

- See [ADAPTER_IMPLEMENTATION_GUIDE.md](ADAPTER_IMPLEMENTATION_GUIDE.md) for step-by-step implementation
- See [DOMAIN_ONBOARDING_CHECKLIST.md](DOMAIN_ONBOARDING_CHECKLIST.md) for progress tracking
- See [API_REFERENCE.md](API_REFERENCE.md) for interface specifications
- See [src/Lfm.Framework/README.md](../src/Lfm.Framework/README.md) for architecture overview
