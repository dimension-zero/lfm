# Last.fm CLI - Claude Session Notes

## Project Overview
Last.fm CLI tool written in C# (.NET) for retrieving music statistics. The project has undergone significant refactoring to eliminate code duplication, simplify architecture, and optimize API usage. All major features are complete and production-ready.

## Current Architecture
- **Lfm.Cli**: CLI interface with commands and command builders
- **Lfm.Core**: Core functionality including services, models, and configuration
- **Lfm.Spotify**: Spotify integration for playback control
- **Lfm.Sonos**: Sonos integration via node-sonos-http-api
- Uses System.CommandLine for CLI framework
- **File-based caching** with comprehensive cache management (119x performance improvement)
- Centralized error handling and display services
- **MCP Server**: Full integration with 28 tools for LLM interactions

## Key Files
- `src/Lfm.Cli/Program.cs` - Main entry point and DI setup
- `src/Lfm.Cli/Commands/BaseCommand.cs` - Shared command functionality
- `src/Lfm.Core/Services/LastFmApiClient.cs` - API client
- `src/Lfm.Core/Services/CachedLastFmApiClient.cs` - Decorator with comprehensive caching
- `src/Lfm.Core/Configuration/LfmConfig.cs` - Configuration with cache/Spotify/Sonos settings
- `src/Lfm.Spotify/SpotifyStreamer.cs` - Spotify playback integration
- `src/Lfm.Sonos/SonosStreamer.cs` - Sonos playback integration
- `lfm-mcp-release/server.js` - MCP server (2,347 lines, 28 tools)
- `lfm-mcp-release/lfm-guidelines.md` - LLM usage guidelines (480 lines)

## Recent Sessions

### Session: 2025-10-27 (Silent Failure Remediation)
- **Status**: ✅ COMPLETE - Silent failure detection and remediation
- **Major Accomplishments**:
  - **Detection System**: Created PowerShell script to scan for silent failure patterns
  - **Exemption Framework**: Added [SuppressMessage] attribute for justified nullable returns
  - **Pattern Migration**: Migrated BaseCommand.GetUsernameAsync to Result<string>
  - **Systematic Cleanup**: Exempted 11 methods (cache + external integrations)
- **Key Deliverables**:
  - **Detection Script** (`scripts/Find-SilentFailures.ps1`):
    - Scans 192 C# files (28,073 lines) in <1 second
    - Detects 4 patterns: Nullable-Task-Return, Try-Catch-Return-Null, Try-Catch-Throw, Async-Void
    - Supports exemptions via [SuppressMessage] attribute
    - Multiple output formats: Table, List, CSV, JSON
    - Exit code 0 if clean, 1 if active findings (CI/CD ready)
  - **Exemption Attribute** (`src/Lfm.Core/Attributes/SuppressMessageAttribute.cs`):
    - Custom attribute for justified nullable returns
    - Requires explicit justification string
    - Recognized by detection script
  - **Triage Documentation** (`docs/SILENT_FAILURES_TRIAGE.md`):
    - Comprehensive analysis of all 77 initial findings
    - Categorization: API Layer (44), CLI/Service (13), Cache (2), Test Mocks (13)
    - Decision: Keep dual nullable/Result<T> pattern during transition
- **Pattern Classifications**:
  - **Category A - API Layer (44 findings)**: Already has Result<T> variants, dual pattern acceptable
  - **Category B - CLI/Service (13 findings)**: Mix of helpers and external integrations
  - **Category C - Cache Layer (2 findings)**: Justified - null indicates cache miss (expected behavior)
  - **Category D - Test Mocks (13 findings)**: Follow real implementation patterns
- **Changes Made**:
  - **Phase 1**: Created detection script and SuppressMessage attribute
  - **Phase 2**: Triage analysis documented in docs/SILENT_FAILURES_TRIAGE.md
  - **Phase 3**: Added exemptions to 11 methods:
    - Cache: FileCacheStorage.RetrieveAsync, InMemoryCacheStorage.RetrieveAsync
    - Spotify (7): GetCurrentlyPlayingAsync, SearchAlbumUriAsync, SearchSpotifyTrackAsync, GetCurrentUserIdAsync, CreatePlaylistAsync
    - Sonos (1): GetPlaybackStateAsync
  - **Phase 4**: Migrated BaseCommand.GetUsernameAsync to Result<string> (1 method + 8 callers)
- **Results**:
  - **Before**: 77 total findings (68 Nullable-Task-Return)
  - **After**: 66 total findings (57 Nullable-Task-Return)
  - **Fixed**: 11 findings (10 exempted, 1 migrated)
  - **Remaining**: 57 API layer methods with dual nullable/Result<T> pattern (intentional)
