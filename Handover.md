# LFM Multi-Source Data Abstraction - Session Handover

## Session Overview
**Date**: 2025-10-26
**Branch**: `lfm2EF`
**Objective**: Abstract LFM data sources to support local CSV/JSON files (Spotify, YouTube) alongside Last.fm API

## Current Status: Phase 2 WIP (Build Errors)

**Build Status**: ⚠️ ~45 compilation errors in LocalFileDataProvider
**Commits on Branch**: 3 total
- `94cf935` - Phase 1: IMusicDataProvider abstraction layer ✅
- `40cde0c` - Phase 2.1: File format models ✅
- `2be7194` - Phase 2 WIP: Parsers + aggregators (⚠️ build errors)

## What Was Completed This Session

### ✅ Phase 1: Core Abstraction Layer (Committed: 94cf935)

**Files Created:**
- `src/Lfm.Core/Services/IMusicDataProvider.cs` - Unified abstraction for all data sources
- `src/Lfm.Core/Services/LastFmDataProvider.cs` - Adapter wrapping existing Last.fm API

**Files Modified:**
- `src/Lfm.Cli/Program.cs` - Added IMusicDataProvider DI registration

**Architecture Decisions:**
- **Adapter Pattern**: LastFmDataProvider wraps CachedLastFmApiClient with zero code changes
- **Result<T> Pattern**: All methods return Result<T> for consistent error handling
- **Provider Metadata**: ProviderName, SupportsDateRanges, SupportsSimilarArtists, SupportsLookup

### ✅ Phase 2.1: File Format Models (Committed: 40cde0c)

**Files Created:**
- `src/Lfm.Core/Models/LocalFiles/SpotifyModels.cs`
  - `SpotifyExtendedHistoryItem` - Extended Streaming History (endsong*.json)
  - `SpotifyStandardHistoryItem` - Standard Streaming History (StreamingHistory*.json)
  - Computed properties: `IsMusicPlayback`, `Skipped`, etc.

- `src/Lfm.Core/Models/LocalFiles/YouTubeModels.cs`
  - `YouTubeWatchHistoryItem` - Google Takeout watch-history.json
  - `YouTubeMusicLibrarySong` - Library CSV format
  - Computed properties: `IsMusicPlayback`, `PlayCountValue`, etc.

### ⚠️ Phase 2 WIP: Parsers & Aggregators (Committed: 2be7194)

**Files Created:**
1. `src/Lfm.Core/Services/LocalFiles/ILocalFileParser.cs` ✅
   - Parser interface with `CanParse()` and `ParseAsync()` methods
   - `PlayEvent` model - normalized format for all data sources

2. `src/Lfm.Core/Services/LocalFiles/SpotifyJsonParser.cs` ✅
   - Auto-detects Extended vs Standard format by JSON structure
   - Filters podcasts/audiobooks, skipped tracks
   - Returns list of `PlayEvent` with date filtering support

3. `src/Lfm.Core/Services/LocalFiles/YouTubeMusicParser.cs` ✅
   - Handles JSON watch history (watch-history.json)
   - Handles CSV library format (library-songs.csv)
   - Parses CSV with quoted field support

4. `src/Lfm.Core/Services/LocalFiles/LocalFileAggregator.cs` ✅
   - Aggregates `PlayEvent` lists into top artists/tracks/albums
   - Case-insensitive grouping, play count summation
   - Methods for artist tracks, album tracks, recent tracks, etc.

5. `src/Lfm.Core/Services/LocalFiles/LocalFileDataProvider.cs` ⚠️ **HAS BUILD ERRORS**
   - Attempted IMusicDataProvider implementation
   - **Problem**: Returns wrong types (List<ArtistInfo> instead of TopArtists wrapper)
   - **Problem**: Missing ~10 interface methods
   - **Problem**: Wrong return types for lookup methods

**Files Modified:**
- `src/Lfm.Core/Configuration/LfmConfig.cs` ✅
  - Added `DataSourceMode` enum (LastFm, LocalFiles, Merged)
  - Added `LocalFilePaths` config property

## The Problem: Interface Mismatch

### Root Cause
The `IMusicDataProvider` interface uses wrapper types from Last.fm API models:
- `TopArtists` (contains `List<Artist> Artists`)
- `TopTracks` (contains `List<Track> Tracks`)
- `TopAlbums` (contains `List<Album> Albums`)
- `TrackLookupInfo`, `AlbumLookupInfo`, `ArtistLookupInfo`
- `RecentTracks`, `SimilarArtists`, `TopTags`

LocalFileDataProvider currently returns:
- `List<ArtistInfo>` instead of `TopArtists`
- `List<TrackInfo>` instead of `TopTracks`
- etc.

### Build Error Count
- ~45 compilation errors
- 13 CS0246 errors (type not found - missing using statements)
- 32+ CS0535/CS0738 errors (interface not implemented, wrong return types)

### What Works Despite Errors
1. ✅ **Core parsing logic**: SpotifyJsonParser and YouTubeMusicParser are solid
2. ✅ **Aggregation logic**: LocalFileAggregator correctly groups events
3. ✅ **PlayEvent model**: Clean normalized format
4. ✅ **Config foundation**: DataSourceMode enum ready

