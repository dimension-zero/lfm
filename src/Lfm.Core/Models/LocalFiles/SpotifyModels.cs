using System.Text.Json.Serialization;

namespace Lfm.Core.Models.LocalFiles;

/// <summary>
/// Spotify Extended Streaming History format (endsong*.json files)
/// Downloaded via Spotify Privacy Settings -> "Extended streaming history"
/// Contains detailed listening history with timestamps and track metadata
/// </summary>
public class SpotifyEndsong
{
    /// <summary>
    /// UTC timestamp when the track finished playing
    /// </summary>
    [JsonPropertyName("ts")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Username of the Spotify account
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Platform used for playback (e.g., "Windows", "Android", "iOS")
    /// </summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>
    /// Milliseconds played (0 if skipped immediately)
    /// </summary>
    [JsonPropertyName("ms_played")]
    public int MsPlayed { get; set; }

    /// <summary>
    /// Country code where playback occurred
    /// </summary>
    [JsonPropertyName("conn_country")]
    public string? Country { get; set; }

    /// <summary>
    /// Spotify URI for the track (e.g., "spotify:track:...")
    /// </summary>
    [JsonPropertyName("spotify_track_uri")]
    public string? TrackUri { get; set; }

    /// <summary>
    /// Track name from Spotify's master metadata
    /// </summary>
    [JsonPropertyName("master_metadata_track_name")]
    public string? TrackName { get; set; }

    /// <summary>
    /// Artist name from Spotify's master metadata
    /// </summary>
    [JsonPropertyName("master_metadata_album_artist_name")]
    public string? ArtistName { get; set; }

    /// <summary>
    /// Album name from Spotify's master metadata
    /// </summary>
    [JsonPropertyName("master_metadata_album_album_name")]
    public string? AlbumName { get; set; }

    /// <summary>
    /// Reason playback started (e.g., "trackdone", "fwdbtn", "backbtn", "clickrow")
    /// </summary>
    [JsonPropertyName("reason_start")]
    public string? ReasonStart { get; set; }

    /// <summary>
    /// Reason playback ended (e.g., "trackdone", "fwdbtn", "backbtn", "logout")
    /// </summary>
    [JsonPropertyName("reason_end")]
    public string? ReasonEnd { get; set; }

    /// <summary>
    /// Whether the track was skipped (true if played < 30 seconds or < 50% of duration)
    /// </summary>
    [JsonPropertyName("skipped")]
    public bool? Skipped { get; set; }

    /// <summary>
    /// Whether shuffle mode was enabled
    /// </summary>
    [JsonPropertyName("shuffle")]
    public bool? Shuffle { get; set; }

    /// <summary>
    /// Whether offline mode was used
    /// </summary>
    [JsonPropertyName("offline")]
    public bool? Offline { get; set; }

    /// <summary>
    /// Timestamp when playback ended (usually same as ts or slightly after)
    /// </summary>
    [JsonPropertyName("offline_timestamp")]
    public long? OfflineTimestamp { get; set; }

    /// <summary>
    /// Whether playback was considered "incognito" (private session)
    /// </summary>
    [JsonPropertyName("incognito_mode")]
    public bool? IncognitoMode { get; set; }
}

/// <summary>
/// Spotify Standard Streaming History format (StreamingHistory*.json files)
/// Downloaded via Spotify Account Privacy -> "Download your data"
/// Contains last 1 year of listening history (less detailed than Extended History)
/// </summary>
public class SpotifyStreamingHistory
{
    /// <summary>
    /// UTC timestamp when the track ended
    /// Format: "YYYY-MM-DD HH:MM" (not full ISO8601)
    /// </summary>
    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    /// <summary>
    /// Artist name
    /// </summary>
    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }

    /// <summary>
    /// Track name
    /// </summary>
    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }

    /// <summary>
    /// Milliseconds played
    /// </summary>
    [JsonPropertyName("msPlayed")]
    public int MsPlayed { get; set; }

    /// <summary>
    /// Parsed DateTime from EndTime string (computed property)
    /// </summary>
    [JsonIgnore]
    public DateTime? EndTimeDateTime
    {
        get
        {
            if (string.IsNullOrEmpty(EndTime))
                return null;

            // Standard format: "2024-01-15 13:45" (no seconds, no timezone)
            if (DateTime.TryParse(EndTime, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            return null;
        }
    }
}
