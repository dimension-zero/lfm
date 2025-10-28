using Lfm.EfModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lfm.EfModels.Configuration;

/// <summary>
/// Entity type configuration for Artist entity.
/// Defines composite key (Name + User) and relationships.
/// </summary>
public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        // Composite primary key: Name + User
        builder.HasKey(a => new { a.Name, a.User });

        // Index on PlayCount for sorting queries
        builder.HasIndex(a => a.PlayCount);

        // Index on Rank for ordering
        builder.HasIndex(a => a.Rank);

        // Index on User for multi-user queries
        builder.HasIndex(a => a.User);

        // One-to-many: Artist -> Tracks
        builder.HasMany(a => a.Tracks)
            .WithOne(t => t.Artist)
            .HasForeignKey(t => new { t.ArtistName, t.User })
            .HasPrincipalKey(a => new { a.Name, a.User })
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Artist -> Albums
        builder.HasMany(a => a.Albums)
            .WithOne(album => album.Artist)
            .HasForeignKey(album => new { album.ArtistName, album.User })
            .HasPrincipalKey(a => new { a.Name, a.User })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
