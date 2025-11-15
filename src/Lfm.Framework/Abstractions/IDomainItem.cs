namespace Lfm.Framework.Abstractions;

/// <summary>
/// Base interface for any domain item (music track, wine, book, car, etc.)
/// Defines minimal properties required for all domain items
/// </summary>
public interface IDomainItem
{
    /// <summary>
    /// Unique identifier for this item (e.g., MBID for music, ISBN for books)
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name of the item
    /// </summary>
    string Name { get; }

    /// <summary>
    /// URL to an image representing this item (if available)
    /// </summary>
    string? ImageUrl { get; }
}
