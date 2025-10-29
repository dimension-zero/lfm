using System.Text.Json;
using Lfm.Shared.Models;
using Lfm.Shared.Models.LocalFiles;
using Lfm.Shared.Models.Results;

namespace Lfm.Data.Direct.LocalFiles;

/// <summary>
/// Parses Spotify Extended History and Standard History JSON files.
/// Auto-detects format based on presence of "ms_played" field.
/// </summary>
public class SpotifyJsonParser : ILocalFileParser
{
    private const int MinimumPlayDurationMs = 30000; // 30 seconds for "full play"

    public string DataSource => "Spotify";

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        // Spotify files typically named like:
        // - Streaming_History_Audio_2023_0.json (Extended)
        // - StreamingHistory0.json (Standard)
        return fileName.Contains("streaming") &&
               fileName.Contains("history") &&
               fileName.EndsWith(".json");
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

            var json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return Result<List<PlayEvent>>.DataError("File is empty");

            // Try to detect format by parsing first item
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                return Result<List<PlayEvent>>.DataError("Expected JSON array at root");

            if (root.GetArrayLength() == 0)
                return Result<List<PlayEvent>>.Ok(new List<PlayEvent>());

            var firstItem = root[0];
            var isExtendedFormat = firstItem.TryGetProperty("ms_played", out _);

            if (isExtendedFormat)
                return await ParseExtendedHistoryAsync(json, startDate, endDate);
            else
                return await ParseStandardHistoryAsync(json, startDate, endDate);
        }
        catch (JsonException ex)
        {
            return Result<List<PlayEvent>>.DataError($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.DataError($"Error parsing Spotify file: {ex.Message}");
        }
    }

    private async Task<Result<List<PlayEvent>>> ParseExtendedHistoryAsync(
        string json,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<SpotifyExtendedHistoryItem>>(json, options);

            if (items == null)
                return Result<List<PlayEvent>>.DataError("Failed to deserialize Spotify Extended History");

            var events = items
                .Where(item => item.IsMusicPlayback) // Filter out podcasts/audiobooks
                .Where(item => item.Skipped != true) // Filter out skipped tracks (handle nullable)
                .Select(item => new PlayEvent
                {
                    Artist = item.ArtistName ?? "Unknown Artist",
                    Track = item.TrackName ?? "Unknown Track",
                    Album = item.AlbumName,
                    PlayedAt = item.Timestamp,
                    DataSource = DataSource,
                    DurationMs = item.MsPlayed,
                    IsFullPlay = item.MsPlayed >= MinimumPlayDurationMs
                })
                .ToList();

            // Apply date filters
            if (startDate.HasValue)
                events = events.Where(e => e.PlayedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                events = events.Where(e => e.PlayedAt <= endDate.Value).ToList();

            return Result<List<PlayEvent>>.Ok(events);
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.DataError($"Error parsing Extended History: {ex.Message}");
        }
    }

    private async Task<Result<List<PlayEvent>>> ParseStandardHistoryAsync(
        string json,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var items = JsonSerializer.Deserialize<List<SpotifyStandardHistoryItem>>(json, options);

            if (items == null)
                return Result<List<PlayEvent>>.DataError("Failed to deserialize Spotify Standard History");

            var events = items
                .Where(item => item.IsMusicPlayback) // Filter out podcasts
                .Where(item => item.EndTimeDateTime.HasValue) // Must have valid timestamp
                .Select(item => new PlayEvent
                {
                    Artist = item.ArtistName ?? "Unknown Artist",
                    Track = item.TrackName ?? "Unknown Track",
                    Album = null, // Standard format doesn't include album
                    PlayedAt = item.EndTimeDateTime!.Value,
                    DataSource = DataSource,
                    DurationMs = item.MsPlayed,
                    IsFullPlay = true // Standard format only includes completed plays
                })
                .ToList();

            // Apply date filters
            if (startDate.HasValue)
                events = events.Where(e => e.PlayedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                events = events.Where(e => e.PlayedAt <= endDate.Value).ToList();

            return Result<List<PlayEvent>>.Ok(events);
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.DataError($"Error parsing Standard History: {ex.Message}");
        }
    }
}
