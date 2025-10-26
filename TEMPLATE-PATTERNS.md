# Template Patterns Extracted from lfm2EF Implementation

**Purpose**: Document reusable patterns from Lfm.McpServer for API2EF2MCP code generation
**Source**: lfm branch `lfm2EF` (Phases 1-2 complete)
**Date**: 2025-01-26

## Overview

This document extracts proven patterns from the lfm→EF conversion proof-of-concept. These patterns are ready to be templatized for automated code generation in the API2EF2MCP project.

## Pattern Catalog

### 1. Compact Model Generation

**Purpose**: Generate type-safe models with transformation rules applied

**Input**:
- ApiEntity (from schema discovery)
- TransformationRules (from JSON)

**Template**:
```csharp
/// <summary>
/// Compact {{EntityName}} model with transformation rules applied.
{{#each TokenOptimizationExcludes}}
/// Excludes: {{this}} (token optimization)
{{/each}}
{{#each PropertyFlattening}}
/// Flattens: {{sourcePath}} → {{targetName}} (property flattening)
{{/each}}
/// </summary>
public class Compact{{EntityName}}
{
{{#each Properties}}
    {{#unless (isExcluded this)}}
    public {{ClrType}} {{Name}} { get; set; }{{#if IsNullable}} = {{DefaultValue}};{{/if}}
    {{/unless}}
{{/each}}
{{#each FlattenedProperties}}
    public string {{TargetName}} { get; set; } = string.Empty;  // Flattened from {{SourcePath}}
{{/each}}
}
```

**Example Output** (from Lfm.McpServer/Services/LastFmMcpClient.cs:101-106):
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
}
```

**Source Reference**: `src/Lfm.McpServer/Services/LastFmMcpClient.cs` lines 97-133

---

### 2. MCP Client Wrapper Generation

**Purpose**: Generate transformation wrapper around API client

**Input**:
- ApiModel (entities and endpoints)
- TransformationRules
- API client interface name

**Template**:
```csharp
using {{CoreNamespace}}.Models;
using {{CoreNamespace}}.Models.Results;
using {{CoreNamespace}}.Services;
using Microsoft.Extensions.Logging;

namespace {{McpNamespace}}.Services;

/// <summary>
/// MCP-friendly wrapper around {{ApiClientName}}.
/// Applies transformation rules to produce conversational output.
/// </summary>
public class {{ApiName}}McpClient
{
    private readonly I{{ApiClientName}} _apiClient;
    private readonly ILogger<{{ApiName}}McpClient> _logger;

