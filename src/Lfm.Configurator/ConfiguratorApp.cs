using Lfm.Core.Configuration;
using Spectre.Console;

namespace Lfm.Configurator;

/// <summary>
/// Interactive configuration utility for Lfm CLI application
/// Provides a user-friendly menu system for configuring Last.fm, Spotify, Sonos, and other settings
/// </summary>
public class ConfiguratorApp
{
    private readonly IConfigurationManager _configManager;
    private LfmConfig _config;
    private bool _modified = false;

    public ConfiguratorApp(IConfigurationManager configManager, LfmConfig config)
    {
        _configManager = configManager;
        _config = config;
    }

    public async Task RunAsync()
    {
        bool running = true;
        while (running)
        {
            AnsiConsole.Clear();
            DisplayMenu();

            var options = new Dictionary<string, int>
            {
                { "Last.fm Settings", 1 },
                { "Spotify Settings", 2 },
                { "Sonos Settings", 3 },
                { "Cache Settings", 4 },
                { "View Configuration", 5 },
                { "Exit", 6 }
            };

            var selectedOption = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select an option:[/]")
                    .AddChoices(options.Keys));

            var choice = options[selectedOption];

            switch (choice)
            {
                case 1:
                    ConfigureLastFmAsync();
                    break;
                case 2:
                    ConfigureSpotifyAsync();
                    break;
                case 3:
                    ConfigureSonosAsync();
                    break;
                case 4:
                    ConfigureCacheAsync();
                    break;
                case 5:
                    DisplayConfiguration();
                    break;
                case 6:
                    running = false;
                    break;
            }

            if (running && choice != 5 && choice != 6)
            {
                AnsiConsole.Ask<string>("\nPress [green]Enter[/] to continue...");
            }
        }

        if (_modified)
        {
            if (AnsiConsole.Confirm("Save changes before exiting?"))
            {
                await _configManager.SaveAsync(_config);
                AnsiConsole.MarkupLine("[green]✓ Configuration saved successfully[/]");
            }
        }

