using System.Globalization;
using System.Text.Json;
using Lfm.Shared.Models;
using Lfm.Shared.Models.LocalFiles;
using Lfm.Shared.Models.Results;

namespace Lfm.Data.Direct.LocalFiles;

/// <summary>
/// Parses YouTube Music history files (JSON Watch History and CSV Library).
/// </summary>
public class YouTubeMusicParser : ILocalFileParser
{
    public string DataSource => "YouTube Music";

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        // YouTube Music files typically named:
        // - watch-history.json
        // - library-songs.csv
        return (fileName.Contains("watch-history") && fileName.EndsWith(".json")) ||
               (fileName.Contains("library") && fileName.Contains("song") && fileName.EndsWith(".csv"));
    }

    public async Task<Result<List<PlayEvent>>> ParseAsync(
        string filePath,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result<List<PlayEvent>>.DataError($"File not found: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".json")
                return await ParseWatchHistoryJsonAsync(filePath, startDate, endDate);
            else if (extension == ".csv")
                return await ParseLibraryCsvAsync(filePath, startDate, endDate);
            else
                return Result<List<PlayEvent>>.DataError($"Unsupported file extension: {extension}");
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.DataError($"Error parsing YouTube Music file: {ex.Message}");
        }
    }

    private async Task<Result<List<PlayEvent>>> ParseWatchHistoryJsonAsync(
        string filePath,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return Result<List<PlayEvent>>.DataError("File is empty");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<YouTubeWatchHistoryItem>>(json, options);

            if (items == null)
                return Result<List<PlayEvent>>.DataError("Failed to deserialize YouTube Watch History");

            var events = items
                .Where(item => item.IsMusicPlayback) // Filter to music only
                .Where(item => !string.IsNullOrWhiteSpace(item.Artist)) // Must have artist
                .Select(item => new PlayEvent
                {
                    Artist = item.Artist!,
                    Track = item.Title ?? "Unknown Track",
                    Album = item.Album, // May be null; future: enrich from AllMusic.com or playlist metadata
                    PlayedAt = item.Time,
                    DataSource = DataSource,
                    DurationMs = null, // Not available in watch history
                    IsFullPlay = true // Assume all entries are full plays
                })
                .ToList();

            // Apply date filters
            if (startDate.HasValue)
                events = events.Where(e => e.PlayedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                events = events.Where(e => e.PlayedAt <= endDate.Value).ToList();

            return Result<List<PlayEvent>>.Ok(events);
        }
        catch (JsonException ex)
        {
            return Result<List<PlayEvent>>.DataError($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.DataError($"Error parsing Watch History JSON: {ex.Message}");
        }
    }

    private async Task<Result<List<PlayEvent>>> ParseLibraryCsvAsync(
        string filePath,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);

            if (lines.Length < 2) // Header + at least one row
                return Result<List<PlayEvent>>.Ok(new List<PlayEvent>());

            var header = lines[0].Split(',');
            var titleIdx = Array.FindIndex(header, h => h.Trim().Equals("Title", StringComparison.OrdinalIgnoreCase));
            var artistIdx = Array.FindIndex(header, h => h.Trim().Equals("Artist", StringComparison.OrdinalIgnoreCase));
            var albumIdx = Array.FindIndex(header, h => h.Trim().Equals("Album", StringComparison.OrdinalIgnoreCase));
            var playCountIdx = Array.FindIndex(header, h => h.Trim().Equals("Play count", StringComparison.OrdinalIgnoreCase));
            var removedIdx = Array.FindIndex(header, h => h.Trim().Equals("Removed", StringComparison.OrdinalIgnoreCase));

            if (titleIdx == -1 || artistIdx == -1)
                return Result<List<PlayEvent>>.DataError("CSV missing required columns: Title, Artist");

            var events = new List<PlayEvent>();

            for (int i = 1; i < lines.Length; i++)
            {
                var row = ParseCsvLine(lines[i]);

                if (row.Count <= Math.Max(titleIdx, artistIdx))
                    continue; // Skip malformed rows

                // Filter out removed songs
                if (removedIdx >= 0 && removedIdx < row.Count)
                {
                    var removedValue = row[removedIdx].Trim();
                    if (removedValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        continue; // Skip removed songs
                }

                var artist = row[artistIdx].Trim();
                var title = row[titleIdx].Trim();

                if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
                    continue;

                var album = albumIdx >= 0 && albumIdx < row.Count ? row[albumIdx].Trim() : null;
                var playCountStr = playCountIdx >= 0 && playCountIdx < row.Count ? row[playCountIdx].Trim() : "1";

                if (!int.TryParse(playCountStr, out var playCount))
                    playCount = 1;

                // CSV doesn't have timestamps - use current date as placeholder
                // These will be aggregated by play count, not chronological order
                var placeholderDate = DateTime.UtcNow.Date;

                // Create multiple events for play count > 1
                for (int j = 0; j < playCount; j++)
                {
                    events.Add(new PlayEvent
                    {
                        Artist = artist,
                        Track = title,
                        Album = album,
                        PlayedAt = placeholderDate.AddMinutes(-j), // Slight offset to preserve order
                        DataSource = DataSource,
                        DurationMs = null,
                        IsFullPlay = true
                    });
                }
            }

            // Apply date filters (though dates are placeholders for CSV)
            if (startDate.HasValue)
                events = events.Where(e => e.PlayedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                events = events.Where(e => e.PlayedAt <= endDate.Value).ToList();

            return Result<List<PlayEvent>>.Ok(events);
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.DataError($"Error parsing Library CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Simple CSV parser that handles quoted fields.
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
