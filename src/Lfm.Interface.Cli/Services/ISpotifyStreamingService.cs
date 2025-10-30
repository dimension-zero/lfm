using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Interface.Cli.Services;

public interface ISpotifyStreamingService
{
    Task<bool> IsAvailableAsync();
    Task StreamTracksAsync(List<Track> tracks, bool playNow, string? playlistName, string defaultPlaylistTitle, bool shuffle = false, string? device = null, bool verbose = false);
    Task StreamRecommendationsAsync(List<RecommendationResult> recommendations, bool playNow, string? playlistName, string defaultPlaylistTitle, bool shuffle = false, string? device = null, bool verbose = false);
}