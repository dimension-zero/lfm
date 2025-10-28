using System.ComponentModel.DataAnnotations;

namespace Lfm.EfModels.Entities;

/// <summary>
/// Base class for all Last.fm EF entities.
/// Provides common properties for user context and query tracking.
/// </summary>
public abstract class LastFmEntity
{
    /// <summary>
    /// The Last.fm username this entity belongs to.
    /// Used to support multi-user queries and ensure data isolation.
    /// </summary>
    [Required]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this entity was queried from the API.
    /// Used for cache invalidation and data freshness tracking.
    /// </summary>
    public DateTime QueryTimestamp { get; set; } = DateTime.UtcNow;
}