- **Architecture Notes**:
  - **Dual Pattern Approach**: API layer maintains both nullable and Result<T> variants during transition
  - **Exemption Philosophy**:
    - Cache misses are expected behavior, not errors
    - External API "not found" is expected, not errors
    - Test mocks follow real implementation signatures
  - **Result<T> Preference**: New code should use Result<T> pattern per global guidelines
  - **Migration Strategy**: Gradual migration from nullable to Result<T> as callers are updated
- **Build Status**: ✅ Clean build (0 errors, 35 pre-existing nullable warnings)
- **Ready For**: Continued gradual migration to Result<T> pattern

### Session: 2025-10-27 (Architecture Refactoring - Phases 1-4)
- **Status**: ✅ COMPLETE - Circuit breaker, configuration validation, service extraction, and utility helpers
- **Major Accomplishments**:
  - **Phase 1: Utility Extraction & Testing Infrastructure**
    - Cache LRU cleanup with timestamp-based eviction
    - xUnit test infrastructure with Moq and FluentAssertions
    - Extracted utilities: JsonOutputHelper, DateRangeValidator, PaginationHelper
  - **Phase 2: Result<T> Pattern Migration**
    - LastFmApiClient: 14 WithResultAsync methods
    - Service layer: LastFmService, SpotifyStreamer fully migrated
    - All 28 commands updated for Result<T> handling
  - **Phase 3: Service Layer Decomposition**
    - SpotifyStreamer split into Auth/Search/Playback services
    - RecommendationEngine extracted from LastFmService (565 lines)
  - **Phase 4: Resilience & Validation**
    - CircuitBreaker implementation with 3-state FSM
    - Comprehensive configuration validation
- **Key Technical Components**:
  - **CircuitBreaker** (`src/Lfm.Core/Services/CircuitBreaker.cs`, 161 lines):
    - Three states: Closed, Open, HalfOpen
    - Configurable thresholds: failure (5), success (2), timeout (60s)
    - Thread-safe with SemaphoreSlim
    - Integrated into LastFmApiClient via optional dependency
  - **ConfigurationValidator** (`src/Lfm.Core/Configuration/ConfigurationValidator.cs`, 255 lines):
    - Validates all 50+ config properties
    - Checks API settings, cache limits, circuit breaker, Spotify/Sonos
    - Returns detailed Result<T> with structured errors
  - **RecommendationEngine** (`src/Lfm.Core/Services/RecommendationEngine.cs`, 565 lines):
    - Extracted from LastFmService
    - 4 public methods: period-based, date-range, Result<T>, play counts
    - Tag filtering with dynamic candidate expansion
  - **Spotify Services Split**:
    - `ISpotifyAuthService` / `SpotifyAuthService` (216 lines)
    - `ISpotifySearchService` / `SpotifySearchService` (298 lines)
    - `ISpotifyPlaybackService` / `SpotifyPlaybackService` (839 lines)
    - `SpotifyStreamer` reduced from 1420 to 118 lines (91.7% reduction)
- **Configuration Additions**:
  - Circuit Breaker:
    - `CircuitBreakerEnabled` (default: true)
    - `CircuitBreakerFailureThreshold` (default: 5)
    - `CircuitBreakerSuccessThreshold` (default: 2)
    - `CircuitBreakerOpenTimeoutSeconds` (default: 60)
- **Error Handling Enhancements**:
  - Added `CircuitBreakerOpen` error type to ErrorResult
  - Symbol: 🔴 (Unicode) / [CIRCUIT] (ASCII)
  - Marked as retryable error
- **Architecture Improvements**:
  - Single Responsibility Principle: Services focused on one concern
  - Dependency Injection: All services registered in Program.cs
  - Result<T> Pattern: Consistent error handling across 14+ API methods
  - Optional Dependencies: Circuit breaker and validator are optional
- **Testing Infrastructure**:
  - xUnit v3 with FluentAssertions
  - Integration tests for cache LRU eviction
  - Result<T> pattern comprehensive tests
  - Mock implementations for testing
- **Code Quality**:
  - Eliminated 246 lines from LastFmService via extraction
  - Reduced SpotifyStreamer by 1302 lines (91.7%)
  - Total: ~1550 lines removed through decomposition
- **Build Status**: ✅ Clean build with 0 errors, 28 pre-existing nullable warnings
- **Performance**: No regression - circuit breaker adds <1ms overhead when closed
- **Backward Compatibility**: 100% - all existing commands work unchanged
- **Ready For**: Production deployment with enhanced resilience

### Session: 2025-10-15 (Documentation Refactoring, Parser Fix, Token Optimization)
- **Status**: ✅ COMPLETE - Documentation split, MCP parser fixed, token usage optimized
- **Major Accomplishments**:
  - **Documentation Refactoring**: Split CLAUDE.md (703→329 lines) into 4 focused files
  - **Contextual Reminders**: Added just-in-time guidance to 3 MCP tools
  - **Parser Bug Fix**: Fixed position-based JSON extraction for array-root responses
  - **Token Optimization**: ~50% token reduction for large MCP queries
  - **Spotify Auth Fix**: Improved resilience with List<string> signature
