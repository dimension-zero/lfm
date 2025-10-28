using Serilog;
using Serilog.Events;

namespace Lfm.Core.Logging;

/// <summary>
/// Centralized Serilog logger factory (Singleton pattern).
/// Creates and configures a static Serilog logger for the entire application.
/// </summary>
public static class LoggerFactory
{
    private static ILogger? _logger;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the singleton Serilog logger instance.
    /// </summary>
    public static ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                lock (_lock)
                {
                    _logger ??= CreateLogger();
                }
            }
            return _logger;
        }
    }

    /// <summary>
    /// Creates and configures the Serilog logger with console and file sinks.
    /// </summary>
    private static ILogger CreateLogger()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "lfm",
            "logs"
        );

        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "lfm-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Warning // Only warnings/errors to console
            )
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7, // Keep last 7 days
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug
            )
            .CreateLogger();
    }

    /// <summary>
    /// Updates the minimum log level for console output (for debugging).
    /// </summary>
    public static void SetConsoleLogLevel(LogEventLevel level)
    {
        // Note: This recreates the logger - use sparingly
        lock (_lock)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: level
                )
                .WriteTo.File(
                    path: Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "lfm", "logs", "lfm-.log"
                    ),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Debug
                )
                .CreateLogger();
        }
    }

    /// <summary>
    /// Closes and flushes the logger (call on application shutdown).
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
