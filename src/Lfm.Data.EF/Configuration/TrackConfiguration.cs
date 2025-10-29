using Lfm.Data.EF.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lfm.Data.EF.Configuration;

/// <summary>
/// Entity type configuration for Track entity.
/// Defines composite key (Name + ArtistName + User) and relationships.
/// </summary>
public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        // Composite primary key: Name + ArtistName + User
        builder.HasKey(t => new { t.Name, t.ArtistName, t.User });

        // Index on PlayCount for sorting queries
        builder.HasIndex(t => t.PlayCount);

        // Index on Rank for ordering
        builder.HasIndex(t => t.Rank);

        // Index on User for multi-user queries
        builder.HasIndex(t => t.User);

        // Index on ArtistName for artist-based filtering
        builder.HasIndex(t => t.ArtistName);

        // Index on AlbumName for album-based filtering (nullable)
        builder.HasIndex(t => t.AlbumName);

        // Many-to-one: Track -> Artist
        // Configured in ArtistConfiguration.HasMany()

        // Many-to-one: Track -> Album (optional)
        builder.HasOne(t => t.Album)
            .WithMany(a => a.Tracks)
            .HasForeignKey(t => new { t.AlbumName, t.ArtistName, t.User })
            .HasPrincipalKey(a => new { a.Name, a.ArtistName, a.User })
            .OnDelete(DeleteBehavior.SetNull);
    }
}
