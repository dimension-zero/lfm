using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Lfm.Core.Services;

/// <summary>
/// Service layer implementation for Last.fm business logic operations.
/// Encapsulates complex business operations and abstracts them from CLI command implementation details.
/// </summary>
public class LastFmService : ILastFmService
{
    private readonly IMusicDataProvider _dataProvider;
    private readonly IConfigurationManager _configManager;
    private readonly ITagFilterService _tagFilterService;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly ILogger<LastFmService> _logger;
    private readonly object? _spotifyStreamer;

    public LastFmService(
        IMusicDataProvider dataProvider,
        IConfigurationManager configManager,
        ITagFilterService tagFilterService,
        IRecommendationEngine recommendationEngine,
        ILogger<LastFmService> logger,
        object? spotifyStreamer = null)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _tagFilterService = tagFilterService ?? throw new ArgumentNullException(nameof(tagFilterService));
        _recommendationEngine = recommendationEngine ?? throw new ArgumentNullException(nameof(recommendationEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _spotifyStreamer = spotifyStreamer; // Optional dependency - IPlaylistStreamer from Lfm.Spotify
    }


    // Basic user content retrieval
    public async Task<TopArtists?> GetUserTopArtistsAsync(string username, LastFmPeriod period, int limit = 10, int page = 1)
    {
        var result = await _dataProvider.GetTopArtistsAsync(username, period, limit, page);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<TopTracks?> GetUserTopTracksAsync(string username, LastFmPeriod period, int limit = 10, int page = 1)
    {
        var result = await _dataProvider.GetTopTracksAsync(username, period, limit, page);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<TopAlbums?> GetUserTopAlbumsAsync(string username, LastFmPeriod period, int limit = 10, int page = 1)
    {
        var result = await _dataProvider.GetTopAlbumsAsync(username, period, limit, page);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<RecentTracks?> GetUserRecentTracksAsync(string username, int limit = 20, int? hoursBack = null)
    {
        DateTime to = DateTime.UtcNow;
        DateTime from;

        if (hoursBack.HasValue)
        {
            from = to.AddHours(-hoursBack.Value);
        }
        else
        {
            // Default: last 7 days
            from = to.AddDays(-7);
        }

        var result = await _dataProvider.GetRecentTracksAsync(username, from, to, limit, page: 1);
        return result.IsSuccess ? result.Data : null;
    }

    // Date range variants
    public async Task<TopArtists?> GetUserTopArtistsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        var result = await _dataProvider.GetTopArtistsForDateRangeAsync(username, from, to, limit);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<TopTracks?> GetUserTopTracksForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        var result = await _dataProvider.GetTopTracksForDateRangeAsync(username, from, to, limit);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<TopAlbums?> GetUserTopAlbumsForDateRangeAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        var result = await _dataProvider.GetTopAlbumsForDateRangeAsync(username, from, to, limit);
        return result.IsSuccess ? result.Data : null;
    }

    // Artist-specific content
    public async Task<TopTracks?> GetArtistTopTracksAsync(string artistName, int limit = 10)
    {
        var result = await _dataProvider.GetArtistTopTracksAsync(artistName, limit);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<TopAlbums?> GetArtistTopAlbumsAsync(string artistName, int limit = 10)
    {
        var result = await _dataProvider.GetArtistTopAlbumsAsync(artistName, limit);
        return result.IsSuccess ? result.Data : null;
    }

    public async Task<SimilarArtists?> GetSimilarArtistsAsync(string artistName, int limit = 50)
    {
        var result = await _dataProvider.GetSimilarArtistsAsync(artistName, limit);
        return result.IsSuccess ? result.Data : null;
    }

    // Range queries - complex operations that span multiple pages
    public async Task<(List<Artist> items, string totalCount)> GetUserTopArtistsRangeAsync(string username, LastFmPeriod period, int startIndex, int endIndex)
    {
        return await ExecuteRangeQueryAsync<Artist, TopArtists>(
            username,
            period,
            startIndex,
            endIndex,
            async (user, per, limit, page) => {
                var result = await _dataProvider.GetTopArtistsAsync(user, per, limit, page);
                return result.IsSuccess ? result.Data : null;
            },
            response => response.Artists,
            response => response.Attributes.Total);
    }

    public async Task<(List<Track> items, string totalCount)> GetUserTopTracksRangeAsync(string username, LastFmPeriod period, int startIndex, int endIndex)
    {
        return await ExecuteRangeQueryAsync<Track, TopTracks>(
            username,
            period,
            startIndex,
            endIndex,
            async (user, per, limit, page) => {
                var result = await _dataProvider.GetTopTracksAsync(user, per, limit, page);
                return result.IsSuccess ? result.Data : null;
            },
            response => response.Tracks,
            response => response.Attributes.Total);
    }

    public async Task<(List<Album> items, string totalCount)> GetUserTopAlbumsRangeAsync(string username, LastFmPeriod period, int startIndex, int endIndex)
    {
        return await ExecuteRangeQueryAsync<Album, TopAlbums>(
            username,
            period,
            startIndex,
            endIndex,
            async (user, per, limit, page) => {
                var result = await _dataProvider.GetTopAlbumsAsync(user, per, limit, page);
                return result.IsSuccess ? result.Data : null;
            },
            response => response.Albums,
            response => response.Attributes.Total);
    }

    // Deep search operations - search through user's entire history
    public async Task<List<Track>> SearchUserTracksForArtistAsync(string username, string artistName, int limit, int maxDepth = int.MaxValue, CancellationToken cancellationToken = default)
    {
        var foundTracks = new List<Track>();
        var page = 1;
        var processedItems = 0;

        while (foundTracks.Count < limit && processedItems < maxDepth && !cancellationToken.IsCancellationRequested)
        {
            // Apply throttling between API calls (except for first page)
            if (page > 1)
            {
                await Task.Delay(100); // Use configured throttle value
            }

            var providerResult = await _dataProvider.GetTopTracksAsync(username, LastFmPeriod.Overall, SearchConstants.Api.MaxItemsPerPage, page);
            var result = providerResult.IsSuccess ? providerResult.Data : null;

            if (result?.Tracks == null || !result.Tracks.Any())
                break;

            var matchingTracks = result.Tracks
                .Where(track => string.Equals(track.Artist.Name, artistName, StringComparison.OrdinalIgnoreCase))
                .Take(limit - foundTracks.Count)
                .ToList();

            foundTracks.AddRange(matchingTracks);
            processedItems += result.Tracks.Count;
            page++;
        }

        return foundTracks.Take(limit).ToList();
    }

    public async Task<List<Album>> SearchUserAlbumsForArtistAsync(string username, string artistName, int limit, int maxDepth = int.MaxValue, CancellationToken cancellationToken = default)
    {
        var foundAlbums = new List<Album>();
        var page = 1;
        var processedItems = 0;

        while (foundAlbums.Count < limit && processedItems < maxDepth && !cancellationToken.IsCancellationRequested)
        {
            // Apply throttling between API calls (except for first page)
            if (page > 1)
            {
                await Task.Delay(100); // Use configured throttle value
            }

            var providerResult = await _dataProvider.GetTopAlbumsAsync(username, LastFmPeriod.Overall, SearchConstants.Api.MaxItemsPerPage, page);
            var result = providerResult.IsSuccess ? providerResult.Data : null;

            if (result?.Albums == null || !result.Albums.Any())
                break;

            var matchingAlbums = result.Albums
                .Where(album => string.Equals(album.Artist.Name, artistName, StringComparison.OrdinalIgnoreCase))
                .Take(limit - foundAlbums.Count)
                .ToList();

            foundAlbums.AddRange(matchingAlbums);
            processedItems += result.Albums.Count;
            page++;
        }

        return foundAlbums.Take(limit).ToList();
    }

    // Advanced features
    public Task<List<RecommendationResult>> GetMusicRecommendationsAsync(string username,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0,
        int tracksPerArtist = 0,
        LastFmPeriod period = LastFmPeriod.Overall,
        bool excludeTags = false)
        => _recommendationEngine.GetMusicRecommendationsAsync(username, analysisLimit, recommendationLimit, filterThreshold, tracksPerArtist, period, excludeTags);

    public Task<List<RecommendationResult>> GetMusicRecommendationsForDateRangeAsync(string username,
        DateTime from,
        DateTime to,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0,
        int tracksPerArtist = 0,
        bool excludeTags = false)
        => _recommendationEngine.GetMusicRecommendationsForDateRangeAsync(username, from, to, analysisLimit, recommendationLimit, filterThreshold, tracksPerArtist, excludeTags);

    public Task<Dictionary<string, int>> GetUserArtistPlayCountsAsync(string username, int maxArtists = int.MaxValue)
        => _recommendationEngine.GetUserArtistPlayCountsAsync(username, maxArtists);

    // New Result-based methods for gradual migration
    public async Task<Result<TopArtists>> GetUserTopArtistsWithResultAsync(string username, LastFmPeriod period, int limit = 10, int page = 1)
    {
        // Service-layer validation
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopArtists>.ValidationError("Username cannot be empty");

        if (limit <= 0 || limit > 1000)
            return Result<TopArtists>.ValidationError("Limit must be between 1 and 1000");

        // Call API client WithResultAsync - preserves error details
        var result = await _dataProvider.GetTopArtistsAsync(username, period, limit, page);

        // Add service-layer business logic checks if needed
        if (result.IsSuccess && (result.Data?.Artists == null || !result.Data.Artists.Any()))
        {
            return Result<TopArtists>.DataError($"No artists found for user '{username}' in period '{period.ToApiString()}'");
        }

        return result;
    }

    public async Task<Result<TopTracks>> GetUserTopTracksWithResultAsync(string username, LastFmPeriod period, int limit = 10, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopTracks>.ValidationError("Username cannot be empty");

        if (limit <= 0 || limit > 1000)
            return Result<TopTracks>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetTopTracksAsync(username, period, limit, page);

        if (result.IsSuccess && (result.Data?.Tracks == null || !result.Data.Tracks.Any()))
        {
            return Result<TopTracks>.DataError($"No tracks found for user '{username}' in period '{period.ToApiString()}'");
        }

        return result;
    }

    public async Task<Result<TopAlbums>> GetUserTopAlbumsWithResultAsync(string username, LastFmPeriod period, int limit = 10, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopAlbums>.ValidationError("Username cannot be empty");

        if (limit <= 0 || limit > 1000)
            return Result<TopAlbums>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetTopAlbumsAsync(username, period, limit, page);

        if (result.IsSuccess && (result.Data?.Albums == null || !result.Data.Albums.Any()))
        {
            return Result<TopAlbums>.DataError($"No albums found for user '{username}' in period '{period.ToApiString()}'");
        }

        return result;
    }

    public async Task<Result<TopArtists>> GetUserTopArtistsForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopArtists>.ValidationError("Username cannot be empty");

        if (from > to)
            return Result<TopArtists>.ValidationError("'from' date must be before 'to' date");

        if (limit <= 0 || limit > 1000)
            return Result<TopArtists>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetTopArtistsForDateRangeAsync(username, from, to, limit);

        if (result.IsSuccess && (result.Data?.Artists == null || !result.Data.Artists.Any()))
        {
            return Result<TopArtists>.DataError($"No artists found for user '{username}' in date range");
        }

        return result;
    }

    public async Task<Result<TopTracks>> GetUserTopTracksForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopTracks>.ValidationError("Username cannot be empty");

        if (from > to)
            return Result<TopTracks>.ValidationError("'from' date must be before 'to' date");

        if (limit <= 0 || limit > 1000)
            return Result<TopTracks>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetTopTracksForDateRangeAsync(username, from, to, limit);

        if (result.IsSuccess && (result.Data?.Tracks == null || !result.Data.Tracks.Any()))
        {
            return Result<TopTracks>.DataError($"No tracks found for user '{username}' in date range");
        }

        return result;
    }

    public async Task<Result<TopAlbums>> GetUserTopAlbumsForDateRangeWithResultAsync(string username, DateTime from, DateTime to, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<TopAlbums>.ValidationError("Username cannot be empty");

        if (from > to)
            return Result<TopAlbums>.ValidationError("'from' date must be before 'to' date");

        if (limit <= 0 || limit > 1000)
            return Result<TopAlbums>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetTopAlbumsForDateRangeAsync(username, from, to, limit);

        if (result.IsSuccess && (result.Data?.Albums == null || !result.Data.Albums.Any()))
        {
            return Result<TopAlbums>.DataError($"No albums found for user '{username}' in date range");
        }

        return result;
    }

    public async Task<Result<RecentTracks>> GetRecentTracksWithResultAsync(string username, DateTime from, DateTime to, int limit = 200, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<RecentTracks>.ValidationError("Username cannot be empty");

        if (from > to)
            return Result<RecentTracks>.ValidationError("'from' date must be before 'to' date");

        if (limit <= 0 || limit > 1000)
            return Result<RecentTracks>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetRecentTracksAsync(username, from, to, limit, page);

        if (result.IsSuccess && (result.Data?.Tracks == null || !result.Data.Tracks.Any()))
        {
            return Result<RecentTracks>.DataError($"No recent tracks found for user '{username}' in date range");
        }

        return result;
    }

    public async Task<Result<TopTracks>> GetArtistTopTracksWithResultAsync(string artist, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<TopTracks>.ValidationError("Artist name cannot be empty");

        if (limit <= 0 || limit > 1000)
            return Result<TopTracks>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetArtistTopTracksAsync(artist, limit);

        if (result.IsSuccess && (result.Data?.Tracks == null || !result.Data.Tracks.Any()))
        {
            return Result<TopTracks>.DataError($"No tracks found for artist '{artist}'");
        }

        return result;
    }

    public async Task<Result<TopAlbums>> GetArtistTopAlbumsWithResultAsync(string artist, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<TopAlbums>.ValidationError("Artist name cannot be empty");

        if (limit <= 0 || limit > 1000)
            return Result<TopAlbums>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetArtistTopAlbumsAsync(artist, limit);

        if (result.IsSuccess && (result.Data?.Albums == null || !result.Data.Albums.Any()))
        {
            return Result<TopAlbums>.DataError($"No albums found for artist '{artist}'");
        }

        return result;
    }

    public async Task<Result<SimilarArtists>> GetSimilarArtistsWithResultAsync(string artist, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<SimilarArtists>.ValidationError("Artist name cannot be empty");

        if (limit <= 0 || limit > 1000)
            return Result<SimilarArtists>.ValidationError("Limit must be between 1 and 1000");

        var result = await _dataProvider.GetSimilarArtistsAsync(artist, limit);

        if (result.IsSuccess && (result.Data?.Artists == null || !result.Data.Artists.Any()))
        {
            return Result<SimilarArtists>.DataError($"No similar artists found for '{artist}'");
        }

        return result;
    }

    public async Task<Result<TopTags>> GetArtistTopTagsWithResultAsync(string artist, bool autocorrect = true)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<TopTags>.ValidationError("Artist name cannot be empty");

        var result = await _dataProvider.GetArtistTopTagsAsync(artist, autocorrect);

        if (result.IsSuccess && (result.Data?.Tags == null || !result.Data.Tags.Any()))
        {
            return Result<TopTags>.DataError($"No tags found for artist '{artist}'");
        }

        return result;
    }

    public async Task<Result<ArtistLookupInfo>> GetArtistInfoWithResultAsync(string artist, string username)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<ArtistLookupInfo>.ValidationError("Artist name cannot be empty");

        return await _dataProvider.GetArtistInfoAsync(artist, username);
    }

