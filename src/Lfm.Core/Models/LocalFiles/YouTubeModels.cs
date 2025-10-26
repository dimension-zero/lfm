using System.Text.Json.Serialization;

namespace Lfm.Core.Models.LocalFiles;

/// <summary>
/// YouTube Music watch history format (watch-history.json from Google Takeout)
/// Downloaded via Google Takeout -> YouTube and YouTube Music
/// Contains YouTube and YouTube Music viewing/listening history
/// </summary>
public class YouTubeWatchHistoryItem
{
    /// <summary>
    /// Header text (e.g., "Watched a video from YouTube Music")
    /// </summary>
    [JsonPropertyName("header")]
    public string? Header { get; set; }

    /// <summary>
    /// Video/track title
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// URL to the video/track (e.g., "https://www.youtube.com/watch?v=...")
    /// </summary>
    [JsonPropertyName("titleUrl")]
    public string? TitleUrl { get; set; }

    /// <summary>
    /// Timestamp when watched (ISO 8601 format with timezone)
    /// Format: "2024-01-15T13:45:30.123Z"
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// Products where this was watched (e.g., ["YouTube Music"])
    /// </summary>
    [JsonPropertyName("products")]
    public List<string>? Products { get; set; }

    /// <summary>
    /// Activity controls that apply to this entry
    /// </summary>
    [JsonPropertyName("activityControls")]
    public List<string>? ActivityControls { get; set; }

    /// <summary>
    /// Subtitles/description (may contain artist information)
    /// </summary>
    [JsonPropertyName("subtitles")]
    public List<YouTubeSubtitle>? Subtitles { get; set; }

    /// <summary>
    /// Details about the viewing/listening session
    /// </summary>
    [JsonPropertyName("details")]
    public List<YouTubeDetail>? Details { get; set; }

    /// <summary>
    /// Whether this entry is from YouTube Music specifically
    /// </summary>
    [JsonIgnore]
    public bool IsYouTubeMusic => Products?.Contains("YouTube Music") == true;

    /// <summary>
    /// Whether this entry represents music playback (vs video watching)
    /// Heuristic: Check header for "Music" or products list
    /// </summary>
    [JsonIgnore]
    public bool IsMusicPlayback => IsYouTubeMusic ||
                                   (Header?.Contains("Music", StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>
    /// Computed property: Artist name extracted from subtitles
    /// First subtitle entry is typically the artist/channel name
    /// </summary>
    [JsonIgnore]
    public string? Artist => Subtitles?.FirstOrDefault()?.Name;
}

/// <summary>
/// Subtitle information from YouTube watch history
/// Often contains artist/channel name
/// </summary>
public class YouTubeSubtitle
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Detail information from YouTube watch history
/// May contain "From Google Ads" or other metadata
/// </summary>
public class YouTubeDetail
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// YouTube Music Library Songs (music-library-songs.csv from Google Takeout)
/// CSV format with columns: Title, Album, Artist, Duration, Rating, Play Count, Removed
/// </summary>
public class YouTubeMusicLibrarySong
{
    /// <summary>
    /// Song title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Album name (may be empty)
    /// </summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>
    /// Artist name
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Duration in format "MM:SS" or "H:MM:SS"
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// User rating (0-5 stars, or empty)
    /// </summary>
    public string Rating { get; set; } = string.Empty;

    /// <summary>
    /// Number of times played (may be empty if not tracked)
    /// </summary>
    public string PlayCount { get; set; } = string.Empty;

    /// <summary>
    /// Whether the song was removed from library (Yes/No)
    /// </summary>
    public string Removed { get; set; } = string.Empty;

    /// <summary>
    /// Parsed play count as integer (0 if empty or invalid)
    /// </summary>
    [JsonIgnore]
    public int PlayCountValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PlayCount))
                return 0;
            return int.TryParse(PlayCount, out var count) ? count : 0;
        }
    }

    /// <summary>
    /// Whether this song is still in the library (not removed)
    /// </summary>
    [JsonIgnore]
    public bool IsInLibrary => !string.Equals(Removed, "Yes", StringComparison.OrdinalIgnoreCase);
}