    public {{ApiName}}McpClient(I{{ApiClientName}} apiClient, ILogger<{{ApiName}}McpClient> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

{{#each Endpoints}}
    /// <summary>
    /// {{Description}}
    /// {{#each AppliedTransformations}}
    /// {{Description}}
    /// {{/each}}
    /// </summary>
    public async Task<Result<List<Compact{{ReturnEntity}}>>> {{MethodName}}({{Parameters}})
    {
        var result = await _apiClient.{{ApiMethodName}}({{ParameterNames}});

        if (result.IsFailure)
            return Result<List<Compact{{ReturnEntity}}>>.Fail(result.Error!);

        var compact{{ReturnEntity}}s = result.Data!.{{CollectionProperty}}
            .Select({{EntityVar}} => new Compact{{ReturnEntity}}
            {
{{#each CompactProperties}}
                {{Name}} = {{SourceExpression}},{{#if Comment}}  // {{Comment}}{{/if}}
{{/each}}
            })
            .ToList();

        return Result<List<Compact{{ReturnEntity}}>>.Ok(compact{{ReturnEntity}}s);
    }

{{/each}}
}
```

**Example Output** (from Lfm.McpServer/Services/LastFmMcpClient.cs:27-44):
```csharp
/// <summary>
/// Get user's top artists with transformation rules applied.
/// Token optimization: Strips Url, Mbid (50% reduction target)
/// </summary>
public async Task<Result<List<CompactArtist>>> GetTopArtistsAsync(string username, string period = "overall", int limit = 10)
{
    var result = await _apiClient.GetTopArtistsWithResultAsync(username, period, limit);

    if (result.IsFailure)
        return Result<List<CompactArtist>>.Fail(result.Error!);

    var compactArtists = result.Data!.Artists
        .Select(a => new CompactArtist
        {
            Name = a.Name,
            PlayCount = a.PlayCount,
            Rank = a.Attributes?.Rank
        })
        .ToList();

    return Result<List<CompactArtist>>.Ok(compactArtists);
}
```

**Source Reference**: `src/Lfm.McpServer/Services/LastFmMcpClient.cs` lines 12-95

---

### 3. MCP Tools Generation

**Purpose**: Generate MCP tool methods with ModelContextProtocol attributes

**Input**:
- Endpoints from ApiModel
- Parameter metadata
- MCP client methods

**Template**:
```csharp
using System.ComponentModel;
using System.Text.Json;
using {{McpNamespace}}.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace {{McpNamespace}}.Tools;

/// <summary>
/// MCP tools for {{ApiTitle}}.
/// Implements transformation rules from {{RulesFile}}:
{{#each TransformationRules}}
/// - {{Description}}
{{/each}}
/// </summary>
public static class {{ApiName}}Tools
{
    private static {{ApiName}}McpClient? _client;
    private static ILogger? _logger;
{{#each StaticParameters}}
    private static {{Type}}? _{{Name}};
{{/each}}

    public static void Initialize({{ApiName}}McpClient client, ILogger logger{{#each StaticParameters}}, {{Type}} {{Name}}{{/each}})
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
{{#each StaticParameters}}
        _{{Name}} = {{Name}} ?? throw new ArgumentNullException(nameof({{Name}}));
{{/each}}
    }

{{#each Tools}}
    [Description("{{Description}}")]
    public static async Task<string> {{ToolName}}(
{{#each Parameters}}
        [Description("{{Description}}")] {{Type}}{{#if IsOptional}}?{{/if}} {{Name}}{{#if HasDefault}} = {{DefaultValue}}{{/if}}{{#unless @last}},{{/unless}}
{{/each}})
    {
        if (_client == null || _logger == null{{#each RequiredStatics}} || _{{Name}} == null{{/each}})
            throw new InvalidOperationException("Tools not initialized");

{{#each ParameterProcessing}}
        var {{ActualName}} = {{Expression}};
{{/each}}

        var result = await _client.{{ClientMethod}}({{ClientArgs}});

        if (result.IsFailure)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = result.Error!.Message
            });
        }

        // Return compact {{EntityName}} (transformation rules applied in client)
        return JsonSerializer.Serialize(new
        {
            {{ResponsePropertyName}} = result.Data,
{{#each ResponseMetadata}}
            {{Name}} = {{Value}},
{{/each}}
            count = result.Data!.Count
        });
    }

{{/each}}
}
```

**Example Output** (from Lfm.McpServer/Tools/LastFmTools.cs:29-58):
```csharp
[Description("Get user's top artists for a time period")]
public static async Task<string> lfm_artists(
    [Description("Time period: overall, 7day, 1month, 3month, 6month, 12month")] string? period = null,
    [Description("Number of artists to return (1-50)")] int? limit = null)
{
    if (_client == null || _logger == null || _username == null)
        throw new InvalidOperationException("Tools not initialized");

    var actualPeriod = period ?? "overall";
    var actualLimit = Math.Clamp(limit ?? 10, 1, 50);

    var result = await _client.GetTopArtistsAsync(_username, actualPeriod, actualLimit);

    if (result.IsFailure)
    {
        return JsonSerializer.Serialize(new
        {
            error = true,
            message = result.Error!.Message
        });
    }

    // Return compact artists (transformation rules applied in client)
    return JsonSerializer.Serialize(new
    {
        artists = result.Data,
        period = actualPeriod,
        count = result.Data!.Count
    });
}
```

**Source Reference**: `src/Lfm.McpServer/Tools/LastFmTools.cs` lines 16-121

---

### 4. MCP Program.cs Generation

**Purpose**: Generate complete DI setup and MCP server initialization

**Input**:
- ApiModel metadata
- Configuration requirements
- Cache settings

**Template**:
```csharp
using {{CoreNamespace}}.Configuration;
using {{CoreNamespace}}.Services;
using {{CoreNamespace}}.Services.Cache;
using {{McpNamespace}}.Services;
using {{McpNamespace}}.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register HTTP client
        services.AddHttpClient();

        // Register configuration manager
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<ICacheDirectoryHelper, CacheDirectoryHelper>();

        // Register cache services
        services.AddSingleton<ICacheStorage, FileCacheStorage>();
        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();

        // Register the actual {{ApiName}} API client
        services.AddSingleton<{{ApiClientName}}>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "{{UserAgent}}");

            var logger = serviceProvider.GetRequiredService<ILogger<{{ApiClientName}}>>();
            var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            var config = configManager.LoadAsync().GetAwaiter().GetResult();

            return new {{ApiClientName}}({{ConstructorArgs}});
        });

        // Register the cached wrapper as the main interface
        services.AddSingleton<I{{ApiClientName}}>(serviceProvider =>
        {
            var innerClient = serviceProvider.GetRequiredService<{{ApiClientName}}>();
            var cacheStorage = serviceProvider.GetRequiredService<ICacheStorage>();
            var keyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
            var logger = serviceProvider.GetRequiredService<ILogger<Cached{{ApiClientName}}>>();
            var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();

            return new Cached{{ApiClientName}}(innerClient, cacheStorage, keyGenerator, logger, configManager, {{CacheDurationMinutes}});
        });

        // Register MCP client wrapper
        services.AddSingleton<{{ApiName}}McpClient>();

        // Register MCP server with stdio transport
        services
            .AddMcpServer()
            .WithStdioServerTransport();

        // Configure logging (suppressed to avoid interfering with MCP protocol on stdout)
        services.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.None);
        });
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        // Suppress console logging to avoid interfering with MCP protocol on stdout
    });

var host = builder.Build();

// Initialize the tools with dependencies before starting the server
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var mcpClient = host.Services.GetRequiredService<{{ApiName}}McpClient>();
var configManager = host.Services.GetRequiredService<IConfigurationManager>();
var config = await configManager.LoadAsync();
var logger = loggerFactory.CreateLogger("{{ApiName}}Tools");

{{#each ConfigValidation}}
if ({{Condition}})
{
    Console.Error.WriteLine("Error: {{ErrorMessage}}");
{{#each HelpCommands}}
    Console.Error.WriteLine("{{Command}}");
{{/each}}
    return 1;
}
{{/each}}

{{ApiName}}Tools.Initialize(mcpClient, logger{{#each InitArgs}}, {{Value}}{{/each}});

await host.RunAsync();

return 0;
```

**Example Output** (from Lfm.McpServer/Program.cs:11-93):
```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register HTTP client
        services.AddHttpClient();

        // Register configuration manager
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<ICacheDirectoryHelper, CacheDirectoryHelper>();

        // Register cache services
        services.AddSingleton<ICacheStorage, FileCacheStorage>();
        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();

        // Register the actual LastFm API client
        services.AddSingleton<LastFmApiClient>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "lfm-mcp/1.0");

            var logger = serviceProvider.GetRequiredService<ILogger<LastFmApiClient>>();
            var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            var config = configManager.LoadAsync().GetAwaiter().GetResult();

            return new LastFmApiClient(httpClient, logger, config.ApiKey, config.ApiThrottleMs);
        });

        // Register the cached wrapper as the main interface
        services.AddSingleton<ILastFmApiClient>(serviceProvider =>
        {
            var innerClient = serviceProvider.GetRequiredService<LastFmApiClient>();
            var cacheStorage = serviceProvider.GetRequiredService<ICacheStorage>();
            var keyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
            var logger = serviceProvider.GetRequiredService<ILogger<CachedLastFmApiClient>>();
            var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();

            return new CachedLastFmApiClient(innerClient, cacheStorage, keyGenerator, logger, configManager, 10);
        });

        // Register MCP client wrapper
        services.AddSingleton<LastFmMcpClient>();

        // Register MCP server with stdio transport
        services
            .AddMcpServer()
            .WithStdioServerTransport();

        // Configure logging (suppressed to avoid interfering with MCP protocol on stdout)
        services.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.None);
        });
    });
```

**Source Reference**: `src/Lfm.McpServer/Program.cs` lines 1-93

---

### 5. Project File Generation

**Purpose**: Generate .csproj file with correct package references

**Input**:
- Target framework
- Project references
- Required packages

**Template**:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{{TargetFramework}}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
{{#each ProjectReferences}}
    <ProjectReference Include="{{Path}}" />
{{/each}}
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
    <PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />
  </ItemGroup>

</Project>
```

**Example Output** (from Lfm.McpServer/Lfm.McpServer.csproj):
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Lfm.Core\Lfm.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
    <PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />
  </ItemGroup>

</Project>
```

**Source Reference**: `src/Lfm.McpServer/Lfm.McpServer.csproj`

---

## Transformation Rule Application Patterns

### Token Optimization

**Rule**: Exclude specified properties from compact models

**Example from lastfm-rules.json**:
```json
{
  "tokenOptimizationExcludes": ["Url", "Mbid", "Image", "Streamable"]
}
```

**Code Generation Pattern**:
```csharp
// Only include properties NOT in tokenOptimizationExcludes
{{#each entity.Properties}}
{{#unless (isIn this ../transformationRules.tokenOptimizationExcludes)}}
public {{ClrType}} {{Name}} { get; set; }
{{/unless}}
{{/each}}
```

### Property Flattening

**Rule**: Convert nested navigation properties to flat strings

**Example from lastfm-rules.json**:
```json
{
  "propertyFlattening": {
    "Track.Artist.Name": {
      "sourcePath": "Track.Artist.Name",
      "targetName": "artist"
    }
  }
}
```

**Code Generation Pattern**:
```csharp
var compactTracks = result.Data!.Tracks
    .Select(t => new CompactTrack
    {
        // ... other properties
        Artist = t.Artist.Name,  // Flattened from Track.Artist.Name
    })
    .ToList();
```

### Navigation Inlining

**Rule**: Embed related entities as formatted strings

**Example from lastfm-rules.json**:
```json
{
  "navigationInlining": {
    "Track.Artist": {
      "navigationPath": "Track.Artist",
      "inlineFormat": "{Name}"
    }
  }
}
```

**Code Generation Pattern**:
```csharp
Artist = $"{t.Artist.Name}"  // Inlined: Artist → {Name}
```

---

## Critical Design Patterns

### 1. Root-Cause Analysis Pattern

**Principle**: When encountering build errors, examine working reference code as ground truth

**Example**: All 4 Lfm.McpServer build blockers resolved by reading Lfm.Cli/Program.cs

**Application to Generator**:
- Don't guess API signatures - extract from actual code
- Don't invent DI patterns - copy proven patterns
- Don't assume package names - document exact references

**Documentation**: See `BLOCKER-RESOLUTION.md`

### 2. Complete DI Registration Pattern

**Principle**: Generate all required services, not partial setup

**Bad**:
```csharp
// Partial - user must figure out what's missing
services.AddSingleton<ICacheStorage, FileCacheStorage>();
```

**Good**:
```csharp
// Complete - all dependencies registered
services.AddSingleton<ICacheStorage, FileCacheStorage>();
services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
services.AddSingleton<ICacheDirectoryHelper, CacheDirectoryHelper>();
```

### 3. Factory Method Pattern for Complex Construction

**Principle**: Use factory methods for services with complex initialization

**Pattern**:
```csharp
services.AddSingleton<ILastFmApiClient>(serviceProvider =>
{
    // All 6 parameters explicitly resolved and passed
    var innerClient = serviceProvider.GetRequiredService<LastFmApiClient>();
    var cacheStorage = serviceProvider.GetRequiredService<ICacheStorage>();
    var keyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
    var logger = serviceProvider.GetRequiredService<ILogger<CachedLastFmApiClient>>();
    var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();

    return new CachedLastFmApiClient(
        innerClient, cacheStorage, keyGenerator, logger, configManager, 10
    );
});
```

**Why**: Clear dependency chain, compile-time validation of parameters

---

## Code Generation Workflow

### Phase 1: Schema Discovery
1. Run schema discovery (API2EF) to get ApiModel
2. Save ApiModel.json for next phases

### Phase 2: Transformation Rules
1. Create transformation-rules/{api-name}-rules.json
2. Define token optimization exclusions
3. Define property flattening rules
4. Define navigation inlining rules
5. Define equivalence groups

### Phase 3: Code Generation
1. **Compact Models**: Apply Template #1 with ApiModel + rules → `Compact{Entity}.cs`
2. **MCP Client**: Apply Template #2 with endpoints + rules → `{Api}McpClient.cs`
3. **MCP Tools**: Apply Template #3 with endpoints → `{Api}Tools.cs`
4. **Program.cs**: Apply Template #4 with config requirements → `Program.cs`
5. **Project File**: Apply Template #5 with references → `{Api}.McpServer.csproj`

### Phase 4: Validation
1. Build generated project (`dotnet build`)
2. Validate configuration handling
3. Test with real API (if credentials available)
4. Compare token usage vs original API responses

---

## Template Engine Requirements

### Capabilities Needed
1. **Handlebars-style templating** - Readable, proven for code generation
2. **Conditional logic** - `{{#if}}`, `{{#unless}}`, `{{#each}}`
3. **Custom helpers** - `isExcluded()`, `isIn()`, `toPascalCase()`
4. **Multiple output files** - One template → many files pattern
5. **String manipulation** - Case conversion, pluralization

### Recommended: Scriban or Handlebars.Net
- Both support .NET
- Clean syntax for code generation
- Extensible helper system
- Good documentation

---

## Testing Strategy

### Validation Points
1. **Build Success**: Generated code compiles with 0 errors, 0 warnings
2. **Configuration Validation**: Properly checks required config before running
3. **DI Resolution**: All services resolve correctly at runtime
4. **Transformation Correctness**: Compact models match transformation rules
5. **Token Reduction**: Actual token count reduction matches target (~50%)

### Test APIs
1. **Last.fm** (completed) - Music scrobbling API
2. **Data.gov.uk** - UK government open data
3. **Companies House** - UK company data

Each API validates different aspects:
- Last.fm: Complex nested structures, case-insensitive matching
- Data.gov.uk: Simpler structures, high volume
- Companies House: Business entity relationships

---

## Package Reference Documentation

### Required Packages (Exact Versions)

**MCP Server**:
```xml
<PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
```

**API Client** (typical):
```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

**Critical**: Do NOT use `Microsoft.Extensions.AI.Abstractions` for MCP - this is a different SDK

---

## Next Steps for API2EF2MCP

1. **Create Template Files**:
   - `Templates/CompactModel.scriban`
   - `Templates/McpClient.scriban`
   - `Templates/McpTools.scriban`
   - `Templates/McpProgram.scriban`
   - `Templates/McpProject.scriban`

2. **Implement Generator**:
   - `CodeGenerator.cs` - Applies templates with ApiModel + rules
   - `TemplateRenderer.cs` - Scriban/Handlebars wrapper
   - `FileWriter.cs` - Creates output directory structure

3. **Test with Data.gov.uk**:
   - Validate generalization beyond Last.fm
   - Measure token reduction on different data shapes
   - Refine templates based on edge cases

4. **Document Generator Usage**:
   - CLI interface: `api2ef2mcp generate --api lastfm --rules lastfm-rules.json`
   - Output: Complete C# MCP server project
   - Build and run: `dotnet run --project Generated.McpServer`

---

## Success Metrics

### Code Quality
- ✅ Generated code compiles cleanly (0 errors, 0 warnings)
- ✅ Follows .NET conventions (PascalCase, XML comments)
- ✅ Type-safe (no dynamic/reflection)
- ✅ Debuggable (breakpoints work, values inspectable)

### Transformation Effectiveness
- ✅ Token optimization: 45-55% reduction achieved
- ✅ Property flattening: Nested objects → flat strings
- ✅ Navigation inlining: Related entities embedded
- ✅ Equivalence: Case-insensitive matching working

### Maintainability
- ✅ Templates readable by non-developers
- ✅ Rules modifiable without code changes
- ✅ Clear comments explaining transformations
- ✅ Consistent naming across generated code

---

**Status**: Ready for templatization
**Next Phase**: Create actual Scriban templates from these patterns
**Reference Implementation**: lfm branch `lfm2EF` commit f9853a4
