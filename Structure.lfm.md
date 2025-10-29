# lfm Codebase Structure

**Last Updated**: 2025-01-26
**Total Projects**: 8 (.NET projects)
**Total Source Files**: ~250 C# files (excluding obj/ generated files)

---

## 📁 Root Directory

```
lfm/
├── Lfm.sln                          # Visual Studio solution file
├── README.md                        # Main project documentation
├── CLAUDE.md                        # Claude session notes and history
├── Architecture.LFM.md              # Architecture documentation
├── CHANGELOG.md                     # Version history
├── INSTALL.md                       # Installation guide
├── QUICKSTART.md                    # Quick start guide
├── RELEASE.md                       # Release process documentation
├── Report.lfm2ef.md                 # lfm2EF branch analysis report
├── ORIGINAL-VS-EF-COMPARISON.md     # Original vs EF Core comparison
├── Handover.md                      # Phase 2 work-in-progress handover
├── install.ps1                      # Windows installation script
├── install.sh                       # macOS/Linux installation script
├── .gitignore                       # Git ignore rules
└── .git/                            # Git repository metadata
```

---

## 📂 Source Code (`src/`)

### **Lfm.Cli** - Command Line Interface (Entry Point)
**Purpose**: CLI application using System.CommandLine framework
**Target Framework**: .NET 8
**Lines of Code**: ~5,000

```
src/Lfm.Cli/
├── Program.cs                       # Application entry point, DI setup
├── Lfm.Cli.csproj                   # Project file
│
├── Commands/                        # Command implementations
│   ├── BaseCommand.cs               # Shared command functionality
│   ├── BasePlaybackCommand.cs       # Base for playback commands
│   ├── ArtistsCommand.cs            # Get top artists
│   ├── TracksCommand.cs             # Get top tracks
│   ├── AlbumsCommand.cs             # Get top albums
│   ├── ArtistSearchCommand.cs       # Artist-specific queries
│   ├── CheckCommand.cs              # Check if user listened to track/album
│   ├── RecommendationsCommand.cs    # Generate recommendations
│   ├── TopTracksCommand.cs          # Top tracks by various metrics
│   ├── MixtapeCommand.cs            # Generate mixtapes
│   ├── PlayCommand.cs               # Play music (Spotify/Sonos)
│   ├── CreatePlaylistCommand.cs     # Create Spotify playlists
│   ├── CurrentCommand.cs            # Current playing track
│   ├── RecentCommand.cs             # Recent listening history
│   ├── SimilarCommand.cs            # Similar artists
│   ├── NewReleasesCommand.cs        # New releases from top artists
│   ├── ConfigCommand.cs             # Configuration management
│   ├── CacheStatusCommand.cs        # Cache status and stats
│   ├── CacheClearCommand.cs         # Clear cache
│   ├── BenchmarkCacheCommand.cs     # Cache performance testing
│   ├── TestCacheCommand.cs          # Cache validation
│   ├── ApiStatusCommand.cs          # Last.fm API status check
│   ├── SpotifyCommand.cs            # Spotify-specific commands
│   ├── SonosRoomsCommand.cs         # List Sonos rooms
│   ├── SonosStatusCommand.cs        # Sonos playback status
│   ├── PauseCommand.cs              # Pause playback
│   ├── ResumeCommand.cs             # Resume playback
│   └── SkipCommand.cs               # Skip tracks
│
├── CommandBuilders/                 # System.CommandLine command builders
│   ├── StandardCommandOptions.cs    # Shared CLI options
│   ├── CommandOptionBuilders.cs     # Common option builders
│   ├── ArtistsCommandBuilder.cs
│   ├── TracksCommandBuilder.cs
│   ├── AlbumsCommandBuilder.cs
│   ├── ArtistAlbumsCommandBuilder.cs
│   ├── ArtistTracksCommandBuilder.cs
│   ├── CheckCommandBuilder.cs
│   ├── RecommendationsCommandBuilder.cs
│   ├── TopTracksCommandBuilder.cs
│   ├── MixtapeCommandBuilder.cs
│   ├── PlayCommandBuilder.cs
│   ├── CreatePlaylistCommandBuilder.cs
│   ├── CurrentCommandBuilder.cs
│   ├── RecentCommandBuilder.cs
│   ├── SimilarCommandBuilder.cs
│   ├── NewReleasesCommandBuilder.cs
│   ├── ConfigCommandBuilder.cs
│   ├── CacheStatusCommandBuilder.cs
│   ├── CacheClearCommandBuilder.cs
│   ├── BenchmarkCacheCommandBuilder.cs
│   ├── TestCacheCommandBuilder.cs
│   ├── ApiStatusCommandBuilder.cs
│   ├── SpotifyCommandBuilder.cs
│   ├── SonosCommandBuilder.cs
│   ├── PauseCommandBuilder.cs
│   ├── ResumeCommandBuilder.cs
│   └── SkipCommandBuilder.cs
│
└── Services/                        # CLI-specific services
    ├── ISpotifyStreamingService.cs  # Spotify streaming interface
    └── SpotifyStreamingService.cs   # Spotify streaming implementation
```

