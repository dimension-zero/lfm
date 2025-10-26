using Lfm.Core.Models.Results;

namespace Lfm.Schema;

/// <summary>
/// Discovers and builds the Last.fm API schema model.
/// Since Last.fm doesn't provide an OpenAPI spec, this uses manual endpoint definition
/// based on the existing lfm implementation.
/// </summary>
public class LastFmSchemaDiscovery
{
    /// <summary>
    /// Discovers the complete Last.fm API schema.
    /// </summary>
    public Result<ApiModel> Discover()
    {
        try
        {
            var model = new ApiModel
            {
                ApiUrl = "https://ws.audioscrobbler.com/2.0/",
                ApiTitle = "Last.fm",
                ApiDescription = "Music streaming and discovery API",
                SpecificationType = ApiSpecificationType.Rest,
                Authentication = new ApiAuthenticationConfig
                {
                    Type = AuthenticationType.ApiKey,
                    ApiKeyParameterName = "api_key",
                    ApiKeyLocation = ParameterLocation.Query
                }
            };

            // Define core entities
            DefineEntities(model);

            // Define relationships between entities
            DefineRelationships(model);

            // Define API endpoints
            DefineEndpoints(model);

            return Result<ApiModel>.Ok(model);
        }
        catch (Exception ex)
        {
            return Result<ApiModel>.Fail(
                ErrorType.UnknownError,
                "Failed to discover Last.fm API schema",
                ex.Message
            );
        }
    }

