namespace Lfm.Tests.Fixtures;

/// <summary>
/// Spotify API response fixtures for testing
/// Contains JSON constants and helper methods for mocking Spotify HTTP responses
/// </summary>
public static class SpotifyApiFixtures
{
    // ========== Search Responses ==========

    public static string SearchTrackResponse_Money_PinkFloyd => """
        {
          "tracks": {
            "href": "https://api.spotify.com/v1/search?query=Money+Pink+Floyd&type=track",
            "items": [
              {
                "album": {
                  "album_type": "album",
                  "external_urls": {
                    "spotify": "https://open.spotify.com/album/4LH4d3cOWNNsVCJS7isY8p"
                  },
                  "href": "https://api.spotify.com/v1/albums/4LH4d3cOWNNsVCJS7isY8p",
                  "id": "4LH4d3cOWNNsVCJS7isY8p",
                  "images": [
                    {
                      "height": 640,
                      "url": "https://i.scdn.co/image/ab67616d0000b27334e4f6e94f7e0d96b94b4d17",
                      "width": 640
                    }
                  ],
                  "name": "The Dark Side of the Moon",
                  "release_date": "1973-03-01",
                  "total_tracks": 10,
                  "type": "album",
                  "uri": "spotify:album:4LH4d3cOWNNsVCJS7isY8p"
                },
                "artists": [
                  {
                    "external_urls": {
                      "spotify": "https://open.spotify.com/artist/0k17h0D3J5VfYkk9zwXOj2"
                    },
                    "href": "https://api.spotify.com/v1/artists/0k17h0D3J5VfYkk9zwXOj2",
                    "id": "0k17h0D3J5VfYkk9zwXOj2",
                    "name": "Pink Floyd",
                    "type": "artist",
                    "uri": "spotify:artist:0k17h0D3J5VfYkk9zwXOj2"
                  }
                ],
                "disc_number": 1,
                "duration_ms": 244973,
                "explicit": false,
                "external_ids": {
                  "isrc": "GBUM71209351"
                },
                "external_urls": {
                  "spotify": "https://open.spotify.com/track/3z8h0TU7RvxVfncizLUDVi"
                },
                "href": "https://api.spotify.com/v1/tracks/3z8h0TU7RvxVfncizLUDVi",
                "id": "3z8h0TU7RvxVfncizLUDVi",
                "is_local": false,
                "name": "Money",
                "popularity": 83,
                "preview_url": "https://p.scdn.co/mp3-preview/...",
                "track_number": 4,
                "type": "track",
                "uri": "spotify:track:3z8h0TU7RvxVfncizLUDVi"
              }
            ],
            "limit": 20,
            "next": null,
            "offset": 0,
            "previous": null,
            "total": 14
          }
        }
        """;

    public static string SearchTrackResponse_NoResults => """
        {
          "tracks": {
            "href": "https://api.spotify.com/v1/search?query=nonexistent&type=track",
            "items": [],
            "limit": 20,
            "next": null,
            "offset": 0,
            "previous": null,
            "total": 0
          }
        }
        """;

    public static string SearchTrackResponse_MultipleVersions => """
        {
          "tracks": {
            "items": [
              {
                "name": "Hey Jude",
                "artists": [{"name": "The Beatles"}],
                "album": {"name": "Hey Jude", "release_date": "1968-08-26"},
                "uri": "spotify:track:v1",
                "id": "v1"
              },
              {
                "name": "Hey Jude",
                "artists": [{"name": "The Beatles"}],
                "album": {"name": "1967-1970", "release_date": "1973-04-19"},
                "uri": "spotify:track:v2",
                "id": "v2"
              },
              {
                "name": "Hey Jude",
                "artists": [{"name": "The Beatles"}],
                "album": {"name": "Live at Abbey Road", "release_date": "2019-09-27"},
                "uri": "spotify:track:v3",
                "id": "v3"
              }
            ],
            "total": 3
          }
        }
        """;

    // ========== Album Responses ==========

    public static string SearchAlbumResponse_DarkSide => """
        {
          "albums": {
            "items": [
              {
                "album_type": "album",
                "external_urls": {
                  "spotify": "https://open.spotify.com/album/4LH4d3cOWNNsVCJS7isY8p"
                },
                "href": "https://api.spotify.com/v1/albums/4LH4d3cOWNNsVCJS7isY8p",
                "id": "4LH4d3cOWNNsVCJS7isY8p",
                "images": [
                  {
                    "height": 640,
                    "url": "https://i.scdn.co/image/ab67616d0000b27334e4f6e94f7e0d96b94b4d17",
                    "width": 640
                  }
                ],
                "name": "The Dark Side of the Moon",
                "release_date": "1973-03-01",
                "total_tracks": 10,
                "type": "album",
                "uri": "spotify:album:4LH4d3cOWNNsVCJS7isY8p"
              }
            ],
            "total": 1
          }
        }
        """;