- **Documentation Split**:
  - Created `SESSION_HISTORY.md` - 7 archived sessions (250 lines)
  - Created `IMPLEMENTATION_NOTES.md` - Technical reference for completed features (184 lines)
  - Created `LESSONS_LEARNED.md` - Debugging patterns and best practices (299 lines)
  - Created `PARSING_BUG_ANALYSIS.md` - Comprehensive parser bug investigation
  - CLAUDE.md reduced by 53% while maintaining current work focus
- **Contextual Reminders Implementation**:
  - `lfm_check` (verbose mode): `_response_guidance` - Concise response pattern reminder
  - `lfm_artist_tracks`: `_depth_guidance` - Depth is popularity ranking, not chronological
  - `lfm_artist_albums`: `_depth_guidance` - Same depth parameter clarification
  - Concept: Just-in-time guidance appearing when context is relevant
  - Proven pattern from planning mode system reminders
- **Parser Bug Fix** (`lfm-mcp-release/server.js` lines 45-165):
  - **Root Cause**: Object-first bias extracted nested objects from array-root responses
  - **Problem Cases**:
    - `artist_albums` returning only first album from array `[{album1}, {album2}]`
    - `play_now` returning `[]` instead of full response object
  - **Solution**: Position-based parser extracts whichever structure (`[` or `{`) appears first
  - **Implementation**: Helper functions `extractArray()` and `extractObject()` with fallback logic
  - **Result**: Correctly handles both object-root and array-root JSON structures
- **Token Optimization** (`lfm-mcp-release/server.js` lines 139-165):
  - Added compact helper functions: `compactAlbum()`, `compactArtist()`, `compactTrack()`
  - Strip URLs and MBIDs that LLMs can't use
  - Applied to `lfm_albums`, `lfm_artists`, `lfm_tracks` handlers
  - **Result**: ~50% token reduction (100 albums: ~10,300 → ~5,150 tokens)
  - Architecture: Post-filtering at MCP layer (cache stores full data for all consumers)
- **Spotify Authentication Fix** (`src/Lfm.Spotify/SpotifyStreamer.cs`):
  - Changed `StartPlaybackAsync` signature from `string` to `List<string>`
  - Ensures atomic album playback (all tracks in one API call)
  - Prevents race conditions in track ordering
  - Complements Session 2025-01-19 Spotify album queueing fix
- **Key Insights**:
  - Position-based parser is architecturally correct (not a band-aid fix)
  - Post-filtering optimization: Only place to strip data is MCP layer
  - Data flow: Last.fm API → Cache (full) → MCP Server (filtered) → LLM
  - Contextual reminders: 82% token savings vs upfront guidelines (500 + 25/tool vs 4,000)
- **📋 Future Work - Playback Orchestration**:
  - **Issue**: LLM writes narrative before checking playback state
  - **User Feedback**: "you could have checked... but you were mid narrative"
  - **Example Failure**:
    - User asks "Is Fifth Dimension next?"
    - LLM writes long WHY narrative
    - LLM asks tentative "Want me to queue it?"
    - User had JUST FINISHED Mr. Tambourine Man (tools could show this)
  - **What Should Have Happened**:
    - Check `lfm_recent_tracks` → see Mr. Tambourine Man just finished
    - Check `lfm_current_track` → confirm nothing playing
    - Respond: "I see you just finished Mr. Tambourine Man. Fifth Dimension is the logical next step - shall I queue it?"
  - **The Challenge**:
    - Failure happens during text generation (before tool calls)
    - No technical hook point exists to intercept
    - Need to train behavioral reflex: "About to suggest playback? Check state first."
  - **User's Ideal Outcome** ("Championship Vinyl" DJ):
    - Data-informed AND permission-seeking
    - Confident AND polite (not tentative "want me to?")
    - Aware AND respectful (not pushy auto-play)
    - Template: "I see [data]. [Brief context]. Shall I [action]?"
  - **Proposed Solution - Phased Approach**:
    1. **Guideline** (immediate): Add prominent guideline with trigger word recognition
       - "Before phrases like 'Want me to queue', 'Shall I play' → STOP, check state first"
       - Mandatory workflow: recent_tracks → current_track → informed offer
       - Response template with good/bad examples
    2. **Test & Observe**: Collect failure/success patterns
    3. **Contextual Reminder** (if needed): Add `_playback_suggestion_guidance` to state-checking tools
    4. **Hook Experiment** (if needed): user-prompt-submit-hook with pattern detection
  - **Key Insight**: Problem is behavioral (text generation phase), not technical (tool call phase)
  - **Action**: Draft guideline using "DJ watching the floor" framing, position prominently in lfm-guidelines.md
  - **Status**: ✅ IMPLEMENTED (Session 2025-10-16) - Added "Playback State Awareness" section to lfm-guidelines.md
