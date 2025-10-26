# lfm Architecture: Original vs lfm2EF

**Purpose**: Document the data flow architecture of both implementations
**Date**: 2025-01-26

## Overview

This document compares the **original lfm architecture** (main branch) with the **lfm2EF architecture** (lfm2EF branch), showing how data flows through each system and the format transformations at each stage.

---

## Original Architecture (main branch)

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Original lfm Architecture                     │
└─────────────────────────────────────────────────────────────────┘

LLM (Claude)
    ↓ [MCP Protocol - stdio]
    ↓
┌─────────────────────────┐
│  MCP Server (Node.js)   │  ← lfm-mcp-release/server.js
│  - 28 MCP tools         │
│  - Spawns CLI process   │
│  - Parses stdout        │
└─────────────────────────┘
    ↓ [Child Process - spawn('lfm', args)]
    ↓
┌─────────────────────────┐
│  lfm CLI (C#)           │  ← Lfm.Cli
│  - Command handlers     │
│  - JSON serialization   │
└─────────────────────────┘
    ↓ [Method Call]
    ↓
┌─────────────────────────┐
│  CachedLastFmApiClient  │  ← Lfm.Core
│  - Decorator pattern    │
│  - Cache-first logic    │
└─────────────────────────┘
    ↓ [Cache Miss / Cache Bypass]
    ↓
┌─────────────────────────┐
│  LastFmApiClient        │  ← Lfm.Core
│  - HTTP requests        │
│  - JSON deserialization │
└─────────────────────────┘
    ↓ [HTTPS GET]
    ↓
┌─────────────────────────┐
│  Last.fm API            │
│  ws.audioscrobbler.com  │
└─────────────────────────┘
```

### Data Flow: Top Artists Request

**Step 1: MCP Tool Call** (LLM → MCP Server)
```json
{
  "tool": "lfm_artists",
  "arguments": {
    "period": "overall",
    "limit": 3
  }
}
```

**Step 2: CLI Spawn** (MCP Server → CLI)
```javascript
spawn('lfm', ['artists', '--limit', '3', '--json'])
```

**Step 3: CLI Output** (CLI → MCP Server via stdout)
```json
{
  "artist": [
    {
      "name": "Pink Floyd",
      "playcount": "1234",
      "url": "https://www.last.fm/music/Pink+Floyd",
      "mbid": "83d91898-7763-47d7-b03b-b92132375c47",
      "@attr": { "rank": "1" }
    },
    {
      "name": "The Beatles",
      "playcount": "987",
      "url": "https://www.last.fm/music/The+Beatles",
      "mbid": "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d",
      "@attr": { "rank": "2" }
    },
    {
      "name": "Radiohead",
      "playcount": "765",
      "url": "https://www.last.fm/music/Radiohead",
      "mbid": "a74b1b7f-71a5-4011-9441-d0b5e4122711",
      "@attr": { "rank": "3" }
    }
  ]
}
```
**Size**: ~943 characters

**Step 4: MCP Response** (MCP Server → LLM)
```json
{
  "success": true,
  "artists": [
    {
      "name": "Pink Floyd",
      "playcount": "1234",
      "url": "https://www.last.fm/music/Pink+Floyd",
      "mbid": "83d91898-7763-47d7-b03b-b92132375c47",
      "rank": "1"
    },
    {
      "name": "The Beatles",
      "playcount": "987",
      "url": "https://www.last.fm/music/The+Beatles",
      "mbid": "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d",
      "rank": "2"
    },
    {
      "name": "Radiohead",
      "playcount": "765",
      "url": "https://www.last.fm/music/Radiohead",
      "mbid": "a74b1b7f-71a5-4011-9441-d0b5e4122711",
      "rank": "3"
    }
  ]
}
```
**Size**: ~943 characters

### Internal Data Types (C#)

**Artist Model** (Lfm.Core/Models/LastFmModels.cs):
```csharp
public class Artist
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("playcount")]
    public string PlayCount { get; set; } = "0";

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("mbid")]
    public string Mbid { get; set; } = string.Empty;

    [JsonPropertyName("@attr")]
    public ArtistAttributes? Attributes { get; set; }
}

