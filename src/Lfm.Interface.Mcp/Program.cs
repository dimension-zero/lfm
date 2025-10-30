using Lfm.Data.Direct;
using Lfm.Shared.Interfaces;
using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Data.Direct.Cache;
using Lfm.Interface.Mcp.Services;
using Lfm.Interface.Mcp.Tools;
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
var configManager = host.Services.GetRequiredService<IConfigurationManager>();
var config = await configManager.LoadAsync();
var logger = loggerFactory.CreateLogger("LastFmTools");

if (string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.DefaultUsername))
{
    Console.Error.WriteLine("Error: Last.fm API key and username must be configured");
    Console.Error.WriteLine("Run: lfm config set-api-key <key>");
    Console.Error.WriteLine("Run: lfm config set-username <username>");
    return 1;
}

LastFmTools.Initialize(mcpClient, logger, config.DefaultUsername);

await host.RunAsync();

return 0;
