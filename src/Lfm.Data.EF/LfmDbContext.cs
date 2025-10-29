using Lfm.Data.EF.Configuration;
using Lfm.Data.EF.Entities;
using Lfm.Data.EF.Extensions;
using Lfm.Data.EF.Provider;
using Microsoft.EntityFrameworkCore;

namespace Lfm.Data.EF;

/// <summary>
/// Entity Framework Core DbContext for Last.fm API data.
/// Provides LINQ query capabilities over Last.fm entities (Artist, Track, Album, RecentTrack).
///
/// Usage:
/// <code>
/// var context = new LfmDbContext(options);
/// var topArtists = await context.Artists
///     .Where(a => a.User == "smarshal")
///     .OrderByDescending(a => a.PlayCount)
///     .Take(10)
///     .ToListAsync();
/// </code>
/// </summary>
public class LfmDbContext : DbContext
{
    private readonly LastFmQueryProvider? _queryProvider;

    /// <summary>
    /// Artists from user.getTopArtists API endpoint.
    /// </summary>
    public IQueryable<Artist> Artists => GetQueryable<Artist>();

    /// <summary>
    /// Tracks from user.getTopTracks and artist.getTopTracks API endpoints.
    /// </summary>
    public IQueryable<Track> Tracks => GetQueryable<Track>();

    /// <summary>
    /// Albums from user.getTopAlbums and artist.getTopAlbums API endpoints.
    /// </summary>
    public IQueryable<Album> Albums => GetQueryable<Album>();

    /// <summary>
    /// Recently played tracks from user.getRecentTracks API endpoint.
    /// </summary>
    public IQueryable<RecentTrack> RecentTracks => GetQueryable<RecentTrack>();

    /// <summary>
    /// Parameterless constructor for design-time tools (migrations, etc.).
    /// </summary>
    public LfmDbContext()
    {
    }

    /// <summary>
    /// Constructor with options (used at runtime).
    /// </summary>
    /// <param name="options">DbContext options (configured via UseLastFm() extension)</param>
    public LfmDbContext(DbContextOptions<LfmDbContext> options)
        : base(options)
    {
        // Extract options extension to create query provider
        var extension = options.FindExtension<LfmOptionsExtension>();
        if (extension?.DataProvider != null && extension.DefaultUser != null)
        {
            _queryProvider = new LastFmQueryProvider(extension.DataProvider, extension.DefaultUser);
        }
    }

    /// <summary>
    /// Get a queryable for an entity type using the Last.fm query provider.
    /// </summary>
    private IQueryable<T> GetQueryable<T>() where T : class
    {
        if (_queryProvider == null)
        {
            // Fallback to empty set for design-time tools
            return Set<T>();
        }

        return new LastFmQueryable<T>(_queryProvider);
    }

    /// <summary>
    /// Configure entity mappings and relationships.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ArtistConfiguration());
        modelBuilder.ApplyConfiguration(new TrackConfiguration());
        modelBuilder.ApplyConfiguration(new AlbumConfiguration());
        modelBuilder.ApplyConfiguration(new RecentTrackConfiguration());

        // Global query filters (optional - for soft delete, tenant isolation, etc.)
        // Can be added here if needed in future
    }

    /// <summary>
    /// Configure the database provider (called when no options are provided).
    /// For Last.fm, we use a custom in-memory provider.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Default configuration for design-time tools
            // At runtime, options should be configured via dependency injection
            optionsBuilder.UseInMemoryDatabase("LastFmDesignTime");
        }
    }
}
