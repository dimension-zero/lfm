using Lfm.Data.Direct;
using Lfm.Shared.Interfaces;
using Lfm.Data.EF;
using Lfm.Data.EF.Entities;
using Lfm.Data.EF.Extensions;
using Lfm.Data.EF.Provider;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Lfm.Tests;

public class ExpressionVisitorDebugTests
{
    [Fact]
    public void ExpressionVisitor_ExtractsArtistName_FromTrackQuery()
    {
        // Arrange
        var mockClient = new Mock<ILastFmApiClient>();
        var options = new DbContextOptionsBuilder<LfmDbContext>()
            .UseLastFm(mockClient.Object, "testuser")
            .Options;
        var context = new LfmDbContext(options);

        const string artist = "Pink Floyd";

        // Build query
        var query = context.Tracks
            .Where(t => t.ArtistName == artist)
            .Take(5);

        // Get expression
        var expression = ((IQueryable<Track>)query).Expression;

        // Create visitor
        var visitor = new LastFmExpressionVisitor(typeof(Track));

        // Act
        var descriptor = visitor.Analyze(expression);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(artist, descriptor.Artist);
        Assert.Equal(QueryType.TopTracks, descriptor.QueryType);
        Assert.Equal(5, descriptor.Limit);
    }
}