---

### **Lfm.Core** - Core Business Logic
**Purpose**: Core services, configuration, caching, and business logic
**Target Framework**: .NET 8
**Lines of Code**: ~8,000

```
src/Lfm.Core/
├── Lfm.Core.csproj                  # Project file
│
├── Configuration/                   # Configuration management
│   ├── LfmConfig.cs                 # Main configuration model
│   ├── CacheBehavior.cs             # Cache behavior flags
│   ├── CacheDirectoryHelper.cs      # Cache directory resolution
│   ├── IConfigurationValidator.cs   # Validator interface
│   ├── ConfigurationValidator.cs    # Configuration validation
│   └── ErrorMessages.cs             # Centralized error messages
│
├── Services/                        # Core services
│   ├── LastFmApiClient.cs           # Last.fm API HTTP client
│   ├── CachedLastFmApiClient.cs     # Caching decorator
│   ├── LastFmApiProvider.cs         # ILastFmApiClient → IMusicDataProvider wrapper
│   ├── LastFmDataProvider.cs        # Alternative data provider implementation
│   ├── LastFmService.cs             # High-level Last.fm operations
│   ├── ILastFmService.cs            # Service interface
│   ├── CircuitBreaker.cs            # Circuit breaker pattern
│   ├── ICircuitBreaker.cs           # Circuit breaker interface
│   ├── DateRangeParser.cs           # Parse date ranges from user input
│   ├── DisplayService.cs            # Console output formatting
│   ├── SymbolProvider.cs            # Unicode/ASCII symbol provider
│   ├── TagFilterService.cs          # Filter artists by tags
│   ├── RecommendationEngine.cs      # Generate music recommendations
│   ├── IRecommendationEngine.cs     # Recommendation interface
│   ├── PlaylistInputParser.cs       # Parse playlist input formats
│   ├── IPlaylistInputParser.cs      # Parser interface
│   ├── MergedDataProvider.cs        # Merge multiple data sources
│   │
│   ├── Cache/                       # Cache subsystem
│   │   ├── ICacheStorage.cs         # Storage abstraction
│   │   ├── FileCacheStorage.cs      # File-based cache implementation
│   │   └── CacheKeyGenerator.cs     # Generate cache keys
│   │
│   ├── LocalFiles/                  # Local file parsing
│   │   ├── ILocalFileParser.cs      # Parser interface
│   │   ├── LocalFileDataProvider.cs # Local file data provider
│   │   ├── LocalFileAggregator.cs   # Aggregate local file data
│   │   ├── SpotifyJsonParser.cs     # Spotify Extended Streaming History
│   │   └── YouTubeMusicParser.cs    # YouTube Music history
│   │
│   └── Enrichment/                  # Album metadata enrichment
│       ├── IAlbumEnricher.cs        # Enricher interface
│       ├── AlbumEnrichmentService.cs # Enrichment orchestrator
│       ├── LastFmAlbumEnricher.cs   # Last.fm metadata enrichment
│       ├── SpotifyAlbumEnricher.cs  # Spotify metadata enrichment
│       ├── MusicBrainzAlbumEnricher.cs # MusicBrainz enrichment
│       └── YouTubeDataEnricher.cs   # YouTube Data API enrichment
│
├── Models/Results/                  # Result pattern models
│   ├── Result.cs                    # Result<T> for functional error handling
│   └── ErrorResult.cs               # Error details model
│
├── Utilities/                       # Utility classes
│   ├── DateRangeValidator.cs        # Validate date ranges
│   ├── JsonOutputHelper.cs          # JSON output formatting
│   └── PaginationHelper.cs          # Pagination utilities
│
├── Logging/                         # Logging infrastructure
│   └── LoggerFactory.cs             # Logger factory
│
└── Attributes/                      # Custom attributes
    └── SuppressMessageAttribute.cs  # Code analysis suppression
```

