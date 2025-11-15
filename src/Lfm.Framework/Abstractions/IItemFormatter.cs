namespace Lfm.Framework.Abstractions;

/// <summary>
/// Formats domain items for display in CLI or other output
/// Implementations provide domain-specific formatting logic
/// </summary>
/// <typeparam name="TItem">The domain item type</typeparam>
public interface IItemFormatter<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// Format an item for single-line display (e.g., in a list)
    /// </summary>
    /// <param name="item">The item to format</param>
    /// <param name="maxLength">Maximum length of output (for truncation)</param>
    /// <returns>Formatted string suitable for display</returns>
    string FormatItemForLine(TItem item, int maxLength = 100);

    /// <summary>
    /// Format an item for detailed multi-line display
    /// </summary>
    /// <param name="item">The item to format</param>
    /// <returns>Formatted string with all relevant details</returns>
    string FormatItemDetailed(TItem item);

    /// <summary>
    /// Format a list of items for table display
    /// </summary>
    /// <param name="items">List of items to format</param>
    /// <param name="columns">Column names to display (e.g., ["Name", "PlayCount", "Score"])</param>
    /// <returns>Formatted table string</returns>
    string FormatItemsAsTable(List<TItem> items, List<string> columns);

    /// <summary>
    /// Get recommended column widths for table display
    /// </summary>
    /// <returns>Dictionary mapping column name to recommended width</returns>
    Dictionary<string, int> GetRecommendedColumnWidths();
}
