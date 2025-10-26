using System.Text.Json;
using Lfm.Core.Models;
using Lfm.Core.Models.LocalFiles;

namespace Lfm.Core.Services.LocalFiles;

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
                return Result<List<PlayEvent>>.Failure($"File not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return Result<List<PlayEvent>>.Failure("File is empty");

            // Try to detect format by parsing first item
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                return Result<List<PlayEvent>>.Failure("Expected JSON array at root");

            if (root.GetArrayLength() == 0)
                return Result<List<PlayEvent>>.Success(new List<PlayEvent>());

            var firstItem = root[0];
            var isExtendedFormat = firstItem.TryGetProperty("ms_played", out _);

            if (isExtendedFormat)
                return await ParseExtendedHistoryAsync(json, startDate, endDate);
            else
                return await ParseStandardHistoryAsync(json, startDate, endDate);
        }
        catch (JsonException ex)
        {
            return Result<List<PlayEvent>>.Failure($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.Failure($"Error parsing Spotify file: {ex.Message}");
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
                return Result<List<PlayEvent>>.Failure("Failed to deserialize Spotify Extended History");

            var events = items
                .Where(item => item.IsMusicPlayback) // Filter out podcasts/audiobooks
                .Where(item => !item.Skipped) // Filter out skipped tracks
                .Select(item => new PlayEvent
                {
                    Artist = item.MasterMetadataAlbumArtistName ?? item.ArtistName ?? "Unknown Artist",
                    Track = item.MasterMetadataTrackName ?? item.TrackName ?? "Unknown Track",
                    Album = item.MasterMetadataAlbumAlbumName,
                    PlayedAt = item.Ts,
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

            return Result<List<PlayEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.Failure($"Error parsing Extended History: {ex.Message}");
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
                return Result<List<PlayEvent>>.Failure("Failed to deserialize Spotify Standard History");

            var events = items
                .Where(item => item.IsMusicPlayback) // Filter out podcasts
                .Select(item => new PlayEvent
                {
                    Artist = item.ArtistName ?? "Unknown Artist",
                    Track = item.TrackName ?? "Unknown Track",
                    Album = null, // Standard format doesn't include album
                    PlayedAt = item.EndTime,
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

            return Result<List<PlayEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            return Result<List<PlayEvent>>.Failure($"Error parsing Standard History: {ex.Message}");
        }
    }
}