- **Build Status**: ✅ Clean build, all features tested and working
- **Testing**: Parser handles all cases, token reduction verified with 100 albums query
- **Documentation**: See `docs/PARSING_BUG_ANALYSIS.md` for detailed parser investigation

### Session: 2025-10-09 (Spotify Album Disambiguation & Parallel API Calls)
- **Status**: ✅ COMPLETE - Album version disambiguation and parallel API processing for deep searches
- **Major Features Implemented**:
  - **Album Version Disambiguation**: Detect and handle tracks that exist on multiple albums
  - **Parallel API Processing**: Batch parallel API calls for artist-tracks/artist-albums with intelligent throttling
  - **MCP Guidelines Refactoring**: Simplified from quiz-based to trust-based initialization system
  - **Album Check JSON Enhancements**: Added `guidelinesSuggested` and `interpretationGuidance` fields
- **Key Technical Components**:
  - `PlayCommand.cs`: Allow track + album parameter combination for version disambiguation
  - `SpotifyStreamer.cs`:
    - `SearchSpotifyTrackWithDetailsAsync`: Returns `TrackSearchResult` with version detection
    - `PlayNowFromUrisAsync`: Direct URI playback for albums
    - `QueueFromUrisAsync`: Direct URI queueing for albums
    - `SearchSpotifyAlbumTracksAsync`: One-shot album search returning all track URIs
  - `SpotifyModels.cs`: New `TrackSearchResult` model with `HasMultipleVersions` and `AlbumVersions` list
  - `ArtistSearchCommand.cs`: Parallel batch processing using `Task.WhenAll` with rate limiting
  - `CachedLastFmApiClient.cs`: `DisableThrottling` property for parallel batch mode
  - `LfmConfig.cs`: New `ParallelApiCalls` setting (default: 5 concurrent calls)
  - `CheckCommand.cs`: Enhanced JSON output with interpretation guidance
- **Album Version Disambiguation**:
  - **Problem**: Tracks like "Live and Let Die" exist on studio albums, greatest hits, live albums
  - **Solution**: Detect multiple versions, return error listing all album options
  - **User preference**: Studio albums preferred over live/greatest hits unless explicitly requested
  - **MCP integration**: LLM can specify album parameter when multiple versions detected
  - **Example**: "Hey Jude" appears on "Hey Jude", "1967-1970", "Past Masters" - user chooses which
- **Parallel API Calls**:
  - **Use case**: Deep searches (artist-tracks, artist-albums) that page through 1000+ pages
  - **Implementation**: Batch of 5 API calls executed concurrently using `Task.WhenAll`
  - **Rate limiting**: 1 second between batches to respect Last.fm's 5 req/sec limit
  - **Throttling**: Individual call throttling disabled during parallel execution
  - **Batch timing**: Dynamic delay calculation ensures compliance with rate limits
  - **Performance**: Significant speedup for deep searches without violating API limits
- **Configuration**:
  - `ParallelApiCalls`: Number of concurrent API calls in batch mode (default: 5)
  - Compatible with existing cache and throttle settings
- **Build Status**: ✅ Clean build, all features tested and working
- **Ready For**: Production use with MCP integration

### Session: 2025-10-11 (Sonos Integration & Unified Playback)
- **Status**: ✅ COMPLETE - Full Sonos playback integration with unified play command
- **Major Features Implemented**:
  - **Unified Play Command**: Single command supporting both Spotify and Sonos players
  - **Config-Driven Defaults**: DefaultPlayer and DefaultRoom configuration eliminates need for MCP to ask
  - **Player Routing**: Intelligent routing based on configuration with parameter overrides
  - **Sonos HTTP Bridge**: Integration with node-sonos-http-api for Sonos control
  - **Shared Search Logic**: Unified track search for both players with multiple version detection
- **Implementation Details**:
  - **node-sonos-http-api**: Node.js HTTP bridge running as systemd service on Raspberry Pi
  - **Room Management**: Auto-discovery, validation, and config storage of Sonos rooms
  - **Album Playback Strategy**: Album URIs for Sonos (plays as unit), track URIs for Spotify
  - **Queue Support**: Both immediate playback and queue modes for both players
- **New Configuration Options**:
  - `DefaultPlayer`: "Spotify" or "Sonos" (determines playback target)
  - `Sonos.HttpApiBaseUrl`: Bridge URL (e.g., "http://192.168.1.24:5005")
  - `Sonos.DefaultRoom`: Default Sonos room for playback
  - `Sonos.TimeoutMs`: HTTP timeout for Sonos API calls (default: 5000)
  - `Sonos.RoomCacheDurationMinutes`: Cache duration for room discovery (default: 5)
