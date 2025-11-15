using Lfm.Framework.Abstractions;
using Lfm.Wine.Models;
using Lfm.Shared.Models.Results;

namespace Lfm.Wine.Actions;

/// <summary>
/// Wine action executor implementing IActionExecutor<WineItem>
/// Executes wine-specific actions: add to cellar, rate, purchase, etc.
/// </summary>
public class WineActionExecutor : IActionExecutor<WineItem>
{
    private static readonly List<string> AvailableActions = new()
    {
        "addcellar",      // Add to wine cellar
        "rate",           // Rate the wine
        "purchase",       // Mark for purchase
        "viewdetails",    // View full wine details
        "viewsimilar",    // View similar wines
        "exportwinelist"  // Export cellar to file
    };

    public async Task<Result> ExecuteActionAsync(WineItem item, string actionType, Dictionary<string, object>? parameters = null)
    {
        if (!AvailableActions.Contains(actionType.ToLowerInvariant()))
            return Result.Fail(ErrorType.ValidationError, $"Unknown action '{actionType}'");

        if (item == null)
            return Result.Fail(ErrorType.ValidationError, "Wine item cannot be null");

        try
        {
            return actionType.ToLowerInvariant() switch
            {
                "addcellar" => Result.Ok(),
                "rate" => ExecuteRateAction(item, parameters),
                "purchase" => Result.Ok(),
                "viewdetails" => Result.Ok(),
                "viewsimilar" => Result.Ok(),
                "exportwinelist" => Result.Ok(),
                _ => Result.Fail(ErrorType.ValidationError, $"Unsupported action: {actionType}")
            };
        }
        catch (Exception ex)
        {
            return Result.Fail(ErrorType.UnknownError, $"Failed to execute action '{actionType}'", ex.Message);
        }
    }

    public async Task<Result<List<string>>> GetAvailableActionsAsync()
    {
        return await Task.FromResult(Result<List<string>>.Ok(new List<string>(AvailableActions)));
    }

    public async Task<Result<bool>> IsActionAvailableAsync(string actionType)
    {
        var available = AvailableActions.Contains(actionType.ToLowerInvariant());
        return await Task.FromResult(Result<bool>.Ok(available));
    }

    private Result ExecuteRateAction(WineItem item, Dictionary<string, object>? parameters)
    {
        if (parameters?.TryGetValue("rating", out var ratingObj) != true)
            return Result.Fail(ErrorType.ValidationError, "Rating parameter required");

        if (!float.TryParse(ratingObj?.ToString(), out var rating) || rating < 1 || rating > 5)
            return Result.Fail(ErrorType.ValidationError, "Rating must be between 1 and 5");

        return Result.Ok();
    }
}