---

### **Lfm.Shared** - Shared Models and Interfaces
**Purpose**: Shared models, DTOs, and abstractions
**Target Framework**: .NET 8
**Lines of Code**: ~1,500

```
src/Lfm.Shared/
├── Lfm.Shared.csproj                # Project file
├── AssemblyInfo.cs                  # Assembly metadata
│
├── Services/                        # Service interfaces
│   └── IMusicDataProvider.cs        # Multi-source data abstraction (15 methods)
│
├── Models/                          # Data models
│   ├── LastFmModels.cs              # Last.fm API response models
│   ├── ArtistLookupInfo.cs          # Artist lookup result
│   ├── TrackLookupInfo.cs           # Track lookup result
│   ├── AlbumLookupInfo.cs           # Album lookup result
│   ├── TrackRequest.cs              # Track request model
│   ├── NewReleasesModels.cs         # New releases models
│   │
│   ├── Results/                     # Result pattern models
│   │   ├── Result.cs                # Result<T> pattern
│   │   ├── ErrorResult.cs           # Error details
│   │   └── RecommendationResult.cs  # Recommendation result
│   │
│   └── LocalFiles/                  # Local file format models
│       ├── SpotifyModels.cs         # Spotify JSON format
│       └── YouTubeModels.cs         # YouTube Music JSON format
│
└── Configuration/                   # Shared configuration
    └── SearchConstants.cs           # Search depth constants
```

---

### **Lfm.Data.EF** - EF Core LINQ Provider
**Purpose**: Custom EF Core query provider for LINQ over Last.fm API
**Target Framework**: .NET 9
**Lines of Code**: ~3,500

```
src/Lfm.Data.EF/
├── Lfm.Data.EF.csproj              # Project file
├── LfmDbContext.cs                  # DbContext with Artists/Tracks/Albums/RecentTracks
├── README.md                        # Usage documentation
│
├── Entities/                        # EF Core entity models
│   ├── LastFmEntity.cs              # Base entity
│   ├── Artist.cs                    # Artist entity
│   ├── Track.cs                     # Track entity
│   ├── Album.cs                     # Album entity
│   └── RecentTrack.cs               # Recent track entity
│
├── Configuration/                   # EF Core entity configurations
│   ├── ArtistConfiguration.cs
│   ├── TrackConfiguration.cs
│   ├── AlbumConfiguration.cs
│   └── RecentTrackConfiguration.cs
│
├── Provider/                        # LINQ query provider
│   ├── LastFmQueryProvider.cs       # Main query provider (implements IQueryProvider)
│   ├── LastFmQueryable.cs           # IQueryable<T> implementation
│   ├── LastFmExpressionVisitor.cs   # LINQ expression tree visitor
│   ├── QueryTranslator.cs           # Translate descriptors to API calls
│   ├── ResultMapper.cs              # Map API responses to EF entities
│   ├── QueryDescriptor.cs           # Intermediate query representation
│   └── QueryType.cs                 # Query type enumeration
│
├── Extensions/                      # Extension methods
│   └── LastFmDbContextOptionsExtensions.cs  # UseLastFm(), UseLocalFiles()
│
└── Utilities/                       # Utility classes
    └── StringNormalizer.cs          # Apostrophe normalization
```

---

### **Lfm.Spotify** - Spotify Integration
**Purpose**: Spotify Web API integration for playback control
**Target Framework**: .NET 8
**Lines of Code**: ~2,000

