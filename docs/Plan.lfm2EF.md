# Test Harness Plan: Compare Original vs EF Implementation

## Goal

Prove that the lfm2EF implementation produces **identical output** to the original implementation, then measure performance differences.

## Architecture

### 1. New Test Project: `Lfm.Tests`
- xUnit test project targeting .NET 9.0
- References both `Lfm.Core` and `Lfm.McpServer`
- Package: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`

### 2. Test Data (JSON Fixtures)
**Location**: `test-data/lastfm-responses/`

**Files to create**:
- `top-artists-response.json` - Sample Last.fm user.getTopArtists response
- `top-tracks-response.json` - Sample Last.fm user.getTopTracks response
- `top-albums-response.json` - Sample Last.fm user.getTopAlbums response

**Fixture Structure**: Real Last.fm API JSON structure including all fields (Name, PlayCount, Url, Mbid, etc.)

### 3. Mock Implementation: `MockLastFmApiClient`
- Implements `ILastFmApiClient`
- Loads JSON fixtures and deserializes to model instances
- Returns same data for both original and EF paths
- Lives in `Lfm.Tests/Mocks/MockLastFmApiClient.cs`

### 4. Test Classes

#### `TransformationAccuracyTests.cs`
**Tests transformation correctness**:
- `CompactArtist_MatchesOriginalData()` - Verify Name, PlayCount, Rank are preserved
- `CompactTrack_FlattenedArtistIsCorrect()` - Verify Track.Artist.Name → artist flattening
- `CompactAlbum_FlattenedArtistIsCorrect()` - Verify Album.Artist.Name → artist flattening
- `TokenOptimization_RemovesUrlAndMbid()` - Verify Url/Mbid are excluded

#### `OutputComparisonTests.cs`
**Tests output equivalence**:
- `GetTopArtists_ProducesSameNames()` - Verify artist names match original
- `GetTopTracks_ProducesSameNamesAndArtists()` - Verify track data matches
- `GetTopAlbums_ProducesSameNamesAndArtists()` - Verify album data matches

#### `PerformanceComparisonTests.cs` (Secondary priority)
**Tests performance**:
- `Transformation_ExecutionTime()` - Measure LINQ transformation overhead
- `Memory_CompactVsFull()` - Compare memory footprint

### 5. Test Strategy

**For each test**:
1. Load fixture data via `MockLastFmApiClient`
2. Call original API path → get full models (Artist, Track, Album)
3. Call MCP transformation path → get compact models (CompactArtist, CompactTrack, CompactAlbum)
4. Assert:
   - Essential data preserved (Name, PlayCount, Rank)
   - Flattening correct (Track.Artist.Name → artist string)
   - Token optimization applied (Url/Mbid excluded)

## Implementation Steps

### Phase 1: Setup (20 min)
1. Create `Lfm.Tests` project
2. Add to `Lfm.sln`
3. Add project references (Lfm.Core, Lfm.McpServer)
4. Add xUnit packages

### Phase 2: Test Data (15 min)
1. Create `test-data/lastfm-responses/` directory
2. Create realistic JSON fixtures based on Last.fm API structure
3. Include 3-5 artists/tracks/albums per fixture

### Phase 3: Mock Client (30 min)
1. Create `MockLastFmApiClient : ILastFmApiClient`
2. Implement `GetTopArtistsWithResultAsync()` - load from fixture
3. Implement `GetTopTracksWithResultAsync()` - load from fixture
4. Implement `GetTopAlbumsWithResultAsync()` - load from fixture

### Phase 4: Accuracy Tests (45 min)
1. `TransformationAccuracyTests.cs` - 4 tests verifying transformation correctness
2. `OutputComparisonTests.cs` - 3 tests comparing original vs compact output

### Phase 5: Performance Tests (30 min, optional)
1. `PerformanceComparisonTests.cs` - Execution time and memory comparison

## Key Files to Create

```
src/Lfm.Tests/
├── Lfm.Tests.csproj
├── Mocks/
│   └── MockLastFmApiClient.cs
├── TransformationAccuracyTests.cs
├── OutputComparisonTests.cs
└── PerformanceComparisonTests.cs (optional)

test-data/
└── lastfm-responses/
    ├── top-artists-response.json
    ├── top-tracks-response.json
    └── top-albums-response.json
```

## Success Criteria

### Accuracy (Primary):
- ✅ All essential data preserved (Name, PlayCount, Rank)
- ✅ Property flattening works correctly (Artist.Name → artist)
- ✅ Token optimization excludes Url/Mbid
- ✅ No data loss between original and compact models

### Performance (Secondary):
- ✅ Transformation overhead measured
- ✅ Memory comparison documented

## What This Proves

1. **Transformation correctness** - Compact models contain all essential data
2. **No information loss** - Flattening doesn't lose artist/album information
3. **Token optimization works** - Url/Mbid excluded as designed
4. **Performance impact** - Quantify LINQ transformation cost

This will validate or refute the "~50% token reduction" claim with actual data.

---

## Implementation Status

**Status**: ✅ COMPLETE (2025-01-26)

### Actual Results

**Test Execution**: ALL 15 TESTS PASSED ✅

**Token Reduction** (measured, not theoretical):
- **Top Artists: 75.0%** reduction (943 → 236 chars)
- **Top Tracks: 75.4%** reduction (890 → 219 chars)
- **Top Albums: 66.9%** reduction (468 → 155 chars)

**Conclusion**: The ~50% claim was **CONSERVATIVE** - actual reduction is **66.9% - 75.4%**!

### Files Created

**Test Infrastructure**:
- `src/Lfm.Tests/Lfm.Tests.csproj` - xUnit test project
- `src/Lfm.Tests/Mocks/MockLastFmApiClient.cs` (157 lines)
- `src/Lfm.Tests/TransformationAccuracyTests.cs` (9 tests, 207 lines)
- `src/Lfm.Tests/OutputComparisonTests.cs` (6 tests, 198 lines)

**Test Data**:
- `test-data/lastfm-responses/top-artists-response.json`
- `test-data/lastfm-responses/top-tracks-response.json`
- `test-data/lastfm-responses/top-albums-response.json`

**Documentation**:
- `TEST-RESULTS.md` - Complete test documentation with measurements

### Validation Summary

✅ **Transformation Accuracy**: 100% (15/15 tests passed)
✅ **Token Reduction**: 66.9% - 75.4% (exceeded 50% claim)
✅ **Data Preservation**: 100% (all essential fields intact)
✅ **Type Safety**: Verified via reflection
✅ **No Information Loss**: Element-by-element comparison passed

**Reference**: See `TEST-RESULTS.md` for complete measurements and analysis.