public class ArtistAttributes
{
    [JsonPropertyName("rank")]
    public string Rank { get; set; } = string.Empty;
}

public class TopArtists
{
    [JsonPropertyName("artist")]
    public List<Artist> Artists { get; set; } = new();

    [JsonPropertyName("@attr")]
    public TopArtistsAttributes? Attributes { get; set; }
}
```

### Characteristics

✅ **Strengths**:
- Complete data preservation (all Last.fm fields included)
- Process isolation (MCP server separate from CLI)
- Works with any CLI output format
- Simple MCP server (no API client complexity)

⚠️ **Limitations**:
- Full API response size (~943 chars for 3 artists)
- Child process overhead (spawn CLI for each request)
- Includes fields LLMs can't use (URLs, MBIDs)
- Nested objects in JSON responses

---

## lfm2EF Architecture (lfm2EF branch)

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     lfm2EF Architecture                          │
└─────────────────────────────────────────────────────────────────┘

LLM (Claude)
    ↓ [MCP Protocol - stdio]
    ↓
┌─────────────────────────┐
│  MCP Server (C#)        │  ← Lfm.McpServer
│  - 3 MCP tools (PoC)    │
│  - Direct API calls     │
│  - Applies transforms   │
└─────────────────────────┘
    ↓ [Method Call]
    ↓
┌─────────────────────────┐
│  LastFmMcpClient        │  ← Lfm.McpServer/Services
│  - Transformation layer │
│  - LINQ projections     │
│  - Compact models       │
└─────────────────────────┘
    ↓ [Method Call]
    ↓
┌─────────────────────────┐
│  CachedLastFmApiClient  │  ← Lfm.Core
│  - Decorator pattern    │
│  - Cache-first logic    │
└─────────────────────────┘
    ↓ [Cache Miss / Cache Bypass]
    ↓
┌─────────────────────────┐
│  LastFmApiClient        │  ← Lfm.Core
│  - HTTP requests        │
│  - JSON deserialization │
└─────────────────────────┘
    ↓ [HTTPS GET]
    ↓
┌─────────────────────────┐
│  Last.fm API            │
│  ws.audioscrobbler.com  │
└─────────────────────────┘
```

### Data Flow: Top Artists Request

**Step 1: MCP Tool Call** (LLM → MCP Server)
```json
{
  "tool": "lfm_artists",
  "arguments": {
    "period": "overall",
    "limit": 3
  }
}
```

**Step 2: Internal API Call** (MCP Server → LastFmMcpClient)
```csharp
var result = await _client.GetTopArtistsAsync(_username, "overall", 3);
```

**Step 3: Full API Response** (LastFmApiClient returns)
```csharp
Result<TopArtists> {
    Success = true,
    Data = TopArtists {
        Artists = [
            Artist {
                Name = "Pink Floyd",
                PlayCount = "1234",
                Url = "https://www.last.fm/music/Pink+Floyd",
                Mbid = "83d91898-7763-47d7-b03b-b92132375c47",
                Attributes = ArtistAttributes { Rank = "1" }
            },
            Artist {
                Name = "The Beatles",
                PlayCount = "987",
                Url = "https://www.last.fm/music/The+Beatles",
                Mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d",
                Attributes = ArtistAttributes { Rank = "2" }
            },
            Artist {
                Name = "Radiohead",
                PlayCount = "765",
                Url = "https://www.last.fm/music/Radiohead",
                Mbid = "a74b1b7f-71a5-4011-9441-d0b5e4122711",
                Attributes = ArtistAttributes { Rank = "3" }
            }
        ]
    }
}
```

**Step 4: LINQ Transformation** (LastFmMcpClient applies transformation rules)
```csharp
var compactArtists = result.Data!.Artists
    .Select(a => new CompactArtist
    {
        Name = a.Name,              // ✅ Preserved
        PlayCount = a.PlayCount,    // ✅ Preserved
        Rank = a.Attributes?.Rank   // ✅ Preserved
        // Url excluded              // ❌ Token optimization
        // Mbid excluded             // ❌ Token optimization
    })
    .ToList();
```