```
src/Lfm.Spotify/
├── Lfm.Spotify.csproj               # Project file
├── SpotifyStreamer.cs               # Main Spotify streaming facade
├── IPlaylistStreamer.cs             # Playlist streaming interface
├── Class1.cs                        # (Legacy/unused)
│
├── Services/                        # Spotify services
│   ├── ISpotifyAuthService.cs       # OAuth authentication interface
│   ├── SpotifyAuthService.cs        # OAuth implementation
│   ├── ISpotifyPlaybackService.cs   # Playback interface
│   ├── SpotifyPlaybackService.cs    # Playback implementation
│   ├── ISpotifySearchService.cs     # Search interface
│   └── SpotifySearchService.cs      # Search implementation
│
└── Models/                          # Spotify models
    └── SpotifyModels.cs             # Spotify API response models
```

---

### **Lfm.Sonos** - Sonos Integration
**Purpose**: Sonos HTTP API integration via node-sonos-http-api bridge
**Target Framework**: .NET 8
**Lines of Code**: ~800

```
src/Lfm.Sonos/
├── Lfm.Sonos.csproj                 # Project file
├── SonosStreamer.cs                 # Main Sonos streaming facade
├── ISonosStreamer.cs                # Sonos streaming interface
│
└── Models/                          # Sonos models
    ├── SonosConfig.cs               # Configuration model
    ├── SonosRoom.cs                 # Room model
    ├── SonosPlaybackState.cs        # Playback state model
    └── SkipDirection.cs             # Skip direction enum
```

---

### **Lfm.McpServer** - MCP Server (Phase 2)
**Purpose**: Model Context Protocol server with transformation rules
**Target Framework**: .NET 9
**Lines of Code**: ~400
**Status**: Proof-of-concept (3 tools implemented, 25 remaining)

```
src/Lfm.McpServer/
├── Lfm.McpServer.csproj             # Project file
├── Program.cs                       # MCP server setup and DI
│
├── Services/                        # MCP services
│   └── LastFmMcpClient.cs           # Transformation wrapper
│
└── Tools/                           # MCP tools
    └── LastFmTools.cs               # lfm_artists, lfm_tracks, lfm_albums
```

---

### **Lfm.Schema** - Schema Discovery (Phase 1)
**Purpose**: API schema discovery for API2EF2MCP pipeline
**Target Framework**: .NET 9
**Lines of Code**: ~450
**Status**: Proof-of-concept

```
src/Lfm.Schema/
├── Lfm.Schema.csproj                # Project file
├── ApiModel.cs                      # API schema metadata model
└── LastFmSchemaDiscovery.cs         # Heuristic schema discovery
```

---

### **Lfm.Tests** - Test Suite
**Purpose**: Unit tests, integration tests, and benchmarks
**Target Framework**: .NET 9
**Lines of Code**: ~4,800
**Test Count**: 70+ unit tests, 11 integration tests, 25+ benchmarks

```
src/Lfm.Tests/
├── Lfm.Tests.csproj                 # Project file with BenchmarkDotNet
│
├── Unit/                            # Unit tests
│   ├── Utilities/
│   │   └── StringNormalizerTests.cs # 13 apostrophe normalization tests
│   ├── Services/
│   │   ├── CacheKeyGeneratorTests.cs
│   │   ├── DateRangeParserTests.cs
│   │   └── SymbolProviderTests.cs
│   └── Models/
│       └── ResultPatternTests.cs
│
├── Integration/                     # Integration tests
│   ├── CacheBehaviorTests.cs        # Cache behavior validation
│   ├── CachePerformanceTests.cs     # Cache performance tests
│   ├── FileCacheStorageTests.cs     # File cache tests
│   ├── ErrorHandlingTests.cs        # Error handling tests
│   ├── LastFmMcpClientTests.cs      # MCP transformation tests
│   └── LocalFileDataProviderTests.cs # Local file parsing tests
│
├── Benchmarks/                      # Performance benchmarks
│   ├── StringNormalizationBenchmarks.cs # Apostrophe normalization (25+ benchmarks)
│   └── README.md                    # Benchmark execution guide
│
├── Mocks/                           # Test mocks
│   ├── MockLastFmApiClient.cs       # Mock API client
│   ├── InMemoryCacheStorage.cs      # In-memory cache for testing
│   └── OriginalVsEfTestFixture.cs   # Comparison test fixture
│
├── OriginalVsEfComparisonTests.cs   # 11 comparison tests (Original vs EF)
├── TransformationAccuracyTests.cs   # Transformation rule validation
├── OutputComparisonTests.cs         # Token reduction measurements
├── ExpressionVisitorDebugTests.cs   # LINQ expression visitor tests
└── EfProviderPerformanceBenchmarks.cs # EF provider benchmarks
```

