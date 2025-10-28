using Lfm.EfModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lfm.EfModels.Configuration;

/// <summary>
/// Entity type configuration for RecentTrack entity.
/// Uses auto-generated ID since tracks can be played multiple times.
/// </summary>
public class RecentTrackConfiguration : IEntityTypeConfiguration<RecentTrack>
{
    public void Configure(EntityTypeBuilder<RecentTrack> builder)
    {
        // Primary key: Auto-generated ID
        builder.HasKey(rt => rt.Id);

        // Index on PlayedAt for chronological queries
        builder.HasIndex(rt => rt.PlayedAt);

        // Index on User for multi-user queries
        builder.HasIndex(rt => rt.User);

        // Index on ArtistName for artist-based filtering
        builder.HasIndex(rt => rt.ArtistName);

        // Index on IsNowPlaying for current track queries
        builder.HasIndex(rt => rt.IsNowPlaying);

        // Composite index for user + date range queries (common pattern)
        builder.HasIndex(rt => new { rt.User, rt.PlayedAt });
    }
}