    public static string GetAlbumResponse_DarkSide => """
        {
          "album_type": "album",
          "artists": [
            {
              "external_urls": {
                "spotify": "https://open.spotify.com/artist/0k17h0D3J5VfYkk9zwXOj2"
              },
              "href": "https://api.spotify.com/v1/artists/0k17h0D3J5VfYkk9zwXOj2",
              "id": "0k17h0D3J5VfYkk9zwXOj2",
              "name": "Pink Floyd",
              "type": "artist",
              "uri": "spotify:artist:0k17h0D3J5VfYkk9zwXOj2"
            }
          ],
          "external_urls": {
            "spotify": "https://open.spotify.com/album/4LH4d3cOWNNsVCJS7isY8p"
          },
          "href": "https://api.spotify.com/v1/albums/4LH4d3cOWNNsVCJS7isY8p",
          "id": "4LH4d3cOWNNsVCJS7isY8p",
          "images": [
            {
              "height": 640,
              "url": "https://i.scdn.co/image/ab67616d0000b27334e4f6e94f7e0d96b94b4d17",
              "width": 640
            }
          ],
          "name": "The Dark Side of the Moon",
          "release_date": "1973-03-01",
          "release_date_precision": "day",
          "total_tracks": 10,
          "tracks": {
            "href": "https://api.spotify.com/v1/albums/4LH4d3cOWNNsVCJS7isY8p/tracks",
            "items": [
              {
                "artists": [{"name": "Pink Floyd", "uri": "spotify:artist:0k17h0D3J5VfYkk9zwXOj2"}],
                "disc_number": 1,
                "duration_ms": 154267,
                "explicit": false,
                "external_urls": {"spotify": "https://open.spotify.com/track/track1"},
                "href": "https://api.spotify.com/v1/tracks/track1",
                "id": "track1",
                "is_local": false,
                "name": "Speak to Me",
                "preview_url": null,
                "track_number": 1,
                "type": "track",
                "uri": "spotify:track:track1"
              },
              {
                "artists": [{"name": "Pink Floyd", "uri": "spotify:artist:0k17h0D3J5VfYkk9zwXOj2"}],
                "disc_number": 1,
                "duration_ms": 174400,
                "explicit": false,
                "external_urls": {"spotify": "https://open.spotify.com/track/track2"},
                "href": "https://api.spotify.com/v1/tracks/track2",
                "id": "track2",
                "is_local": false,
                "name": "Breathe (In the Air)",
                "preview_url": null,
                "track_number": 2,
                "type": "track",
                "uri": "spotify:track:track2"
              },
              {
                "artists": [{"name": "Pink Floyd", "uri": "spotify:artist:0k17h0D3J5VfYkk9zwXOj2"}],
                "disc_number": 1,
                "duration_ms": 244973,
                "explicit": false,
                "external_urls": {"spotify": "https://open.spotify.com/track/track3"},
                "href": "https://api.spotify.com/v1/tracks/track3",
                "id": "track3",
                "is_local": false,
                "name": "Money",
                "preview_url": null,
                "track_number": 4,
                "type": "track",
                "uri": "spotify:track:track3"
              }
            ],
            "limit": 50,
            "next": null,
            "offset": 0,
            "previous": null,
            "total": 10
          },
          "type": "album",
          "uri": "spotify:album:4LH4d3cOWNNsVCJS7isY8p"
        }
        """;

    // ========== Device Responses ==========

    public static string GetDevicesResponse => """
        {
          "devices": [
            {
              "id": "device-1",
              "is_active": true,
              "is_private_session": false,
              "is_restricted": false,
              "name": "Desktop",
              "type": "Computer",
              "volume_percent": 50
            },
            {
              "id": "device-2",
              "is_active": false,
              "is_private_session": false,
              "is_restricted": false,
              "name": "Mobile",
              "type": "Smartphone",
              "volume_percent": 80
            }
          ]
        }
        """;

    public static string GetDevicesResponse_NoDevices => """
        {
          "devices": []
        }
        """;

    // ========== Playback Responses ==========

    public static string GetCurrentlyPlayingResponse => """
        {
          "timestamp": 1629865600000,
          "progress_ms": 45000,
          "item": {
            "album": {
              "name": "The Dark Side of the Moon",
              "uri": "spotify:album:4LH4d3cOWNNsVCJS7isY8p"
            },
            "artists": [{"name": "Pink Floyd"}],
            "duration_ms": 244973,
            "id": "3z8h0TU7RvxVfncizLUDVi",
            "name": "Money",
            "type": "track",
            "uri": "spotify:track:3z8h0TU7RvxVfncizLUDVi"
          },
          "is_playing": true
        }
        """;

    public static string GetCurrentlyPlayingResponse_NotPlaying => """
        {
          "timestamp": 1629865600000,
          "progress_ms": 0,
          "item": null,
          "is_playing": false
        }
        """;

    // ========== Error Responses ==========

    public static string ErrorResponse_Unauthorized => """
        {
          "error": {
            "status": 401,
            "message": "The access token expired"
          }
        }
        """;

    public static string ErrorResponse_TooManyRequests => """
        {
          "error": {
            "status": 429,
            "message": "Rate limit exceeded"
          }
        }
        """;

    public static string ErrorResponse_NotFound => """
        {
          "error": {
            "status": 404,
            "message": "The resource you requested could not be found"
          }
        }
        """;
}