---

## 📂 Documentation (`docs/`)

```
docs/
├── SESSION_HISTORY.md               # Archived session notes (2025-01-16 to 2025-10-06)
├── IMPLEMENTATION_NOTES.md          # Completed implementations reference
├── LESSONS_LEARNED.md               # Debugging patterns and best practices
├── MCP_SETUP.md                     # Claude integration setup
├── TROUBLESHOOTING.md               # Common issues reference
├── PARSING_BUG_ANALYSIS.md          # MCP parser bug investigation
├── Handover.md                      # Phase 2 WIP status
├── LOCAL_FILE_FORMATS.md            # Spotify/YouTube JSON format specs
│
├── Plan.lfm2EF.md                   # lfm2EF implementation plan
├── LFM2EF-CONVERSION.md             # Original conversion plan (500+ lines)
├── LFM2EF-SUMMARY.md                # Overall summary and learnings (470+ lines)
├── PHASE1-COMPLETE.md               # Phase 1 summary (Schema Discovery)
├── PHASE2-PROGRESS.md               # Phase 2 status (historical)
├── PHASE2-COMPLETE.md               # Phase 2 results (MCP Server)
├── PHASE2.5-TEMPLATE-EXTRACTION.md  # Code generation templates
├── TEMPLATE-PATTERNS.md             # Template catalog (465 lines)
├── BLOCKER-RESOLUTION.md            # Root-cause analysis methodology
├── TEST-RESULTS.md                  # Test execution results
├── CODE_REVIEW.md                   # Code quality review
└── SILENT_FAILURES_TRIAGE.md        # Error handling analysis
```

---

## 📂 MCP Server Release (`lfm-mcp-release/`)

**Purpose**: Production MCP server (Node.js/JavaScript)
**Lines of Code**: 2,347 (server.js)
**Tools**: 28 MCP tools for LLM interaction

```
lfm-mcp-release/
├── server.js                        # MCP server implementation (2,347 lines)
├── lfm-guidelines.md                # LLM usage guidelines (480 lines)
├── README.md                        # MCP server documentation
├── package.json                     # Node.js dependencies
└── package-lock.json                # Dependency lock file
```

### MCP Tools (28 total):
- `lfm_artists` - Get top artists
- `lfm_tracks` - Get top tracks
- `lfm_albums` - Get top albums
- `lfm_toptracks` - Top tracks by various metrics
- `lfm_artist_albums` - Albums by artist
- `lfm_artist_tracks` - Tracks by artist
- `lfm_recent` - Recent listening history
- `lfm_current_track` - Currently playing
- `lfm_check` - Check if user listened to track/album
- `lfm_bulk_check` - Batch check multiple items
- `lfm_similar` - Similar artists
- `lfm_recommendations` - Generate recommendations
- `lfm_mixtape` - Generate mixtapes
- `lfm_trackplaylist` - Track-based playlists
- `lfm_create_playlist` - Create Spotify playlist
- `lfm_new_releases` - New releases from top artists
- `lfm_config` - Configuration management
- `lfm_cache_status` - Cache statistics
- `lfm_cache_clear` - Clear cache
- `lfm_play_now` - Play track/album immediately
- `lfm_queue` - Queue track/album
- `lfm_current_playback` - Playback status
- `lfm_pause` - Pause playback
- `lfm_resume` - Resume playback
- `lfm_skip` - Skip tracks
- `lfm_spotify_devices` - List Spotify devices
- `lfm_init` - Initialize configuration
- `lfm_help` - Help and documentation

---

## 📂 Test Data (`test-data/`)

