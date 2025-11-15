using Lfm.Wine.Models;

namespace Lfm.Wine.Data;

/// <summary>
/// Mock wine data provider for POC demonstration
/// Returns sample wine data to showcase framework versatility
/// In production, this would integrate with real wine APIs
/// </summary>
public class MockWineDataProvider
{
    private static readonly List<WineItem> SampleWines = new()
    {
        new WineItem
        {
            Type = "red",
            Id = "2019-pinot-noir-domaine-drouhin",
            Name = "Pinot Noir",
            ProducerName = "Domaine Drouhin",
            Vintage = 2019,
            Region = "Willamette Valley",
            Variety = "Pinot Noir",
            UserTastingCount = 5,
            UserRating = 4.5f,
            AverageRating = 4.3f,
            Price = 35.99m,
            AlcoholContent = 13.5f,
            Description = "Elegant Pinot Noir with notes of cherry and spice",
            Url = "https://example.com/wines/pinot-noir-drouhin"
        },
        new WineItem
        {
            Type = "white",
            Id = "2021-sauvignon-blanc-cloudy-bay",
            Name = "Sauvignon Blanc",
            ProducerName = "Cloudy Bay",
            Vintage = 2021,
            Region = "Marlborough",
            Variety = "Sauvignon Blanc",
            UserTastingCount = 3,
            UserRating = 4.0f,
            AverageRating = 4.1f,
            Price = 22.99m,
            AlcoholContent = 13.0f,
            Description = "Crisp Sauvignon Blanc with tropical fruit notes",
            Url = "https://example.com/wines/sauvignon-blanc-cloudy-bay"
        },
        new WineItem
        {
            Type = "red",
            Id = "2018-cabernet-sauvignon-caymus",
            Name = "Cabernet Sauvignon",
            ProducerName = "Caymus Vineyards",
            Vintage = 2018,
            Region = "Napa Valley",
            Variety = "Cabernet Sauvignon",
            UserTastingCount = 2,
            UserRating = 4.8f,
            AverageRating = 4.6f,
            Price = 85.00m,
            AlcoholContent = 14.2f,
            Description = "Full-bodied Cabernet with blackcurrant and oak",
            Url = "https://example.com/wines/cabernet-sauvignon-caymus"
        },
        new WineItem
        {
            Type = "rosé",
            Id = "2022-rose-provence-bandol",
            Name = "Rosé",
            ProducerName = "Domaines Ott",
            Vintage = 2022,
            Region = "Provence",
            Variety = "Grenache, Cinsault, Mourvèdre",
            UserTastingCount = 4,
            UserRating = 3.8f,
            AverageRating = 4.0f,
            Price = 19.99m,
            AlcoholContent = 12.5f,
            Description = "Dry Provence Rosé with strawberry and minerality",
            Url = "https://example.com/wines/rose-provence-ott"
        },
        new WineItem
        {
            Type = "sparkling",
            Id = "nv-champagne-moet-chandon",
            Name = "Champagne Brut Impérial",
            ProducerName = "Moët & Chandon",
            Vintage = null,
            Region = "Champagne",
            Variety = "Chardonnay, Pinot Noir, Pinot Meunier",
            UserTastingCount = 6,
            UserRating = 4.2f,
            AverageRating = 4.1f,
            Price = 45.00m,
            AlcoholContent = 12.0f,
            Description = "Classic Champagne with notes of apple and brioche",
            Url = "https://example.com/wines/champagne-moet-chandon"
        }
    };

    /// <summary>
    /// Get all sample wines
    /// </summary>
    public List<WineItem> GetAllWines() => new(SampleWines);

    /// <summary>
    /// Get wines by type
    /// </summary>
    public List<WineItem> GetWinesByType(string type)
    {
        return SampleWines
            .Where(w => w.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Get wines by region
    /// </summary>
    public List<WineItem> GetWinesByRegion(string region)
    {
        return SampleWines
            .Where(w => w.Region?.Contains(region, StringComparison.OrdinalIgnoreCase) ?? false)
            .ToList();
    }

    /// <summary>
    /// Get top rated wines
    /// </summary>
    public List<WineItem> GetTopRatedWines(int limit = 10)
    {
        return SampleWines
            .OrderByDescending(w => w.AverageRating ?? 0)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Get user's most tasted wines
    /// </summary>
    public List<WineItem> GetUserMostTastedWines(int limit = 10)
    {
        return SampleWines
            .OrderByDescending(w => w.UserTastingCount)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Find wine by name or producer
    /// </summary>
    public List<WineItem> SearchWines(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        return SampleWines
            .Where(w =>
                w.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                w.ProducerName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                w.Region?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
    }

    /// <summary>
    /// Get similar wines based on variety and region
    /// </summary>
    public List<WineItem> GetSimilarWines(string variety, string? region = null)
    {
        return SampleWines
            .Where(w =>
                (w.Variety?.Contains(variety, StringComparison.OrdinalIgnoreCase) ?? false) &&
                (region == null || (w.Region?.Equals(region, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToList();
    }
}
