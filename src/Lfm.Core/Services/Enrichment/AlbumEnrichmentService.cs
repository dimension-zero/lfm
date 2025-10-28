using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Lfm.Core.Services.LocalFiles;

namespace Lfm.Core.Services.Enrichment;

/// <summary>
/// Orchestrates album enrichment using multiple enricher implementations.
/// Tries enrichers in priority order with timeout and caching support.
/// </summary>
public class AlbumEnrichmentService
{
    private readonly ILogger<AlbumEnrichmentService> _logger;
    private readonly AlbumEnrichmentConfig _config;
    private readonly Dictionary<string, IAlbumEnricher> _enrichers;
    private readonly ConcurrentDictionary<string, string?> _cache;

    public AlbumEnrichmentService(
        ILogger<AlbumEnrichmentService> logger,
        AlbumEnrichmentConfig config,
        IEnumerable<IAlbumEnricher> enrichers)
    {
        _logger = logger;
        _config = config;
        _enrichers = enrichers.ToDictionary(e => e.EnricherName, StringComparer.OrdinalIgnoreCase);
        _cache = new ConcurrentDictionary<string, string?>();
    }

    /// <summary>
    /// Enriches a list of play events with album information.
    /// </summary>
    public async Task<Result<List<PlayEvent>>> EnrichAlbumsAsync(List<PlayEvent> events)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Album enrichment disabled");
            return Result<List<PlayEvent>>.Ok(events);
        }

        _logger.LogInformation("Enriching {Count} play events (Mode: {Mode})", events.Count, _config.Mode);

        var enrichedEvents = new List<PlayEvent>();
        var failures = 0;
        var enriched = 0;
        var fromCache = 0;

        foreach (var playEvent in events)
        {
            // Skip if already has album
            if (!string.IsNullOrWhiteSpace(playEvent.Album))
            {
                enrichedEvents.Add(playEvent);
                continue;
            }

            var result = await EnrichSingleEventAsync(playEvent.Artist, playEvent.Track);

            if (result.IsSuccess && result.Data != null)
            {
                // Create new PlayEvent with enriched album
                enrichedEvents.Add(new PlayEvent
                {
                    Artist = playEvent.Artist,
                    Track = playEvent.Track,
                    Album = result.Data,
                    PlayedAt = playEvent.PlayedAt,
                    DataSource = playEvent.DataSource,
                    DurationMs = playEvent.DurationMs,
                    IsFullPlay = playEvent.IsFullPlay
                });
                enriched++;
            }
            else
            {
                // Keep original event (Album = null)
                enrichedEvents.Add(playEvent);
                failures++;

                if (_config.Mode == EnrichmentMode.Mandatory)
                {
                    var error = $"Mandatory enrichment failed for {playEvent.Artist} - {playEvent.Track}: {result.ErrorMessage}";
                    _logger.LogError(error);
                    return Result<List<PlayEvent>>.DataError(error);
                }
            }
        }

        _logger.LogInformation(
            "Enrichment complete: {Enriched} enriched, {FromCache} from cache, {Failures} failures",
            enriched, fromCache, failures);

        return Result<List<PlayEvent>>.Ok(enrichedEvents);
    }

    /// <summary>
    /// Enriches a single track with album information.
    /// Tries enrichers in priority order until one succeeds.
    /// </summary>
    private async Task<Result<string?>> EnrichSingleEventAsync(string artist, string track)
    {
        var cacheKey = $"{artist}|{track}".ToLowerInvariant();

        // Check cache first
        if (_config.CacheResults && _cache.TryGetValue(cacheKey, out var cachedAlbum))
        {
            _logger.LogDebug("Cache hit for {Artist} - {Track}", artist, track);
            return Result<string?>.Ok(cachedAlbum);
        }

        // Try enrichers in priority order
        foreach (var enricherName in _config.EnricherPriority)
        {
            if (!_enrichers.TryGetValue(enricherName, out var enricher))
            {
                _logger.LogWarning("Enricher '{Name}' not found, skipping", enricherName);
                continue;
            }

            try
            {
                _logger.LogDebug("Trying enricher '{Name}' for {Artist} - {Track}", enricherName, artist, track);

                using var cts = new CancellationTokenSource(_config.TimeoutMs);
                var sw = Stopwatch.StartNew();

                var result = await enricher.GetAlbumAsync(artist, track);
                sw.Stop();

                if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data))
                {
                    _logger.LogInformation(
                        "Enricher '{Name}' found album '{Album}' in {Ms}ms",
                        enricherName, result.Data, sw.ElapsedMilliseconds);

                    // Cache result
                    if (_config.CacheResults)
                    {
                        _cache.TryAdd(cacheKey, result.Data);
                    }

                    return result;
                }
                else
                {
                    _logger.LogDebug(
                        "Enricher '{Name}' did not find album (took {Ms}ms): {Error}",
                        enricherName, sw.ElapsedMilliseconds, result.ErrorMessage);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Enricher '{Name}' timed out after {Timeout}ms for {Artist} - {Track}",
                    enricherName, _config.TimeoutMs, artist, track);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Enricher '{Name}' threw exception for {Artist} - {Track}",
                    enricherName, artist, track);
            }
        }

        // No enricher found album
        _logger.LogDebug("No enricher found album for {Artist} - {Track}", artist, track);

        // Cache null result to avoid repeated failed lookups
        if (_config.CacheResults)
        {
            _cache.TryAdd(cacheKey, null);
        }

        return Result<string?>.Ok(null);
    }

    /// <summary>
    /// Clears the enrichment cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Enrichment cache cleared");
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public (int Count, int WithAlbum, int WithoutAlbum) GetCacheStats()
    {
        var withAlbum = _cache.Count(kvp => !string.IsNullOrWhiteSpace(kvp.Value));
        var withoutAlbum = _cache.Count - withAlbum;
        return (_cache.Count, withAlbum, withoutAlbum);
    }
}
