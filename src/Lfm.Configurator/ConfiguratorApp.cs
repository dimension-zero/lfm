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
    private readonly bool _dryRun;
    private readonly List<string> _dryRunLog;

    public ConfiguratorApp(IConfigurationManager configManager, LfmConfig config, bool dryRun = false)
    {
        _configManager = configManager;
        _config = config;
        _dryRun = dryRun;
        _dryRunLog = new List<string>();
    }

    /// <summary>
    /// Returns the log of all operations performed during dry-run mode
    /// </summary>
    public IReadOnlyList<string> DryRunLog => _dryRunLog.AsReadOnly();

    /// <summary>
    /// Returns the current configuration state (useful for testing)
    /// </summary>
    public LfmConfig GetCurrentConfig() => _config;

    public async Task RunAsync()
    {
        if (_dryRun)
        {
            _dryRunLog.Add("DryRun mode enabled - no configuration changes will be persisted");
            AnsiConsole.MarkupLine("[yellow]⚠ DRY RUN MODE - Changes will not be saved[/]");
            AnsiConsole.WriteLine();
        }

        bool running = true;
        while (running)
        {
            if (!_dryRun)
            {
                AnsiConsole.Clear();
            }

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
            if (_dryRun)
            {
                _dryRunLog.Add("Configuration changes were made but NOT saved (dry-run mode)");
                AnsiConsole.MarkupLine("[yellow]✓ Changes in dry-run mode - not persisted to disk[/]");
            }
            else if (AnsiConsole.Confirm("Save changes before exiting?"))
            {
                await _configManager.SaveAsync(_config);
                _dryRunLog.Add("Configuration saved successfully");
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
        if (!_dryRun)
        {
            AnsiConsole.Clear();
        }

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
                var oldApiKey = _config.ApiKey;
                _config.ApiKey = AnsiConsole.Ask<string>("Enter Last.fm API Key (leave blank to skip):");
                if (!string.IsNullOrEmpty(_config.ApiKey) && _config.ApiKey != oldApiKey)
                {
                    _dryRunLog.Add($"Last.fm API Key changed from '{MaskValue(oldApiKey)}' to '{MaskValue(_config.ApiKey)}'");
                    _modified = true;
                }
                break;
            case "Default Username":
                var oldUsername = _config.DefaultUsername;
                _config.DefaultUsername = AnsiConsole.Ask<string>("Enter Last.fm Username (leave blank to skip):");
                if (!string.IsNullOrEmpty(_config.DefaultUsername) && _config.DefaultUsername != oldUsername)
                {
                    _dryRunLog.Add($"Last.fm Default Username changed from '{oldUsername}' to '{_config.DefaultUsername}'");
                    _modified = true;
                }
                break;
            case "API Throttle (ms)":
                var oldThrottle = _config.ApiThrottleMs;
                var throttle = AnsiConsole.Ask<int>($"Enter API throttle in milliseconds (current: {_config.ApiThrottleMs}):");
                if (throttle > 0 && throttle != oldThrottle)
                {
                    _dryRunLog.Add($"Last.fm API Throttle changed from {oldThrottle}ms to {throttle}ms");
                    _config.ApiThrottleMs = throttle;
                    _modified = true;
                }
                break;
        }
    }

    private void ConfigureSpotifyAsync()
    {
        if (!_dryRun)
        {
            AnsiConsole.Clear();
        }

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
                var oldClientId = _config.Spotify.ClientId;
                _config.Spotify.ClientId = AnsiConsole.Ask<string>("Enter Spotify Client ID (leave blank to skip):");
                if (!string.IsNullOrEmpty(_config.Spotify.ClientId) && _config.Spotify.ClientId != oldClientId)
                {
                    _dryRunLog.Add($"Spotify Client ID changed from '{MaskValue(oldClientId)}' to '{MaskValue(_config.Spotify.ClientId)}'");
                    _modified = true;
                }
                break;
            case "Client Secret":
                var oldSecret = _config.Spotify.ClientSecret;
                _config.Spotify.ClientSecret = AnsiConsole.Ask<string>("Enter Spotify Client Secret (leave blank to skip):");
                if (!string.IsNullOrEmpty(_config.Spotify.ClientSecret) && _config.Spotify.ClientSecret != oldSecret)
                {
                    _dryRunLog.Add($"Spotify Client Secret changed from '{MaskValue(oldSecret)}' to '{MaskValue(_config.Spotify.ClientSecret)}'");
                    _modified = true;
                }
                break;
            case "Default Device":
                var oldDevice = _config.Spotify.DefaultDevice;
                _config.Spotify.DefaultDevice = AnsiConsole.Ask<string>("Enter Spotify Device Name (leave blank to skip):");
                if (!string.IsNullOrEmpty(_config.Spotify.DefaultDevice) && _config.Spotify.DefaultDevice != oldDevice)
                {
                    _dryRunLog.Add($"Spotify Default Device changed from '{oldDevice}' to '{_config.Spotify.DefaultDevice}'");
                    _modified = true;
                }
                break;
        }
    }

    private void ConfigureSonosAsync()
    {
        if (!_dryRun)
        {
            AnsiConsole.Clear();
        }

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
                var oldUrl = _config.Sonos.HttpApiBaseUrl;
                _config.Sonos.HttpApiBaseUrl = AnsiConsole.Ask<string>("Enter Sonos HTTP API Bridge URL (e.g., http://192.168.1.24:5005):");
                if (!string.IsNullOrEmpty(_config.Sonos.HttpApiBaseUrl) && _config.Sonos.HttpApiBaseUrl != oldUrl)
                {
                    _dryRunLog.Add($"Sonos API Bridge URL changed from '{oldUrl}' to '{_config.Sonos.HttpApiBaseUrl}'");
                    _modified = true;
                }
                break;
            case "Default Room":
                var oldRoom = _config.Sonos.DefaultRoom;
                _config.Sonos.DefaultRoom = AnsiConsole.Ask<string>("Enter Default Sonos Room Name (leave blank to skip):");
                if (!string.IsNullOrEmpty(_config.Sonos.DefaultRoom) && _config.Sonos.DefaultRoom != oldRoom)
                {
                    _dryRunLog.Add($"Sonos Default Room changed from '{oldRoom}' to '{_config.Sonos.DefaultRoom}'");
                    _modified = true;
                }
                break;
            case "API Timeout (ms)":
                var oldTimeout = _config.Sonos.TimeoutMs;
                var timeout = AnsiConsole.Ask<int>($"Enter API timeout in milliseconds (current: {_config.Sonos.TimeoutMs}):");
                if (timeout > 0 && timeout != oldTimeout)
                {
                    _dryRunLog.Add($"Sonos API Timeout changed from {oldTimeout}ms to {timeout}ms");
                    _config.Sonos.TimeoutMs = timeout;
                    _modified = true;
                }
                break;
        }
    }

    private void ConfigureCacheAsync()
    {
        if (!_dryRun)
        {
            AnsiConsole.Clear();
        }

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
                var oldCacheEnabled = _config.CacheEnabled;
                _config.CacheEnabled = !_config.CacheEnabled;
                _dryRunLog.Add($"Cache enabled changed from {oldCacheEnabled} to {_config.CacheEnabled}");
                _modified = true;
                break;
            case var s when s.StartsWith("Cache Expiry"):
                var oldExpiry = _config.CacheExpiryMinutes;
                var expiry = AnsiConsole.Ask<int>("Enter cache expiry in minutes:");
                if (expiry > 0 && expiry != oldExpiry)
                {
                    _dryRunLog.Add($"Cache expiry changed from {oldExpiry} minutes to {expiry} minutes");
                    _config.CacheExpiryMinutes = expiry;
                    _modified = true;
                }
                break;
        }
    }

    private void DisplayConfiguration()
    {
        if (!_dryRun)
        {
            AnsiConsole.Clear();
        }

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
