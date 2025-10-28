# Test Fixtures for Local File Parsing

This directory contains sample data files for testing LocalFileDataProvider implementations.

## Spotify Test Fixtures

### 1. Streaming_History_Audio_2024_0.json (Extended Format)
**Format**: Spotify Extended Streaming History
**Source**: Spotify Privacy Settings → "Extended streaming history"
**Content**: 5 play events including:
- 3 full plays (Pink Floyd tracks with albums)
- 1 skipped track (< 30 seconds)
- 1 podcast episode (should be filtered out by `IsMusicPlayback`)

**Key Test Scenarios**:
- ✅ Full play detection (ms_played >= 30000)
- ✅ Skipped track filtering (skipped: true)
- ✅ Podcast/audiobook filtering (spotify:episode: URI)
- ✅ Album metadata present
- ✅ UTC timestamp parsing

**Expected Results**:
- Artist play counts: Pink Floyd = 3 plays (2 full + 1 skipped, but skipped filtered out = 2)
- Album play counts: The Wall = 1, Wish You Were Here = 1, Dark Side = 1
- Track play counts: Comfortably Numb = 1, Wish You Were Here = 1, Money = 1

### 2. StreamingHistory0.json (Standard Format)
**Format**: Spotify Standard Streaming History
**Source**: Spotify Account Privacy → "Download your data"
**Content**: 5 play events from The Beatles and Led Zeppelin

**Key Test Scenarios**:
- ✅ Standard format detection (no ms_played field)
- ✅ DateTime parsing from "YYYY-MM-DD HH:MM" format
- ✅ No album metadata (Standard format doesn't include albums)
- ✅ All tracks treated as full plays

**Expected Results**:
- Artist play counts: The Beatles = 3, Led Zeppelin = 2
- Track play counts: Hey Jude = 1, Let It Be = 1, Come Together = 1, Stairway = 1, Kashmir = 1

## YouTube Music Test Fixtures

### 3. watch-history.json
**Format**: YouTube Music Watch History
**Source**: Google Takeout → YouTube and YouTube Music
**Content**: 5 entries including:
- 4 YouTube Music plays (Queen tracks)
- 1 regular YouTube video (should be filtered out)

**Key Test Scenarios**:
- ✅ YouTube Music detection (products: ["YouTube Music"])
- ✅ Regular YouTube video filtering
- ✅ Artist extraction from subtitles
- ✅ No album metadata (watch history doesn't include albums)

**Expected Results**:
- Artist play counts: Queen = 4
- Track play counts: Bohemian Rhapsody = 1, We Will Rock You = 1, Killer Queen = 1, Somebody To Love = 1

### 4. music-library-songs.csv
**Format**: YouTube Music Library CSV
**Source**: Google Takeout → YouTube Music library
**Content**: 10 songs with play counts, ratings, and metadata

**Key Test Scenarios**:
- ✅ CSV parsing with quoted fields
- ✅ Play count extraction
- ✅ Removed song filtering (Removed = "Yes")
- ✅ Album metadata present
- ✅ Rating information (not used but should parse)

**Expected Results**:
- Artist play counts: The Beatles = 80 (42+38), Pink Floyd = 85 (45+40), Queen = 50, John Lennon = 25, etc.
- Album play counts: The Wall = 45, Wish You Were Here = 40, Imagine = 25, etc.
- Songs with Removed="Yes" should be filtered out (1 song)

## Testing Usage

### Manual Testing
```bash
# Test Spotify Extended History
dotnet run --project src/Lfm.Cli -- artists test-data/local-files/Streaming_History_Audio_2024_0.json

# Test Spotify Standard History
dotnet run --project src/Lfm.Cli -- artists test-data/local-files/StreamingHistory0.json

# Test YouTube Music Watch History
dotnet run --project src/Lfm.Cli -- artists test-data/local-files/watch-history.json

# Test YouTube Music Library CSV
dotnet run --project src/Lfm.Cli -- artists test-data/local-files/music-library-songs.csv
```

### Integration Tests
These fixtures are designed to be used with LocalFileDataProvider integration tests:
- Verify parser format detection (CanParse)
- Verify data aggregation accuracy
- Verify filtering logic (skipped tracks, podcasts, removed songs)
- Verify date range filtering

## File Format Notes

### Spotify Extended vs Standard
- **Extended**: More fields, better for testing edge cases (skipped, podcasts)
- **Standard**: Simpler format, tests basic parsing

### YouTube Music JSON vs CSV
- **JSON (watch-history)**: Timestamped events, real-time data
- **CSV (library)**: Play count totals, no timestamps

Both formats are complementary - watch history for recent activity, library for cumulative stats.
