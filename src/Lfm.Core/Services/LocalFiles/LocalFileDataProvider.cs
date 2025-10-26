using Lfm.Core.Models;

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

            return Result<List<PlayEvent>>.Success(filtered.ToList());
        }

        // Find appropriate parser
        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));

        if (parser == null)
            return Result<List<PlayEvent>>.Failure(
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

    public async Task<Result<List<ArtistInfo>>> GetTopArtistsAsync(
        string? username,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? limit = null,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<List<ArtistInfo>>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, startDate, endDate);

        if (!eventsResult.IsSuccess)
            return Result<List<ArtistInfo>>.Failure(eventsResult.ErrorMessage!);

        var artists = _aggregator.AggregateTopArtists(eventsResult.Value, limit);

        // Apply pagination if needed
        if (page > 1 && limit.HasValue)
        {
            var skip = (page - 1) * limit.Value;
            artists = artists.Skip(skip).Take(limit.Value).ToList();
        }

        return Result<List<ArtistInfo>>.Success(artists);
    }

    public async Task<Result<List<TrackInfo>>> GetTopTracksAsync(
        string? username,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? limit = null,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<List<TrackInfo>>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, startDate, endDate);

        if (!eventsResult.IsSuccess)
            return Result<List<TrackInfo>>.Failure(eventsResult.ErrorMessage!);

        var tracks = _aggregator.AggregateTopTracks(eventsResult.Value, limit);

        // Apply pagination if needed
        if (page > 1 && limit.HasValue)
        {
            var skip = (page - 1) * limit.Value;
            tracks = tracks.Skip(skip).Take(limit.Value).ToList();
        }

        return Result<List<TrackInfo>>.Success(tracks);
    }

    public async Task<Result<List<AlbumInfo>>> GetTopAlbumsAsync(
        string? username,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? limit = null,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<List<AlbumInfo>>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, startDate, endDate);

        if (!eventsResult.IsSuccess)
            return Result<List<AlbumInfo>>.Failure(eventsResult.ErrorMessage!);

        var albums = _aggregator.AggregateTopAlbums(eventsResult.Value, limit);

        // Apply pagination if needed
        if (page > 1 && limit.HasValue)
        {
            var skip = (page - 1) * limit.Value;
            albums = albums.Skip(skip).Take(limit.Value).ToList();
        }

        return Result<List<AlbumInfo>>.Success(albums);
    }

    public async Task<Result<List<TrackInfo>>> GetArtistTopTracksAsync(
        string artistName,
        string? username = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<List<TrackInfo>>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, startDate, endDate);

        if (!eventsResult.IsSuccess)
            return Result<List<TrackInfo>>.Failure(eventsResult.ErrorMessage!);

        var tracks = _aggregator.GetArtistTracks(eventsResult.Value, artistName);

        if (limit.HasValue)
            tracks = tracks.Take(limit.Value).ToList();

        return Result<List<TrackInfo>>.Success(tracks);
    }

    public async Task<Result<List<AlbumInfo>>> GetArtistTopAlbumsAsync(
        string artistName,
        string? username = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<List<AlbumInfo>>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username, startDate, endDate);

        if (!eventsResult.IsSuccess)
            return Result<List<AlbumInfo>>.Failure(eventsResult.ErrorMessage!);

        var albums = _aggregator.GetArtistAlbums(eventsResult.Value, artistName);

        if (limit.HasValue)
            albums = albums.Take(limit.Value).ToList();

        return Result<List<AlbumInfo>>.Success(albums);
    }

    public async Task<Result<List<TrackInfo>>> GetAlbumTracksAsync(
        string artistName,
        string albumName,
        string? username = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<List<TrackInfo>>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<List<TrackInfo>>.Failure(eventsResult.ErrorMessage!);

        var tracks = _aggregator.GetAlbumTracks(eventsResult.Value, artistName, albumName);

        return Result<List<TrackInfo>>.Success(tracks);
    }

    public async Task<Result<TrackInfo>> GetTrackInfoAsync(
        string artistName,
        string trackName,
        string? username = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TrackInfo>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<TrackInfo>.Failure(eventsResult.ErrorMessage!);

        var playCount = _aggregator.GetTrackPlayCount(eventsResult.Value, artistName, trackName);

        if (playCount == 0)
            return Result<TrackInfo>.Failure($"Track not found: {artistName} - {trackName}");

        var trackEvent = eventsResult.Value
            .FirstOrDefault(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase) &&
                                 e.Track.Equals(trackName, StringComparison.OrdinalIgnoreCase));

        var trackInfo = new TrackInfo
        {
            Name = trackEvent?.Track ?? trackName,
            Artist = trackEvent?.Artist ?? artistName,
            PlayCount = playCount,
            AlbumName = trackEvent?.Album,
            Url = string.Empty,
            Mbid = string.Empty,
            Streamable = false
        };

        return Result<TrackInfo>.Success(trackInfo);
    }

    public async Task<Result<AlbumInfo>> GetAlbumInfoAsync(
        string artistName,
        string albumName,
        string? username = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<AlbumInfo>.Failure("File path required (username parameter)");

        var eventsResult = await LoadEventsAsync(username);

        if (!eventsResult.IsSuccess)
            return Result<AlbumInfo>.Failure(eventsResult.ErrorMessage!);

        var playCount = _aggregator.GetAlbumPlayCount(eventsResult.Value, artistName, albumName);

        if (playCount == 0)
            return Result<AlbumInfo>.Failure($"Album not found: {artistName} - {albumName}");

        var albumEvent = eventsResult.Value
            .FirstOrDefault(e => e.Artist.Equals(artistName, StringComparison.OrdinalIgnoreCase) &&
                                 e.Album != null &&
                                 e.Album.Equals(albumName, StringComparison.OrdinalIgnoreCase));

        var albumInfo = new AlbumInfo
        {
            Name = albumEvent?.Album ?? albumName,
            Artist = albumEvent?.Artist ?? artistName,
            PlayCount = playCount,
            Url = string.Empty,
            Mbid = string.Empty
        };

        return Result<AlbumInfo>.Success(albumInfo);
    }

    public Task<Result<List<ArtistInfo>>> GetSimilarArtistsAsync(string artistName, int? limit = null)
    {
        // Local files don't have similarity data
        return Task.FromResult(Result<List<ArtistInfo>>.Failure(
            "Similar artists not available for local file data sources"));
    }

    public Task<Result<List<TrackInfo>>> GetSimilarTracksAsync(string artistName, string trackName, int? limit = null)
    {
        // Local files don't have similarity data
        return Task.FromResult(Result<List<TrackInfo>>.Failure(
            "Similar tracks not available for local file data sources"));
    }
}