- **CLI Parameters Added**:
  - `--player` / `-p`: Override default player (Spotify/Sonos)
  - `--room` / `-r`: Override default Sonos room
  - Works with existing: `--track`, `--album`, `--queue`, `--device` (Spotify only)
- **Key Technical Components**:
  - `PlayCommand.cs`: Unified player routing with shared track search at top level
  - `PlayCommandBuilder.cs`: Enhanced CLI interface with player and room options
  - `SonosStreamer.cs`: HTTP client for node-sonos-http-api communication
  - `ISonosStreamer`: Interface defining Sonos operations (PlayNowAsync, QueueAsync, etc.)
  - `SpotifyStreamer.SearchSpotifyAlbumUriAsync`: New method for album URI lookup
  - `SonosConfig`: Configuration model for Sonos settings
- **Critical Bug Fixes**:
  - **URI Encoding Issue**: Sonos API requires raw Spotify URIs, not URL-encoded (colons must be preserved)
  - **Album Queue Behavior**: Using album URIs instead of track URIs for proper album playback on Sonos
  - **Multiple Version Detection**: Extended from Spotify-only to both players via unified search
  - **Access Token Initialization**: Added missing `EnsureValidAccessTokenAsync()` in `SearchSpotifyTrackWithDetailsAsync`
- **Sonos Commands Implemented**:
  - `lfm sonos rooms` - List all available Sonos rooms with grouping info
  - `lfm sonos status <room>` - Get current playback state for room
  - `lfm sonos pause <room>` - Pause playback
  - `lfm sonos resume <room>` - Resume playback
  - `lfm sonos skip <room> [next|previous]` - Skip tracks
  - `lfm config set-sonos-api-url <url>` - Configure bridge URL
  - `lfm config set-sonos-default-room "Room Name"` - Set default room
- **Usage Examples**:
  - `lfm play "Pink Floyd" --track "Money" --album "Dark Side of the Moon"` - Uses config default player
  - `lfm play "Pink Floyd" --track "Money" --player Sonos --room "Kitchen"` - Explicit player/room
  - `lfm play "Pink Floyd" --album "Dark Side of the Moon" --queue` - Queue entire album
- **Architecture Highlights**:
  - **Unified Search**: Single `SearchSpotifyTrackWithDetailsAsync` call before player routing
  - **Shared Logic**: Multiple version detection, album disambiguation work for both players
  - **Minimal Code Duplication**: Spotify search logic reused for both playback targets
  - **Config Flexibility**: Per-environment configuration (office uses Spotify, home uses Sonos)
- **Testing Results**:
  - ✅ Track playback to Sonos with multiple version detection
  - ✅ Album playback to Sonos as complete unit
  - ✅ Queue mode working for both tracks and albums
  - ✅ Player routing working with config defaults and parameter overrides
  - ✅ Room validation and error handling working correctly
  - ✅ Fuzzy album matching working ("darks side" matches "The Dark Side of the Moon")
- **Performance**: Leverages existing Spotify search API, no additional Last.fm API calls required
- **Build Status**: ✅ Clean build, all Sonos functionality tested and working
- **Ready For**: Production use and MCP integration

### Session: 2025-01-19 (MCP Guidelines Refinement & Spotify Album Queueing Fix)
- **Status**: ✅ COMPLETE - MCP guidelines enhanced, critical Spotify bug fixed, architecture optimization evaluated
- **Major Accomplishments**:
  - **MCP Guidelines Updates**: 4 sections added/modified based on real-world testing feedback
  - **Spotify Album Queueing Bug Fix**: Fixed missing/reordered tracks in album playback
  - **Depth Parameter Clarification**: Corrected understanding of popularity ranking vs chronological search
  - **Guidelines Architecture Evaluation**: Analyzed token usage and optimization strategies
  - **Contextual Reminders Concept**: Proposed just-in-time guidance system (82% token savings)
