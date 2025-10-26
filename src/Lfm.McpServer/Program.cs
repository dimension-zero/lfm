using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Core.Services.Cache;
using Lfm.McpServer.Services;
using Lfm.McpServer.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Load configuration
var config = LfmConfig.Load();
if (string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.Username))
{
    Console.Error.WriteLine("Error: Last.fm API key and username must be configured");
    Console.Error.WriteLine("Run: lfm config set-api-key <key>");
    Console.Error.WriteLine("Run: lfm config set-username <username>");
    return 1;
}

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register HTTP client
        services.AddHttpClient();

        // Register cache storage
        services.AddSingleton<IFileCacheStorage, FileCacheStorage>();

        // Register Last.fm API client with caching
        services.AddSingleton<ILastFmApiClient>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var cacheStorage = provider.GetRequiredService<IFileCacheStorage>();

            var httpClient = httpClientFactory.CreateClient();
            var apiLogger = loggerFactory.CreateLogger<LastFmApiClient>();
            var cacheLogger = loggerFactory.CreateLogger<CachedLastFmApiClient>();

            var innerClient = new LastFmApiClient(
                httpClient,
                apiLogger,
                config.ApiKey,
                config.ApiThrottleMs,
                config.EnableDebugLogging
            );

            return new CachedLastFmApiClient(
                innerClient,
                cacheStorage,
                cacheLogger,
                config.CacheBehavior,
                config.CacheDurationMinutes
            );
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
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        // Suppress console logging to avoid interfering with MCP protocol on stdout
    });

var host = builder.Build();

// Initialize the tools with dependencies before starting the server
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var mcpClient = host.Services.GetRequiredService<LastFmMcpClient>();
var logger = loggerFactory.CreateLogger("LastFmTools");

LastFmTools.Initialize(mcpClient, logger, config.Username);

await host.RunAsync();

return 0;
