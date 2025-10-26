using Lfm.Core.Models;
using Lfm.Core.Models.Results;

namespace Lfm.Core.Services.LocalFiles;

/// <summary>
/// IMusicDataProvider implementation for local file data sources.
/// Loads play events from files and aggregates them into the same format as Last.fm API.
/// </summary>
public class LocalFileDataProvider : IMusicDataProvider
{
    private readonly List<ILocalFileParser> _parsers;
    private readonly LocalFileAggregator _aggregator;
    private List<PlayEvent>? _cachedEvents;
    private string? _cachedFilePath;

    public LocalFileDataProvider()
    {
        _parsers = new List<ILocalFileParser>
        {
            new SpotifyJsonParser(),
            new YouTubeMusicParser()
        };
        _aggregator = new LocalFileAggregator();
    }

    // Interface properties
    public string ProviderName => "Local Files";
    public bool SupportsDateRanges => true;
    public bool SupportsSimilarArtists => false;
    public bool SupportsLookup => true;

    /// <summary>
    /// Loads play events from a file. Caches events to avoid re-parsing.
    /// </summary>
    private async Task<Result<List<PlayEvent>>> LoadEventsAsync(
        string filePath,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Check if we can use cached events
        if (_cachedEvents != null && _cachedFilePath == filePath)
        {
            var filtered = _cachedEvents.AsEnumerable();

            if (startDate.HasValue)
                filtered = filtered.Where(e => e.PlayedAt >= startDate.Value);

            if (endDate.HasValue)
                filtered = filtered.Where(e => e.PlayedAt <= endDate.Value);

            return Result<List<PlayEvent>>.Ok(filtered.ToList());
        }

        // Find appropriate parser
        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));

        if (parser == null)
            return Result<List<PlayEvent>>.DataError(
                $"No parser found for file: {filePath}. Supported formats: Spotify JSON, YouTube Music JSON/CSV");

        // Parse file
        var parseResult = await parser.ParseAsync(filePath, startDate, endDate);

        if (!parseResult.IsSuccess)
            return parseResult;

        // Cache events (without date filtering for future queries)
        var allEventsResult = await parser.ParseAsync(filePath);
        if (allEventsResult.IsSuccess)
        {
            _cachedEvents = allEventsResult.Value;
            _cachedFilePath = filePath;
        }

        return parseResult;
    }

    // Core query methods

    public async Task<Result<TopArtists>> GetTopArtistsAsync(
        string username,
        string period = "overall",
        int limit = 10,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopArtists>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<TopArtists>.DataError(eventsResult.ErrorMessage!);

        var artistList = _aggregator.AggregateTopArtists(eventsResult.Value, limit * page);

        // Apply pagination
        if (page > 1)
        {
            var skip = (page - 1) * limit;
            artistList = artistList.Skip(skip).Take(limit).ToList();
        }
        else if (limit > 0)
        {
            artistList = artistList.Take(limit).ToList();
        }

        // Convert to TopArtists wrapper
        var artists = artistList.Select((a, index) => new Artist
        {
            Name = a.Name,
            PlayCount = a.PlayCount.ToString(),
            Url = string.Empty,
            Mbid = string.Empty,
            Attributes = new ArtistAttributes { Rank = (index + 1).ToString() }
        }).ToList();

        return Result<TopArtists>.Ok(new TopArtists
        {
            Artists = artists,
            Attributes = new TopArtistsAttributes
            {
                User = username,
                TotalPages = "1",
                Page = page.ToString(),
                PerPage = limit.ToString(),
                Total = artists.Count.ToString()
            }
        });
    }

    public async Task<Result<TopTracks>> GetTopTracksAsync(
        string username,
        string period = "overall",
        int limit = 10,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopTracks>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<TopTracks>.DataError(eventsResult.ErrorMessage!);

        var trackList = _aggregator.AggregateTopTracks(eventsResult.Value, limit * page);

        // Apply pagination
        if (page > 1)
        {
            var skip = (page - 1) * limit;
            trackList = trackList.Skip(skip).Take(limit).ToList();
        }
        else if (limit > 0)
        {
            trackList = trackList.Take(limit).ToList();
        }

        // Convert to TopTracks wrapper
        var tracks = trackList.Select((t, index) => new Track
        {
            Name = t.Name,
            PlayCount = t.PlayCount.ToString(),
            Url = string.Empty,
            Mbid = string.Empty,
            Artist = new ArtistInfo
            {
                Name = t.Artist,
                Mbid = string.Empty,
                Url = string.Empty
            },
            Attributes = new TrackAttributes { Rank = (index + 1).ToString() }
        }).ToList();

        return Result<TopTracks>.Ok(new TopTracks
        {
            Tracks = tracks,
            Attributes = new TopTracksAttributes
            {
                User = username,
                TotalPages = "1",
                Page = page.ToString(),
                PerPage = limit.ToString(),
                Total = tracks.Count.ToString()
            }
        });
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsAsync(
        string username,
        string period = "overall",
        int limit = 10,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopAlbums>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<TopAlbums>.DataError(eventsResult.ErrorMessage!);

        var albumList = _aggregator.AggregateTopAlbums(eventsResult.Value, limit * page);

        // Apply pagination
        if (page > 1)
        {
            var skip = (page - 1) * limit;
            albumList = albumList.Skip(skip).Take(limit).ToList();
        }
        else if (limit > 0)
        {
            albumList = albumList.Take(limit).ToList();
        }

        // Convert to TopAlbums wrapper
        var albums = albumList.Select((a, index) => new Album
        {
            Name = a.Name,
            PlayCount = a.PlayCount.ToString(),
            Url = string.Empty,
            Mbid = string.Empty,
            Artist = new ArtistInfo
            {
                Name = a.Artist,
                Mbid = string.Empty,
                Url = string.Empty
            },
            Attributes = new AlbumAttributes { Rank = (index + 1).ToString() }
        }).ToList();

        return Result<TopAlbums>.Ok(new TopAlbums
        {
            Albums = albums,
            Attributes = new TopAlbumsAttributes
            {
                User = username,
                TotalPages = "1",
                Page = page.ToString(),
                PerPage = limit.ToString(),
                Total = albums.Count.ToString()
            }
        });
    }

    // Date range methods

    public async Task<Result<TopArtists>> GetTopArtistsForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopArtists>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, from, to);

        if (!eventsResult.IsSuccess)
            return Result<TopArtists>.DataError(eventsResult.ErrorMessage!);

        var artistList = _aggregator.AggregateTopArtists(eventsResult.Value, limit);

        // Convert to TopArtists wrapper
        var artists = artistList.Select((a, index) => new Artist
        {
            Name = a.Name,
            PlayCount = a.PlayCount.ToString(),
            Url = string.Empty,
            Mbid = string.Empty,
            Attributes = new ArtistAttributes { Rank = (index + 1).ToString() }
        }).ToList();

        return Result<TopArtists>.Ok(new TopArtists
        {
            Artists = artists,
            Attributes = new TopArtistsAttributes
            {
                User = username,
                TotalPages = "1",
                Page = "1",
                PerPage = limit.ToString(),
                Total = artists.Count.ToString()
            }
        });
    }

    public async Task<Result<TopTracks>> GetTopTracksForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopTracks>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, from, to);

        if (!eventsResult.IsSuccess)
            return Result<TopTracks>.DataError(eventsResult.ErrorMessage!);

        var trackList = _aggregator.AggregateTopTracks(eventsResult.Value, limit);

        // Convert to TopTracks wrapper
        var tracks = trackList.Select((t, index) => new Track
        {
            Name = t.Name,
            PlayCount = t.PlayCount.ToString(),
            Url = string.Empty,
            Mbid = string.Empty,
            Artist = new ArtistInfo
            {
                Name = t.Artist,
                Mbid = string.Empty,
                Url = string.Empty
            },
            Attributes = new TrackAttributes { Rank = (index + 1).ToString() }
        }).ToList();

        return Result<TopTracks>.Ok(new TopTracks
        {
            Tracks = tracks,
            Attributes = new TopTracksAttributes
            {
                User = username,
                TotalPages = "1",
                Page = "1",
                PerPage = limit.ToString(),
                Total = tracks.Count.ToString()
            }
        });
    }

    public async Task<Result<TopAlbums>> GetTopAlbumsForDateRangeAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopAlbums>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, from, to);

        if (!eventsResult.IsSuccess)
            return Result<TopAlbums>.DataError(eventsResult.ErrorMessage!);

        var albumList = _aggregator.AggregateTopAlbums(eventsResult.Value, limit);

        // Convert to TopAlbums wrapper
        var albums = albumList.Select((a, index) => new Album
        {
            Name = a.Name,
            PlayCount = a.PlayCount.ToString(),
            Url = string.Empty,
            Mbid = string.Empty,
            Artist = new ArtistInfo
            {
                Name = a.Artist,
                Mbid = string.Empty,
                Url = string.Empty
            },
            Attributes = new AlbumAttributes { Rank = (index + 1).ToString() }
        }).ToList();

        return Result<TopAlbums>.Ok(new TopAlbums
        {
            Albums = albums,
            Attributes = new TopAlbumsAttributes
            {
                User = username,
                TotalPages = "1",
                Page = "1",
                PerPage = limit.ToString(),
                Total = albums.Count.ToString()
            }
        });
    }

    public async Task<Result<RecentTracks>> GetRecentTracksAsync(
        string username,
        DateTime from,
        DateTime to,
        int limit = 200,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<RecentTracks>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, from, to);

        if (!eventsResult.IsSuccess)
            return Result<RecentTracks>.DataError(eventsResult.ErrorMessage!);

        // Sort by date descending and paginate
        var events = eventsResult.Value
            .OrderByDescending(e => e.PlayedAt)
            .ToList();

        var totalCount = events.Count;
        var skip = (page - 1) * limit;
        var pagedEvents = events.Skip(skip).Take(limit).ToList();

        // Convert to RecentTracks wrapper
        var tracks = pagedEvents.Select(e => new RecentTrack
        {
            Name = e.Track,
            Artist = new RecentTrackArtistInfo
            {
                Name = e.Artist,
                Mbid = string.Empty,
                Url = string.Empty
            },
            Album = new AlbumInfo
            {
                Name = e.Album ?? string.Empty,
                Mbid = string.Empty
            },
            Url = string.Empty,
            Mbid = string.Empty,
            Date = new DateInfo
            {
                UnixTimestamp = ((DateTimeOffset)e.PlayedAt).ToUnixTimeSeconds().ToString(),
                Text = e.PlayedAt.ToString("dd MMM yyyy, HH:mm")
            }
        }).ToList();

        return Result<RecentTracks>.Ok(new RecentTracks
        {
            Tracks = tracks,
            Attributes = new RecentTracksAttributes
            {
                User = username,
                TotalPages = ((totalCount + limit - 1) / limit).ToString(),
                Page = page.ToString(),
                PerPage = limit.ToString(),
                Total = totalCount.ToString()
            }
        });
    }

    // Artist-specific methods

    public async Task<Result<TopTracks>> GetArtistTopTracksAsync(string artist, int limit = 10)
    {
        // Note: Local files need a file path. This method doesn't have username parameter in interface.
        // We'll return a failure message indicating this limitation.
        return Result<TopTracks>.DataError(
            "Artist queries require a file path. Use GetTopTracksAsync or GetTopTracksForDateRangeAsync with a file path.");
    }

    public async Task<Result<TopAlbums>> GetArtistTopAlbumsAsync(string artist, int limit = 10)
    {
        // Note: Local files need a file path. This method doesn't have username parameter in interface.
        return Result<TopAlbums>.DataError(
            "Artist queries require a file path. Use GetTopAlbumsAsync or GetTopAlbumsForDateRangeAsync with a file path.");
    }

    public Task<Result<SimilarArtists>> GetSimilarArtistsAsync(string artist, int limit = 50)
    {
        return Task.FromResult(Result<SimilarArtists>.DataError(
            "Similar artists not available for local file data sources"));
    }

    public Task<Result<TopTags>> GetArtistTopTagsAsync(string artist, bool autocorrect = true)
    {
        return Task.FromResult(Result<TopTags>.DataError(
            "Tags not available for local file data sources"));
    }

    // Lookup methods

    public async Task<Result<ArtistLookupInfo>> GetArtistInfoAsync(string artist, string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<ArtistLookupInfo>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<ArtistLookupInfo>.DataError(eventsResult.ErrorMessage!);

        var playCount = _aggregator.GetArtistPlayCount(eventsResult.Value, artist);

        if (playCount == 0)
            return Result<ArtistLookupInfo>.DataError($"Artist not found: {artist}");

        return Result<ArtistLookupInfo>.Ok(new ArtistLookupInfo
        {
            Artist = new ArtistLookupInfo.ArtistDetails
            {
                Name = artist,
                Mbid = string.Empty,
                Url = string.Empty,
                Stats = new ArtistLookupInfo.ArtistStats
                {
                    Listeners = "0",
                    Playcount = "0",
                    UserPlaycount = playCount.ToString()
                }
            }
        });
    }

    public async Task<Result<TrackLookupInfo>> GetTrackInfoAsync(
        string artist,
        string track,
        string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TrackLookupInfo>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<TrackLookupInfo>.DataError(eventsResult.ErrorMessage!);

        var playCount = _aggregator.GetTrackPlayCount(eventsResult.Value, artist, track);

        if (playCount == 0)
            return Result<TrackLookupInfo>.DataError($"Track not found: {artist} - {track}");

        var trackEvent = eventsResult.Value
            .FirstOrDefault(e => e.Artist.Equals(artist, StringComparison.OrdinalIgnoreCase) &&
                                 e.Track.Equals(track, StringComparison.OrdinalIgnoreCase));

        return Result<TrackLookupInfo>.Ok(new TrackLookupInfo
        {
            Track = new TrackLookupInfo.TrackDetails
            {
                Name = trackEvent?.Track ?? track,
                Mbid = string.Empty,
                Url = string.Empty,
                Duration = "0",
                Listeners = "0",
                Playcount = "0",
                UserPlaycount = playCount.ToString(),
                UserLoved = "0",
                Artist = new TrackLookupInfo.ArtistBasic
                {
                    Name = trackEvent?.Artist ?? artist,
                    Mbid = string.Empty,
                    Url = string.Empty
                },
                Album = trackEvent?.Album != null
                    ? new TrackLookupInfo.AlbumBasic
                    {
                        Artist = trackEvent.Artist,
                        Title = trackEvent.Album,
                        Url = string.Empty
                    }
                    : null
            }
        });
    }

    public async Task<Result<AlbumLookupInfo>> GetAlbumInfoAsync(
        string artist,
        string album,
        string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<AlbumLookupInfo>.DataError("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<AlbumLookupInfo>.DataError(eventsResult.ErrorMessage!);

        var playCount = _aggregator.GetAlbumPlayCount(eventsResult.Value, artist, album);

        if (playCount == 0)
            return Result<AlbumLookupInfo>.DataError($"Album not found: {artist} - {album}");

        var albumEvent = eventsResult.Value
            .FirstOrDefault(e => e.Artist.Equals(artist, StringComparison.OrdinalIgnoreCase) &&
                                 e.Album != null &&
                                 e.Album.Equals(album, StringComparison.OrdinalIgnoreCase));

        return Result<AlbumLookupInfo>.Ok(new AlbumLookupInfo
        {
            Album = new AlbumLookupInfo.AlbumDetails
            {
                Name = albumEvent?.Album ?? album,
                Artist = albumEvent?.Artist ?? artist,
                Mbid = string.Empty,
                Url = string.Empty,
                Listeners = "0",
                Playcount = "0",
                UserPlaycount = playCount
            }
        });
    }
}