    private void DefineEntities(ApiModel model)
    {
        // Artist entity
        var artist = new ApiEntity
        {
            Name = "Artist",
            Description = "Represents a music artist"
        };
        artist.Properties.Add(new ApiProperty { Name = "Name", ClrType = "string", IsRequired = true });
        artist.Properties.Add(new ApiProperty { Name = "PlayCount", ClrType = "string", Description = "String to preserve API fidelity" });
        artist.Properties.Add(new ApiProperty { Name = "Url", ClrType = "string" });
        artist.Properties.Add(new ApiProperty { Name = "Mbid", ClrType = "string", Description = "MusicBrainz ID" });
        artist.Properties.Add(new ApiProperty { Name = "Rank", ClrType = "string", Description = "Ranking in result set" });
        artist.Annotations["TokenOptimization"] = "Exclude: Url, Mbid";
        model.Entities.Add(artist);

        // Track entity
        var track = new ApiEntity
        {
            Name = "Track",
            Description = "Represents a music track"
        };
        track.Properties.Add(new ApiProperty { Name = "Name", ClrType = "string", IsRequired = true });
        track.Properties.Add(new ApiProperty { Name = "PlayCount", ClrType = "string" });
        track.Properties.Add(new ApiProperty { Name = "Url", ClrType = "string" });
        track.Properties.Add(new ApiProperty { Name = "Mbid", ClrType = "string" });
        track.Properties.Add(new ApiProperty { Name = "Rank", ClrType = "string" });
        track.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Artist",
            TargetEntity = "Artist",
            IsCollection = false,
            Description = "The artist who performed this track"
        });
        track.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Album",
            TargetEntity = "Album",
            IsCollection = false,
            Description = "The album this track appears on"
        });
        track.Annotations["TokenOptimization"] = "Exclude: Url, Mbid";
        track.Annotations["Flattening"] = "Artist.Name → artistName, Album.Name → albumName";
        model.Entities.Add(track);

        // Album entity
        var album = new ApiEntity
        {
            Name = "Album",
            Description = "Represents a music album"
        };
        album.Properties.Add(new ApiProperty { Name = "Name", ClrType = "string", IsRequired = true });
        album.Properties.Add(new ApiProperty { Name = "PlayCount", ClrType = "string" });
        album.Properties.Add(new ApiProperty { Name = "Url", ClrType = "string" });
        album.Properties.Add(new ApiProperty { Name = "Mbid", ClrType = "string" });
        album.Properties.Add(new ApiProperty { Name = "Rank", ClrType = "string" });
        album.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Artist",
            TargetEntity = "Artist",
            IsCollection = false,
            Description = "The artist who created this album"
        });
        album.Annotations["TokenOptimization"] = "Exclude: Url, Mbid";
        album.Annotations["Flattening"] = "Artist.Name → artistName";
        model.Entities.Add(album);

        // TopArtists aggregate entity
        var topArtists = new ApiEntity
        {
            Name = "TopArtists",
            Description = "Collection of top artists with metadata"
        };
        topArtists.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Artists",
            TargetEntity = "Artist",
            IsCollection = true
        });
        topArtists.Properties.Add(new ApiProperty { Name = "User", ClrType = "string" });
        topArtists.Properties.Add(new ApiProperty { Name = "Total", ClrType = "string" });
        topArtists.Properties.Add(new ApiProperty { Name = "Page", ClrType = "string" });
        topArtists.Properties.Add(new ApiProperty { Name = "PerPage", ClrType = "string" });
        topArtists.Properties.Add(new ApiProperty { Name = "TotalPages", ClrType = "string" });
        model.Entities.Add(topArtists);

        // TopTracks aggregate entity
        var topTracks = new ApiEntity
        {
            Name = "TopTracks",
            Description = "Collection of top tracks with metadata"
        };
        topTracks.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Tracks",
            TargetEntity = "Track",
            IsCollection = true
        });
        topTracks.Properties.Add(new ApiProperty { Name = "User", ClrType = "string" });
        topTracks.Properties.Add(new ApiProperty { Name = "Total", ClrType = "string" });
        topTracks.Properties.Add(new ApiProperty { Name = "Page", ClrType = "string" });
        topTracks.Properties.Add(new ApiProperty { Name = "PerPage", ClrType = "string" });
        topTracks.Properties.Add(new ApiProperty { Name = "TotalPages", ClrType = "string" });
        model.Entities.Add(topTracks);

        // TopAlbums aggregate entity
        var topAlbums = new ApiEntity
        {
            Name = "TopAlbums",
            Description = "Collection of top albums with metadata"
        };
        topAlbums.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Albums",
            TargetEntity = "Album",
            IsCollection = true
        });
        topAlbums.Properties.Add(new ApiProperty { Name = "User", ClrType = "string" });
        topAlbums.Properties.Add(new ApiProperty { Name = "Total", ClrType = "string" });
        topAlbums.Properties.Add(new ApiProperty { Name = "Page", ClrType = "string" });
        topAlbums.Properties.Add(new ApiProperty { Name = "PerPage", ClrType = "string" });
        topAlbums.Properties.Add(new ApiProperty { Name = "TotalPages", ClrType = "string" });
        model.Entities.Add(topAlbums);

        // RecentTracks entity
        var recentTracks = new ApiEntity
        {
            Name = "RecentTracks",
            Description = "Recently played tracks"
        };
        recentTracks.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Tracks",
            TargetEntity = "Track",
            IsCollection = true
        });
        recentTracks.Properties.Add(new ApiProperty { Name = "User", ClrType = "string" });
        recentTracks.Properties.Add(new ApiProperty { Name = "Total", ClrType = "string" });
        recentTracks.Properties.Add(new ApiProperty { Name = "Page", ClrType = "string" });
        recentTracks.Properties.Add(new ApiProperty { Name = "PerPage", ClrType = "string" });
        recentTracks.Properties.Add(new ApiProperty { Name = "TotalPages", ClrType = "string" });
        model.Entities.Add(recentTracks);

        // SimilarArtists entity
        var similarArtists = new ApiEntity
        {
            Name = "SimilarArtists",
            Description = "Artists similar to a given artist"
        };
        similarArtists.NavigationProperties.Add(new ApiNavigationProperty
        {
            Name = "Artists",
            TargetEntity = "Artist",
            IsCollection = true
        });
        model.Entities.Add(similarArtists);
    }

    private void DefineRelationships(ApiModel model)
    {
        // Track → Artist relationship
        model.Relationships.Add(new ApiRelationship
        {
            Name = "Track_Artist",
            PrincipalEntity = "Artist",
            DependentEntity = "Track",
            Type = RelationshipType.ManyToOne
        });

        // Album → Artist relationship
        model.Relationships.Add(new ApiRelationship
        {
            Name = "Album_Artist",
            PrincipalEntity = "Artist",
            DependentEntity = "Album",
            Type = RelationshipType.ManyToOne
        });

        // Track → Album relationship (optional)
        model.Relationships.Add(new ApiRelationship
        {
            Name = "Track_Album",
            PrincipalEntity = "Album",
            DependentEntity = "Track",
            Type = RelationshipType.ManyToOne
        });
    }

    private void DefineEndpoints(ApiModel model)
    {
        // user.getTopArtists
        var getTopArtists = new ApiEndpoint
        {
            Name = "GetTopArtists",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get user's top artists for a time period",
            ResponseEntity = "TopArtists"
        };
        getTopArtists.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "user.getTopArtists" });
        getTopArtists.Parameters.Add(new ApiParameter { Name = "user", Type = "string", IsRequired = true });
        getTopArtists.Parameters.Add(new ApiParameter { Name = "period", Type = "string", DefaultValue = "overall" });
        getTopArtists.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "10" });
        getTopArtists.Parameters.Add(new ApiParameter { Name = "page", Type = "int", DefaultValue = "1" });
        model.Endpoints.Add(getTopArtists);

        // user.getTopTracks
        var getTopTracks = new ApiEndpoint
        {
            Name = "GetTopTracks",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get user's top tracks for a time period",
            ResponseEntity = "TopTracks"
        };
        getTopTracks.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "user.getTopTracks" });
        getTopTracks.Parameters.Add(new ApiParameter { Name = "user", Type = "string", IsRequired = true });
        getTopTracks.Parameters.Add(new ApiParameter { Name = "period", Type = "string", DefaultValue = "overall" });
        getTopTracks.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "10" });
        getTopTracks.Parameters.Add(new ApiParameter { Name = "page", Type = "int", DefaultValue = "1" });
        model.Endpoints.Add(getTopTracks);

        // user.getTopAlbums
        var getTopAlbums = new ApiEndpoint
        {
            Name = "GetTopAlbums",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get user's top albums for a time period",
            ResponseEntity = "TopAlbums"
        };
        getTopAlbums.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "user.getTopAlbums" });
        getTopAlbums.Parameters.Add(new ApiParameter { Name = "user", Type = "string", IsRequired = true });
        getTopAlbums.Parameters.Add(new ApiParameter { Name = "period", Type = "string", DefaultValue = "overall" });
        getTopAlbums.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "10" });
        getTopAlbums.Parameters.Add(new ApiParameter { Name = "page", Type = "int", DefaultValue = "1" });
        model.Endpoints.Add(getTopAlbums);

        // user.getRecentTracks
        var getRecentTracks = new ApiEndpoint
        {
            Name = "GetRecentTracks",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get user's recently played tracks",
            ResponseEntity = "RecentTracks"
        };
        getRecentTracks.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "user.getRecentTracks" });
        getRecentTracks.Parameters.Add(new ApiParameter { Name = "user", Type = "string", IsRequired = true });
        getRecentTracks.Parameters.Add(new ApiParameter { Name = "from", Type = "long", Description = "Unix timestamp" });
        getRecentTracks.Parameters.Add(new ApiParameter { Name = "to", Type = "long", Description = "Unix timestamp" });
        getRecentTracks.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "200" });
        getRecentTracks.Parameters.Add(new ApiParameter { Name = "page", Type = "int", DefaultValue = "1" });
        model.Endpoints.Add(getRecentTracks);

        // artist.getTopTracks
        var getArtistTopTracks = new ApiEndpoint
        {
            Name = "GetArtistTopTracks",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get top tracks for an artist",
            ResponseEntity = "TopTracks"
        };
        getArtistTopTracks.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "artist.getTopTracks" });
        getArtistTopTracks.Parameters.Add(new ApiParameter { Name = "artist", Type = "string", IsRequired = true });
        getArtistTopTracks.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "10" });
        model.Endpoints.Add(getArtistTopTracks);

        // artist.getTopAlbums
        var getArtistTopAlbums = new ApiEndpoint
        {
            Name = "GetArtistTopAlbums",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get top albums for an artist",
            ResponseEntity = "TopAlbums"
        };
        getArtistTopAlbums.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "artist.getTopAlbums" });
        getArtistTopAlbums.Parameters.Add(new ApiParameter { Name = "artist", Type = "string", IsRequired = true });
        getArtistTopAlbums.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "10" });
        model.Endpoints.Add(getArtistTopAlbums);

        // artist.getSimilar
        var getSimilarArtists = new ApiEndpoint
        {
            Name = "GetSimilarArtists",
            Path = "/",
            HttpMethod = "GET",
            Description = "Get artists similar to a given artist",
            ResponseEntity = "SimilarArtists"
        };
        getSimilarArtists.Parameters.Add(new ApiParameter { Name = "method", Type = "string", IsRequired = true, DefaultValue = "artist.getSimilar" });
        getSimilarArtists.Parameters.Add(new ApiParameter { Name = "artist", Type = "string", IsRequired = true });
        getSimilarArtists.Parameters.Add(new ApiParameter { Name = "limit", Type = "int", DefaultValue = "50" });
        model.Endpoints.Add(getSimilarArtists);
    }
}
