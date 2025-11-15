using Lfm.Framework.Abstractions;
using Lfm.Core.Music.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Core.Music.Actions;

/// <summary>
/// Executes music-specific actions on MusicItem objects
/// Supports actions like: play, queue, addPlaylist
/// </summary>
public class MusicActionExecutor : IActionExecutor<MusicItem>
{
    /// <summary>
    /// Available actions for music items
    /// </summary>
    private static readonly List<string> AvailableActions = new()
    {
        "play",           // Play the artist's music
        "queue",          // Queue the artist's music
        "addPlaylist",    // Add to a playlist
        "viewTracks",     // View top tracks
        "viewAlbums",     // View top albums
        "viewSimilar"     // View similar artists
    };

    /// <summary>
    /// Execute an action on a music item
    /// </summary>
    public async Task<Result> ExecuteActionAsync(MusicItem item, string actionType, Dictionary<string, object>? parameters = null)
    {
        // Validate action type
        if (!AvailableActions.Contains(actionType.ToLowerInvariant()))
            return Result.Fail(
                ErrorType.ValidationError,
                $"Unknown action type '{actionType}' for music items");

        if (item == null)
            return Result.Fail(
                ErrorType.ValidationError,
                "Item cannot be null");

        try
        {
            return actionType.ToLowerInvariant() switch
            {
                "play" => ExecutePlayAction(item, parameters),
                "queue" => ExecuteQueueAction(item, parameters),
                "addplaylist" => ExecuteAddPlaylistAction(item, parameters),
                "viewtracks" => ExecuteViewTracksAction(item, parameters),
                "viewalbums" => ExecuteViewAlbumsAction(item, parameters),
                "viewsimilar" => ExecuteViewSimilarAction(item, parameters),
                _ => Result.Fail(ErrorType.ValidationError, $"Unsupported action: {actionType}")
            };
        }
        catch (Exception ex)
        {
            return Result.Fail(
                ErrorType.UnknownError,
                $"Failed to execute action '{actionType}' on item '{item.Name}'",
                ex.Message);
        }
    }

    /// <summary>
    /// Get available actions for music items
    /// </summary>
    public async Task<Result<List<string>>> GetAvailableActionsAsync()
    {
        return await Task.FromResult(Result<List<string>>.Ok(new List<string>(AvailableActions)));
    }

    /// <summary>
    /// Check if a specific action is available
    /// </summary>
    public async Task<Result<bool>> IsActionAvailableAsync(string actionType)
    {
        var isAvailable = AvailableActions.Contains(actionType.ToLowerInvariant());
        return await Task.FromResult(Result<bool>.Ok(isAvailable));
    }

    /// <summary>
    /// Execute play action (placeholder - would integrate with Spotify/Sonos)
    /// </summary>
    private Result ExecutePlayAction(MusicItem item, Dictionary<string, object>? parameters)
    {
        // In a real implementation, this would:
        // 1. Check parameters for target player (Spotify, Sonos, etc.)
        // 2. Call appropriate playback service
        // For now, return success indicating the action was recognized
        return Result.Ok();
    }

    /// <summary>
    /// Execute queue action (placeholder)
    /// </summary>
    private Result ExecuteQueueAction(MusicItem item, Dictionary<string, object>? parameters)
    {
        return Result.Ok();
    }

    /// <summary>
    /// Execute add to playlist action (placeholder)
    /// </summary>
    private Result ExecuteAddPlaylistAction(MusicItem item, Dictionary<string, object>? parameters)
    {
        if (parameters?.TryGetValue("playlistName", out var playlistObj) != true)
            return Result.Fail(
                ErrorType.ValidationError,
                "playlistName parameter is required for addPlaylist action");

        return Result.Ok();
    }

    /// <summary>
    /// Execute view tracks action (placeholder)
    /// </summary>
    private Result ExecuteViewTracksAction(MusicItem item, Dictionary<string, object>? parameters)
    {
        return Result.Ok();
    }

    /// <summary>
    /// Execute view albums action (placeholder)
    /// </summary>
    private Result ExecuteViewAlbumsAction(MusicItem item, Dictionary<string, object>? parameters)
    {
        return Result.Ok();
    }

    /// <summary>
    /// Execute view similar action (placeholder)
    /// </summary>
    private Result ExecuteViewSimilarAction(MusicItem item, Dictionary<string, object>? parameters)
    {
        return Result.Ok();
    }
}