    public async Task<Result<TrackLookupInfo>> GetTrackInfoWithResultAsync(string artist, string track, string username)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<TrackLookupInfo>.ValidationError("Artist name cannot be empty");

        if (string.IsNullOrWhiteSpace(track))
            return Result<TrackLookupInfo>.ValidationError("Track name cannot be empty");

        return await _dataProvider.GetTrackInfoAsync(artist, track, username);
    }

    public async Task<Result<AlbumLookupInfo>> GetAlbumInfoWithResultAsync(string artist, string album, string username)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return Result<AlbumLookupInfo>.ValidationError("Artist name cannot be empty");

        if (string.IsNullOrWhiteSpace(album))
            return Result<AlbumLookupInfo>.ValidationError("Album name cannot be empty");

        return await _dataProvider.GetAlbumInfoAsync(artist, album, username);
    }

    public Task<Result<List<RecommendationResult>>> GetMusicRecommendationsWithResultAsync(string username,
        int analysisLimit = 20,
        int recommendationLimit = 20,
        int filterThreshold = 0,
        int tracksPerArtist = 0,
        LastFmPeriod period = LastFmPeriod.Overall,
        bool excludeTags = false)
        => _recommendationEngine.GetMusicRecommendationsWithResultAsync(username, analysisLimit, recommendationLimit, filterThreshold, tracksPerArtist, period, excludeTags);

    public async Task<Result> ValidateUserConfigurationAsync(string? username = null)
    {
        try
        {
            var config = await _configManager.LoadAsync();
            
            // Check API key
            if (string.IsNullOrEmpty(config.ApiKey))
            {
                return Result.ConfigurationError(
                    "Last.fm API key is not configured", 
                    "Use 'lfm config set api-key YOUR_KEY' to set your API key");
            }
            
            // Check username (either provided or default)
            var effectiveUsername = username ?? config.DefaultUsername;
            if (string.IsNullOrEmpty(effectiveUsername))
            {
                return Result.ConfigurationError(
                    "No username specified and no default username configured",
                    "Provide --user parameter or use 'lfm config set username YOUR_USERNAME'");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user configuration");
            return Result.ConfigurationError("Failed to validate configuration", ex.Message);
        }
    }

    // Private helper methods
    private async Task<(List<T> items, string totalCount)> ExecuteRangeQueryAsync<T, TResponse>(
        string username,
        LastFmPeriod period,
        int startIndex,
        int endIndex,
        Func<string, LastFmPeriod, int, int, Task<TResponse?>> apiCall,
        Func<TResponse, List<T>> extractItems,
        Func<TResponse, string> extractTotal)
        where TResponse : class
    {
        var allItems = new List<T>();
        int rangeSize = endIndex - startIndex + 1;
        string totalCount = "0";
        
        // Calculate which pages we need
        int startPage = ((startIndex - 1) / SearchConstants.Api.MaxItemsPerPage) + 1;
        int endPage = ((endIndex - 1) / SearchConstants.Api.MaxItemsPerPage) + 1;
        
        for (int page = startPage; page <= endPage && allItems.Count < rangeSize; page++)
        {
            // Apply throttling between API calls (except for first page)
            if (page > startPage)
            {
                // Throttling now handled by CachedLastFmApiClient
            }

            var pageResult = await apiCall(username, period, SearchConstants.Api.MaxItemsPerPage, page);
            
            if (pageResult == null)
                break;
            
            var pageItems = extractItems(pageResult);
            if (pageItems == null || !pageItems.Any())
                break;
            
            totalCount = extractTotal(pageResult);
            
            // Calculate which items from this page we need
            int pageStartPosition = (page - 1) * SearchConstants.Api.MaxItemsPerPage + 1;
            
            int takeStartIndex = Math.Max(0, startIndex - pageStartPosition);
            int takeEndIndex = Math.Min(SearchConstants.Api.MaxItemsPerPage - 1, endIndex - pageStartPosition);
            int takeCount = takeEndIndex - takeStartIndex + 1;
            
            if (takeCount > 0)
            {
                var pageSelection = pageItems
                    .Skip(takeStartIndex)
                    .Take(takeCount)
                    .ToList();
                
                allItems.AddRange(pageSelection);
            }
        }
        
        var rangeItems = allItems.Take(rangeSize).ToList();
        return (rangeItems, totalCount);
    }

    /// <summary>
    /// Generates a mixtape playlist with weighted random sampling from user's listening history
    /// </summary>
    public async Task<List<Track>> GetMixtapeTracksAsync(string username,
        int targetTracks,
        LastFmPeriod? period = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        float bias = 0.3f,
        int minPlays = 0,
        int tracksPerArtist = 1,
        bool applyTagFiltering = true,
        int? seed = null)
    {
        _logger.LogInformation("Generating mixtape for user {Username}: {TargetTracks} tracks, bias={Bias}, minPlays={MinPlays}",
            username, targetTracks, bias, minPlays);

        // Load configuration for limits
        var config = await _configManager.LoadAsync();
        var maxSampleSize = config.MaxMixtapeSampleSize;

        // Step 1: Get complete track dataset using existing infrastructure
        List<Track> allTracks;
        bool isDateRange = fromDate.HasValue && toDate.HasValue;

        if (isDateRange)
        {
            // For mixtape, we want maximum diversity so use the full sample size available
            var sampleSize = maxSampleSize;

            _logger.LogDebug("Fetching up to {SampleSize} tracks for date range {From} to {To} with minPlays={MinPlays}",
                sampleSize, fromDate, toDate, minPlays);

            // Get tracks using existing infrastructure
            var dateRangeResult = await GetUserTopTracksForDateRangeAsync(username, fromDate!.Value, toDate!.Value, sampleSize);
            allTracks = dateRangeResult?.Tracks ?? new List<Track>();

            // Since date range results come back ordered by play count, filter early
            if (minPlays > 0 && allTracks.Any())
            {
                var cutoffIndex = allTracks.FindIndex(t => !int.TryParse(t.PlayCount, out var pc) || pc < minPlays);
                if (cutoffIndex > 0)
                {
                    allTracks = allTracks.Take(cutoffIndex).ToList();
                    _logger.LogInformation("Applied min-plays filter early, reduced from full set to {Count} tracks", allTracks.Count);
                }
            }
        }
        else
        {
            // Use existing range function for periods with smart min-plays optimization
            period ??= LastFmPeriod.Overall;

            // For mixtape, we want maximum diversity so use the full sample size
            var endIndex = maxSampleSize;

            _logger.LogDebug("Fetching tracks 1-{EndIndex} for period {Period}", endIndex, period);

            // Use existing GetUserTopTracksRangeAsync which handles pagination efficiently
            var (rangeTracks, totalCount) = await GetUserTopTracksRangeAsync(username, period.Value, 1, endIndex);
            allTracks = rangeTracks;

            // Apply min-plays cutoff (tracks are already ordered by play count)
            if (minPlays > 0 && allTracks.Any())
            {
                var cutoffIndex = allTracks.FindIndex(t => !int.TryParse(t.PlayCount, out var pc) || pc < minPlays);
                if (cutoffIndex > 0)
                {
                    allTracks = allTracks.Take(cutoffIndex).ToList();
                    _logger.LogInformation("Applied min-plays filter, reduced from {Original} to {Filtered} tracks", rangeTracks.Count, allTracks.Count);
                }
                else if (cutoffIndex == 0)
                {
                    // All tracks are below threshold
                    allTracks = new List<Track>();
                    _logger.LogWarning("All tracks below min-plays threshold of {MinPlays}", minPlays);
                }
            }

            _logger.LogInformation("Retrieved {TotalTracks} tracks from period {Period} (total available: {TotalCount})",
                allTracks.Count, period, totalCount);
        }

        if (!allTracks.Any())
        {
            _logger.LogWarning("No tracks found for user {Username} with minPlays={MinPlays}", username, minPlays);
            return new List<Track>();
        }

        _logger.LogInformation("Working with {TotalTracks} tracks after filtering", allTracks.Count);

        // Step 2: Generate weighted random sample with post-filtering
        var selectedTracks = new HashSet<Track>(new TrackEqualityComparer());
        var artistTrackCounts = new Dictionary<string, int>();

        // Initialize random number generator with seed for reproducibility
        var effectiveSeed = seed ?? Random.Shared.Next();
        var random = new Random(effectiveSeed);

        _logger.LogInformation("Using random seed: {Seed} (use --seed {Seed} to reproduce this mixtape)", effectiveSeed, effectiveSeed);
        var maxAttempts = targetTracks * 5; // Safety limit
        var attempts = 0;

        // Prepare weighted sampling data
        var weights = allTracks.Select(t => {
            int.TryParse(t.PlayCount, out var playCount);
            return Math.Pow(playCount, bias);
        }).ToList();
        var totalWeight = weights.Sum();

        _logger.LogDebug("Starting weighted random sampling with bias {Bias}, total weight {TotalWeight}",
            bias, totalWeight);

        while (selectedTracks.Count < targetTracks && attempts < maxAttempts)
        {
            attempts++;

            // Weighted random selection
            var randomValue = random.NextDouble() * totalWeight;
            var cumulativeWeight = 0.0;
            Track? selectedTrack = null;

            for (int i = 0; i < allTracks.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                {
                    selectedTrack = allTracks[i];
                    break;
                }
            }

            if (selectedTrack == null) continue;

            // Check for duplicates
            if (selectedTracks.Contains(selectedTrack)) continue;

            // Check artist diversity limit
            var artistName = selectedTrack.Artist.Name;
            var currentArtistCount = artistTrackCounts.GetValueOrDefault(artistName, 0);

            if (currentArtistCount >= tracksPerArtist) continue;

            // Add track
            selectedTracks.Add(selectedTrack);
            artistTrackCounts[artistName] = currentArtistCount + 1;
        }

        _logger.LogInformation("Generated {SelectedCount} tracks after {Attempts} sampling attempts",
            selectedTracks.Count, attempts);

        var mixtapeTracks = selectedTracks.ToList();

        // Step 3: Apply tag filtering if enabled (post-selection)
        if (applyTagFiltering)
        {
            if ((config.EnableTagFiltering || applyTagFiltering) && config.ExcludedTags.Any())
            {
                _logger.LogInformation("Applying tag filtering to {TrackCount} selected tracks", mixtapeTracks.Count);
                mixtapeTracks = await ApplyPostSelectionTagFilteringAsync(mixtapeTracks, config, targetTracks);

                // Step 4: Backfill if we don't have enough tracks after filtering
                await BackfillMixtapeTracksAsync(mixtapeTracks, selectedTracks, allTracks, weights, totalWeight,
                    artistTrackCounts, targetTracks, tracksPerArtist, bias, config, random);
            }
        }

        // Shuffle final result to remove any remaining bias from selection order
        for (int i = mixtapeTracks.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (mixtapeTracks[i], mixtapeTracks[j]) = (mixtapeTracks[j], mixtapeTracks[i]);
        }

        _logger.LogInformation("Mixtape generation complete: {FinalCount} tracks from {ArtistCount} artists",
            mixtapeTracks.Count, mixtapeTracks.Select(t => t.Artist.Name).Distinct().Count());

        return mixtapeTracks.Take(targetTracks).ToList();
    }

    /// <summary>
    /// Applies tag filtering to selected tracks with iterative replacement
    /// </summary>
    private async Task<List<Track>> ApplyPostSelectionTagFilteringAsync(List<Track> selectedTracks,
        LfmConfig config, int targetTracks)
    {
        var filteredTracks = new List<Track>();
        var uniqueArtists = selectedTracks.Select(t => t.Artist.Name).Distinct().ToList();

        // Cache artist tags for efficiency
        var artistTagCache = new Dictionary<string, bool>(); // artist -> shouldExclude
        int tagApiCalls = 0;

        foreach (var artistName in uniqueArtists)
        {
            if (tagApiCalls >= config.MaxTagLookups)
            {
                // If we've hit the API limit, include remaining artists by default
                artistTagCache[artistName] = false;
                continue;
            }

            try
            {
                // Apply throttling between API calls
                if (tagApiCalls > 0)
                {
                    await Task.Delay(config.ApiThrottleMs);
                }

                _logger.LogInformation("MIXTAPE TAG FILTER - Checking tags for artist: {ArtistName}", artistName);
                var tagsResult = await _dataProvider.GetArtistTopTagsAsync(artistName, autocorrect: true);
                var artistTags = tagsResult.IsSuccess ? tagsResult.Data : null;
                tagApiCalls++;

                // Use the same tag filter service as recommendations for consistency
                var shouldExclude = _tagFilterService.ShouldExcludeArtist(artistTags, config);

                // Only log exclusions and interesting cases
                if (shouldExclude)
                {
                    var matchingTags = artistTags?.Tags?
                        .Where(tag => tag.Count >= config.TagFilterThreshold &&
                                     config.ExcludedTags.Any(excludedTag =>
                                         string.Equals(excludedTag, tag.Name, StringComparison.OrdinalIgnoreCase)))
                        .Select(tag => $"{tag.Name}:{tag.Count}")
                        .ToList() ?? new List<string>();

                    _logger.LogInformation("MIXTAPE TAG FILTER - EXCLUDED '{Artist}' due to tags: {MatchingTags}",
                        artistName, string.Join(", ", matchingTags));
                }
                else
                {
                    _logger.LogInformation("MIXTAPE TAG FILTER - INCLUDED '{Artist}' (no problematic tags found)", artistName);
                }

                artistTagCache[artistName] = shouldExclude;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get tags for artist {Artist}, including by default", artistName);
                artistTagCache[artistName] = false; // Include by default on error
            }
        }

        // Filter tracks based on cached artist decisions
        foreach (var track in selectedTracks)
        {
            var shouldExclude = artistTagCache.GetValueOrDefault(track.Artist.Name, false);
            if (!shouldExclude)
            {
                filteredTracks.Add(track);
            }
        }

        var excludedCount = selectedTracks.Count - filteredTracks.Count;
        if (excludedCount > 0)
        {
            _logger.LogInformation("Tag filtering excluded {ExcludedCount} tracks, {RemainingCount} remaining",
                excludedCount, filteredTracks.Count);
        }

        return filteredTracks;
    }

    /// <summary>
    /// Backfills mixtape tracks if tag filtering reduced the count below the target
    /// </summary>
    private async Task BackfillMixtapeTracksAsync(
        List<Track> mixtapeTracks,
        HashSet<Track> selectedTracks,
        List<Track> allTracks,
        List<double> weights,
        double totalWeight,
        Dictionary<string, int> artistTrackCounts,
        int targetTracks,
        int tracksPerArtist,
        float bias,
        LfmConfig config,
        Random random)
    {
        if (mixtapeTracks.Count >= targetTracks)
        {
            _logger.LogDebug("No backfill needed: {Current} >= {Target} tracks", mixtapeTracks.Count, targetTracks);
            return;
        }

        var tracksNeeded = targetTracks - mixtapeTracks.Count;
        _logger.LogInformation("Backfill needed: {Current} tracks, need {More} more to reach {Target}",
            mixtapeTracks.Count, tracksNeeded, targetTracks);

        var backfillAttempts = 0;
        var maxBackfillAttempts = tracksNeeded * 10; // Safety limit
        var newTracks = new List<Track>();

        while (mixtapeTracks.Count < targetTracks && backfillAttempts < maxBackfillAttempts)
        {
            backfillAttempts++;

            // Weighted random selection from all tracks
            var randomValue = random.NextDouble() * totalWeight;
            var cumulativeWeight = 0.0;
            Track? candidate = null;

            for (int i = 0; i < allTracks.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                {
                    candidate = allTracks[i];
                    break;
                }
            }

            if (candidate == null) continue;

            // Skip if already selected
            if (selectedTracks.Contains(candidate)) continue;

            // Check artist diversity limit
            var artistName = candidate.Artist.Name;
            var currentArtistCount = artistTrackCounts.GetValueOrDefault(artistName, 0);
            if (currentArtistCount >= tracksPerArtist) continue;

            // Add to potential backfill candidates
            newTracks.Add(candidate);
            selectedTracks.Add(candidate); // Prevent selecting again
            artistTrackCounts[artistName] = currentArtistCount + 1;

            // Process candidates in batches for tag filtering efficiency
            if (newTracks.Count >= 10 || (mixtapeTracks.Count + newTracks.Count >= targetTracks))
            {
                _logger.LogDebug("Processing {Count} backfill candidates for tag filtering", newTracks.Count);

                var filteredCandidates = await ApplyPostSelectionTagFilteringAsync(newTracks, config, newTracks.Count);

                // Add the filtered tracks to the mixtape
                mixtapeTracks.AddRange(filteredCandidates);

                _logger.LogInformation("Backfill batch: {Filtered}/{Total} candidates passed filtering, mixtape now has {Current} tracks",
                    filteredCandidates.Count, newTracks.Count, mixtapeTracks.Count);

                // Clear for next batch
                newTracks.Clear();

                // Break early if we've reached the target
                if (mixtapeTracks.Count >= targetTracks)
                    break;
            }
        }

        // Process any remaining candidates
        if (newTracks.Any() && mixtapeTracks.Count < targetTracks)
        {
            _logger.LogDebug("Processing final {Count} backfill candidates", newTracks.Count);
            var filteredCandidates = await ApplyPostSelectionTagFilteringAsync(newTracks, config, newTracks.Count);
            mixtapeTracks.AddRange(filteredCandidates);
        }

        var finalCount = mixtapeTracks.Count;
        if (finalCount >= targetTracks)
        {
            _logger.LogInformation("Backfill successful: reached {Count} tracks after {Attempts} attempts", finalCount, backfillAttempts);
        }
        else
        {
            _logger.LogWarning("Backfill incomplete: only {Count}/{Target} tracks after {Attempts} attempts", finalCount, targetTracks, backfillAttempts);
        }
    }

    /// <summary>
    /// Gets new album releases from Spotify
    /// </summary>
    public async Task<NewReleasesResult> GetNewReleasesAsync(int limit = 50)
    {
        try
        {
            // Check if Spotify is available (using dynamic to avoid assembly reference)
            if (_spotifyStreamer == null)
            {
                return new NewReleasesResult
                {
                    Success = false,
                    Message = "Spotify is not configured. Please set up your Spotify Client ID and Client Secret using 'lfm config set-spotify' command.",
                    Albums = new List<NewAlbumRelease>(),
                    Total = 0
                };
            }

            dynamic spotifyStreamer = _spotifyStreamer;

            // Check if available
            bool isAvailable = await spotifyStreamer.IsAvailableAsync();
            if (!isAvailable)
            {
                return new NewReleasesResult
                {
                    Success = false,
                    Message = "Spotify is not configured. Please set up your Spotify Client ID and Client Secret using 'lfm config set-spotify' command.",
                    Albums = new List<NewAlbumRelease>(),
                    Total = 0
                };
            }

            // Get new releases from Spotify (returns List<SpotifyNewReleaseAlbum>)
            dynamic spotifyAlbums = await spotifyStreamer.GetNewReleasesAsync(limit);

            // Convert to our model
            var albums = new List<NewAlbumRelease>();
            foreach (dynamic album in spotifyAlbums)
            {
                var newAlbum = new NewAlbumRelease
                {
                    Name = album.Name,
                    Artist = album.Artists.Count > 0 ? album.Artists[0].Name : "Unknown Artist",
                    ReleaseDate = album.ReleaseDate,
                    ReleaseDatePrecision = album.ReleaseDatePrecision,
                    TotalTracks = album.TotalTracks,
                    AlbumType = album.AlbumType,
                    SpotifyId = album.Id,
                    SpotifyUrl = album.ExternalUrls?.Spotify ?? string.Empty,
                    ImageUrl = album.Images.Count > 0 ? album.Images[0].Url : null
                };
                albums.Add(newAlbum);
            }

            return new NewReleasesResult
            {
                Success = true,
                Message = $"Retrieved {albums.Count} new album releases from Spotify",
                Albums = albums,
                Total = albums.Count,
                Source = "spotify"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving new releases from Spotify");
            return new NewReleasesResult
            {
                Success = false,
                Message = $"Error retrieving new releases: {ex.Message}",
                Albums = new List<NewAlbumRelease>(),
                Total = 0
            };
        }
    }

    /// <summary>
    /// Comparer for track equality based on name and artist
    /// </summary>
    private class TrackEqualityComparer : IEqualityComparer<Track>
    {
        public bool Equals(Track? x, Track? y)
        {
            if (x == null || y == null) return x == y;
            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Artist.Name, y.Artist.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Track obj)
        {
            if (obj == null) return 0;
            return HashCode.Combine(
                obj.Name?.ToLowerInvariant(),
                obj.Artist.Name?.ToLowerInvariant());
        }
    }
}