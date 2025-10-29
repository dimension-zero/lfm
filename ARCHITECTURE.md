# lfm Architecture

**Document Version**: 1.1
**Last Updated**: 2025-01-29
**Purpose**: Comprehensive architectural overview of the lfm system

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Assembly Architecture](#assembly-architecture)
3. [Dual Implementation Pattern](#dual-implementation-pattern)
4. [CLI Architecture](#cli-architecture)
5. [MCP Server Integration](#mcp-server-integration)
6. [LLM Interaction Model](#llm-interaction-model)
7. [Data Flow Patterns](#data-flow-patterns)
8. [Cross-Cutting Concerns](#cross-cutting-concerns)

---

## System Overview

### Purpose

**lfm** is a Last.fm CLI tool and MCP server that provides:
1. **Human Interface**: Command-line tool for querying Last.fm music statistics
2. **LLM Interface**: Model Context Protocol (MCP) server for natural language interaction via Claude
3. **Dual Implementation**: Two parallel architectures for data access (Original hand-built + EF Core LINQ provider)

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        User Interfaces                           │
│  ┌──────────────────┐              ┌──────────────────────┐     │
│  │  CLI (Human)     │              │  MCP Server (LLM)    │     │
│  │  System.Command  │              │  Claude/ChatGPT      │     │
│  │  Line            │              │  Integration         │     │
│  └──────────────────┘              └──────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Data Access Layer (Dual)                      │
│  ┌──────────────────────────┐  ┌──────────────────────────┐    │
│  │  Original Implementation │  │  EF Core Implementation  │    │
│  │  (Direct API Calls)      │  │  (LINQ Provider)         │    │
│  └──────────────────────────┘  └──────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Shared Infrastructure                          │
│  Cache (119x speedup) • Circuit Breaker • Configuration         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                   External APIs                                  │
│  Last.fm API • Spotify Web API • Sonos HTTP API                 │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Principles

1. **Separation of Concerns**: Clear boundaries between CLI, data access, and external services
2. **Dual Implementation**: Two parallel data access strategies for comparison and optimization
3. **Decorator Pattern**: Caching wraps API client transparently
4. **Result Pattern**: Functional error handling without exceptions
5. **Dependency Injection**: Constructor injection throughout
6. **Interface Segregation**: Small, focused interfaces
7. **MCP Protocol**: Standard protocol for LLM tool integration

---

## Assembly Architecture

### Project Dependency Graph

```
┌────────────────────────────────────────────────────────────────┐
│                         Lfm.Cli                                │
│                    (Entry Point - .NET 8)                      │
│  Commands • CommandBuilders • CLI-specific services           │
└────────────────────────────────────────────────────────────────┘
     │                    │                    │
     │                    │                    │
     ↓                    ↓                    ↓
┌──────────┐      ┌────────────┐      ┌──────────────┐
│Lfm.Core  │      │Lfm.Spotify │      │Lfm.Sonos     │
│(.NET 8)  │      │(.NET 8)    │      │(.NET 8)      │
└──────────┘      └────────────┘      └──────────────┘
     │                    │                    │
     │                    └────────┬───────────┘
     │                             │
     ↓                             ↓
┌──────────────────────────────────────────┐
│            Lfm.Shared                    │
│            (.NET 8)                      │
│  Models • Interfaces • Utilities         │
└──────────────────────────────────────────┘
     ↑                             ↑
     │                             │
┌────────────────┐      ┌────────────────────┐
│Lfm.Data.Direct│      │Lfm.Data.EF         │
│(.NET 8)        │      │(.NET 9)            │
│API Client +    │      │EF Core Provider    │
│Cache + Local   │      │                    │
│Files           │      │                    │
└────────────────┘      └────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                    Lfm.McpServer (C# PoC)                      │
│                         (.NET 9)                                │
│  Tools • Services • MCP Protocol Implementation                │
└────────────────────────────────────────────────────────────────┘
     │
     └──→ Lfm.Core, Lfm.Shared, Lfm.Data.Direct, Lfm.Data.EF

┌────────────────────────────────────────────────────────────────┐
│                lfm-mcp-release (Production)                    │
│                    (Node.js 16+)                               │
│  server.js • 28 MCP Tools • lfm-guidelines.md                 │
└────────────────────────────────────────────────────────────────┘
     │
     └──→ Spawns Lfm.Cli processes (IPC via stdin/stdout)
```

### Assembly Descriptions

#### **Lfm.Cli** - Command Line Interface
- **Purpose**: Entry point for human users
- **Technology**: .NET 8, System.CommandLine framework
- **Responsibilities**:
  - Parse command-line arguments
  - Execute commands via dependency injection
  - Format console output (Unicode/ASCII support)
  - Handle user configuration
- **Key Files**:
  - `Program.cs` (DI container setup, 100 lines)
  - `Commands/` (28 command implementations)
  - `CommandBuilders/` (System.CommandLine builders)
- **Lines of Code**: ~5,000

#### **Lfm.Core** - Core Business Logic
- **Purpose**: Core business logic and services (no data provider implementation)
- **Technology**: .NET 8
- **Responsibilities**:
  - Configuration management
  - Result<T> pattern for error handling
  - Recommendation engine
  - Display services
  - Tag filtering
  - Service orchestration (LastFmService)
- **Key Components**:
  - `LastFmService` - Orchestrates data providers and business logic
  - `RecommendationEngine` - Music recommendation algorithm
  - `TagFilterService` - Genre/tag filtering logic
  - `DisplayService` - Console output formatting
  - `ConfigurationManager` - Config file management
- **Lines of Code**: ~4,000

#### **Lfm.Shared** - Shared Models and Abstractions
- **Purpose**: Cross-cutting models, interfaces, and utilities
- **Technology**: .NET 8
- **Responsibilities**:
  - Data models (Artist, Track, Album, etc.)
  - Interfaces (`ILastFmApiClient`, `IMusicDataProvider`, etc.)
  - Result<T> pattern types
  - Utility classes (DateRangeParser, StringNormalizer)
  - Configuration models
  - Local file format models (Spotify, YouTube Music)
- **Key Interfaces**:
  - `ILastFmApiClient` - Last.fm API client contract
  - `IMusicDataProvider` - Multi-source data abstraction (15 methods)
- **Lines of Code**: ~2,000

#### **Lfm.Data.Direct** - Direct API Implementation
- **Purpose**: Direct Last.fm API implementation with caching and local file support
- **Technology**: .NET 8
- **Responsibilities**:
  - Last.fm API HTTP client
  - File-based caching (119x performance improvement)
  - Circuit breaker pattern
  - Local file parsing (Spotify, YouTube Music)
  - Album enrichment services
  - Data provider implementations
- **Key Components**:
  - `LastFmApiClient` - HTTP client for Last.fm API
  - `CachedLastFmApiClient` - Decorator with file-based caching
  - `CircuitBreaker` - Resilience pattern (3-state FSM)
  - `LastFmDataProvider` - IMusicDataProvider implementation for API
  - `LocalFileDataProvider` - Parse Spotify/YouTube JSON exports
  - `MergedDataProvider` - Combines API and local file sources
- **Lines of Code**: ~6,000

#### **Lfm.Data.EF** - EF Core LINQ Provider
- **Purpose**: LINQ query provider for Last.fm API (experimental)
- **Technology**: .NET 9, Entity Framework Core 9.0
- **Responsibilities**:
  - Translate LINQ queries to Last.fm API calls
  - Map API responses to EF entities
  - Provide `DbContext` with `DbSet<Artist>`, `DbSet<Track>`, etc.
  - Enable composable, type-safe queries
- **Key Components**:
  - `LfmDbContext` - DbContext with Artists/Tracks/Albums/RecentTracks
  - `LastFmQueryProvider` - Implements `IAsyncQueryProvider`
  - `LastFmExpressionVisitor` - LINQ expression tree analyzer
  - `ResultMapper` - API response → EF entity mapping
  - `StringNormalizer` - Apostrophe normalization utility
- **Lines of Code**: ~3,500
- **Status**: Proof-of-concept with 11 passing comparison tests

#### **Lfm.Spotify** - Spotify Integration
- **Purpose**: Spotify Web API integration for playback control
- **Technology**: .NET 8
- **Responsibilities**:
  - OAuth authentication
  - Search for tracks/albums
  - Playback control (play, pause, skip)
  - Device management
  - Playlist creation
- **Key Services**:
  - `SpotifyAuthService` - OAuth token management
  - `SpotifySearchService` - Track/album search
  - `SpotifyPlaybackService` - Playback control
- **Lines of Code**: ~2,000

#### **Lfm.Sonos** - Sonos Integration
- **Purpose**: Sonos HTTP API integration via node-sonos-http-api bridge
- **Technology**: .NET 8
- **Responsibilities**:
  - Room discovery and management
  - Playback control on Sonos speakers
  - HTTP bridge communication
- **Key Components**:
  - `SonosStreamer` - Main facade
  - `SonosRoom` - Room model
  - HTTP client for node-sonos-http-api
- **Lines of Code**: ~800

#### **Lfm.McpServer** - MCP Server (C# Proof-of-Concept)
- **Purpose**: Experimental C# MCP server with transformation rules
- **Technology**: .NET 9, ModelContextProtocol SDK
- **Responsibilities**:
  - MCP protocol implementation
  - Direct in-process API calls (no CLI spawning)
  - Data transformation (token optimization)
- **Key Components**:
  - `LastFmMcpClient` - Transformation wrapper
  - `LastFmTools` - 3 MCP tools (artists, tracks, albums)
- **Lines of Code**: ~400
- **Status**: Proof-of-concept (3 tools vs 28 in production)

#### **lfm-mcp-release** - Production MCP Server (Node.js)
- **Purpose**: Production MCP server for Claude integration
- **Technology**: Node.js 16+, @modelcontextprotocol/sdk
- **Responsibilities**:
  - Spawn Lfm.Cli child processes
  - 28 MCP tools for LLM interaction
  - Token optimization (50% reduction via compact functions)
  - Session initialization with guidelines
- **Key Files**:
  - `server.js` (2,347 lines) - MCP server implementation
  - `lfm-guidelines.md` (480 lines) - LLM usage guidelines
- **Lines of Code**: ~2,347 (JavaScript)
- **Status**: Production-ready

#### **Lfm.Tests** - Test Suite
- **Purpose**: Comprehensive testing
- **Technology**: .NET 9, xUnit 3, BenchmarkDotNet
- **Test Types**:
  - Unit tests (70+)
  - Integration tests (11 Original vs EF comparison)
  - Benchmarks (25+ performance tests)
- **Lines of Code**: ~4,800

---

## Dual Implementation Pattern

### Overview

lfm maintains two parallel data access implementations in the **lfm2EF branch**:

1. **Original Implementation** - Direct API client calls (production)
2. **EF Core Implementation** - LINQ provider over API (experimental)

Both implementations:
- Share the same `CachedLastFmApiClient` (caching and throttling)
- Use identical Result<T> error handling
- Return compatible data structures
- Support the same feature set

### Original Implementation (Direct API)

#### Architecture

```
Command
  ↓
ILastFmService
  ↓
ILastFmApiClient (CachedLastFmApiClient)
  ↓
LastFmApiClient (HTTP client)
  ↓
Last.fm API
```

#### Characteristics

- **Imperative API calls**: Direct method calls with explicit parameters
- **No abstraction layer**: Commands call API client directly
- **Method signatures**: `GetTopArtistsAsync(user, period, limit, page)`
- **Error handling**: Nullable Task returns + Result<T> pattern
- **Performance**: Baseline (API call + cache lookup)
- **Usage**: Production (CLI commands, MCP server)

#### Code Example

```csharp
// ArtistsCommand.cs
public async Task ExecuteAsync(string user, int limit, LastFmPeriod period)
{
    // Direct API client call
    var result = await _apiClient.GetTopArtistsAsync(user, period, limit, page: 1);

    if (result?.Artists == null || !result.Artists.Any())
    {
        _displayService.DisplayError("No artists found");
        return;
    }

    // Work with result.Artists (List<Artist>)
    _displayService.DisplayArtists(result.Artists);
}
```

### EF Core Implementation (LINQ Provider)

#### Architecture

```
Query
  ↓
LfmDbContext.Artists (IQueryable<Artist>)
  ↓
LastFmQueryProvider (LINQ expression analysis)
  ↓
LastFmExpressionVisitor (parse expression tree)
  ↓
QueryDescriptor (intermediate representation)
  ↓
QueryTranslator (descriptor → API parameters)
  ↓
ILastFmApiClient (CachedLastFmApiClient) [SHARED]
  ↓
LastFmApiClient (HTTP client) [SHARED]
  ↓
Last.fm API
  ↓
ResultMapper (API response → EF entities)
  ↓
List<Artist>
```

#### Characteristics

- **Declarative LINQ queries**: Composable, type-safe expressions
- **Expression tree analysis**: LINQ → API call translation
- **Standard LINQ syntax**: Familiar patterns (Where, Take, OrderBy)
- **Compile-time validation**: Type errors caught at build time
- **Performance**: Adds <10% overhead for expression parsing
- **Usage**: Experimental (3 MCP tools in C# PoC)

#### Code Example

```csharp
// Using LfmDbContext
public async Task ExecuteAsync(string user, int limit, LastFmPeriod period)
{
    // LINQ query
    var artists = await _context.Artists
        .Where(a => a.User == user && a.Period == period)
        .OrderByDescending(a => a.PlayCount)
        .Take(limit)
        .ToListAsync();

    if (!artists.Any())
    {
        _displayService.DisplayError("No artists found");
        return;
    }

    // Work with artists (List<Artist>)
    _displayService.DisplayArtists(artists);
}
```

### Comparison Matrix

| Aspect | Original (Direct API) | EF Core (LINQ Provider) |
|--------|----------------------|-------------------------|
| **Query Style** | Imperative method calls | Declarative LINQ expressions |
| **Type Safety** | Runtime parameter validation | Compile-time query validation |
| **Composability** | Fixed method signatures | Build queries incrementally |
| **Performance** | Baseline (API + cache) | +<10% expression parsing overhead |
| **Complexity** | Simple, direct | Additional abstraction layer |
| **Learning Curve** | Custom API knowledge | Standard LINQ knowledge |
| **Testability** | Mock API client methods | In-memory DbContext |
| **Navigation Properties** | Manual loading | Automatic with Include() |
| **Status** | Production (28 CLI commands) | Experimental (3 MCP tools) |
| **Code Example** | `GetTopArtistsAsync(user, period, limit, page)` | `Artists.Where(a => a.User == user).Take(limit)` |

### Translation Examples

#### Example 1: Top 10 Artists

**Original**:
```csharp
var result = await _apiClient.GetTopArtistsAsync("smarshal", LastFmPeriod.Overall, 10, 1);
```

**EF Core**:
```csharp
var artists = await _context.Artists
    .Where(a => a.User == "smarshal")
    .Take(10)
    .ToListAsync();
```

**API Call**: Both → `user.getTopArtists(user=smarshal, limit=10, page=1)`

#### Example 2: Artist's Top Tracks

**Original**:
```csharp
var tracks = await _apiClient.GetArtistTopTracksAsync("Pink Floyd", 5);
```

**EF Core**:
```csharp
var tracks = await _context.Tracks
    .Where(t => t.ArtistName == "Pink Floyd")
    .Take(5)
    .ToListAsync();
```

**API Call**: Both → `artist.getTopTracks(artist="Pink Floyd", limit=5)`

#### Example 3: Pagination

**Original**:
```csharp
var page2 = await _apiClient.GetTopArtistsAsync(user, period, 50, page: 2);
```

**EF Core**:
```csharp
var page2 = await _context.Artists
    .Where(a => a.User == user)
    .Skip(50)   // Automatically calculates page=2
    .Take(50)
    .ToListAsync();
```

**API Call**: Both → `user.getTopArtists(user=..., limit=50, page=2)`

### Shared Infrastructure

Both implementations use identical infrastructure:

1. **CachedLastFmApiClient** - 119x speedup from file-based caching
2. **CircuitBreaker** - 3-state FSM for API resilience
3. **Configuration** - Same LfmConfig model
4. **Result<T> Pattern** - Functional error handling
5. **Throttling** - 200ms default API delay

### Design Decision: Why Two Implementations?

1. **Comparison Testing**: Validate LINQ provider correctness against production API client
2. **Performance Benchmarking**: Measure LINQ translation overhead (<10% validated)
3. **Future Flexibility**: Option to migrate MCP tools to LINQ for composability
4. **Learning**: Demonstrate custom EF Core query provider implementation
5. **A/B Testing**: Compare developer experience between imperative and declarative

### Migration Path

Commands can use either implementation via dependency injection:

```csharp
// Option 1: Original (production)
services.AddSingleton<ILastFmApiClient, CachedLastFmApiClient>();

// Option 2: EF Core (experimental)
services.AddDbContext<LfmDbContext>(options =>
    options.UseLastFm(apiClient, defaultUser));
```

---

## CLI Architecture

### Command Execution Flow

```
User Input (bash/powershell)
  ↓
System.CommandLine Parser
  ↓
CommandBuilder (parse options, validate args)
  ↓
Command.ExecuteAsync() [DI injected dependencies]
  ↓
ILastFmService / ILastFmApiClient
  ↓
CachedLastFmApiClient (decorator)
  ├─→ Cache Hit → Return cached data (< 1ms)
  └─→ Cache Miss → LastFmApiClient → Last.fm API (200-500ms)
  ↓
DisplayService (format output, Unicode/ASCII)
  ↓
Console Output
```

### Dependency Injection Setup

**Program.cs** (Lfm.Cli):

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // HTTP client factory
        services.AddHttpClient();

        // Configuration
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();

        // Cache infrastructure
        services.AddSingleton<ICacheStorage, FileCacheStorage>();
        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();

        // Last.fm API client (inner)
        services.AddSingleton<LastFmApiClient>(provider => {
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = provider.GetRequiredService<ILogger<LastFmApiClient>>();
            var config = provider.GetRequiredService<IConfigurationManager>().LoadAsync().Result;

            return new LastFmApiClient(httpClient, logger, config.ApiKey, config.ApiThrottleMs);
        });

        // Cached wrapper (decorator pattern)
        services.AddSingleton<ILastFmApiClient>(provider => {
            var innerClient = provider.GetRequiredService<LastFmApiClient>();
            var cache = provider.GetRequiredService<ICacheStorage>();
            var keyGen = provider.GetRequiredService<ICacheKeyGenerator>();
            var logger = provider.GetRequiredService<ILogger<CachedLastFmApiClient>>();
            var configMgr = provider.GetRequiredService<IConfigurationManager>();

            return new CachedLastFmApiClient(innerClient, cache, keyGen, logger, configMgr, 10);
        });

        // Commands registered as scoped (one per invocation)
        services.AddScoped<ArtistsCommand>();
        services.AddScoped<TracksCommand>();
        // ... 26 more commands
    });
```

### Command Pattern

**BaseCommand.cs** - Abstract base for all commands:

```csharp
public abstract class BaseCommand
{
    protected readonly ILastFmApiClient _apiClient;
    protected readonly IConfigurationManager _configManager;
    protected readonly DisplayService _displayService;

    protected BaseCommand(
        ILastFmApiClient apiClient,
        IConfigurationManager configManager,
        DisplayService displayService)
    {
        _apiClient = apiClient;
        _configManager = configManager;
        _displayService = displayService;
    }

    // Shared functionality
    protected async Task<string> GetUsernameAsync(string? providedUsername)
    {
        if (!string.IsNullOrEmpty(providedUsername))
            return providedUsername;

        var config = await _configManager.LoadAsync();
        return config.DefaultUsername;
    }

    protected async Task<bool> ValidateApiKeyAsync()
    {
        var config = await _configManager.LoadAsync();

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            _displayService.DisplayError("API key not configured");
            return false;
        }

        return true;
    }

    // Abstract method each command implements
    public abstract Task ExecuteAsync(...);
}
```

**Concrete Command Example**:

```csharp
public class ArtistsCommand : BaseCommand
{
    public ArtistsCommand(
        ILastFmApiClient apiClient,
        IConfigurationManager configManager,
        DisplayService displayService)
        : base(apiClient, configManager, displayService)
    {
    }

    public async Task ExecuteAsync(
        int limit,
        string? period,
        string? username,
        string? from,
        string? to)
    {
        // Validate API key
        if (!await ValidateApiKeyAsync())
            return;

        // Resolve username
        var user = await GetUsernameAsync(username);

        // Execute query
        var result = await _apiClient.GetTopArtistsAsync(
            user,
            ParsePeriod(period),
            limit,
            page: 1);

        // Handle errors
        if (result?.Artists == null || !result.Artists.Any())
        {
            _displayService.DisplayError("No artists found");
            return;
        }

        // Display results
        _displayService.DisplayArtists(result.Artists);
    }
}
```

### CLI to Data Access Interaction

```
┌────────────────────────────────────────────────────────────────┐
│                      CLI Layer (Lfm.Cli)                       │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ Command (e.g., ArtistsCommand)                       │     │
│  │  - Validate input                                    │     │
│  │  - Resolve username from config                      │     │
│  │  - Call _apiClient.GetTopArtistsAsync()             │     │
│  │  - Format and display results                        │     │
│  └──────────────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│                Data Access Layer (Two Options)                  │
│                                                                 │
│  Option 1: Original (Direct API)          Option 2: EF Core    │
│  ┌─────────────────────────┐          ┌──────────────────┐    │
│  │ ILastFmApiClient        │          │ LfmDbContext     │    │
│  │   ↓                     │          │   ↓              │    │
│  │ CachedLastFmApiClient   │          │ Artists.Where()  │    │
│  │   ↓                     │          │   ↓              │    │
│  │ LastFmApiClient         │          │ QueryProvider    │    │
│  │   ↓                     │          │   ↓              │    │
│  │ Last.fm API             │          │ ILastFmApiClient │──┐ │
│  └─────────────────────────┘          └──────────────────┘  │ │
│                                                              │ │
│                                                              ↓ │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │         Shared: CachedLastFmApiClient                    │  │
│  │         (File-based cache, 119x speedup)                 │  │
│  └─────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│                      External API                               │
│                   Last.fm Web Service                           │
└────────────────────────────────────────────────────────────────┘
```

### Command Categories

| Category | Commands | Purpose |
|----------|----------|---------|
| **Query** | artists, tracks, albums, toptracks, recent | Fetch music statistics |
| **Search** | artist-tracks, artist-albums, similar | Artist-specific queries |
| **Check** | check | Verify listening history |
| **Recommendations** | recommendations, mixtape | Generate suggestions |
| **Playback** | play, pause, resume, skip, current | Control Spotify/Sonos |
| **Playlist** | create-playlist | Create Spotify playlists |
| **Configuration** | config | Manage settings |
| **Cache** | cache-status, cache-clear, test-cache, benchmark-cache | Cache management |
| **Diagnostics** | api-status | API health check |

---

## MCP Server Integration

### MCP Protocol Overview

**Model Context Protocol (MCP)** is a standard protocol for connecting LLMs to external tools and data sources. It defines:
- **Tools**: Functions LLMs can call (similar to OpenAI function calling)
- **Resources**: Data sources LLMs can read
- **Prompts**: Templated prompts with parameters
- **Stdio Transport**: Communication via standard input/output

### Architecture: Production MCP Server (Node.js)

```
┌────────────────────────────────────────────────────────────────┐
│                   Claude / ChatGPT (LLM)                        │
│                                                                 │
│  "What are my top 5 artists from 2023?"                        │
└────────────────────────────────────────────────────────────────┘
                              ↓
                         MCP Protocol
                      (JSON-RPC over stdio)
                              ↓
┌────────────────────────────────────────────────────────────────┐
│              lfm-mcp-release/server.js (Node.js)               │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ MCP Tools (28 total):                                │     │
│  │  - lfm_init (session initialization)                 │     │
│  │  - lfm_artists, lfm_tracks, lfm_albums              │     │
│  │  - lfm_recommendations, lfm_similar                  │     │
│  │  - lfm_check, lfm_bulk_check                        │     │
│  │  - lfm_play_now, lfm_queue                          │     │
│  │  - ... 19 more tools                                 │     │
│  └──────────────────────────────────────────────────────┘     │
│                              ↓                                  │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ Token Optimization:                                   │     │
│  │  - compactArtist() - strips Url, Mbid (~50% tokens) │     │
│  │  - compactTrack() - strips Url, Mbid                │     │
│  │  - compactAlbum() - strips Url, Mbid                │     │
│  └──────────────────────────────────────────────────────┘     │
│                              ↓                                  │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ Child Process Execution:                             │     │
│  │  exec("lfm artists --limit 5 --from 2023-01-01")    │     │
│  │  Parse stdout JSON response                          │     │
│  │  Apply compact transformations                       │     │
│  │  Return to LLM                                       │     │
│  └──────────────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────────────┘
                              ↓
                        Spawn Process
                              ↓
┌────────────────────────────────────────────────────────────────┐
│                    Lfm.Cli (Child Process)                      │
│                                                                 │
│  CLI Command → ILastFmApiClient → Cache → Last.fm API         │
│  Return JSON to stdout                                         │
└────────────────────────────────────────────────────────────────┘
```

### MCP Tool Example

**lfm_artists Tool**:

```javascript
// server.js (Node.js MCP Server)
server.tool(
  "lfm_artists",
  "Get top artists from Last.fm with optional time period or date range filtering",
  {
    limit: z.number().optional().default(10),
    period: z.enum(["overall", "7day", "1month", "3month", "6month", "12month"]).optional(),
    from: z.string().optional(),
    to: z.string().optional(),
    year: z.number().optional()
  },
  async ({ limit, period, from, to, year }) => {
    // Build CLI command
    let cmd = `lfm artists --json --limit ${limit}`;

    if (period) cmd += ` --period ${period}`;
    if (from) cmd += ` --from ${from}`;
    if (to) cmd += ` --to ${to}`;
    if (year) cmd += ` --year ${year}`;

    // Execute CLI as child process
    const result = await execAsync(cmd);
    const data = JSON.parse(result.stdout);

    // Apply token optimization
    const compact = data.artists.map(compactArtist);

    return {
      content: [{
        type: "text",
        text: JSON.stringify({ artists: compact }, null, 2)
      }]
    };
  }
);

// Token optimization helper
function compactArtist(artist) {
  return {
    name: artist.name,
    playcount: artist.playcount,
    rank: artist.rank
    // Url and Mbid stripped → ~50% token reduction
  };
}
```

### Architecture: Experimental C# MCP Server

```
┌────────────────────────────────────────────────────────────────┐
│                   Claude / ChatGPT (LLM)                        │
└────────────────────────────────────────────────────────────────┘
                              ↓
                         MCP Protocol
                      (JSON-RPC over stdio)
                              ↓
┌────────────────────────────────────────────────────────────────┐
│              Lfm.McpServer (C# .NET 9)                         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ MCP Tools (3 implemented):                           │     │
│  │  - lfm_artists                                       │     │
│  │  - lfm_tracks                                        │     │
│  │  - lfm_albums                                        │     │
│  └──────────────────────────────────────────────────────┘     │
│                              ↓                                  │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ LastFmMcpClient (transformation wrapper):           │     │
│  │  - Calls ILastFmApiClient directly (in-process)     │     │
│  │  - Maps API response → CompactArtist/Track/Album    │     │
│  │  - No child process spawn overhead                   │     │
│  └──────────────────────────────────────────────────────┘     │
│                              ↓                                  │
│  ┌──────────────────────────────────────────────────────┐     │
│  │ Option 1: Original API                              │     │
│  │  ILastFmApiClient → CachedLastFmApiClient           │     │
│  │                                                       │     │
│  │ Option 2: EF Core LINQ                              │     │
│  │  LfmDbContext.Artists.Where(...).ToListAsync()      │     │
│  └──────────────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────────────┘
                              ↓
                      (No IPC overhead)
                              ↓
┌────────────────────────────────────────────────────────────────┐
│                Shared Infrastructure (Lfm.Core)                │
│         Cache → Last.fm API (same as CLI and Node MCP)        │
└────────────────────────────────────────────────────────────────┘
```

### MCP Server Comparison

| Aspect | Production (Node.js) | Experimental (C#) |
|--------|---------------------|-------------------|
| **Language** | JavaScript (Node.js) | C# (.NET 9) |
| **Tool Count** | 28 tools (complete) | 3 tools (PoC) |
| **Communication** | Spawn CLI child processes | Direct in-process calls |
| **Process Overhead** | ~50-100ms per spawn | ~0ms (in-process) |
| **Token Optimization** | compactArtist/Track/Album | CompactArtist/Track/Album models |
| **Data Access** | CLI stdout JSON parsing | ILastFmApiClient or EF LINQ |
| **Cache Sharing** | Shared via file system | Same cache instance |
| **Status** | Production-ready | Proof-of-concept |
| **Guidelines** | lfm-guidelines.md (480 lines) | Not yet implemented |

### Session Initialization Flow

```
┌────────────────────────────────────────────────────────────────┐
│  User: "Initialize my Last.fm session"                         │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│  Claude Code recognizes initialization request                 │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│  MCP Call: lfm_init()                                          │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│  MCP Server loads lfm-guidelines.md (480 lines)                │
│  Returns guidelines to LLM as context                          │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│  Guidelines include:                                           │
│   - Response style ("Be a DJ buddy, not a data analyst")      │
│   - Metadata interpretation rules                             │
│   - Tool selection best practices                             │
│   - User music preferences                                    │
│   - Track position hallucination prevention                   │
│   - Playback state awareness                                  │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌────────────────────────────────────────────────────────────────┐
│  Claude Code: "Session initialized. Ready for music queries!"  │
└────────────────────────────────────────────────────────────────┘
```

---

## LLM Interaction Model

### Purpose of LLM Integration

lfm integrates with Large Language Models (Claude, ChatGPT) to enable:
1. **Natural Language Queries**: "What are my top artists from 2023?" instead of `lfm artists --from 2023-01-01 --to 2023-12-31`
2. **Music Discovery**: LLM generates recommendations based on listening history understanding
3. **Contextual Conversations**: "Play something similar to my last album" (LLM tracks context)
4. **Intelligent Interpretation**: LLM handles ambiguous queries ("artists I listened to last summer")

### LLM as Query Orchestrator

```
User Natural Language Input
         ↓
┌─────────────────────────────────────────────────────────────────┐
│                      LLM (Claude/GPT)                            │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Understanding Phase:                                    │    │
│  │  - Parse user intent                                    │    │
│  │  - Extract parameters (time period, limit, filters)    │    │
│  │  - Resolve ambiguities ("last summer" → dates)         │    │
│  └────────────────────────────────────────────────────────┘    │
│                          ↓                                       │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Tool Selection:                                         │    │
│  │  - Choose appropriate MCP tool(s)                       │    │
│  │  - Plan multi-step queries if needed                    │    │
│  │  - Consider data dependencies                           │    │
│  └────────────────────────────────────────────────────────┘    │
│                          ↓                                       │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Execution:                                              │    │
│  │  - Call lfm_artists(from="2023-06-01", to="2023-08-31")│    │
│  │  - Receive JSON response                                │    │
│  │  - Parse and analyze data                               │    │
│  └────────────────────────────────────────────────────────┘    │
│                          ↓                                       │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ Response Generation:                                    │    │
│  │  - Synthesize natural language response                 │    │
│  │  - Add musical context and insights                     │    │
│  │  - Follow "DJ buddy" tone (per guidelines)              │    │
│  └────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
         ↓
User-Friendly Natural Language Response
```

### MCP Tools as LLM Function Calls

**Conceptual Mapping**:

```
LLM Function Call Model:
  function_name: "lfm_artists"
  parameters: { limit: 10, year: 2023 }

         ↓ (translates to)

MCP Tool Call:
  {
    "method": "tools/call",
    "params": {
      "name": "lfm_artists",
      "arguments": {
        "limit": 10,
        "year": 2023
      }
    }
  }

         ↓ (executes as)

CLI Command:
  lfm artists --json --limit 10 --year 2023

         ↓ (returns)

JSON Response:
  {
    "artists": [
      { "name": "Pink Floyd", "playcount": "1234", "rank": "1" },
      ...
    ]
  }

         ↓ (LLM receives)

Structured Data for Reasoning
```

### Guidelines-Driven Behavior

**lfm-guidelines.md** (480 lines) shapes LLM behavior:

#### 1. Response Style

```
Guideline: "Be a DJ buddy, not a data analyst"

❌ Bad (Data Analyst):
"Based on the data, you have listened to 'White Light/White Heat'
21 times total. Breaking down by track: 'White Light/White Heat'
has 4 plays, 'The Gift' has 3 plays..."

✅ Good (DJ Buddy):
"Yes, you've listened to White Light/White Heat 4-5 times
(21 plays total, including the 17-minute 'Sister Ray')."
```

#### 2. Metadata Interpretation

```
Guideline: "Track positions are edge-case data for LLMs"

❌ Hallucination Risk:
"Track 2 is 'Breathe'" (without verification)

✅ Verified:
Use lfm_check(verbose: true) to verify track positions
```

#### 3. Tool Selection

```
Guideline: "lfm_recommendations is NOT a great starting point"

For well-known artists:
  Use LLM's musical knowledge first
  Only supplement with recommendations for obscure artists

For discovery:
  LLM reasoning → lfm_recommendations → augment/filter → curate
```

#### 4. Playback State Awareness

```
Guideline: "Check state before suggesting playback"

Before phrases like "Want me to queue":
  1. Check lfm_recent_tracks (what just finished?)
  2. Check lfm_current_track (anything playing?)
  3. Make data-informed offer

Template: "I see [data]. [Brief context]. Shall I [action]?"
```

### Multi-Step Query Example

```
User: "Play something similar to what I listened to most last month"

┌─────────────────────────────────────────────────────────────────┐
│ LLM Reasoning:                                                   │
│  1. Need to find top artist from last month                     │
│  2. Need to get similar artists                                 │
│  3. Need to initiate playback                                   │
└─────────────────────────────────────────────────────────────────┘
         ↓
Step 1: lfm_artists(period="1month", limit=1)
  → Returns: [{ name: "Radiohead", playcount: "50" }]

Step 2: lfm_similar(artist="Radiohead", limit=5)
  → Returns: [{ name: "Thom Yorke" }, { name: "Portishead" }, ...]

Step 3: lfm_play_now(artist="Thom Yorke", album="The Eraser")
  → Spotify playback initiated

LLM Response:
  "Last month, Radiohead was your top artist with 50 plays.
   I've started playing Thom Yorke's 'The Eraser' - similar
   atmospheric, experimental sound."
```

### Token Optimization Impact on LLM

**Original Full Response** (without optimization):
```json
{
  "artists": [
    {
      "name": "Pink Floyd",
      "playcount": "1234",
      "rank": "1",
      "url": "https://www.last.fm/music/Pink+Floyd",
      "mbid": "83d91898-7763-47d7-b03b-b92132375c47",
      "image": [...]
    }
  ]
}
// ~180 tokens per artist
```

**Compact Response** (with optimization):
```json
{
  "artists": [
    {
      "name": "Pink Floyd",
      "playcount": "1234",
      "rank": "1"
    }
  ]
}
// ~60 tokens per artist (66% reduction)
```

**Impact**:
- **100 artists**: ~10,300 tokens → ~5,150 tokens (50% reduction)
- **Faster responses**: Less data to process
- **Lower costs**: Fewer input tokens
- **Better context**: More room for conversation history

---

## Data Flow Patterns

### Pattern 1: Simple Query (Cache Hit)

```
User: lfm artists --limit 10
  ↓
ArtistsCommand.ExecuteAsync()
  ↓
ILastFmApiClient.GetTopArtistsAsync(user, period, 10, 1)
  ↓
CachedLastFmApiClient (decorator)
  ↓
Cache Key: "top_artists_smarshal_overall_10_1"
  ↓
FileCacheStorage.RetrieveAsync(key)
  ↓
Cache Hit! (< 1ms)
  ↓
Return cached TopArtists object
  ↓
DisplayService formats and prints to console
```

### Pattern 2: Simple Query (Cache Miss)

```
User: lfm artists --limit 10
  ↓
ArtistsCommand.ExecuteAsync()
  ↓
ILastFmApiClient.GetTopArtistsAsync(user, period, 10, 1)
  ↓
CachedLastFmApiClient (decorator)
  ↓
Cache Key: "top_artists_smarshal_overall_10_1"
  ↓
FileCacheStorage.RetrieveAsync(key)
  ↓
Cache Miss
  ↓
LastFmApiClient.GetTopArtistsAsync()
  ↓
Circuit Breaker check (is API healthy?)
  ↓
HttpClient.GetAsync("https://ws.audioscrobbler.com/2.0/?method=user.gettopartists&...")
  ↓
API Response (200-500ms)
  ↓
Deserialize JSON → TopArtists object
  ↓
FileCacheStorage.StoreAsync(key, object, ttl=10min)
  ↓
Return TopArtists object
  ↓
DisplayService formats and prints to console
```

### Pattern 3: MCP Tool Call (Node.js Production)

```
Claude: "Show me top 5 artists from 2023"
  ↓
MCP Protocol: tools/call("lfm_artists", {limit: 5, year: 2023})
  ↓
server.js (Node.js MCP Server)
  ↓
Build CLI command: "lfm artists --json --limit 5 --year 2023"
  ↓
exec() spawns child process
  ↓
Lfm.Cli runs in child process
  ↓
ArtistsCommand.ExecuteAsync() (same as Pattern 1/2)
  ↓
Returns JSON to stdout
  ↓
server.js parses stdout JSON
  ↓
Apply compactArtist() transformation (strip Url, Mbid)
  ↓
Return compact JSON via MCP protocol
  ↓
Claude receives data and generates response
  ↓
User sees natural language: "Here are your top 5 artists from 2023..."
```

### Pattern 4: MCP Tool Call (C# Experimental)

```
Claude: "Show me top 5 artists from 2023"
  ↓
MCP Protocol: tools/call("lfm_artists", {limit: 5, year: 2023})
  ↓
Lfm.McpServer (C# .NET 9)
  ↓
LastFmTools.GetArtists(limit: 5, year: 2023)
  ↓
LastFmMcpClient.GetTopArtistsAsync() (in-process, no spawn)
  ↓
ILastFmApiClient.GetTopArtistsAsync() (same as Pattern 2)
  ↓
CachedLastFmApiClient → Cache or API
  ↓
Return TopArtists object
  ↓
Map to CompactArtist models (strip Url, Mbid)
  ↓
Serialize to JSON
  ↓
Return via MCP protocol
  ↓
Claude receives data and generates response
```

### Pattern 5: EF Core LINQ Query

```
User query (hypothetical MCP tool using EF):
  ↓
LfmDbContext.Artists
  .Where(a => a.User == "smarshal" && a.Period == LastFmPeriod.Month)
  .Take(10)
  .ToListAsync()
  ↓
LastFmQueryable<Artist>.GetAsyncEnumerator()
  ↓
LastFmQueryProvider.ExecuteAsync<Artist>(expression)
  ↓
LastFmExpressionVisitor.Analyze(expression)
  → Parses LINQ expression tree
  → Builds QueryDescriptor:
    - QueryType: UserTopArtists
    - User: "smarshal"
    - Period: Month
    - Limit: 10
    - Page: 1
  ↓
QueryTranslator.Translate(descriptor)
  → Converts to API parameters
  ↓
ILastFmApiClient.GetTopArtistsAsync("smarshal", Month, 10, 1)
  ↓
CachedLastFmApiClient → Cache or API (same as Pattern 2)
  ↓
Return TopArtists API response
  ↓
ResultMapper.MapArtists(response, "smarshal")
  → API response → EF entities
  → Inject User, QueryTimestamp
  → Normalize artist names (apostrophes)
  → Flatten Attributes.Rank → Rank
  ↓
Return List<Artist> (EF entities)
```

### Pattern 6: Local File Processing

```
User: lfm artists --data-source local-files
  ↓
ArtistsCommand determines data source mode
  ↓
IMusicDataProvider = LocalFileDataProvider (instead of LastFmApiProvider)
  ↓
LocalFileDataProvider.GetTopArtistsAsync()
  ↓
SpotifyJsonParser.ParseAsync("spotify-history.json")
  ↓
Read JSON array of streaming records
  ↓
Extract artist name from each record
  ↓
Group by artist, count plays
  ↓
Sort by play count descending
  ↓
Take top N artists
  ↓
Convert to TopArtists model (compatible with Last.fm API model)
  ↓
Return TopArtists object
  ↓
DisplayService formats and prints (same as API response)
```

---

## Cross-Cutting Concerns

### Caching Strategy

**Implementation**: File-based caching with TTL expiration

**Cache Key Generation**:
```csharp
public string GenerateKey(string method, Dictionary<string, string> parameters)
{
    var sortedParams = parameters.OrderBy(p => p.Key)
        .Select(p => $"{p.Key}={p.Value}");

    return $"{method}_{string.Join("_", sortedParams)}";

    // Example: "top_artists_smarshal_overall_10_1"
}
```

**Cache Location**:
- Windows: `%APPDATA%\lfm\cache\`
- Linux/macOS: `~/.lfm/cache/`

**TTL Configuration**:
```csharp
// LfmConfig.cs
public int CacheDurationMinutes { get; set; } = 10;  // Default 10 minutes
```

**Performance Impact**:
- Cache hit: < 1ms
- Cache miss: 200-500ms (API call)
- **119x speedup** measured in benchmarks

### Error Handling

**Result<T> Pattern**:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ErrorResult? Error { get; }

    public static Result<T> Success(T value);
    public static Result<T> Failure(string message, string technicalDetails);
    public static Result<T> ApiError(string message, int statusCode);
    public static Result<T> NetworkError(string message, Exception ex);
}
```

**Usage**:
```csharp
var result = await _apiClient.GetTopArtistsWithResultAsync(user, period, limit);

if (!result.IsSuccess)
{
    _displayService.DisplayError(result.Error!.UserMessage);
    _logger.LogError(result.Error.TechnicalDetails);
    return;
}

var artists = result.Value!.Artists;
```

### Circuit Breaker Pattern

**State Machine**:
```
Closed (normal operation)
  ↓ (5 failures)
Open (fail fast, don't call API)
  ↓ (30 seconds timeout)
Half-Open (test with one request)
  ↓ (2 successes)
Closed (restored)
```

**Configuration**:
```csharp
public bool CircuitBreakerEnabled { get; set; } = true;
public int CircuitBreakerFailureThreshold { get; set; } = 5;
public int CircuitBreakerSuccessThreshold { get; set; } = 2;
public int CircuitBreakerOpenDurationSeconds { get; set; } = 30;
```

### Configuration Management

**LfmConfig Model** (50+ properties):
```csharp
public class LfmConfig
{
    // API
    public string ApiKey { get; set; }
    public int ApiThrottleMs { get; set; } = 200;
    public int ParallelApiCalls { get; set; } = 5;

    // User
    public string DefaultUsername { get; set; }
    public string DefaultPeriod { get; set; } = "overall";

    // Cache
    public int CacheDurationMinutes { get; set; } = 10;
    public CacheBehavior CacheBehavior { get; set; } = CacheBehavior.Default;

    // Display
    public UnicodeSupport UnicodeSupport { get; set; } = UnicodeSupport.Auto;

    // Spotify
    public string? SpotifyClientId { get; set; }
    public string? SpotifyClientSecret { get; set; }

    // Sonos
    public SonosConfig Sonos { get; set; } = new();

    // Data Source
    public DataSourceMode DataSourceMode { get; set; } = DataSourceMode.LastFm;
    public string? SpotifyHistoryPath { get; set; }
    public string? YouTubeMusicHistoryPath { get; set; }

    // Circuit Breaker
    public bool CircuitBreakerEnabled { get; set; } = true;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
}
```

**Storage**:
- File: `~/.lfm/lfm.json` or `%APPDATA%\lfm\lfm.json`
- Format: JSON
- Access: `IConfigurationManager.LoadAsync()`, `SaveAsync()`

### Logging

**Infrastructure**: Microsoft.Extensions.Logging

**Log Levels**:
- **Error**: API failures, circuit breaker trips, configuration errors
- **Warning**: Cache misses, client-side LINQ filters, rate limit approached
- **Information**: Command execution, API calls, cache hits
- **Debug**: Detailed diagnostics (disabled by default)

**Sinks**:
- CLI: Console (colored output)
- MCP Server: Suppressed (stdout reserved for MCP protocol)

### Unicode/ASCII Support

**Auto-Detection**:
```csharp
public enum UnicodeSupport { Auto, Enabled, Disabled }

// Auto mode detection
private bool SupportsUnicode()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // Check console encoding
        return Console.OutputEncoding.CodePage == 65001; // UTF-8
    }

    // Linux/macOS: check LANG environment variable
    var lang = Environment.GetEnvironmentVariable("LANG");
    return lang?.Contains("UTF-8") ?? false;
}
```

**Symbol Mapping**:
```csharp
// Unicode: ✓ ✗ ★ ▶ ⏸ ⏭
// ASCII:   + - * > || >>
```

---

## Summary

### System Characteristics

| Aspect | Details |
|--------|---------|
| **Primary Use Case** | Query Last.fm music statistics via CLI or LLM |
| **Deployment** | Standalone CLI + MCP server (Node.js or C#) |
| **Target Users** | Music enthusiasts, Last.fm power users, Claude/GPT users |
| **Key Innovation** | Dual implementation (Direct API + EF LINQ) with MCP integration |
| **Performance** | 119x speedup via caching, <10% LINQ overhead |
| **Scalability** | Single-user tool (not multi-tenant) |
| **Extensibility** | Pluggable providers (Last.fm API, local files, future: MusicBrainz) |

### Architectural Highlights

1. **Clean Separation**: CLI, data access, and external APIs are decoupled
2. **Dual Implementation**: Two parallel data access strategies enable comparison and optimization
3. **Decorator Pattern**: Caching wraps API client transparently (119x speedup)
4. **LINQ Provider**: Custom EF Core query provider demonstrates advanced C# techniques
5. **MCP Integration**: Standard protocol enables LLM tool use (28 production tools)
6. **Result Pattern**: Functional error handling without exceptions
7. **Circuit Breaker**: Resilience pattern protects against API failures
8. **Token Optimization**: 50% reduction in MCP responses improves LLM performance

### Future Directions

1. **Merge Implementations**: Migrate CLI commands to EF LINQ provider for consistency
2. **Complete C# MCP Server**: Implement remaining 25 tools (3/28 done)
3. **Local-First Database**: Persist listening history in SQLite (original "lfm2EF" vision)
4. **Multi-Source Aggregation**: Combine Last.fm + Spotify + YouTube Music data
5. **Real-Time Sync**: Background service to sync scrobbles continuously
6. **Enhanced Recommendations**: ML-based similarity beyond Last.fm's algorithm

---

**Document Status**: Complete architectural overview
**Maintained By**: Development team
**Last Review**: 2025-01-26
**Next Review**: After major architectural changes