```
test-data/
├── lastfm-responses/                # Mock Last.fm API responses
│   ├── top-artists-response.json
│   ├── top-tracks-response.json
│   └── top-albums-response.json
│
└── local-files/                     # Sample local file exports
    ├── README.md                    # Local file format documentation
    ├── Streaming_History_Audio_2024_0.json  # Spotify Extended Streaming History
    ├── StreamingHistory0.json       # Spotify standard format
    ├── watch-history.json           # YouTube Music history
    └── music-library-songs.csv      # Music library CSV
```

---

## 📂 Transformation Rules (`transformation-rules/`)

```
transformation-rules/
└── lastfm-rules.json                # Declarative transformation rules
                                     # - Token optimization (4 exclusions)
                                     # - Property flattening (3 rules)
                                     # - Navigation inlining (2 rules)
```

---

## 📂 GitHub Workflows (`.github/`)

```
.github/workflows/
└── release.yml                      # Automated release workflow
                                     # - Builds Windows/macOS/Linux binaries
                                     # - Packages MCP server
                                     # - Creates GitHub releases
```

---

## 📂 Claude Configuration (`.claude/`)

```
.claude/
├── settings.local.json              # Claude Code local settings
│                                    # - Tool usage approvals
│                                    # - Custom commands
│                                    # - Session preferences
│
└── scripts/                         # Custom PowerShell scripts
    └── Run-Git.ps1                  # Automated git commit/push workflow
```

---

## 🏗️ Architecture Overview

### Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     CLI Layer (Lfm.Cli)                     │
│  Commands, CommandBuilders, CLI-specific services           │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   Service Layer (Lfm.Core)                  │
│  LastFmService, RecommendationEngine, Cache, Enrichment     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              Data Access Layer (Multi-source)                │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │LastFmApi    │  │LocalFile     │  │EF Core LINQ      │   │
│  │Provider     │  │DataProvider  │  │Provider          │   │
│  └─────────────┘  └──────────────┘  └──────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   External Services                          │
│  Last.fm API, Spotify API, Sonos HTTP API, Local Files      │
└─────────────────────────────────────────────────────────────┘
```

### Project Dependencies

```
Lfm.Cli
  ├── Lfm.Core
  │   ├── Lfm.Shared
  │   └── Lfm.Data.EF
  │       └── Lfm.Shared
  ├── Lfm.Spotify
  │   └── Lfm.Shared
  └── Lfm.Sonos
      └── Lfm.Shared

Lfm.McpServer
  ├── Lfm.Core
  │   ├── Lfm.Shared
  │   └── Lfm.Data.EF
  └── Lfm.Shared

Lfm.Tests
  ├── Lfm.Core
  ├── Lfm.Data.EF
  ├── Lfm.Shared
  └── Lfm.McpServer