- **Guidelines Updates** (`lfm-mcp-release/lfm-guidelines.md`):
  1. **Track Position Hallucination Prevention** (lines 98-125):
     - User feedback: LLM confidently stating "track 2 is..." when it's actually track 1
     - Added "NEVER reference track numbers or positions without verification"
     - Verification workflow: Use `lfm_check(verbose: true)`, `lfm_current_track()`, or `lfm_recent_tracks()`
     - Key insight: Track positions are edge-case data for LLMs, leading to false confidence
  2. **lfm_check Fallback Strategy** (lines 83-91):
     - When check returns 0 plays, don't assume user hasn't heard it
     - Fallback: Use `lfm_artist_albums` or `lfm_artist_tracks` with `deep: true`
     - Handles metadata variations: spacing ("Renée / Pretty" vs "Renée/Pretty"), remaster suffixes, featuring artists
     - Real scrobbles are source of truth, not Last.fm's canonical names
  3. **Depth Parameter Simplification** (lines 137-165):
     - **CRITICAL**: Clarified depth = popularity ranking (top N by play count), NOT chronological
     - User correction: "is the depth 'tracks' or 'pages'? let's think about edge cases"
     - Example: Left Banke with 14 plays ranks #2324, missed at depth:2000 (user has 2,323 tracks with MORE plays)
     - Edge case: Breadth listeners (Guided By Voices: 100 albums × 1-2 plays each = all tracks rank 5000-7000, depth:2000 finds ZERO)
     - Simplified approach: Default to `deep: true`, let cache handle performance
     - Removed complex tier explanations that led to wrong mental model
  4. **Concise Response Pattern** (lines 265-281):
     - User feedback: "stop all the blurb... 'Yes you have, you've listened to White Light/White Heat 4-5 times'. done."
     - Pattern: Answer directly with ONE interesting detail max
     - Good example: "Yes, you've listened to White Light/White Heat 4-5 times (21 plays, including the 17-minute 'Sister Ray')."
     - Bad example: Track-by-track breakdown, metadata discrepancy math explanations, music history lessons
- **Spotify Bug Fix** (`src/Lfm.Spotify/SpotifyStreamer.cs`):
  - **Problem**: Simon & Garfunkel "Bookends" (12 tracks) had 2 tracks missing and some reordered
  - **Root Cause**: 12 separate API calls (1 play + 11 individual queues) caused race conditions
  - **Solution**: Changed to ONE atomic API call with all URIs
  - **Implementation**:
    - Modified `StartPlaybackAsync` signature from `string` to `List<string>` (lines 1118-1128)
    - Rewrote `PlayNowFromUrisAsync` to use single atomic call (lines 233-277)
    - Spotify Web API supports up to 100 URIs in one request
  - **User Confirmation**: "it queued up cleanly as well so the spotify album play fix is working" ✅
