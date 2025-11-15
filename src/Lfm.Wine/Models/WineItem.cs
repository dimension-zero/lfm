using Lfm.Framework.Abstractions;

namespace Lfm.Wine.Models;

/// <summary>
/// Represents a wine domain item
/// Implements IDomainItem for use with the generic framework
/// </summary>
public class WineItem : IDomainItem
{
    /// <summary>
    /// Wine type: "red", "white", "rosé", "sparkling", "fortified", etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier (wine database ID, UPC, or wine name + producer + vintage)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name (wine name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional image URL (wine label)
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Winery/Producer name
    /// </summary>
    public string? ProducerName { get; set; }

    /// <summary>
    /// Number of times user has tasted/rated this wine
    /// </summary>
    public int UserTastingCount { get; set; }

    /// <summary>
    /// User's personal rating for this wine (1-5 or 1-100 scale)
    /// </summary>
    public float? UserRating { get; set; }

    /// <summary>
    /// URL to wine details/information
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Vintage year (e.g., 2020, 2019)
    /// </summary>
    public int? Vintage { get; set; }

    /// <summary>
    /// Wine region/appellation (e.g., "Burgundy", "Napa Valley")
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Primary grape variety/varietal (e.g., "Pinot Noir")
    /// </summary>
    public string? Variety { get; set; }

    /// <summary>
    /// Average community rating/score
    /// </summary>
    public float? AverageRating { get; set; }

    /// <summary>
    /// Price in USD (if available)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Brief description of the wine
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional ranking (e.g., "Top rated in region", "#1 wine")
    /// </summary>
    public string? Ranking { get; set; }

    /// <summary>
    /// Alcohol content percentage
    /// </summary>
    public float? AlcoholContent { get; set; }
}
