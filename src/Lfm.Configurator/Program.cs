using Lfm.Configurator;
using Lfm.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

AnsiConsole.MarkupLine("[bold cyan]Lfm Configuration Utility[/]");
AnsiConsole.MarkupLine("[yellow]┌─────────────────────────────────┐[/]");
AnsiConsole.MarkupLine("[yellow]│[/] [bold]Interactive Configuration Tool[/] [yellow]│[/]");
AnsiConsole.MarkupLine("[yellow]└─────────────────────────────────┘[/]");
AnsiConsole.WriteLine();

try
{
    // Set up dependency injection
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole());
    var serviceProvider = services.BuildServiceProvider();

    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<ConfigurationManager>();

    var configManager = new ConfigurationManager(logger);
    var config = await configManager.LoadAsync();

    var app = new ConfiguratorApp(configManager, config);
    await app.RunAsync();
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    AnsiConsole.WriteException(ex);
    Environment.Exit(1);
}