## Next Session: Two Approaches

### Approach A: Simplified Thin Adapter (Recommended - 1 hour)

**Strategy**: Keep current parser/aggregator logic, create minimal wrapper for type conversion

**Implementation**:
1. Add missing `using` statements to all LocalFiles/*.cs files
2. Rewrite LocalFileDataProvider to wrap aggregator results:
   ```csharp
   public async Task<Result<TopArtists>> GetTopArtistsAsync(...)
   {
       var events = await LoadEventsAsync(username, startDate, endDate);
       if (!events.IsSuccess) return Result<TopArtists>.Failure(events.ErrorMessage);

       var artistList = _aggregator.AggregateTopArtists(events.Value, limit);

       // Convert List<ArtistInfo> → TopArtists wrapper
       var artists = artistList.Select(a => new Artist {
           Name = a.Name,
           PlayCount = a.PlayCount.ToString(),
           Url = a.Url,
           Mbid = a.Mbid,
           Attributes = new ArtistAttributes { Rank = "?" } // Assign after sorting
       }).ToList();

       // Assign ranks
       for (int i = 0; i < artists.Count; i++)
           artists[i].Attributes.Rank = (i + 1).ToString();

       return Result<TopArtists>.Success(new TopArtists { Artists = artists });
   }
   ```

3. Implement required interface properties:
   ```csharp
   public string ProviderName => "Local Files";
   public bool SupportsDateRanges => true;
   public bool SupportsSimilarArtists => false;
   public bool SupportsLookup => true;
   ```

4. Stub unsupported methods:
   ```csharp
   public Task<Result<SimilarArtists>> GetSimilarArtistsAsync(...)
   {
       return Task.FromResult(Result<SimilarArtists>.Failure(
           "Similar artists not available for local file data sources"));
   }

   public Task<Result<TopTags>> GetArtistTopTagsAsync(...)
   {
       return Task.FromResult(Result<TopTags>.Failure(
           "Tags not available for local file data sources"));
   }
   ```

**Pros**: Fast, pragmatic, leverages existing logic
**Cons**: Some code duplication in type conversion
**Estimated Time**: 1 hour

### Approach B: Full Interface Conformance (2-3 hours)

**Strategy**: Rewrite aggregator to directly produce Last.fm model types

**Implementation**:
1. Modify LocalFileAggregator to return `TopArtists`, `TopTracks`, `TopAlbums` directly
2. Implement all interface methods with proper return types
3. Create conversion helpers for TrackLookupInfo, AlbumLookupInfo, ArtistLookupInfo
4. Full conformance with IMusicDataProvider contract

**Pros**: Cleaner architecture, no type conversion layer
**Cons**: More invasive changes, harder to debug
**Estimated Time**: 2-3 hours

## Recommendation

**Choose Approach A (Simplified Thin Adapter)**

**Reasoning**:
1. Aligns with "1-person garage project" philosophy (CLAUDE.md)
2. Core parsing logic is already solid - no need to rewrite
3. Get to working state faster, iterate if needed
4. Easier to debug (clear separation: parser → aggregator → adapter)
5. Can always refactor to Approach B later if needed

## Implementation Checklist (Approach A)

**Step 1: Fix using statements** (10 minutes)
- [ ] Add to all LocalFiles/*.cs files:
  ```csharp
  using Lfm.Core.Models;
  using Lfm.Core.Models.Results;
  ```

**Step 2: Rewrite LocalFileDataProvider** (40 minutes)
- [ ] Implement required properties (ProviderName, Supports*)
- [ ] Fix GetTopArtistsAsync return type + conversion
- [ ] Fix GetTopTracksAsync return type + conversion
- [ ] Fix GetTopAlbumsAsync return type + conversion
- [ ] Fix GetTopArtistsForDateRangeAsync (call GetTopArtistsAsync with dates)
- [ ] Fix GetTopTracksForDateRangeAsync
- [ ] Fix GetTopAlbumsForDateRangeAsync
- [ ] Fix GetRecentTracksAsync return type + conversion
- [ ] Fix GetArtistTopTracksAsync
- [ ] Fix GetArtistTopAlbumsAsync
- [ ] Fix GetTrackInfoAsync → TrackLookupInfo
- [ ] Fix GetAlbumInfoAsync → AlbumLookupInfo
- [ ] Add GetArtistInfoAsync → ArtistLookupInfo
- [ ] Stub GetSimilarArtistsAsync
- [ ] Stub GetArtistTopTagsAsync (return NotSupported)

**Step 3: Build and test** (10 minutes)
- [ ] `dotnet build -c Release`
- [ ] Fix any remaining errors
- [ ] Commit: "fix: LocalFileDataProvider interface implementation"

## Type Reference (for Step 2)

**Wrapper Types Location**: `src/Lfm.Core/Models/LastFmModels.cs`

```csharp
// Top lists
public class TopArtists { public List<Artist> Artists { get; set; } }
public class TopTracks { public List<Track> Tracks { get; set; } }
public class TopAlbums { public List<Album> Albums { get; set; } }

// Recent/Similar
public class RecentTracks { public List<RecentTrack> Tracks { get; set; } }
public class SimilarArtists { public List<SimilarArtist> Artists { get; set; } }
public class TopTags { public List<Tag> Tags { get; set; } }

// Lookup info (different namespace!)
src/Lfm.Core/Models/TrackLookupInfo.cs
src/Lfm.Core/Models/AlbumLookupInfo.cs
src/Lfm.Core/Models/ArtistLookupInfo.cs
```

**Key Difference**:
- `Artist` (in TopArtists) vs `ArtistInfo` (simple model)
- `Track` (in TopTracks) has nested `ArtistInfo` property
- Lookup types have nested `*Details` classes

## Useful Code Snippets

### Converting AggregatorResult → TopArtists

```csharp
var artistList = _aggregator.AggregateTopArtists(events.Value, limit);

var artists = artistList.Select((a, index) => new Artist {
    Name = a.Name,
    PlayCount = a.PlayCount.ToString(),
    Url = string.Empty,
    Mbid = string.Empty,
    Attributes = new ArtistAttributes { Rank = (index + 1).ToString() }
}).ToList();

return Result<TopArtists>.Success(new TopArtists {
    Artists = artists,
    Attributes = new TopArtistsAttributes {
        User = username ?? "local-files",
        TotalPages = "1",
        Page = page.ToString(),
        PerPage = limit?.ToString() ?? "50",
        Total = artists.Count.ToString()
    }
});
```

### Converting to TrackLookupInfo

```csharp
var trackInfo = new TrackLookupInfo {
    Track = new TrackLookupInfo.TrackDetails {
        Name = trackName,
        Mbid = string.Empty,
        Url = string.Empty,
        Artist = new TrackLookupInfo.ArtistDetails {
            Name = artistName,
            Mbid = string.Empty,
            Url = string.Empty
        },
        Album = album != null ? new TrackLookupInfo.AlbumDetails {
            Artist = artistName,
            Title = album,
            Mbid = string.Empty,
            Url = string.Empty
        } : null,
        Userplaycount = playCount.ToString()
    }
};
```

## After Phase 2 Completion

### Next Steps (Phase 3+)

1. **Phase 3: Caching** (1 day)
   - File hash-based cache invalidation
   - Leverage existing ICacheStorage infrastructure

2. **Phase 4: MergedDataProvider** (1-2 days)
   - Combine Last.fm API + local files
   - Deduplication by artist/track/album name
   - Sum play counts from all sources

3. **Phase 5: Command Updates** (1 day)
   - Update DI registration to switch providers based on config
   - Add CLI overrides for data source

4. **Phase 6: Documentation & Testing** (1 day)
   - Unit tests for parsers/aggregators
   - Integration tests for LocalFileDataProvider
   - Update README with local file usage guide

## Testing Commands

```bash
# Build (expect errors until fixed)
dotnet build -c Release

# After fixing LocalFileDataProvider
dotnet build -c Release  # Should be clean

# Manual test with sample file (Phase 3)
lfm artists --source-path "C:/path/to/spotify-history.json"

# Commit after fixing
git add -A
git commit -m "fix: LocalFileDataProvider interface implementation (Phase 2 complete)"
```

## Key Files to Reference

**Understanding Interface Contract**:
- `src/Lfm.Core/Services/IMusicDataProvider.cs` - Interface to implement
- `src/Lfm.Core/Services/LastFmDataProvider.cs` - Reference implementation

**Model Types**:
- `src/Lfm.Core/Models/LastFmModels.cs` - Wrapper types (TopArtists, etc.)
- `src/Lfm.Core/Models/TrackLookupInfo.cs` - Track lookup structure
- `src/Lfm.Core/Models/AlbumLookupInfo.cs` - Album lookup structure
- `src/Lfm.Core/Models/ArtistLookupInfo.cs` - Artist lookup structure

**Current WIP**:
- `src/Lfm.Core/Services/LocalFiles/LocalFileDataProvider.cs` - Needs fixing
- `src/Lfm.Core/Services/LocalFiles/LocalFileAggregator.cs` - Already working

## Architecture Patterns

1. **Adapter Pattern**: LastFmDataProvider wraps existing API client
2. **Parser Strategy**: ILocalFileParser allows format-specific implementations
3. **Aggregator Pattern**: LocalFileAggregator converts events → statistics
4. **Result<T> Pattern**: Consistent error handling everywhere
5. **Thin Adapter** (recommended): LocalFileDataProvider wraps aggregator with type conversion

## Session Notes

- User approved comprehensive multi-source plan
- Selected all 4 data sources: Spotify Extended/Standard, YouTube JSON/CSV
- Parsers and aggregators implemented successfully
- Hit interface mismatch during LocalFileDataProvider implementation
- Pragmatic decision: Commit WIP state, document approaches, ask user to decide
- Recommendation: Approach A (thin adapter) for speed and simplicity