```

---

## 📊 Code Statistics

### Lines of Code by Project

| Project | Source Files | Lines of Code | Purpose |
|---------|-------------|---------------|---------|
| **Lfm.Cli** | ~85 | ~5,000 | CLI interface and commands |
| **Lfm.Core** | ~45 | ~8,000 | Core business logic and services |
| **Lfm.Shared** | ~20 | ~1,500 | Shared models and interfaces |
| **Lfm.Data.EF** | ~25 | ~3,500 | EF Core LINQ query provider |
| **Lfm.Spotify** | ~10 | ~2,000 | Spotify integration |
| **Lfm.Sonos** | ~6 | ~800 | Sonos integration |
| **Lfm.McpServer** | ~3 | ~400 | MCP server (C# PoC) |
| **Lfm.Schema** | ~2 | ~450 | Schema discovery |
| **Lfm.Tests** | ~40 | ~4,800 | Tests and benchmarks |
| **lfm-mcp-release** | 1 (JS) | ~2,347 | Production MCP server |
| **Total** | ~237 | **~28,797** | Full codebase |

### File Type Distribution

| Type | Count | Purpose |
|------|-------|---------|
| **.cs** (C# source) | ~180 | Application code |
| **.csproj** (projects) | 8 | .NET project files |
| **.md** (documentation) | ~35 | Documentation |
| **.json** (config/data) | ~15 | Configuration and test data |
| **.js** (MCP server) | 1 | Production MCP server |
| **.yml** (CI/CD) | 1 | GitHub Actions workflow |
| **.ps1/.sh** (scripts) | 3 | Installation scripts |

---

## 🔑 Key Files Reference

### Most Important Files (Top 20)

| File | Lines | Purpose |
|------|-------|---------|
| **lfm-mcp-release/server.js** | 2,347 | Production MCP server with 28 tools |
| **src/Lfm.Core/Services/LastFmApiClient.cs** | ~800 | Last.fm API HTTP client |
| **src/Lfm.Core/Services/CachedLastFmApiClient.cs** | ~500 | Caching decorator (119x perf improvement) |
| **src/Lfm.Data.EF/Provider/LastFmExpressionVisitor.cs** | ~350 | LINQ expression tree analyzer |
| **src/Lfm.Data.EF/Provider/ResultMapper.cs** | ~300 | API response → EF entity mapping |
| **src/Lfm.Cli/Program.cs** | ~400 | Application entry point and DI setup |
| **src/Lfm.Cli/Commands/CheckCommand.cs** | ~600 | Check if user listened to track/album |
| **src/Lfm.Cli/Commands/PlayCommand.cs** | ~500 | Unified play command (Spotify/Sonos) |
| **src/Lfm.Core/Configuration/LfmConfig.cs** | ~250 | Configuration model |
| **src/Lfm.Core/Services/RecommendationEngine.cs** | ~400 | Music recommendation algorithm |
| **src/Lfm.Spotify/SpotifyStreamer.cs** | ~800 | Spotify integration facade |
| **src/Lfm.Sonos/SonosStreamer.cs** | ~400 | Sonos integration facade |
| **src/Lfm.Data.EF/LfmDbContext.cs** | ~115 | EF Core DbContext |
| **src/Lfm.Shared/Services/IMusicDataProvider.cs** | ~55 | Multi-source abstraction (15 methods) |
| **src/Lfm.Tests/OriginalVsEfComparisonTests.cs** | ~400 | Original vs EF Core comparison (11 tests) |
| **docs/LFM2EF-SUMMARY.md** | 470 | lfm2EF branch summary and learnings |
| **docs/TEMPLATE-PATTERNS.md** | 465 | Code generation template catalog |
| **lfm-mcp-release/lfm-guidelines.md** | 480 | LLM usage guidelines for MCP |
| **CLAUDE.md** | ~1,500 | Claude session notes and project history |
| **Architecture.LFM.md** | ~2,000 | Architecture documentation |

---

## 🧪 Testing Infrastructure

### Test Categories

| Category | Count | Files |
|----------|-------|-------|
| **Unit Tests** | 70+ | StringNormalizerTests, CacheKeyGeneratorTests, etc. |
| **Integration Tests** | 11 | OriginalVsEfComparisonTests (primary) |
| **Benchmarks** | 25+ | StringNormalizationBenchmarks |
| **Mock Objects** | 3 | MockLastFmApiClient, InMemoryCacheStorage |

### Test Coverage Areas

- ✅ String normalization (apostrophe handling)
- ✅ Cache behavior and performance
- ✅ LINQ expression visitor (query translation)
- ✅ Result mapping (API → EF entities)
- ✅ Local file parsing (Spotify, YouTube Music)
- ✅ Date range parsing
- ✅ Symbol provider (Unicode/ASCII)
- ✅ Original vs EF Core comparison (11 methods)
- ✅ Transformation accuracy (token reduction)
- ✅ MCP transformation layer
- ✅ Error handling
- ✅ Result pattern

---

## 🚀 Build Artifacts

### Output Directories (Not in Git)

```
bin/                                 # Build output (gitignored)
obj/                                 # Intermediate build files (gitignored)
publish/                             # Published binaries (gitignored)
  ├── win-x64/                       # Windows x64 build
  │   └── lfm.exe
  ├── linux-x64/                     # Linux x64 build
  │   └── lfm
  ├── osx-x64/                       # macOS Intel build
  │   └── lfm
  └── osx-arm64/                     # macOS ARM build
      └── lfm