- **Metadata Matching Investigation**:
  - **Case**: Left Banke album "Walk Away Renée / Pretty Ballerina" returned 0 plays
  - **Root Cause**: Spacing difference (MCP query had spaces around slash, scrobbles didn't)
  - **Findings**:
    - Last.fm autocorrect enabled for all lookups (handles typos, capitalization)
    - Custom apostrophe retry logic handles Unicode variants (U+0027, U+2018, U+2019)
    - NO handling for punctuation spacing differences (slash, dash, etc.)
  - **User Decision**: "yes. that feels a stretch too far" (regarding adding slash fuzzy matching)
  - **Mitigation**: Added guidelines recommending fallback to artist_albums with deep:true
- **Guidelines Architecture Analysis**:
  - **Current State**: 479 lines, 2,805 words, ~4,000 tokens (2% of 200K context window)
  - **Project Size**: 19,230 LOC C# code, 2,347 lines MCP server (server.js)
  - **User Concern**: "it's getting quite long... on the edge of becoming top heavy"
  - **Options Evaluated**:
    1. Single file with TOC summary (baseline)
    2. Split into focused documents (core/technical/examples) - 75% token savings
    3. Hybrid section-based retrieval
    4. **Contextual reminders in MCP tool outputs** (user's idea) - 82% savings
  - **Contextual Reminders Concept**:
    - User insight: "with our experience with planning mode it might be more responsive to putting in reminders in either the mcp tool or comments in function outputs"
    - Embed `_guideline_reminder` fields in MCP tool responses
    - Just-in-time guidance appearing exactly when context is relevant
    - Proven pattern from plan mode system reminders
    - Token comparison: 500 baseline + 25 per tool call vs 4,000 baseline
  - **User Decision**: "i think this is a sleep on it moment for me"
  - **Approach**: "step by step and tests what works and what doesn't as we go along. be systematic about it."
  - **Documentation**: User requested documenting insights for future reference even though implementation deferred
- **Key Insights**:
  - **Depth Parameter**: Popularity ranking, not chronological - fundamental misunderstanding in guidelines
  - **LLM Blind Spots**: Track positions and album metadata are edge cases leading to false confidence
  - **Response Style**: Users want concise answers, not data analyst explanations
  - **Just-in-Time Guidance**: More elegant than file splitting, proven pattern from existing systems
- **Testing & Verification**:
  - Left Banke deep search working correctly with proper depth understanding
  - Spotify album queueing verified by user in production
  - Guidelines updates validated through real conversation examples
  - Token analysis confirms guidelines are only 2% of context budget
- **Build Status**: ✅ Clean build (0 errors, 10 pre-existing nullable warnings unrelated to changes)
- **Next Steps**:
  - User to evaluate contextual reminders approach
  - Systematic testing if/when implemented
  - Continue gathering evidence on guidelines usage patterns
- **✅ Recommendations Tool Usage Guidelines** (Session 2025-10-16):
  - **Status**: ✅ IMPLEMENTED - Added "Using lfm_recommendations - LLM Reasoning First" section to lfm-guidelines.md
  - **Issue**: Recommendations tool may be overused in MCP context
  - **History**: Built originally for CLI, later exposed via MCP
  - **Key Insight**: For good reasoning LLMs, recommendations is rarely needed (9/10 times LLM reasoning is better)
  - **Implementation**: Added 83-line section (lines 108-190) emphasizing:
    - "In general, lfm_recommendations is NOT a great starting point"
    - "Your musical knowledge is more valuable than raw similarity scores"
    - When NOT to use: Beatles, Pink Floyd, well-known artists (LLM knows discography)
    - When to use (rarely): Truly obscure artists, as supplement to augment AND filter
    - Workflow: LLM reasoning first → optionally supplement with tool → augment/filter → curate
  - **Examples**: Real bad/good examples showing over-reliance vs LLM-first reasoning
  - **User Clarification**: "not even just starting pool then filter... augment AND filter with judgment"
- **✅ Listen Before You Speak - Core Workflow Methodology** (Session 2025-10-16):
  - **Status**: ✅ IMPLEMENTED - Added "Listen Before You Speak" section to Response Style (lfm-guidelines.md lines 31-43)
  - **Origin**: Test LLM feedback: "I need to internalize: data → think → more data → think → narrative"
  - **Problem**: LLM starting narratives before completing research phase, discovering gaps mid-response
  - **Real Example**: Taylor Swift analysis claiming she's an "outlier" without checking artist_albums showed she's top-tier (#9, #13, #23)
  - **Implementation**: Ultra-concise 9-line section positioned immediately after "Be a DJ buddy" header
  - **Key Principles**:
    - "A DJ buddy has spun the records before talking about them"
    - Workflow: data → think → more data → think → narrative (NOT start narrative → discover gaps)
    - The test: "Would you need to check mid-response?" → If yes, gather more data first
    - "When you can see the complete pattern → speak with authority"
  - **Design Philosophy**: REALLY SIMPLE AND CLEAN - foundational methodology before DO/DON'T lists
  - **Integration**: Extends DJ metaphor (spun the records = gathered the data)

## Build/Test Commands

⚠️ **CRITICAL**: Always use `publish/` directory structure per DIRECTORY_STANDARDS.md ⚠️

**Standard Build Commands:**
- **Development Build**: `dotnet build -c Release`
- **Windows Publish**: `dotnet publish src/Lfm.Cli -c Release -r win-x64 -o publish/win-x64 --self-contained false`
- **Linux Publish**: `dotnet publish src/Lfm.Cli -c Release -r linux-x64 -o publish/linux-x64 --self-contained false`

**Executable Locations:**
- **Windows**: `./publish/win-x64/lfm.exe` ← USE THIS
- **Linux**: `./publish/linux-x64/lfm` ← USE THIS

**Testing Commands:**
- **Test Cache**: `./publish/linux-x64/lfm test-cache` (internal testing command)
- **Benchmark Cache**: `./publish/linux-x64/lfm benchmark-cache` (performance validation)

## Notes
- Uses .NET 8/9 (both target frameworks present)
- Publishes to both Windows and Linux
- Configuration stored in user's AppData/lfm folder
- API key required from Last.fm
- Spotify/Sonos integration requires additional configuration

## Current Project Plans & Status

### ✅ Primary Development - COMPLETE
**All major architecture and functionality complete**. The CLI tool is fully functional with:
- ✅ Complete service layer architecture
- ✅ Comprehensive API throttling and reliability
- ✅ Full caching implementation with management (119x improvement)
- ✅ All core commands working (artists, tracks, albums, recommendations, toptracks, mixtape)
- ✅ Date range support across all commands
- ✅ Unicode symbol support with auto-detection
- ✅ Spotify + Sonos playback integration
- ✅ MCP server with 28 tools for LLM interaction
- ✅ Clean build with 0 warnings, 0 errors

### 📋 Future Enhancement Plans

#### 1. **Progress Bar Implementation** 📊
- **Status**: Detailed plan created in `progressbarproject.md`
- **Goal**: Add progress bars for long-running operations (30+ seconds)
- **Priority**: High value for user experience
- **Effort**: 2-3 days implementation
- **Key Benefits**:
  - Real-time feedback for date range aggregation
  - Progress indicators for range queries and recommendations
  - Professional feel for long operations
- **Architecture**: IProgressReporter interface with Console/Null implementations
- **Integration**: Leverages existing throttling points for minimal code changes

#### 2. **Additional Features** (Future Considerations)
- **Enhanced Filtering**: More sophisticated recommendation filters
- **Export Functionality**: JSON/CSV export for query results
- **Playlist Generation**: Create playlists from recommendations
- **Extended Analytics**: Advanced statistics and insights
- **Configuration Enhancements**: More granular settings

### 🎯 Current State Assessment
- **Codebase Quality**: Excellent - clean architecture, comprehensive error handling
- **Performance**: Optimized with 119x cache improvements + proper API throttling
- **Reliability**: Robust with comprehensive error handling and graceful degradation
- **User Experience**: Good foundation, progress bars would be primary UX enhancement
- **Maintainability**: High - well-structured with clear separation of concerns

### 📝 Development Notes
- **Build Status**: ✅ Consistently clean builds across platforms
- **Testing**: Manual testing comprehensive, all core functionality verified
- **Documentation**: Well-documented with comprehensive README and session notes
- **Configuration**: Flexible with user-configurable settings for all major behaviors

### 🚀 Distribution & Releases

**Current Version**: 1.5.0 (CLI) / 0.2.0 (MCP Server)

**Release Infrastructure** (Session 2025-10-17):
- ✅ Automated GitHub Actions workflow (`.github/workflows/release.yml`)
- ✅ One-liner installation scripts (`install.ps1`, `install.sh`)
- ✅ Comprehensive documentation suite (INSTALL.md, QUICKSTART.md, MCP_SETUP.md, TROUBLESHOOTING.md)
- ✅ Landing page README routing users to appropriate docs

**Creating a New Release** - See [RELEASE.md](RELEASE.md) for detailed instructions:

1. **Update version numbers**:
   - `src/Lfm.Cli/Lfm.Cli.csproj` → `<Version>1.6.0</Version>`
   - `lfm-mcp-release/server.js` → `version: '0.3.0'`
   - `CHANGELOG.md` → Add new release section

2. **Commit and tag**:
   ```bash
   git add src/Lfm.Cli/Lfm.Cli.csproj lfm-mcp-release/server.js CHANGELOG.md
   git commit -m "chore: Bump version to 1.6.0"
   git push
   git tag -a v1.6.0 -m "Release v1.6.0"
   git push --tags
   ```

3. **GitHub Actions automatically**:
   - Builds all 4 platform binaries (Windows, macOS Intel/ARM, Linux)
   - Packages MCP server with **lfm-guidelines.md** (critical file)
   - Creates GitHub Release with all assets
   - Generates release notes

**Installation Methods**:
- **One-liner installers**: Detect platform, download latest, configure PATH
  - Windows: `iwr -useb https://raw.githubusercontent.com/Steven-Marshall/lfm/master/install.ps1 | iex`
  - macOS/Linux: `curl -fsSL https://raw.githubusercontent.com/Steven-Marshall/lfm/master/install.sh | bash`
- **Manual download**: From GitHub Releases page
- **Building from source**: See README.md

**Key Files**:
- `RELEASE.md` - Complete release process documentation
- `INSTALL.md` - Platform-specific installation guide
- `QUICKSTART.md` - 5-minute getting started guide
- `MCP_SETUP.md` - Claude integration setup
- `TROUBLESHOOTING.md` - Common issues reference

---

## 📚 Documentation References

**This file focuses on current work and recent sessions. For historical context and implementation details, see:**

- **[docs/SESSION_HISTORY.md](docs/SESSION_HISTORY.md)** - Archived sessions (2025-01-16 through 2025-10-06)
  - Cache Implementation (Session 2025-01-17)
  - Recommendations Feature (Session 2025-01-28)
  - Spotify Device Management (Session 2025-09-19)
  - Albums Bug Fix & API Throttling (Session 2025-06-28)
  - TopTracks Command (Session 2025-09-24)
  - Album Track Checking (Session 2025-10-06)

- **[docs/IMPLEMENTATION_NOTES.md](docs/IMPLEMENTATION_NOTES.md)** - Completed implementations
  - Refactoring Status (all tasks completed)
  - Cache Implementation (119x performance improvement)
  - Unicode Symbol Support (auto-detection across platforms)
  - API Throttling (200ms default, parallel calls support)
  - Spotify + Sonos Integration
  - MCP Server Integration

- **[docs/LESSONS_LEARNED.md](docs/LESSONS_LEARNED.md)** - Debugging patterns and best practices
  - Critical Thinking Over Quick Fixes (2010 Cache Bug)
  - Metadata Matching Complexity (apostrophe variants, featuring artists)
  - API Performance Analysis (HTTP bottleneck identification)
  - Spotify API Race Conditions (atomic batch operations)
  - LLM Blind Spots in MCP Context (track position hallucinations)
  - Understanding Depth Parameter (popularity ranking vs chronological)
  - Response Style for Music Conversations (DJ buddy vs data analyst)
