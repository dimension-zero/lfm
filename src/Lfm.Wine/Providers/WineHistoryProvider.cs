using Lfm.Framework.Abstractions;
using Lfm.Wine.Models;
using Lfm.Wine.Data;
using Lfm.Shared.Models.Results;

namespace Lfm.Wine.Providers;

/// <summary>
/// Wine history provider implementing IUserHistoryProvider<WineItem>
/// Manages user's wine tasting history, ratings, and collection
/// </summary>
public class WineHistoryProvider : IUserHistoryProvider<WineItem>
{
    private readonly MockWineDataProvider _dataProvider;

    public WineHistoryProvider(MockWineDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    /// <summary>
    /// Get user's complete wine tasting history
    /// </summary>
    public async Task<Result<List<WineItem>>> GetUserHistoryAsync(string userId, int limit = int.MaxValue)
    {
        try
        {
            // Get all wines the user has tasted (where UserTastingCount > 0)
            var wines = _dataProvider.GetAllWines()
                .Where(w => w.UserTastingCount > 0)
                .OrderByDescending(w => w.UserTastingCount)
                .Take(limit)
                .ToList();

            return await Task.FromResult(Result<List<WineItem>>.Ok(wines));
        }
        catch (Exception ex)
        {
            return Result<List<WineItem>>.Fail(
                ErrorType.UnknownError,
                "Failed to get user wine history",
                ex.Message);
        }
    }

    /// <summary>
    /// Get user's top wines in a specific period (for wine: by tasting frequency or rating)
    /// </summary>
    public async Task<Result<List<WineItem>>> GetTopItemsAsync(string userId, string period = "overall", int count = 50)
    {
        try
        {
            // For wine domain, period could refer to:
            // - "overall" = all-time top wines
            // - "recent" = recently tasted wines
            // For POC, we'll use overall tasting frequency and user rating

            var wines = period.ToLower() switch
            {
                "recent" => _dataProvider.GetAllWines()
                    .Where(w => w.UserTastingCount > 0)
                    .OrderByDescending(w => w.UserRating ?? 0)
                    .Take(count)
                    .Select((w, i) => CloneWineWithRanking(w, $"#{i + 1} Recently"))
                    .ToList(),
                _ => _dataProvider.GetAllWines()
                    .Where(w => w.UserTastingCount > 0)
                    .OrderByDescending(w => w.UserRating ?? 0)
                    .ThenByDescending(w => w.UserTastingCount)
                    .Take(count)
                    .Select((w, i) => CloneWineWithRanking(w, $"#{i + 1}"))
                    .ToList()
            };

            return await Task.FromResult(Result<List<WineItem>>.Ok(wines));
        }
        catch (Exception ex)
        {
            return Result<List<WineItem>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get top wines for period '{period}'",
                ex.Message);
        }
    }

    /// <summary>
    /// Get user's wines tasted in a date range (placeholder for real wine tasting history tracking)
    /// </summary>
    public async Task<Result<List<WineItem>>> GetUserHistoryForDateRangeAsync(
        string userId,
        DateTime from,
        DateTime to,
        int count = 50)
    {
        try
        {
            // In a real implementation, this would query a wine tasting journal
            // For POC, return most frequently tasted wines as approximation
            var wines = _dataProvider.GetAllWines()
                .Where(w => w.UserTastingCount > 0)
                .OrderByDescending(w => w.UserTastingCount)
                .Take(count)
                .Select((w, i) => CloneWineWithRanking(w, $"#{i + 1}"))
                .ToList();

            return await Task.FromResult(Result<List<WineItem>>.Ok(wines));
        }
        catch (Exception ex)
        {
            return Result<List<WineItem>>.Fail(
                ErrorType.UnknownError,
                $"Failed to get user wines for date range {from:O} to {to:O}",
                ex.Message);
        }
    }

    /// <summary>
    /// Get user's tasting counts by wine
    /// Helper method for analysis and filtering
    /// </summary>
    public async Task<Result<Dictionary<string, int>>> GetUserInteractionCountsAsync(
        string userId,
        int maxItems = int.MaxValue)
    {
        try
        {
            var counts = _dataProvider.GetAllWines()
                .Where(w => w.UserTastingCount > 0)
                .Take(maxItems)
                .ToDictionary(
                    w => w.Name,
                    w => w.UserTastingCount);

            return await Task.FromResult(Result<Dictionary<string, int>>.Ok(counts));
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, int>>.Fail(
                ErrorType.UnknownError,
                "Failed to get user interaction counts",
                ex.Message);
        }
    }

    /// <summary>
    /// Helper method to clone a wine with updated ranking
    /// </summary>
    private WineItem CloneWineWithRanking(WineItem wine, string ranking)
    {
        return new WineItem
        {
            Type = wine.Type,
            Id = wine.Id,
            Name = wine.Name,
            ImageUrl = wine.ImageUrl,
            ProducerName = wine.ProducerName,
            UserTastingCount = wine.UserTastingCount,
            UserRating = wine.UserRating,
            Url = wine.Url,
            Vintage = wine.Vintage,
            Region = wine.Region,
            Variety = wine.Variety,
            AverageRating = wine.AverageRating,
            Price = wine.Price,
            Description = wine.Description,
            Ranking = ranking,
            AlcoholContent = wine.AlcoholContent
        };
    }
}
