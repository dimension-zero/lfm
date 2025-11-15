using Lfm.Shared.Models.Results;

namespace Lfm.Framework.Abstractions;

/// <summary>
/// Executes domain-specific actions on items
/// Examples: Play music, add to wishlist, queue album, add to reading list
/// </summary>
/// <typeparam name="TItem">The domain item type</typeparam>
public interface IActionExecutor<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// Execute an action on an item
    /// Action types are domain-specific: "play", "queue", "addWishlist", "reserve", etc.
    /// </summary>
    /// <param name="item">The item to perform action on</param>
    /// <param name="actionType">Type of action (domain-specific)</param>
    /// <param name="parameters">Optional parameters for the action (device, quantity, etc.)</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> ExecuteActionAsync(TItem item, string actionType, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Get available actions for this domain
    /// Helps determine what actions can be performed
    /// </summary>
    /// <returns>List of action type names (e.g., ["play", "queue", "addWishlist"])</returns>
    Task<Result<List<string>>> GetAvailableActionsAsync();

    /// <summary>
    /// Check if a specific action is available
    /// </summary>
    /// <param name="actionType">Type of action to check</param>
    /// <returns>True if action is available, false otherwise</returns>
    Task<Result<bool>> IsActionAvailableAsync(string actionType);
}
