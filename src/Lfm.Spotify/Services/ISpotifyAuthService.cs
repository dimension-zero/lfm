using Lfm.Shared.Models.Results;

namespace Lfm.Spotify.Services;

/// <summary>
/// Service for Spotify authentication and token management
/// </summary>
public interface ISpotifyAuthService
{
    /// <summary>
    /// Gets the current access token, refreshing if necessary
    /// </summary>
    Task<Result<string>> GetAccessTokenAsync();

    /// <summary>
    /// Checks if Spotify authentication is configured
    /// </summary>
    bool IsConfigured { get; }
}
