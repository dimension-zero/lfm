# Local File Data Sources

The lfm CLI tool supports reading music listening history from local export files as an alternative to the Last.fm API. This provides instant query results and can supplement your Last.fm data with additional sources.

## Overview

**Supported Sources:**
- Spotify Streaming History (Extended and Standard formats)
- YouTube Music Watch History (Google Takeout)
- YouTube Music Library (Google Takeout) *[Parser pending implementation]*

**Benefits:**
- **Instant queries**: No API rate limiting or throttling
- **Complete data**: Access your full listening history without Last.fm's scrobbling limitations
- **Multiple sources**: Combine Spotify, YouTube Music, and Last.fm data
- **Privacy**: All processing happens locally on your machine

**Limitations:**
- No similar artist recommendations (requires Last.fm API)
- No artist tags (requires Last.fm API)
- Album metadata may require enrichment for some formats
- Historical data only (no real-time scrobbling)

## How to Use

### Basic Usage

Once you have your export files, you can query them directly by passing the file path as the "username" parameter:

```bash
# Query artists from a Spotify export
lfm artists /path/to/StreamingHistory0.json --limit 20

# Query tracks with date range
lfm tracks /path/to/watch-history.json --from 2024-01-01 --to 2024-12-31

# Query albums
lfm albums /path/to/Streaming_History_Audio_2024_0.json --limit 10
```

### Provider Configuration (Phase 5 - Pending)

In a future update, you'll be able to configure the default data provider:

```bash
# Set local files as default provider
lfm config set-provider LocalFiles
lfm config set-local-file-paths /path/to/file1.json,/path/to/file2.json

# Switch back to Last.fm API
lfm config set-provider LastFm

# Use merged provider (both sources)
lfm config set-provider Merged
```

## Spotify Streaming History

Spotify provides two formats for downloading your streaming history. Both formats are fully supported.

### Extended Streaming History (Recommended)

