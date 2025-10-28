using Lfm.EfModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lfm.EfModels.Configuration;

/// <summary>
/// Entity type configuration for Album entity.
/// Defines composite key (Name + ArtistName + User) and relationships.
/// </summary>
public class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        // Composite primary key: Name + ArtistName + User
        builder.HasKey(a => new { a.Name, a.ArtistName, a.User });

        // Index on PlayCount for sorting queries
        builder.HasIndex(a => a.PlayCount);

        // Index on Rank for ordering
        builder.HasIndex(a => a.Rank);

        // Index on User for multi-user queries
        builder.HasIndex(a => a.User);

        // Index on ArtistName for artist-based filtering
        builder.HasIndex(a => a.ArtistName);

        // Many-to-one: Album -> Artist
        // Configured in ArtistConfiguration.HasMany()

        // One-to-many: Album -> Tracks
        // Configured in TrackConfiguration.HasOne()
    }
}
