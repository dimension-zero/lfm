# Lfm.EfModels - Entity Framework Core Provider for Last.fm API

## Overview

This project implements a full LINQ query provider that makes the Last.fm API queryable through Entity Framework Core, similar to how you would query SQL Server or PostgreSQL. It translates LINQ expressions into Last.fm API calls and maps the responses back to EF entities.

**Purpose**: Enable A/B testing between:
- Original implementation: Last.fm API → MCP Tools
- EF implementation: Last.fm API → EF Core → MCP Tools

## Architecture

### Components

1. **Entities** (`Entities/`)
   - `Artist`, `Track`, `Album`, `RecentTrack` - EF Core entities with navigation properties
   - `LastFmEntity` - Base class providing `User` and `QueryTimestamp`
   - Composite primary keys: `Name` + `User` (no integer IDs from API)

2. **Configuration** (`Configuration/`)
   - Entity configurations defining composite keys, indexes, and relationships
   - Foreign key relationships: `Track.Artist`, `Album.Artist`, etc.

3. **Query Provider** (`Provider/`)
   - `LastFmQueryProvider` - Implements `IAsyncQueryProvider` for EF Core
   - `LastFmQueryable<T>` - Implements `IQueryable<T>` and `IAsyncEnumerable<T>`
   - `LastFmExpressionVisitor` - Parses LINQ expression trees
   - `QueryTranslator` - Translates parsed expressions to API calls
   - `ResultMapper` - Maps API responses to EF entities

4. **DbContext**
   - `LastFmDbContext` - Provides `DbSet<Artist>`, `DbSet<Track>`, etc.
   - Configured for in-memory use (no database persistence)

### Data Flow

```
LINQ Query
  ↓
LastFmQueryable<T>.GetAsyncEnumerator()
  ↓
LastFmQueryProvider.ExecuteAsync()
  ↓
LastFmExpressionVisitor.Analyze() → QueryDescriptor
  ↓
QueryTranslator.Translate() → ApiCallInfo
  ↓
ILastFmApiClient (CachedLastFmApiClient) → Last.fm API
  ↓
ResultMapper.Map*() → EF Entities
  ↓
List<T> returned to caller
```

## Supported LINQ Operations

### ✅ Fully Supported

| LINQ Operation | Translation | Example |
|----------------|-------------|---------|
| `Where(a => a.User == "x")` | API `user` parameter | `Artists.Where(a => a.User == "smarshal")` |
| `Where(t => t.ArtistName == "x")` | Changes query type to artist-specific | `Tracks.Where(t => t.ArtistName == "Pink Floyd")` |
| `OrderBy(a => a.PlayCount)` | API sorting (PlayCount only) | `Artists.OrderByDescending(a => a.PlayCount)` |
| `Take(n)` | API `limit` parameter | `Artists.Take(50)` |
| `Skip(n)` | API `page` parameter (must be multiple of limit) | `Artists.Skip(50).Take(50)` // page 2 |
| `Include(a => a.Tracks)` | Additional API calls for navigation properties | `Artists.Include(a => a.Tracks)` |
| `First()` / `FirstOrDefault()` | Sets `limit=1` | `Artists.First()` |
| `Single()` / `SingleOrDefault()` | Sets `limit=2` (validates single result) | `Artists.Single(a => a.Name == "x")` |
| `Count()` / `LongCount()` | API response `total` field | `Artists.Count()` |

### Query Type Mapping

| Entity Set | Where Clause | API Endpoint | Query Type |
|------------|--------------|--------------|------------|
| `Artists` | User only | `user.getTopArtists` | TopArtists |
| `Tracks` | User only | `user.getTopTracks` | TopTracks |
| `Tracks` | User + ArtistName | `artist.getTopTracks` | ArtistTracks |
| `Albums` | User only | `user.getTopAlbums` | TopAlbums |
| `Albums` | User + ArtistName | `artist.getTopAlbums` | ArtistAlbums |
| `RecentTracks` | User | `user.getRecentTracks` | RecentTracks |

### Date Range Support

```csharp
// Supported for RecentTracks only
context.RecentTracks
    .Where(r => r.User == "smarshal")
    .Where(r => r.PlayedAt >= startDate && r.PlayedAt <= endDate)
```

### Period Support

```csharp
// Supported for Top* queries
context.Artists
    .Where(a => a.User == "smarshal")
    .Where(a => a.Period == LastFmPeriod.Month)
```

## Unsupported Operations

### ⚠️ Partially Supported (Client-Side Filtering)

These operations are tracked in `QueryDescriptor.ClientSideFilters` and executed after API results are retrieved:

| Operation | Reason | Workaround |
|-----------|--------|------------|
| `ThenBy()` / `ThenByDescending()` | Last.fm API only supports single-column sorting | Results filtered client-side after retrieval |
| `Where(a => a.Name.Contains("x"))` | API doesn't support partial matching | Retrieve all, filter client-side |
| `Where(a => a.Name.StartsWith("x"))` | API doesn't support prefix matching | Retrieve all, filter client-side |
| `Where(a => a.PlayCount > 100)` | API doesn't support range queries | Retrieve all, filter client-side |

**Warning**: Client-side filters log warnings about potential performance impact.

### ❌ Not Supported

