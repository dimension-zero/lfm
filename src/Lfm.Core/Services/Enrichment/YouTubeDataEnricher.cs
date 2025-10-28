using Microsoft.Extensions.Logging;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models.Results;

namespace Lfm.Core.Services.Enrichment;

/// <summary>
/// Album enrichment using YouTube Music library CSV export.
/// Loads CSV once into memory for fast offline lookups.
/// No API calls required.
/// </summary>
public class YouTubeDataEnricher : IAlbumEnricher
{
    private readonly ILogger<YouTubeDataEnricher> _logger;
    private readonly string _csvPath;
    private readonly Dictionary<string, string> _albumLookup;
    private bool _isInitialized;

    public string EnricherName => "YouTubeCsv";

    public YouTubeDataEnricher(
        ILogger<YouTubeDataEnricher> logger,
        AlbumEnrichmentConfig config)
    {
        _logger = logger;
        _csvPath = config.YouTubeLibraryCsvPath;
        _albumLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _isInitialized = false;
    }

    public async Task<Result<string?>> GetAlbumAsync(string artist, string track)
    {
        try
        {
            // Lazy load CSV on first use
            if (!_isInitialized)
            {
                var loadResult = await LoadCsvAsync();
                if (!loadResult.IsSuccess)
                {
                    return Result<string?>.DataError(loadResult.ErrorMessage ?? "Failed to load YouTube CSV");
                }
            }

            var key = BuildLookupKey(artist, track);

            if (_albumLookup.TryGetValue(key, out var album))
            {
                _logger.LogDebug("YouTube CSV found album '{Album}' for {Artist} - {Track}", album, artist, track);
                return Result<string?>.Ok(album);
            }

            _logger.LogDebug("YouTube CSV: no album found for {Artist} - {Track}", artist, track);
            return Result<string?>.Ok(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube CSV enricher exception for {Artist} - {Track}", artist, track);
            return Result<string?>.DataError($"YouTube CSV error: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the YouTube Music library CSV and builds lookup dictionary.
    /// </summary>
    private async Task<Result<bool>> LoadCsvAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_csvPath))
            {
                _logger.LogWarning("YouTube CSV path not configured");
                _isInitialized = true; // Mark as initialized to avoid repeated attempts
                return Result<bool>.DataError("YouTube library CSV path not configured");
            }

            if (!File.Exists(_csvPath))
            {
                _logger.LogWarning("YouTube CSV file not found: {Path}", _csvPath);
                _isInitialized = true;
                return Result<bool>.DataError($"YouTube library CSV not found: {_csvPath}");
            }

            var lines = await File.ReadAllLinesAsync(_csvPath);

            if (lines.Length < 2)
            {
                _logger.LogWarning("YouTube CSV is empty or has no data rows");
                _isInitialized = true;
                return Result<bool>.DataError("YouTube library CSV is empty");
            }

            var header = lines[0].Split(',');
            var titleIdx = Array.FindIndex(header, h => h.Trim().Equals("Title", StringComparison.OrdinalIgnoreCase));
            var artistIdx = Array.FindIndex(header, h => h.Trim().Equals("Artist", StringComparison.OrdinalIgnoreCase));
            var albumIdx = Array.FindIndex(header, h => h.Trim().Equals("Album", StringComparison.OrdinalIgnoreCase));

            if (titleIdx == -1 || artistIdx == -1 || albumIdx == -1)
            {
                _logger.LogError("YouTube CSV missing required columns: Title, Artist, Album");
                _isInitialized = true;
                return Result<bool>.DataError("YouTube CSV missing required columns");
            }

            var loaded = 0;
            var skipped = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var row = ParseCsvLine(lines[i]);

                if (row.Count <= Math.Max(Math.Max(titleIdx, artistIdx), albumIdx))
                {
                    skipped++;
                    continue;
                }

                var artist = row[artistIdx].Trim();
                var title = row[titleIdx].Trim();
                var album = row[albumIdx].Trim();

                if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
                {
                    skipped++;
                    continue;
                }

                // Skip if no album data
                if (string.IsNullOrWhiteSpace(album))
                {
                    skipped++;
                    continue;
                }

                var key = BuildLookupKey(artist, title);

                // Only add first occurrence (prefer first entry if duplicates exist)
                if (!_albumLookup.ContainsKey(key))
                {
                    _albumLookup[key] = album;
                    loaded++;
                }
            }

            _isInitialized = true;
            _logger.LogInformation(
                "YouTube CSV loaded: {Loaded} tracks with albums, {Skipped} skipped",
                loaded, skipped);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading YouTube CSV from {Path}", _csvPath);
            _isInitialized = true; // Mark as initialized to avoid retry loop
            return Result<bool>.DataError($"Failed to load YouTube CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds normalized lookup key from artist and track.
    /// Format: "artist|track" (lowercase for case-insensitive matching)
    /// </summary>
    private string BuildLookupKey(string artist, string track)
    {
        return $"{artist}|{track}".ToLowerInvariant();
    }

    /// <summary>
    /// Simple CSV parser that handles quoted fields.
    /// Reused pattern from YouTubeMusicParser.
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        fields.Add(currentField.ToString());
        return fields;
    }
}
