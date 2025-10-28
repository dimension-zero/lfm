using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Shared.Models.Results;
using Lfm.Spotify.Models;

namespace Lfm.Spotify.Services;

/// <summary>
/// Handles Spotify OAuth authentication and token management
/// </summary>
public class SpotifyAuthService : ISpotifyAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyConfig _config;
    private readonly IConfigurationManager _configManager;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public bool IsConfigured => !string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.ClientSecret);

    public SpotifyAuthService(SpotifyConfig config, IConfigurationManager configManager, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Result<string>> GetAccessTokenAsync()
    {
        // Token is still valid
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return Result<string>.Ok(_accessToken);
        }

        // Try to refresh the token
        if (!string.IsNullOrEmpty(_config.RefreshToken))
        {
            var refreshSuccess = await RefreshAccessTokenAsync();
            if (refreshSuccess && !string.IsNullOrEmpty(_accessToken))
            {
                return Result<string>.Ok(_accessToken);
            }

            // Refresh failed, need OAuth flow
            Console.WriteLine("⚠️  Refresh token invalid or expired");
            Console.WriteLine("Starting OAuth flow to re-authenticate...\n");
        }

        // Need to do initial OAuth flow
        var oauthSuccess = await DoInitialOAuthFlowAsync();
        if (oauthSuccess && !string.IsNullOrEmpty(_accessToken))
        {
            return Result<string>.Ok(_accessToken);
        }

        return Result<string>.Fail(ErrorType.AuthenticationError,
            "Failed to authenticate with Spotify",
            "OAuth flow did not complete successfully");
    }

    private async Task<bool> RefreshAccessTokenAsync()
    {
        try
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _config.RefreshToken)
            });

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", request);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(json);
                if (tokenResponse != null)
                {
                    _accessToken = tokenResponse.AccessToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // 60 second buffer
                    return true;
                }
            }

            Console.WriteLine($"⚠️  Failed to refresh Spotify token: {json}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Exception refreshing Spotify token: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> DoInitialOAuthFlowAsync()
    {
        try
        {
            var redirectUri = "http://127.0.0.1:8888/callback";

            var authUrl = $"https://accounts.spotify.com/authorize?" +
                         $"client_id={_config.ClientId}&" +
                         $"response_type=code&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"scope={HttpUtility.UrlEncode("user-modify-playback-state user-read-playback-state playlist-modify-private playlist-modify-public playlist-read-private playlist-read-collaborative")}";

            Console.WriteLine("\n🎵 Spotify Authentication Required!");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("1. Click or visit this URL to authorize the application:");
            Console.WriteLine($"   {authUrl}");
            Console.WriteLine();
            Console.WriteLine("2. After authorization, you'll be redirected to a page that won't load");
            Console.WriteLine("   (this is expected - the redirect URL will look like http://127.0.0.1:8888/callback?code=...)");
            Console.WriteLine();
            Console.WriteLine("3. Copy the entire URL from your browser's address bar and paste it here:");
            Console.WriteLine("   (or just copy the 'code' parameter value)");
            Console.WriteLine();
            Console.Write("Paste the redirect URL or authorization code: ");

            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("⚠️  Authorization code is required");
                return false;
            }

            var code = ExtractCodeFromInput(input);
            if (string.IsNullOrEmpty(code))
            {
                Console.WriteLine("⚠️  Could not extract authorization code from input");
                return false;
            }

            return await ExchangeCodeForTokensAsync(code);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  OAuth flow failed: {ex.Message}");
            return false;
        }
    }

    private string ExtractCodeFromInput(string input)
    {
        // If it looks like a URL, extract the code parameter
        if (input.Contains("code="))
        {
            var uri = new Uri(input.Contains("://") ? input : $"http://127.0.0.1:8888{input}");
            var query = HttpUtility.ParseQueryString(uri.Query);
            return query["code"] ?? string.Empty;
        }

        // Otherwise assume it's the code directly
        return input.Trim();
    }

    private async Task<bool> ExchangeCodeForTokensAsync(string code)
    {
        try
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", "http://127.0.0.1:8888/callback")
            });

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", request);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(json);
                if (tokenResponse != null)
                {
                    _accessToken = tokenResponse.AccessToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

                    // Save refresh token to config automatically
                    if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    {
                        await SaveRefreshTokenAsync(tokenResponse.RefreshToken);
                        Console.WriteLine($"\n✅ Refresh token saved! Future commands won't require re-authorization.");
                    }

                    return true;
                }
            }

            Console.WriteLine($"⚠️  Failed to exchange code for tokens: {json}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Exception exchanging code for tokens: {ex.Message}");
            return false;
        }
    }

    private async Task SaveRefreshTokenAsync(string refreshToken)
    {
        var config = await _configManager.LoadAsync();
        config.Spotify.RefreshToken = refreshToken;
        await _configManager.SaveAsync(config);
    }
}
