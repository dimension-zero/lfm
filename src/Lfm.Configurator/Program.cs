using Lfm.Configurator;
using Lfm.Core.Configuration;
using Spectre.Console;

AnsiConsole.MarkupLine("[bold cyan]Lfm Configuration Utility[/]");
AnsiConsole.MarkupLine("[yellow]┌─────────────────────────────────┐[/]");
AnsiConsole.MarkupLine("[yellow]│[/] [bold]Interactive Configuration Tool[/] [yellow]│[/]");
AnsiConsole.MarkupLine("[yellow]└─────────────────────────────────┘[/]");
AnsiConsole.WriteLine();

try
{
    var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lfm");
    Directory.CreateDirectory(baseDir);

    var configManager = new ConfigurationManager(baseDir, null);
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
