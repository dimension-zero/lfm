namespace Lfm.Tests.Fixtures;

/// <summary>
/// Sonos HTTP API response fixtures for testing
/// Contains JSON constants for mocking node-sonos-http-api responses
/// </summary>
public static class SonosApiFixtures
{
    // ========== Room/Zone Responses ==========

    public static string GetRoomsResponse_MultipleRooms => """
        {
          "zones": [
            {
              "uuid": "RINCON_ABC123DEF456_0",
              "name": "Living Room",
              "coordinator": "RINCON_ABC123DEF456_0",
              "groupName": null,
              "groupId": null,
              "musicroomGroupId": null,
              "roomIcon": "living-room",
              "playbackState": "PLAYING",
              "currentTrack": {
                "artist": "Pink Floyd",
                "title": "Money",
                "album": "The Dark Side of the Moon",
                "albumArtURI": "http://example.com/art.jpg",
                "trackNumber": 4,
                "duration": 245,
                "uri": "spotify:track:3z8h0TU7RvxVfncizLUDVi"
              },
              "relTime": 45,
              "totalTime": 245,
              "elapsedSeconds": 45,
              "elapsedTime": "0:45",
              "host": "192.168.1.100",
              "port": 1400,
              "volume": 50,
              "volumeNormalized": 0.5,
              "mute": false,
              "equalizer": {},
              "avTransportURI": "",
              "avTransportURIMetaData": "",
              "enqueuedMetaData": null,
              "enqueuedTransportURI": null,
              "enqueuedTransportURIMetaData": null,
              "nextTrackMetaData": null,
              "nextAVTransportURI": null
            },
            {
              "uuid": "RINCON_XYZ789GHI012_0",
              "name": "Kitchen",
              "coordinator": "RINCON_ABC123DEF456_0",
              "groupName": "Living Room",
              "groupId": "RINCON_ABC123DEF456_0:1",
              "musicroomGroupId": "RINCON_ABC123DEF456_0:1",
              "roomIcon": "kitchen",
              "playbackState": "PLAYING",
              "currentTrack": null,
              "relTime": 0,
              "totalTime": 0,
              "elapsedSeconds": 0,
              "elapsedTime": "0:00",
              "host": "192.168.1.101",
              "port": 1400,
              "volume": 40,
              "volumeNormalized": 0.4,
              "mute": false
            }
          ]
        }
        """;

    public static string GetRoomsResponse_SingleRoom => """
        {
          "zones": [
            {
              "uuid": "RINCON_ABC123DEF456_0",
              "name": "Living Room",
              "coordinator": "RINCON_ABC123DEF456_0",
              "groupName": null,
              "roomIcon": "living-room",
              "playbackState": "STOPPED",
              "currentTrack": null,
              "volume": 50,
              "mute": false,
              "host": "192.168.1.100",
              "port": 1400
            }
          ]
        }
        """;

    public static string GetRoomsResponse_Empty => """
        {
          "zones": []
        }
        """;

    // ========== Playback State Responses ==========

    public static string GetPlaybackStateResponse_Playing => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "PLAYING",
          "currentTrack": {
            "artist": "Pink Floyd",
            "title": "Money",
            "album": "The Dark Side of the Moon",
            "albumArtURI": "http://example.com/art.jpg",
            "trackNumber": 4,
            "duration": 245,
            "uri": "spotify:track:3z8h0TU7RvxVfncizLUDVi"
          },
          "elapsedTime": "0:45",
          "elapsedSeconds": 45,
          "totalTime": 245
        }
        """;

    public static string GetPlaybackStateResponse_Paused => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "PAUSED_PLAYBACK",
          "currentTrack": {
            "artist": "Pink Floyd",
            "title": "Money",
            "album": "The Dark Side of the Moon",
            "duration": 245,
            "uri": "spotify:track:3z8h0TU7RvxVfncizLUDVi"
          },
          "elapsedTime": "0:45",
          "elapsedSeconds": 45,
          "totalTime": 245
        }
        """;

    public static string GetPlaybackStateResponse_Stopped => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "STOPPED",
          "currentTrack": null,
          "elapsedTime": "0:00",
          "elapsedSeconds": 0,
          "totalTime": 0
        }
        """;

    // ========== Playback Control Responses ==========

    public static string PlayResponse_Success => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "PLAYING"
        }
        """;

    public static string PauseResponse_Success => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "PAUSED_PLAYBACK"
        }
        """;

    public static string ResumeResponse_Success => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "PLAYING"
        }
        """;

    public static string SkipResponse_Success => """
        {
          "uuid": "RINCON_ABC123DEF456_0",
          "name": "Living Room",
          "state": "PLAYING",
          "currentTrack": {
            "artist": "Pink Floyd",
            "title": "Breathe (In the Air)",
            "album": "The Dark Side of the Moon",
            "duration": 174,
            "uri": "spotify:track:track2"
          }
        }
        """;

    // ========== Error Responses ==========

    public static string ErrorResponse_RoomNotFound => """
        {
          "error": "Could not find a room with name: NonExistent Room",
          "code": "ROOM_NOT_FOUND"
        }
        """;

    public static string ErrorResponse_ApiTimeout => """
        {
          "error": "Request timeout after 5000ms",
          "code": "TIMEOUT"
        }
        """;

    public static string ErrorResponse_InvalidUri => """
        {
          "error": "Invalid Spotify URI format",
          "code": "INVALID_URI"
        }
        """;

    public static string ErrorResponse_ServiceUnavailable => """
        {
          "error": "Sonos HTTP API service unavailable",
          "code": "SERVICE_UNAVAILABLE"
        }
        """;
}
