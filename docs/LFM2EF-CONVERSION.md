# LFM → EF Architecture Conversion Plan

**Branch**: `lfm2EF`
**Purpose**: Dry-run proof-of-concept for API2EF2MCP architecture
**Goal**: Convert existing lfm from direct API consumption to API→EF→Denormalization→MCP pipeline

## Executive Summary

This conversion serves as a **proof-of-concept** for the larger API2EF2MCP project. By converting the working lfm implementation to use the EF-based architecture, we validate:

1. **Schema Discovery**: Can we discover Last.fm API schema?
2. **EF Model Generation**: Can we generate EF models from the schema?
3. **Transformation Rules**: Can we create rules that preserve lfm's current behavior?
4. **MCP Integration**: Can we generate the MCP server from transformation rules?
5. **Equivalence**: Does the new architecture produce the same results as current lfm?

**Success Criteria**: New architecture generates MCP server functionally equivalent to current `lfm-mcp-release/server.js` (28 tools, 50% token reduction)

## Current lfm Architecture

### Data Flow (Current)

```
Last.fm API
     ↓
  HTTP Request
     ↓
  JSON Response
     ↓
  Manual Deserialization (JsonDocument.Parse)
     ↓
  Strongly-Typed Models (Artist, Track, Album)
     ↓
  CachedLastFmApiClient (Decorator Pattern)
     ↓
  LastFmService (Business Logic)
     ↓
  DisplayService (CLI Formatting)
     ↓
  MCP Server (Node.js - server.js)
     ↓
  LLM Conversation
```

### Key Components

**1. Models** (`src/Lfm.Core/Models/LastFmModels.cs` - 800+ lines)
- Direct API response mapping with `[JsonPropertyName]` attributes
- Nested structures: `Track.Artist.Name`, `Album.Artist.Name`
- String-based numeric fields: `PlayCount`, `Rank`
- Metadata attributes: `@attr` properties

**2. API Client** (`src/Lfm.Core/Services/LastFmApiClient.cs` - 1,089 lines)
- Direct HTTP calls to Last.fm API
- Manual JSON parsing with `JsonDocument`
- Dictionary-based aggregation for date ranges
- Result<T> pattern for error handling

**3. Caching Layer** (`src/Lfm.Core/Services/CachedLastFmApiClient.cs` - 800+ lines)
- Decorator pattern wrapping ILastFmApiClient
- File-based cache storage (~/.cache/lfm/)
- 119x performance improvement

**4. Business Logic** (`src/Lfm.Core/Services/LastFmService.cs` - 700+ lines)
- Date range aggregation using `StringComparer.OrdinalIgnoreCase`
- Deep search with pagination
- Recommendation generation with filtering

**5. MCP Server** (`lfm-mcp-release/server.js` - 2,470 lines)
- 28 tools exposing Last.fm data to LLMs
- Token optimization (50% reduction via compact functions)
- Spawns lfm CLI as child process
- Position-based JSON parsing for mixed output

### Current Transformation Patterns

**Already Implemented in lfm**:

1. **Token Optimization** (server.js lines 141-165):
   ```javascript
   function compactTrack(track) {
     return {
       name: track.name,
       playcount: track.playcount,
       artist: track.artist?.name || track.artist,
       rank: track['@attr']?.rank
     };
   }
   // Strips: url, mbid, image (50% token reduction)
   ```

2. **Property Flattening** (implicit in MCP tools):
   - `track.artist.name` → `artist` (string in MCP output)
   - `album.artist.name` → `artist` (string in MCP output)

3. **Equivalence Handling** (LastFmApiClient.cs line 479):
   ```csharp
   var userPlayCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
   ```

4. **Navigation Inlining** (done manually in MCP tools):
   - Album includes artist name directly
   - Track includes both artist and album names

## Proposed EF Architecture

### Data Flow (New)

```
Last.fm API
     ↓
  [1] Schema Discovery (OpenAPI/heuristic)
     ↓
  [2] ApiModel Generation
     ↓
  [3] EF DbContext + Entity Classes
     ↓
  [4] Transformation Rules (JSON)
     ↓
  [5] Denormalization Layer
     ↓
  [6] Generated MCP Tools
     ↓
  [7] MCP Server (C#)
     ↓
  LLM Conversation
```

### New Components to Add