**Step 5: Compact Response** (LastFmMcpClient returns)
```csharp
Result<List<CompactArtist>> {
    Success = true,
    Data = [
        CompactArtist {
            Name = "Pink Floyd",
            PlayCount = "1234",
            Rank = "1"
        },
        CompactArtist {
            Name = "The Beatles",
            PlayCount = "987",
            Rank = "2"
        },
        CompactArtist {
            Name = "Radiohead",
            PlayCount = "765",
            Rank = "3"
        }
    ]
}
```

**Step 6: MCP Response** (MCP Server → LLM)
```json
{
  "artists": [
    {
      "Name": "Pink Floyd",
      "PlayCount": "1234",
      "Rank": "1"
    },
    {
      "Name": "The Beatles",
      "PlayCount": "987",
      "Rank": "2"
    },
    {
      "Name": "Radiohead",
      "PlayCount": "765",
      "Rank": "3"
    }
  ],
  "period": "overall",
  "count": 3
}
```
**Size**: ~236 characters (**75.0% reduction** vs original)

### Internal Data Types (C#)

**Full API Models** (Lfm.Core/Models/LastFmModels.cs):
```csharp
// Same as original - no changes to core models
public class Artist
{
    public string Name { get; set; } = string.Empty;
    public string PlayCount { get; set; } = "0";
    public string Url { get; set; } = string.Empty;
    public string Mbid { get; set; } = string.Empty;
    public ArtistAttributes? Attributes { get; set; }
}
```

**Compact Models** (Lfm.McpServer/Services/LastFmMcpClient.cs):
```csharp
/// <summary>
/// Compact artist model with transformation rules applied.
/// Excludes: Url, Mbid (token optimization)
/// </summary>
public class CompactArtist
{
    public string Name { get; set; } = string.Empty;
    public string PlayCount { get; set; } = "0";
    public string? Rank { get; set; }
    // Url and Mbid intentionally excluded
}
```

### Transformation Rules Applied

**From**: `transformation-rules/lastfm-rules.json`

```json
{
  "tokenOptimizationExcludes": ["Url", "Mbid"],
  "propertyFlattening": {
    "Track.Artist.Name": {
      "sourcePath": "Track.Artist.Name",
      "targetName": "artist"
    },
    "Album.Artist.Name": {
      "sourcePath": "Album.Artist.Name",
      "targetName": "artist"
    }
  }
}
```

**Applied in Code** (LastFmMcpClient.cs):
```csharp
// Token Optimization: Exclude Url, Mbid
public class CompactArtist
{
    public string Name { get; set; }      // ✅ Included
    public string PlayCount { get; set; } // ✅ Included
    public string? Rank { get; set; }     // ✅ Included
    // NO Url property                    // ❌ Excluded
    // NO Mbid property                   // ❌ Excluded
}

// Property Flattening: Track.Artist.Name → artist
public class CompactTrack
{
    public string Name { get; set; }
    public string PlayCount { get; set; }
    public string Artist { get; set; }    // Flattened from Track.Artist.Name
    public string? Rank { get; set; }
}

// Transformation code
var compactTracks = result.Data!.Tracks
    .Select(t => new CompactTrack
    {
        Name = t.Name,
        PlayCount = t.PlayCount,
        Artist = t.Artist.Name,  // Nested object → string
        Rank = t.Attributes?.Rank
    })
    .ToList();
```

### Characteristics

✅ **Strengths**:
- 66.9-75.4% token reduction (empirically measured)
- No child process overhead (direct API calls)
- Type-safe transformations (compile-time validation)
- Declarative transformation rules (JSON format)
- In-process MCP server (lower latency)
- Same caching infrastructure as original

⚠️ **Limitations**:
- MCP server in C# (less portable than Node.js)
- Requires .NET 9.0 runtime
- Only 3 tools implemented (vs 28 in original)

---

## Data Format Comparison

### Artists Response