```

---

## 📦 Dependencies

### NuGet Packages (Key Dependencies)

| Package | Version | Used By | Purpose |
|---------|---------|---------|---------|
| **System.CommandLine** | Latest | Lfm.Cli | CLI framework |
| **Microsoft.Extensions.DependencyInjection** | 9.0.0 | All | DI container |
| **Microsoft.Extensions.Logging** | 9.0.0 | All | Logging |
| **Microsoft.Extensions.Http** | 9.0.0 | Lfm.Core | HTTP client factory |
| **Microsoft.EntityFrameworkCore** | 9.0.0 | Lfm.Data.EF | EF Core |
| **xunit.v3** | 3.1.0 | Lfm.Tests | Unit testing |
| **BenchmarkDotNet** | 0.15.4 | Lfm.Tests | Performance benchmarks |
| **FluentAssertions** | 7.0.0 | Lfm.Tests | Assertion library |
| **Moq** | 4.20.72 | Lfm.Tests | Mocking framework |
| **ModelContextProtocol** | 0.4.0-preview.3 | Lfm.McpServer | MCP SDK |

### Node.js Packages (lfm-mcp-release)

- **@modelcontextprotocol/sdk** - MCP protocol implementation
- **axios** - HTTP client for Last.fm API

---

## 🔄 Development Workflow

### Build Commands

```bash
# Development build
dotnet build -c Release

# Publish Windows
dotnet publish src/Lfm.Cli -c Release -r win-x64 -o publish/win-x64 --self-contained false

# Publish Linux
dotnet publish src/Lfm.Cli -c Release -r linux-x64 -o publish/linux-x64 --self-contained false

# Run tests
dotnet test src/Lfm.Tests -c Release

# Run benchmarks
dotnet test src/Lfm.Tests -c Release --filter "DisplayName~StringNormalizationBenchmarks"
```

### Configuration Locations

- **Windows**: `%APPDATA%\lfm\lfm.json`
- **Linux/macOS**: `~/.lfm/lfm.json`
- **Cache**: `%APPDATA%\lfm\cache\` or `~/.lfm/cache/`

---

## 🌟 Notable Patterns

### Design Patterns Used

1. **Decorator Pattern** - `CachedLastFmApiClient` wraps `LastFmApiClient`
2. **Provider Pattern** - `IMusicDataProvider` abstraction
3. **Circuit Breaker** - `CircuitBreaker` for API resilience
4. **Result Pattern** - `Result<T>` for functional error handling
5. **Custom Query Provider** - EF Core LINQ provider
6. **Strategy Pattern** - Multiple `IAlbumEnricher` implementations
7. **Builder Pattern** - System.CommandLine command builders
8. **Factory Pattern** - Logger factory, cache key generator

### Key Abstractions

1. **IMusicDataProvider** - Multi-source data abstraction (Last.fm API, local files, merged)
2. **ICacheStorage** - Storage abstraction (file-based, in-memory for tests)
3. **IQueryProvider** - Custom LINQ query provider for Last.fm API
4. **Result<T>** - Functional error handling without exceptions

---

## 📝 Notes

### Branch Structure

- **master** - Production branch (stable releases)
- **lfm2EF** - Development branch (EF Core LINQ provider, multi-source support)

### Configuration Files (Gitignored)

- `*.lfm.config` - User configuration files
- `lfm.json` - Configuration JSON (stored in AppData, not repo)
- `.vs/`, `.vscode/`, `.idea/` - IDE settings
- `bin/`, `obj/`, `publish/` - Build artifacts

### Documentation Strategy

- **CLAUDE.md** - Session notes, current work, recent sessions
- **docs/SESSION_HISTORY.md** - Archived sessions (2025-01-16 to 2025-10-06)
- **docs/IMPLEMENTATION_NOTES.md** - Completed implementations reference
- **docs/LESSONS_LEARNED.md** - Debugging patterns and best practices
- **Architecture.LFM.md** - Architecture documentation
- **Report.lfm2ef.md** - Branch analysis (master vs lfm2EF)

---

**Structure Map Generated**: 2025-01-26
**Total Projects**: 8 (.NET) + 1 (Node.js MCP server)
**Total Source Files**: ~250 C# files
**Total Lines of Code**: ~28,797
**Build Status**: ✅ Clean (0 errors)
**Test Status**: ✅ All passing (70+ unit, 11 integration)