**How to Download:**
1. Go to [Spotify Privacy Settings](https://www.spotify.com/account/privacy/)
2. Scroll to "Download your data"
3. Select "Extended streaming history" (⚠️ Can take up to 30 days to receive)
4. You'll receive a .zip file via email containing multiple JSON files

**File Format:**
- Files named like: `Streaming_History_Audio_2024_0.json`
- Contains detailed metadata: albums, play duration, skip information, podcast detection
- UTC timestamps
- More accurate filtering of non-music content

**Example Data:**
```json
{
  "ts": "2024-01-15T14:30:00Z",
  "username": "your-username",
  "platform": "Android",
  "ms_played": 245000,
  "conn_country": "US",
  "ip_addr_decrypted": "xxx.xxx.xxx.xxx",
  "user_agent_decrypted": "unknown",
  "master_metadata_track_name": "Comfortably Numb",
  "master_metadata_album_artist_name": "Pink Floyd",
  "master_metadata_album_album_name": "The Wall",
  "spotify_track_uri": "spotify:track:...",
  "episode_name": null,
  "episode_show_name": null,
  "spotify_episode_uri": null,
  "reason_start": "fwdbtn",
  "reason_end": "fwdbtn",
  "shuffle": false,
  "skipped": false,
  "offline": false,
  "offline_timestamp": 0,
  "incognito_mode": false
}
```

**Filtering Applied:**
- ✅ Skipped tracks (< 30 seconds or `skipped: true`) are excluded
- ✅ Podcasts and audiobooks (`spotify:episode:` URI) are excluded
- ✅ Only tracks with `ms_played >= 30000` (30 seconds) are counted

### Standard Streaming History (Quicker Alternative)

**How to Download:**
1. Go to [Spotify Account Privacy](https://www.spotify.com/account/privacy/)
2. Request "Account data" (⚠️ Available in 5-15 days)
3. You'll receive a .zip file containing `StreamingHistory0.json` (and possibly more)

**File Format:**
- Simpler format with basic metadata
- No album information (requires enrichment)
- Local datetime format (not UTC)
- All plays counted equally (no skip detection)

**Example Data:**
```json
{
  "endTime": "2024-01-15 14:30",
  "artistName": "Pink Floyd",
  "trackName": "Comfortably Numb",
  "msPlayed": 245000
}
```

**Limitations:**
- No album metadata (LocalFileDataProvider returns empty album list)
- No skip detection (all plays counted)
- Less accurate podcast filtering

## YouTube Music

YouTube Music data is available via Google Takeout. Two formats are supported.

### Watch History (Timestamped Events)

**How to Download:**
1. Go to [Google Takeout](https://takeout.google.com/)
2. Deselect all products, then select only "YouTube and YouTube Music"
3. Click "All YouTube data included" and deselect all except:
   - ✅ history (watch-history.json)
4. Choose file format (JSON recommended) and delivery method
5. Export can take hours to days depending on data size

**File Format:**
- File named: `watch-history.json`
- Contains timestamped play events with product metadata
- Artist name extracted from "subtitles" field

**Example Data:**
```json
{
  "header": "YouTube Music",
  "title": "Bohemian Rhapsody",
  "titleUrl": "https://www.youtube.com/watch?v=fJ9rUzIMcZQ",
  "time": "2024-01-15T14:30:00.000Z",
  "products": ["YouTube Music"],
  "activityControls": ["YouTube watch history"],
  "subtitles": [
    {
      "name": "Queen",
      "url": "https://www.youtube.com/channel/..."
    }
  ]
}
```

**Filtering Applied:**
- ✅ Only entries with `"products": ["YouTube Music"]` are included
- ✅ Regular YouTube videos are excluded
- ✅ Artist name extracted from first subtitle

**Limitations:**
- No album metadata (watch history doesn't include albums)
- No play duration information

### Music Library (Play Count Totals)

**Status**: ⚠️ Parser implementation pending (tests are skipped)

**How to Download:**
1. Same Google Takeout process as Watch History
2. Select "YouTube and YouTube Music" → "music-library-songs"
3. Export as CSV format

**File Format:**
- File named: `music-library-songs.csv`
- Contains cumulative play counts (not individual events)
- Includes album metadata and ratings

**Example Data:**
```csv
Title,Album,Artist,Duration,Rating,Play Count,Removed
Imagine,Imagine,John Lennon,3:03,5,25,No
Let It Be,Let It Be,The Beatles,4:03,5,42,No
```

**Filtering Applied (When Implemented):**
- ✅ Songs with `Removed="Yes"` are excluded

**Use Case:**
- Complements watch history with cumulative statistics
- Useful for library-wide analytics

## Data Accuracy & Filtering

### What Gets Counted

**Spotify Extended History:**
- Plays >= 30 seconds
- Not marked as skipped
- Not podcasts or audiobooks
- Music tracks only

**Spotify Standard History:**
- All plays in the file (less filtering available)

**YouTube Music Watch History:**
- Entries marked as "YouTube Music" product
- Regular YouTube videos excluded

### What Gets Filtered Out

**Automatically Excluded:**
- Skipped tracks (Spotify Extended only)
- Podcasts and audiobooks
- Non-music content
- Removed songs (YouTube Library CSV)
- Regular YouTube videos (not YouTube Music)

### Album Enrichment (Optional)

Some formats don't include album metadata. You can optionally enable album enrichment to fetch this data from external sources:

**Enrichment Sources:**
- Last.fm API
- Spotify API
- MusicBrainz API
- YouTube Data API

**Configuration:**
```bash
# Enable album enrichment (future feature)
lfm config set-album-enrichment enabled
lfm config set-enrichment-mode BestEffort  # Optional, BestEffort, Mandatory
```

**Note**: Enrichment is disabled by default for performance. For formats with album data (Spotify Extended, YouTube Library CSV), enrichment is unnecessary.

## Date Range Queries

All local file parsers support date range filtering:

```bash
# Query specific date range
lfm artists /path/to/file.json --from 2024-01-01 --to 2024-12-31

# Last 7 days (if file contains recent data)
lfm tracks /path/to/file.json --period 7day

# Overall (all data in file)
lfm albums /path/to/file.json --period overall
```

Date filtering is applied **after** parsing, so the entire file is still read into memory and cached for subsequent queries.

## Performance Considerations

**First Query:**
- File is parsed completely
- Results are cached in memory
- Typical parse time: < 1 second for files up to 10MB

**Subsequent Queries:**
- Use cached data (instant results)
- Date range filtering applied to cached events
- Different limits or pages use same cache

**Cache Lifetime:**
- Cached until provider is disposed
- Each file path maintains separate cache
- Re-querying same file uses cache

**Memory Usage:**
- Entire file loaded into memory for caching
- Typical memory: ~2-3x file size
- Large files (> 100MB) may require more memory

## Combining Multiple Sources (Phase 6 - Pending)

In a future update, you'll be able to merge data from multiple sources:

**Merged Provider:**
- Combines Last.fm API + local files
- Local files are source of truth (more accurate timestamps)
- API fills gaps for artists/tracks not in local files
- Play counts are summed across all sources

**Example Use Case:**
- Your Spotify export has 50,000 plays
- Your Last.fm account has 30,000 scrobbles (some overlap, some unique)
- Merged provider combines both for complete 70,000+ play history
- Similar artists and tags still available from Last.fm API

## Troubleshooting

### Parser Errors

**"No parser found for file"**
- Check file format matches supported types
- Ensure file extension is correct (.json for JSON, .csv for CSV)
- Verify file is not corrupted

**"Parse failed" / Invalid JSON**
- File may be incomplete (download interrupted)
- File may be corrupted
- Try re-downloading the export

### Empty Results

**No artists/tracks returned:**
- Check date range (may be filtering all events)
- Verify file contains the expected format
- Check for filtering (skipped tracks, podcasts, removed songs)

**Play counts seem low:**
- Spotify Extended filters skipped tracks (< 30 seconds)
- YouTube Music only counts YouTube Music plays (not regular YouTube)
- Date range may be excluding events

### Memory Issues

**Out of memory with large files:**
- Process files in smaller chunks (split by year)
- Use date range queries to reduce in-memory data
- Increase available memory or use 64-bit runtime

## File Organization Recommendations

**Suggested Directory Structure:**
```
~/music-data/
├── spotify/
│   ├── Streaming_History_Audio_2024_0.json
│   ├── Streaming_History_Audio_2024_1.json
│   └── StreamingHistory0.json
├── youtube/
│   ├── watch-history.json
│   └── music-library-songs.csv
└── README.txt  (notes about when data was exported)
```

**Tips:**
- Keep files organized by source and year
- Document export dates in README
- Back up original exports before any processing
- Consider version control for tracking exports over time

## Future Enhancements

**Planned Features (Not Yet Implemented):**
- YouTube Music Library CSV parser (Phase 4 remainder)
- Configuration-based provider switching (Phase 5)
- Merged provider combining API + local files (Phase 6)
- Album enrichment configuration
- Multi-file aggregation (query multiple files at once)
- Incremental updates (merge new exports with existing data)

## Additional Resources

**Spotify Data Export:**
- [Spotify Privacy Settings](https://www.spotify.com/account/privacy/)
- [Understanding Your Spotify Data](https://support.spotify.com/us/article/understanding-my-data/)

**YouTube Music Data Export:**
- [Google Takeout](https://takeout.google.com/)
- [Download your YouTube data](https://support.google.com/youtube/answer/9315727)

**Last.fm Integration:**
- [Last.fm API Documentation](https://www.last.fm/api)
- [Last.fm Scrobbling](https://www.last.fm/about/trackmymusic)

---

**Last Updated**: 2025-10-27
**Version**: 1.0 (Phase 4 - LocalFileDataProvider implementation)