**[1] Schema Discovery**
- Last.fm doesn't provide OpenAPI spec
- Use **heuristic discovery** (similar to API2EF's HeuristicDiscoveryService)
- Infer schema from sample API responses
- Manual endpoint specification for known API methods

**[2] ApiModel Generation**
```csharp
ApiModel:
  - Entities: Artist, Track, Album, RecentTracks, etc.
  - Properties: name, playcount, url, mbid
  - Relationships: Track → Artist, Album → Artist
  - Endpoints: user.getTopArtists, user.getTopTracks, etc.
```

**[3] EF DbContext + Entity Classes**
- Generate from ApiModel (using API2EF patterns)
- Navigation properties for relationships
- Preserve string-based numeric fields (PlayCount, Rank)

**[4] Transformation Rules** (JSON file)
```json
{
  "apiName": "Last.fm",
  "mode": "Hybrid",
  "propertyFlattening": {
    "track.artist.name": {
      "sourcePath": "Track.Artist.Name",
      "targetName": "artistName",
      "includeInConversation": true
    },
    "album.artist.name": {
      "sourcePath": "Album.Artist.Name",
      "targetName": "artistName",
      "includeInConversation": true
    }
  },
  "tokenOptimizationExcludes": [
    "Url", "Mbid", "Image"
  ],
  "equivalenceGroups": {
    "Beatles": ["The Beatles", "Beatles, The"],
    "Mozart": ["W.A. Mozart", "Wolfgang Amadeus Mozart"]
  },
  "navigationInlining": {
    "Artist": {
      "navigationProperty": "Artist",
      "inlineProperties": ["Name"],
      "format": "{Name}"
    }
  }
}
```

**[5] Denormalization Layer**
- FlatteningService: Apply flattening rules
- TokenOptimizer: Strip URLs, MBIDs
- EquivalenceResolver: Case-insensitive, article removal
- NavigationInliner: Embed related entities

**[6] Generated MCP Tools** (C# instead of Node.js)
- Tools classes with static methods
- Apply transformation rules during generation
- Use ApiClientBase pattern from API2MCP
- Result<T> error handling

**[7] MCP Server** (C# instead of Node.js)
- Program.cs with MCP SDK setup
- Direct API calls (no child process spawning)
- Integrated caching (reuse CachedLastFmApiClient)

## Conversion Strategy

### Phase 1: Minimal Viable Conversion (Week 1)

**Goal**: Generate EF models and transformation rules from current lfm

**Steps**:
1. Create Last.fm schema discovery service
2. Run discovery against Last.fm API
3. Generate ApiModel
4. Generate EF DbContext + entities
5. Create transformation rules (manual, based on current lfm patterns)

**Validation**:
- [ ] EF models match current LastFmModels.cs structure
- [ ] Transformation rules capture current compact functions
- [ ] Rules JSON is readable and editable

### Phase 2: MCP Generation (Week 2)

**Goal**: Generate MCP server from transformation rules

**Steps**:
1. Copy MCP templates from API2MCP
2. Create McpCodeGenerator for Last.fm
3. Apply transformation rules during generation
4. Generate 3 core tools: artists, tracks, albums

**Validation**:
- [ ] Generated MCP server builds cleanly
- [ ] Tools return data in same format as current server.js
- [ ] Token optimization achieves ~50% reduction

### Phase 3: Feature Parity (Week 3)

**Goal**: Generate all 28 tools from current MCP server

**Steps**:
1. Map all 28 current tools to transformation rules
2. Generate complete MCP server
3. Add caching integration
4. Add playback integration (Spotify/Sonos)

**Validation**:
- [ ] All 28 tools function equivalently
- [ ] Caching works (119x improvement preserved)
- [ ] Playback commands work (play, queue, etc.)

### Phase 4: Validation & Documentation (Week 4)

**Goal**: Prove equivalence and document learnings

**Steps**:
1. Side-by-side testing: old MCP vs new MCP
2. Performance comparison
3. Document conversion process
4. Extract patterns for API2EF2MCP

**Validation**:
- [ ] New MCP produces identical output for same queries
- [ ] Performance is comparable or better
- [ ] Learnings documented in CONVERSION-LEARNINGS.md

## Architecture Mapping

### Current → New Mapping

| Current Component | New Component | Notes |
|------------------|---------------|-------|
| LastFmModels.cs | Generated EF Entities | Auto-generated from schema |
| LastFmApiClient.cs | ApiClientBase<ApiKeyAuth> | Template from API2MCP |
| CachedLastFmApiClient.cs | Integrated into ApiClient | Same decorator pattern |
| LastFmService.cs | Generated MCP Tools | Business logic in tools |
| DisplayService.cs | Not needed | MCP handles formatting |
| server.js (Node) | Program.cs (C#) | MCP SDK in C# |
| compactTrack() | TokenOptimizer | Transformation rule |
| case-insensitive dict | EquivalenceResolver | Transformation rule |

### Files to Create

```
src/
├── Lfm.Schema/                          # NEW PROJECT
│   ├── LastFmSchemaDiscovery.cs        # Heuristic discovery
│   └── LastFmApiModel.cs               # Discovered schema
├── Lfm.EfModels/                       # NEW PROJECT (Generated)
│   ├── LastFmContext.cs                # Generated DbContext
│   ├── Entities/                       # Generated entities
│   │   ├── Artist.cs
│   │   ├── Track.cs
│   │   ├── Album.cs
│   │   └── ...
│   └── Lfm.EfModels.csproj
├── Lfm.Transformation/                 # NEW PROJECT
│   ├── TransformationRules.cs          # Rules model
│   ├── FlatteningService.cs
│   ├── TokenOptimizer.cs
│   ├── EquivalenceResolver.cs
│   └── NavigationInliner.cs
├── Lfm.McpGenerated/                   # NEW PROJECT (Generated)
│   ├── Program.cs                      # Generated MCP server
│   ├── Tools/                          # Generated tools
│   │   ├── ArtistTools.cs
│   │   ├── TrackTools.cs
│   │   ├── AlbumTools.cs
│   │   └── ...
│   ├── Services/
│   │   ├── LastFmClient.cs            # Generated API client
│   │   └── ApiClientBase.cs           # From API2MCP
│   └── Lfm.McpGenerated.csproj
└── transformation-rules/               # NEW DIRECTORY
    └── lastfm-rules.json               # Transformation rules
```

### Files to Keep (Unchanged)

```
src/
├── Lfm.Core/
│   ├── Configuration/                   # Keep
│   ├── Models/Results/                  # Keep (Result<T>)
│   └── Services/Cache/                  # Keep (reuse in generated)
├── Lfm.Spotify/                        # Keep (reuse in generated)
└── Lfm.Sonos/                          # Keep (reuse in generated)
```

## Key Challenges & Solutions

### Challenge 1: Last.fm Has No OpenAPI Spec

**Solution**: Heuristic Discovery
- Define endpoints manually (we know them from current code)
- Sample API responses to infer schema
- Create ApiModel programmatically

**Example**:
```csharp
var lastFmModel = new ApiModel
{
    Name = "Last.fm",
    BaseUrl = "https://ws.audioscrobbler.com/2.0/",
    Entities = new[]
    {
        new ApiEntity
        {
            Name = "Artist",
            Properties = new[]
            {
                new ApiProperty { Name = "Name", Type = "string", IsRequired = true },
                new ApiProperty { Name = "PlayCount", Type = "string" },
                new ApiProperty { Name = "Url", Type = "string" },
                new ApiProperty { Name = "Mbid", Type = "string" }
            }
        },
        // ... more entities
    },
    Endpoints = new[]
    {
        new ApiEndpoint
        {
            Name = "GetTopArtists",
            HttpMethod = "GET",
            Path = "/",
            Parameters = new[] { "method", "user", "period", "limit", "page" },
            ResponseEntity = "TopArtists"
        },
        // ... more endpoints
    }
};
```

### Challenge 2: String-Based Numeric Fields

**Current**: `PlayCount` is `string` to preserve API fidelity and prevent overflow

**Solution**: Preserve in EF models
- Generate properties as `string` type
- Add custom type mapping in transformation rules
- Document rationale in generated code comments

### Challenge 3: `@attr` Metadata Properties

**Current**: `[JsonPropertyName("@attr")]` for Last.fm's metadata

**Solution**: Custom attribute handling
- Detect `@attr` pattern during discovery
- Generate with custom naming (e.g., `Attributes`)
- Preserve JSON attribute mapping

### Challenge 4: Date Range Aggregation Logic

**Current**: Custom dictionary-based aggregation in LastFmApiClient.cs

**Solution**: Generate as MCP tool logic
- Aggregation becomes part of generated tool
- Use same `StringComparer.OrdinalIgnoreCase` pattern
- Preserve pagination and throttling logic

### Challenge 5: MCP Server in C# vs Node.js

**Current**: Node.js server spawns lfm CLI as child process

**Solution**: Native C# MCP server
- Use MCP SDK for C# (from API2MCP)
- Direct API client integration (no child process)
- Integrated caching and error handling

## Success Metrics

### Functional Equivalence

- [ ] All 28 MCP tools generate same output format
- [ ] Token reduction: 45-55% (current: 50%)
- [ ] Cache performance: 100x+ (current: 119x)
- [ ] API throttling: 200ms default preserved
- [ ] Error handling: Result<T> pattern maintained

### Code Quality

- [ ] Clean build (0 errors, 0 warnings)
- [ ] Generated code is readable and maintainable
- [ ] Transformation rules are clear and editable
- [ ] Documentation explains conversion rationale

### Performance

- [ ] API calls: Same or fewer than current
- [ ] Cache hit rate: Equivalent to current
- [ ] MCP response time: Within 10% of current
- [ ] Memory usage: Comparable or better

### Developer Experience

- [ ] Transformation rules are easy to understand
- [ ] Regeneration is fast (<30 seconds)
- [ ] Debugging is straightforward
- [ ] Documentation is comprehensive

## Validation Test Cases

### Test Case 1: Basic Top Artists Query

**Current**:
```bash
./publish/win-x64/lfm.exe artists --limit 5
```

**New**:
```bash
# MCP tool call
lfm_artists(limit: 5)
```

**Validation**: Output format matches, token count within 5%

### Test Case 2: Date Range Aggregation

**Current**:
```bash
./publish/win-x64/lfm.exe tracks --from 2024-01-01 --to 2024-12-31 --limit 10
```

**New**:
```bash
# MCP tool call
lfm_tracks(year: "2024", limit: 10)
```

**Validation**: Same tracks in same order, same play counts

### Test Case 3: Deep Search

**Current**:
```bash
./publish/win-x64/lfm.exe artist-tracks "Pink Floyd" --deep
```

**New**:
```bash
# MCP tool call
lfm_artist_tracks(artist: "Pink Floyd", deep: true)
```

**Validation**: Same tracks discovered, performance within 20%

### Test Case 4: Cache Performance

**Current**: Cache hit = ~38ms, API call = ~4500ms (119x improvement)

**New**: Should achieve similar ratio

**Validation**: 100x+ cache improvement maintained

### Test Case 5: Token Optimization

**Current**: 100 albums = ~10,300 tokens (full) → ~5,150 tokens (compact) = 50% reduction

**New**: Generated compact functions should achieve 45-55% reduction

**Validation**: Token count comparison on same data

## Learnings to Extract

**For API2EF2MCP Project**:

1. **Schema Discovery Patterns**
   - How to handle APIs without OpenAPI specs
   - Heuristic endpoint discovery strategies
   - Sample response analysis techniques

2. **Transformation Rule Patterns**
   - How to capture existing transformation patterns
   - Rule definition best practices
   - Common flattening/optimization patterns

3. **EF Model Generation**
   - Handling unusual type patterns (string numbers)
   - Custom attribute mapping (`@attr`)
   - Navigation property inference

4. **MCP Generation**
   - Tool method templates
   - Transformation application strategies
   - Error handling in generated code

5. **Testing Strategies**
   - Equivalence validation approaches
   - Performance comparison techniques
   - Regression test design

## Timeline

### Week 1: Schema Discovery & EF Generation
- [ ] Day 1: Create LastFmSchemaDiscovery service
- [ ] Day 2: Generate ApiModel from Last.fm API
- [ ] Day 3: Generate EF DbContext + entities
- [ ] Day 4: Create transformation rules JSON
- [ ] Day 5: Validate EF models match current structure

### Week 2: MCP Generation
- [ ] Day 6: Copy MCP templates from API2MCP
- [ ] Day 7: Create McpCodeGenerator
- [ ] Day 8: Generate 3 core tools (artists, tracks, albums)
- [ ] Day 9: Test generated MCP server
- [ ] Day 10: Validate token optimization

### Week 3: Feature Parity
- [ ] Day 11-12: Generate all 28 tools
- [ ] Day 13: Integrate caching
- [ ] Day 14: Integrate playback (Spotify/Sonos)
- [ ] Day 15: End-to-end testing

### Week 4: Validation & Documentation
- [ ] Day 16-17: Side-by-side testing (old vs new)
- [ ] Day 18: Performance benchmarking
- [ ] Day 19: Extract learnings for API2EF2MCP
- [ ] Day 20: Document conversion process

## Next Steps

1. **Review this plan** - Validate approach makes sense
2. **Begin Phase 1** - Schema discovery and EF generation
3. **Iterate quickly** - Get something working end-to-end fast
4. **Document learnings** - Capture insights for API2EF2MCP
5. **Extract patterns** - Generalize what works for other APIs

## References

- **Current lfm**: `master` branch (baseline for comparison)
- **API2EF**: Schema discovery patterns and EF generation
- **API2MCP**: MCP server templates and tool patterns
- **Plan.API2EF2MCP.md**: Target architecture for generalization

---

**Branch**: `lfm2EF`
**Status**: Planning Complete, Ready for Implementation
**Last Updated**: 2025-01-26
**Next Action**: Begin Phase 1 - Schema Discovery