        AnsiConsole.MarkupLine("\n[cyan]Thank you for using Lfm Configuration Utility![/]");
    }

    private void DisplayMenu()
    {
        var panel = new Panel("[cyan]Lfm Configuration Utility[/]")
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1)
        };
        AnsiConsole.Write(panel);
    }

    private void ConfigureLastFmAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan]Last.fm Configuration[/]");
        AnsiConsole.WriteLine();

        var options = new[] { "API Key", "Default Username", "API Throttle (ms)", "Back" };
        var setting = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to configure?")
                .AddChoices(options));

        switch (setting)
        {
            case "API Key":
                _config.ApiKey = AnsiConsole.Ask<string>("Enter Last.fm API Key (leave blank to skip):");
                _modified = true;
                break;
            case "Default Username":
                _config.DefaultUsername = AnsiConsole.Ask<string>("Enter Last.fm Username (leave blank to skip):");
                _modified = true;
                break;
            case "API Throttle (ms)":
                var throttle = AnsiConsole.Ask<int>($"Enter API throttle in milliseconds (current: {_config.ApiThrottleMs}):");
                if (throttle > 0)
                {
                    _config.ApiThrottleMs = throttle;
                    _modified = true;
                }
                break;
        }
    }

    private void ConfigureSpotifyAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan]Spotify Configuration[/]");
        AnsiConsole.WriteLine();

        var options = new[] { "Client ID", "Client Secret", "Default Device", "Back" };
        var setting = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to configure?")
                .AddChoices(options));

        switch (setting)
        {
            case "Client ID":
                _config.Spotify.ClientId = AnsiConsole.Ask<string>("Enter Spotify Client ID (leave blank to skip):");
                _modified = true;
                break;
            case "Client Secret":
                _config.Spotify.ClientSecret = AnsiConsole.Ask<string>("Enter Spotify Client Secret (leave blank to skip):");
                _modified = true;
                break;
            case "Default Device":
                _config.Spotify.DefaultDevice = AnsiConsole.Ask<string>("Enter Spotify Device Name (leave blank to skip):");
                _modified = true;
                break;
        }
    }

    private void ConfigureSonosAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan]Sonos Configuration[/]");
        AnsiConsole.WriteLine();

        var options = new[] { "API Bridge URL", "Default Room", "API Timeout (ms)", "Back" };
        var setting = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to configure?")
                .AddChoices(options));

        switch (setting)
        {
            case "API Bridge URL":
                _config.Sonos.HttpApiBaseUrl = AnsiConsole.Ask<string>("Enter Sonos HTTP API Bridge URL (e.g., http://192.168.1.24:5005):");
                _modified = true;
                break;
            case "Default Room":
                _config.Sonos.DefaultRoom = AnsiConsole.Ask<string>("Enter Default Sonos Room Name (leave blank to skip):");
                _modified = true;
                break;
            case "API Timeout (ms)":
                var timeout = AnsiConsole.Ask<int>($"Enter API timeout in milliseconds (current: {_config.Sonos.TimeoutMs}):");
                if (timeout > 0)
                {
                    _config.Sonos.TimeoutMs = timeout;
                    _modified = true;
                }
                break;
        }
    }

    private void ConfigureCacheAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan]Cache Configuration[/]");
        AnsiConsole.WriteLine();

        var options = new[]
        {
            $"Enable Cache (current: {(_config.CacheEnabled ? "ON" : "OFF")})",
            $"Cache Expiry Minutes (current: {_config.CacheExpiryMinutes})",
            "Back"
        };
        var setting = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to configure?")
                .AddChoices(options));

        switch (setting)
        {
            case var s when s.StartsWith("Enable Cache"):
                _config.CacheEnabled = !_config.CacheEnabled;
                _modified = true;
                break;
            case var s when s.StartsWith("Cache Expiry"):
                var expiry = AnsiConsole.Ask<int>("Enter cache expiry in minutes:");
                if (expiry > 0)
                {
                    _config.CacheExpiryMinutes = expiry;
                    _modified = true;
                }
                break;
        }
    }

    private void DisplayConfiguration()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold cyan]Current Configuration[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");
        table.AddColumn("Status");

        // Last.fm
        table.AddRow("Last.fm API Key", MaskValue(_config.ApiKey), string.IsNullOrEmpty(_config.ApiKey) ? "[red]✕[/]" : "[green]✓[/]");
        table.AddRow("Last.fm Username", _config.DefaultUsername ?? "(not set)", string.IsNullOrEmpty(_config.DefaultUsername) ? "[red]✕[/]" : "[green]✓[/]");
        table.AddRow("API Throttle", $"{_config.ApiThrottleMs}ms", "[cyan]●[/]");

        // Spotify
        table.AddRow("Spotify Client ID", MaskValue(_config.Spotify.ClientId), string.IsNullOrEmpty(_config.Spotify.ClientId) ? "[red]✕[/]" : "[green]✓[/]");
        table.AddRow("Spotify Authenticated", string.IsNullOrEmpty(_config.Spotify.RefreshToken) ? "No" : "Yes", string.IsNullOrEmpty(_config.Spotify.RefreshToken) ? "[red]✕[/]" : "[green]✓[/]");

        // Sonos
        table.AddRow("Sonos API URL", _config.Sonos.HttpApiBaseUrl ?? "(not set)", string.IsNullOrEmpty(_config.Sonos.HttpApiBaseUrl) ? "[red]✕[/]" : "[green]✓[/]");
        table.AddRow("Sonos Default Room", _config.Sonos.DefaultRoom ?? "(not set)", string.IsNullOrEmpty(_config.Sonos.DefaultRoom) ? "[red]✕[/]" : "[green]✓[/]");

        // Cache
        table.AddRow("Cache Enabled", _config.CacheEnabled ? "Yes" : "No", "[cyan]●[/]");
        table.AddRow("Cache Expiry", $"{_config.CacheExpiryMinutes} min", "[cyan]●[/]");

        AnsiConsole.Write(table);
    }

    private string MaskValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "(not set)";
        if (value.Length <= 4)
            return "****";
        return $"{value.Substring(0, 2)}...{value.Substring(value.Length - 2)}";
    }
}
