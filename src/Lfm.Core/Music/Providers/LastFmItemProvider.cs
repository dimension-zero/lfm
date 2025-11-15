using Lfm.Framework.Abstractions;
using Lfm.Core.Music.Models;
using Lfm.Shared.Interfaces;
using Lfm.Shared.Models.Results;
using Lfm.Shared.Configuration;

namespace Lfm.Core.Music.Providers;

/// <summary>
/// Provides Last.fm item data implementing IItemProvider<MusicItem>
/// Adapter that wraps existing LastFmApiClient to work with the framework
/// </summary>
public class LastFmItemProvider : IItemProvider<MusicItem>
{
    private readonly ILastFmApiClient _apiClient;
    private readonly string _currentUsername;

    public LastFmItemProvider(ILastFmApiClient apiClient, string currentUsername)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _currentUsername = currentUsername ?? throw new ArgumentNullException(nameof(currentUsername));
    }

    /// <summary>
    /// Get detailed information about a specific item (artist)
    /// </summary>
    public async Task<Result<MusicItem>> GetItemDetailsAsync(string itemId)
    {
        try
        {
            // For music items, itemId is typically the artist name
            var artistInfo = await _apiClient.GetArtistInfoWithResultAsync(itemId, _currentUsername);

            if (!artistInfo.IsSuccess)
                return Result<MusicItem>.Fail(artistInfo.Error);

            if (artistInfo.Value?.Artist == null)
                return Result<MusicItem>.Fail(
                    ErrorType.DataError,
                    $"Artist '{itemId}' not found");

            var musicItem = new MusicItem
            {
                Type = "artist",
                Id = artistInfo.Value.Artist.Mbid ?? itemId,
                Name = artistInfo.Value.Artist.Name,
                ImageUrl = null,
                ArtistName = null,
                PlayCount = artistInfo.Value.Artist.Stats.GetUserPlaycount(),
                Url = artistInfo.Value.Artist.Url,
                AlbumName = null,
                Ranking = null
            };

            return Result<MusicItem>.Ok(musicItem);
        }
        catch (Exception ex)
        {
            return Result<MusicItem>.Fail(
                ErrorType.UnknownError,
                $"Failed to get details for item '{itemId}'",
                ex.Message);
        }
    }

    /// <summary>
    /// Search for items by query string
    /// Returns top tracks for a given artist
    /// </summary>
    public async Task<Result<List<MusicItem>>> SearchItemsAsync(string query, int limit = 50)
    {
        try
        {
            // Search by getting an artist's top tracks
            var artistTracks = await _apiClient.GetArtistTopTracksWithResultAsync(query, limit);

            if (!artistTracks.IsSuccess)
                return Result<List<MusicItem>>.Fail(artistTracks.Error);

            var musicItems = artistTracks.Value.Tracks
                .Select((t, i) => MusicItemConverter.ToMusicItem(t, ranking: $"#{i + 1}"))
                .ToList();

            return Result<List<MusicItem>>.Ok(musicItems);
        }
        catch (Exception ex)
        {
            return Result<List<MusicItem>>.Fail(
                ErrorType.UnknownError,
                $"Failed to search for items matching '{query}'",
                ex.Message);
        }
    }

    /// <summary>
    /// Get similar or related items based on a source item
    /// Returns similar artists for a given artist
    /// </summary>
    public async Task<Result<List<MusicItem>>> GetSimilarItemsAsync(string sourceItemId, int limit = 50)
    {
        try
        {
            // Get similar artists
            var similarArtists = await _apiClient.GetSimilarArtistsWithResultAsync(sourceItemId, limit);

            if (!similarArtists.IsSuccess)
                return Result<List<MusicItem>>.Fail(similarArtists.Error);

            var musicItems = similarArtists.Value.Artists
                .Select((a, i) => MusicItemConverter.ToMusicItem(a, ranking: $"#{i + 1}"))
                .ToList();

            return Result<List<MusicItem>>.Ok(musicItems);
        }
        catch (Exception ex)
        {
            return Result<List<MusicItem>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get similar items for '{sourceItemId}'",
                ex.Message);
        }
    }

    /// <summary>
    /// Check if an item exists in Last.fm
    /// </summary>
    public async Task<Result<bool>> ItemExistsAsync(string itemId)
    {
        try
        {
            // Try to get artist info to verify existence
            var artistInfo = await _apiClient.GetArtistInfoWithResultAsync(itemId, _currentUsername);

            return Result<bool>.Ok(artistInfo.IsSuccess && artistInfo.Value?.Artist != null);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(
                ErrorType.UnknownError,
                $"Failed to check if item '{itemId}' exists",
                ex.Message);
        }
    }
}