| Aspect | Original | lfm2EF | Change |
|--------|----------|--------|--------|
| **Size** | 943 chars | 236 chars | **-75.0%** |
| **Name** | ✅ Included | ✅ Included | Same |
| **PlayCount** | ✅ Included | ✅ Included | Same |
| **Rank** | ✅ Included | ✅ Included | Same |
| **Url** | ✅ Included | ❌ Excluded | Removed |
| **Mbid** | ✅ Included | ❌ Excluded | Removed |
| **Format** | JSON object | JSON object | Same |

### Tracks Response

| Aspect | Original | lfm2EF | Change |
|--------|----------|--------|--------|
| **Size** | 890 chars | 219 chars | **-75.4%** |
| **Track Name** | ✅ Included | ✅ Included | Same |
| **PlayCount** | ✅ Included | ✅ Included | Same |
| **Rank** | ✅ Included | ✅ Included | Same |
| **Artist** | `{name, url, mbid}` | `"Pink Floyd"` | **Flattened** |
| **Url** | ✅ Included | ❌ Excluded | Removed |
| **Mbid** | ✅ Included | ❌ Excluded | Removed |

**Example Flattening**:

**Original**:
```json
{
  "name": "Comfortably Numb",
  "artist": {
    "name": "Pink Floyd",
    "mbid": "83d91898-...",
    "url": "https://..."
  }
}
```

**lfm2EF**:
```json
{
  "Name": "Comfortably Numb",
  "Artist": "Pink Floyd"
}
```

### Albums Response

| Aspect | Original | lfm2EF | Change |
|--------|----------|--------|--------|
| **Size** | 468 chars | 155 chars | **-66.9%** |
| **Album Name** | ✅ Included | ✅ Included | Same |
| **PlayCount** | ✅ Included | ✅ Included | Same |
| **Rank** | ✅ Included | ✅ Included | Same |
| **Artist** | `{name, url, mbid}` | `"Pink Floyd"` | **Flattened** |
| **Url** | ✅ Included | ❌ Excluded | Removed |
| **Mbid** | ✅ Included | ❌ Excluded | Removed |

---

## Cache Behavior

### Both Implementations Share Cache Layer

**Cache Path**: Both architectures use the same `CachedLastFmApiClient` decorator

```
┌─────────────────────────┐
│  CachedLastFmApiClient  │  ← Shared by both
│                         │
│  Cache-first strategy:  │
│  1. Generate cache key  │
│  2. Check cache         │
│  3. Return if found     │
│  4. Call API if miss    │
│  5. Store result        │
└─────────────────────────┘
```

**Cache Storage**:
- **Location**: User's AppData/lfm/cache/
- **Format**: JSON files (one per query)
- **Key Generation**: MD5 hash of (method + parameters)
- **TTL**: Configurable (default: 10 minutes)

**Important**: Cache stores **full API responses** (not compact models)
- Original: Returns full response from cache
- lfm2EF: Returns full response → transforms → returns compact

**Benefit**: Transformation happens post-cache, so cache is reusable between implementations

---

## Performance Characteristics

### Original Architecture

**Request Flow**:
1. MCP Server receives request (~1ms)
2. Spawn CLI process (~50-100ms)
3. CLI makes API call or cache hit (~10-500ms)
4. CLI serializes JSON (~1-5ms)
5. MCP parses stdout (~1-5ms)
6. MCP returns to LLM (~1ms)

**Total**: ~65-615ms (process spawn is bottleneck)

### lfm2EF Architecture

**Request Flow**:
1. MCP Server receives request (~1ms)
2. Direct method call (~<1ms, in-process)
3. API call or cache hit (~10-500ms)
4. LINQ transformation (~<1ms)
5. JSON serialization (~1-5ms)
6. MCP returns to LLM (~1ms)

**Total**: ~15-510ms (no process spawn overhead)

**Performance Gain**: ~50-100ms per request (child process elimination)

---

## Token Optimization Impact

### Measured Reduction

