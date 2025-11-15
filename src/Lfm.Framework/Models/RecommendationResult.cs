using Lfm.Framework.Abstractions;

namespace Lfm.Framework.Models;

/// <summary>
/// Generic recommendation result for any domain
/// Represents a single recommended item with scoring metadata
/// </summary>
/// <typeparam name="TItem">The recommended item type</typeparam>
public class RecommendationResult<TItem> where TItem : IDomainItem
{
    /// <summary>
    /// The recommended item
    /// </summary>
    public required TItem Item { get; set; }

    /// <summary>
    /// Composite score based on algorithm (0-100 scale)
    /// Higher scores indicate stronger recommendations
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Average similarity/relevance score across all source items
    /// Algorithm-specific: ranges from 0-1 for similarity, 0-100 for other metrics
    /// </summary>
    public float AverageRelevance { get; set; }

    /// <summary>
    /// Number of user's items that contributed to this recommendation
    /// Higher values indicate broader appeal across user's taste
    /// </summary>
    public int OccurrenceCount { get; set; }

    /// <summary>
    /// User's current interaction count for this item (0 if never interacted)
    /// Play count for music, read count for books, tasting count for wine, etc.
    /// Used for filtering recommendations (hide already-consumed items)
    /// </summary>
    public int UserInteractionCount { get; set; }

    /// <summary>
    /// Names/identifiers of user's source items that led to this recommendation
    /// Useful for explaining why this item was recommended
    /// Example: ["The Beatles", "Pink Floyd"] → recommends "The Rolling Stones"
    /// </summary>
    public List<string> SourceItems { get; set; } = new();

    /// <summary>
    /// Additional metadata specific to the recommendation
    /// Domain adapters can store domain-specific data here
    /// Example: Wine adapter stores flavor profile, region, vintage
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