| Operation | Reason | Error Message |
|-----------|--------|---------------|
| `GroupBy()` | Not applicable to Last.fm data model | `NotSupportedException` |
| `Join()` | API doesn't support joins | `NotSupportedException` |
| `SelectMany()` | No nested collections in API | `NotSupportedException` |
| `Skip(n)` where `n % limit != 0` | Last.fm uses page-based pagination | `InvalidOperationException` with correct Skip value |
| `OrderBy(a => a.Name)` | API only sorts by PlayCount | Tracked as client-side filter |
| Synchronous execution | All API calls are async | `NotSupportedException` - use `ToListAsync()` |

### Pagination Constraints

Last.fm API uses **page-based pagination**, not offset-based:

```csharp
// ✅ Valid - Skip is multiple of Take
artists.Skip(50).Take(50)  // Page 2, limit 50

// ❌ Invalid - Skip not multiple of Take
artists.Skip(25).Take(50)
// Error: "Skip (25) must be a multiple of Take (50) for Last.fm API pagination.
//         Use Skip(0) instead."
```

## Usage Examples

### Basic Query

```csharp
var context = new LastFmDbContext(options);

// Translates to: user.getTopArtists(user="smarshal", limit=10)
var artists = await context.Artists
    .Where(a => a.User == "smarshal")
    .OrderByDescending(a => a.PlayCount)
    .Take(10)
    .ToListAsync();
```

### Artist-Specific Query

```csharp
// Translates to: artist.getTopTracks(artist="Pink Floyd", limit=5)
var tracks = await context.Tracks
    .Where(t => t.ArtistName == "Pink Floyd")
    .Take(5)
    .ToListAsync();
```

### With Navigation Properties

```csharp
// First: user.getTopArtists(user="smarshal", limit=10)
// Then: artist.getTopTracks for each artist (if not cached)
var artistsWithTracks = await context.Artists
    .Where(a => a.User == "smarshal")
    .Include(a => a.Tracks)
    .Take(10)
    .ToListAsync();
```

### Pagination

```csharp
// Page 1
var page1 = await context.Artists
    .Where(a => a.User == "smarshal")
    .Take(50)
    .ToListAsync();

// Page 2
var page2 = await context.Artists
    .Where(a => a.User == "smarshal")
    .Skip(50)
    .Take(50)
    .ToListAsync();
```

### Date Range (Recent Tracks)

```csharp
var startDate = new DateTime(2023, 1, 1);
var endDate = new DateTime(2023, 12, 31);

// Translates to: user.getRecentTracks(user="smarshal", from=..., to=...)
var tracks = await context.RecentTracks
    .Where(r => r.User == "smarshal")
    .Where(r => r.PlayedAt >= startDate && r.PlayedAt <= endDate)
    .ToListAsync();
```

## Design Decisions

### 1. In-Memory Only

The provider does **not** persist data to a database. It uses the existing `CachedLastFmApiClient` for caching (119x performance improvement). The in-memory provider is only used for EF Core design-time tools.

### 2. Composite Keys (Name + User)

Last.fm API doesn't provide integer IDs, so entities use composite keys:

```csharp
builder.HasKey(a => new { a.Name, a.User });
```

This allows proper EF relationships while maintaining API compatibility.

### 3. String-Based Numeric Fields

`PlayCount` and `Rank` are stored as strings, matching the API response format:
- **API Fidelity**: Preserve exact values from API
- **Overflow Prevention**: Some counts exceed `int.MaxValue`
- **EF Compatibility**: Can be converted for LINQ operations

### 4. Navigation Properties

Foreign keys use composite keys for relationships:

```csharp
builder.HasMany(a => a.Tracks)
    .WithOne(t => t.Artist)
    .HasForeignKey(t => new { t.ArtistName, t.User })
    .HasPrincipalKey(a => new { a.Name, a.User })
    .OnDelete(DeleteBehavior.Cascade);
```

### 5. Client-Side Filtering Transparency

Unsupported operations are logged with warnings:

```
Warning: 2 client-side filters applied. This may impact performance.
Filters: Contains: a.Name.Contains("Pink"), OrderBy: Name
```

This makes it clear when queries aren't fully translated to API calls.

## Limitations

1. **No Joins**: Last.fm API is document-oriented, not relational
2. **Limited Sorting**: Only PlayCount sorting supported by API
3. **Page-Based Pagination**: Skip must be multiple of Take
4. **No Complex Filtering**: Partial matching, ranges require client-side filtering
5. **Async Only**: All operations require `ToListAsync()`, `FirstAsync()`, etc.
6. **No Transactions**: Each query is independent (REST API)
7. **No Updates/Deletes**: Read-only provider (Last.fm is read-only)

## Performance Considerations

- **Caching**: All API calls go through `CachedLastFmApiClient` (119x speedup)
- **Client-Side Filters**: May fetch more data than needed - avoid when possible
- **Include()**: Triggers additional API calls per entity (use sparingly)
- **Pagination**: Use proper page-aligned Skip/Take for efficient API usage

## Testing

See Phase 5 for comparison tests between original and EF implementations:
- `OriginalVsEfComparisonTests` - Functional equivalence tests
- Performance benchmarks - Verify caching effectiveness
- `ORIGINAL-VS-EF-COMPARISON.md` - Documentation of differences

## Project References

- `Lfm.Shared` - API models (`TopArtists`, `TopTracks`, etc.)
- `Lfm.Core` - `ILastFmApiClient`, `CachedLastFmApiClient`, configuration
- EF Core 9.0 packages

## Future Enhancements

- Better client-side filter performance (compiled expressions)
- Batch loading for Include() operations
- Query caching at provider level
- Support for more complex LINQ expressions