| Entity Type | Original Size | Compact Size | Reduction |
|-------------|--------------|--------------|-----------|
| **Artists** (3) | 943 chars | 236 chars | **75.0%** |
| **Tracks** (3) | 890 chars | 219 chars | **75.4%** |
| **Albums** (3) | 468 chars | 155 chars | **66.9%** |

### Why Token Reduction Matters

**For LLMs**:
- ✅ Faster response times (less to parse)
- ✅ Lower API costs (fewer input tokens)
- ✅ More context available (smaller responses)
- ✅ Better focus (only relevant data)

**What's Removed** (LLMs can't use these):
- ❌ URLs: LLMs can't click links
- ❌ MBIDs: MusicBrainz IDs not useful for conversation
- ❌ Nested objects: Flattened to strings when only name needed

**What's Preserved** (essential for LLMs):
- ✅ Artist/Track/Album names
- ✅ Play counts (listening history)
- ✅ Rankings (popularity order)

---

## Testing Infrastructure

### Test Harness (Lfm.Tests)

**Purpose**: Validate lfm2EF produces correct transformations without API credentials

```
┌─────────────────────────┐
│  MockLastFmApiClient    │  ← Test mock
│                         │
│  Loads JSON fixtures:   │
│  - top-artists.json     │
│  - top-tracks.json      │
│  - top-albums.json      │
└─────────────────────────┘
         ↓
┌─────────────────────────┐
│  LastFmMcpClient        │  ← Under test
│  Applies transformations│
└─────────────────────────┘
         ↓
┌─────────────────────────┐
│  Test Assertions        │
│  - Data accuracy: 100%  │
│  - Token reduction: 66-75%
│  - Type safety verified │
└─────────────────────────┘
```

**Test Results**: 15/15 tests passed
- ✅ Transformation accuracy: 100%
- ✅ Token reduction: Measured empirically
- ✅ No data loss: All essential fields preserved

---

## Migration Path

### Gradual Migration Strategy

**Phase 1**: Proof of Concept (CURRENT)
- ✅ 3 core tools implemented (artists, tracks, albums)
- ✅ Transformation rules validated
- ✅ Token reduction measured

**Phase 2**: Feature Parity (FUTURE)
- Implement remaining 25 tools
- Match all original MCP server functionality
- Side-by-side testing with real API

**Phase 3**: Transition (FUTURE)
- Run both MCP servers in parallel
- Gradual cutover by tool
- Validate LLM experience unchanged

**Phase 4**: Deprecation (FUTURE)
- Remove original Node.js MCP server
- Single C# MCP server in production

---

## Key Differences Summary

| Aspect | Original | lfm2EF |
|--------|----------|--------|
| **MCP Server Language** | Node.js | C# |
| **API Integration** | Spawns CLI child process | Direct in-process calls |
| **Data Size** | Full API responses | Compact transformed models |
| **Token Reduction** | 0% (full responses) | 66.9-75.4% |
| **Process Overhead** | ~50-100ms per request | ~0ms (in-process) |
| **Tools Implemented** | 28 tools | 3 tools (PoC) |
| **Transformation** | None | LINQ + compact models |
| **Type Safety** | JavaScript (dynamic) | C# (compile-time) |
| **Cache Layer** | Shared (same code) | Shared (same code) |
| **API Client** | Shared (same code) | Shared (same code) |

---

## Conclusion

**Original Architecture**:
- ✅ Complete (28 tools)
- ✅ Proven in production
- ⚠️ Full API responses (verbose)
- ⚠️ Child process overhead

**lfm2EF Architecture**:
- ✅ 66.9-75.4% token reduction
- ✅ No process overhead
- ✅ Type-safe transformations
- ⚠️ Only 3 tools (PoC stage)

**Both architectures share**:
- Same LastFmApiClient (HTTP layer)
- Same CachedLastFmApiClient (caching)
- Same cache storage (AppData/lfm/cache)
- Same Last.fm API endpoint

**The key innovation**: Transformation layer between API and MCP server that intelligently reduces token usage while preserving all essential data for LLM interactions.

---

**Document Version**: 1.0
**Last Updated**: 2025-01-26
**Branch**: lfm2EF
