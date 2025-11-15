using Lfm.Framework.Abstractions;
using Lfm.Wine.Models;
using Lfm.Wine.Data;
using Lfm.Shared.Models.Results;

namespace Lfm.Wine.Providers;

/// <summary>
/// Wine item provider implementing IItemProvider<WineItem>
/// Manages wine search, details, and recommendations
/// </summary>
public class WineItemProvider : IItemProvider<WineItem>
{
    private readonly MockWineDataProvider _dataProvider;

    public WineItemProvider(MockWineDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<Result<WineItem>> GetItemDetailsAsync(string itemId)
    {
        try
        {
            var wine = _dataProvider.GetAllWines()
                .FirstOrDefault(w => w.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));

            if (wine == null)
                return Result<WineItem>.Fail(ErrorType.DataError, $"Wine '{itemId}' not found");

            return await Task.FromResult(Result<WineItem>.Ok(wine));
        }
        catch (Exception ex)
        {
            return Result<WineItem>.Fail(ErrorType.UnknownError, $"Failed to get wine details", ex.Message);
        }
    }

    public async Task<Result<List<WineItem>>> SearchItemsAsync(string query, int limit = 50)
    {
        try
        {
            var results = _dataProvider.SearchWines(query).Take(limit).ToList();
            return await Task.FromResult(Result<List<WineItem>>.Ok(results));
        }
        catch (Exception ex)
        {
            return Result<List<WineItem>>.Fail(ErrorType.UnknownError, $"Search failed", ex.Message);
        }
    }

    public async Task<Result<List<WineItem>>> GetSimilarItemsAsync(string sourceItemId, int limit = 50)
    {
        try
        {
            var sourceWine = _dataProvider.GetAllWines()
                .FirstOrDefault(w => w.Id.Equals(sourceItemId, StringComparison.OrdinalIgnoreCase));

            if (sourceWine == null)
                return Result<List<WineItem>>.Fail(ErrorType.DataError, "Source wine not found");

            var similar = sourceWine.Variety != null
                ? _dataProvider.GetSimilarWines(sourceWine.Variety, sourceWine.Region)
                    .Where(w => w.Id != sourceItemId)
                    .Take(limit)
                    .ToList()
                : new List<WineItem>();

            return await Task.FromResult(Result<List<WineItem>>.Ok(similar));
        }
        catch (Exception ex)
        {
            return Result<List<WineItem>>.Fail(ErrorType.UnknownError, "Failed to get similar wines", ex.Message);
        }
    }

    public async Task<Result<bool>> ItemExistsAsync(string itemId)
    {
        try
        {
            var exists = _dataProvider.GetAllWines()
                .Any(w => w.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
            return await Task.FromResult(Result<bool>.Ok(exists));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ErrorType.UnknownError, "Failed to check wine existence", ex.Message);
        }
    }
}
