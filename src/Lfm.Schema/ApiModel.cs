namespace Lfm.Schema;

/// <summary>
/// Represents the complete metadata model of the Last.fm API.
/// Adapted from API2EF.Core.Metadata.ApiModel for lfm-specific use.
/// </summary>
public class ApiModel
{
    public string ApiUrl { get; set; } = "https://ws.audioscrobbler.com/2.0/";
    public string ApiTitle { get; set; } = "Last.fm";
    public string? ApiDescription { get; set; }
    public ApiSpecificationType SpecificationType { get; set; } = ApiSpecificationType.Rest;

    public IList<ApiEntity> Entities { get; } = new List<ApiEntity>();
    public IList<ApiRelationship> Relationships { get; } = new List<ApiRelationship>();
    public IList<ApiEndpoint> Endpoints { get; } = new List<ApiEndpoint>();
    public ApiAuthenticationConfig? Authentication { get; set; }
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();

    public ApiEntity? FindEntity(string name)
    {
        return Entities.FirstOrDefault(e =>
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public ApiEndpoint? FindEndpoint(string path)
    {
        return Endpoints.FirstOrDefault(ep =>
            string.Equals(ep.Path, path, StringComparison.OrdinalIgnoreCase));
    }
}

public enum ApiSpecificationType
{
    Unknown,
    OpenApi,
    OData,
    Rest,
    GraphQL
}

/// <summary>
/// Represents an entity (resource) in the API, parallel to a database table.
/// </summary>
public class ApiEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IList<ApiProperty> Properties { get; } = new List<ApiProperty>();
    public IList<ApiNavigationProperty> NavigationProperties { get; } = new List<ApiNavigationProperty>();
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();

    public ApiProperty? FindProperty(string name)
    {
        return Properties.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Represents a property of an entity, parallel to a database column.
/// </summary>
public class ApiProperty
{
    public string Name { get; set; } = string.Empty;
    public string ClrType { get; set; } = "string";
    public string? ApiType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; } = true;
    public string? Description { get; set; }
    public object? DefaultValue { get; set; }
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();
}

/// <summary>
/// Represents a navigation property (relationship to another entity).
/// </summary>
public class ApiNavigationProperty
{
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public bool IsCollection { get; set; }
    public string? Description { get; set; }
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();
}

/// <summary>
/// Represents a relationship between two entities.
/// </summary>
public class ApiRelationship
{
    public string Name { get; set; } = string.Empty;
    public string PrincipalEntity { get; set; } = string.Empty;
    public string DependentEntity { get; set; } = string.Empty;
    public string? PrincipalProperty { get; set; }
    public string? DependentProperty { get; set; }
    public RelationshipType Type { get; set; }
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();
}

public enum RelationshipType
{
    OneToMany,
    ManyToOne,
    OneToOne,
    ManyToMany
}

/// <summary>
/// Represents an API endpoint.
/// </summary>
public class ApiEndpoint
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public string? Description { get; set; }
    public IList<ApiParameter> Parameters { get; } = new List<ApiParameter>();
    public string? ResponseEntity { get; set; }
    public string? RequestEntity { get; set; }
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();
}

/// <summary>
/// Represents a parameter for an API endpoint.
/// </summary>
public class ApiParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public ParameterLocation Location { get; set; } = ParameterLocation.Query;
}

public enum ParameterLocation
{
    Query,
    Path,
    Header,
    Body
}

/// <summary>
/// Authentication configuration for the API.
/// </summary>
public class ApiAuthenticationConfig
{
    public AuthenticationType Type { get; set; } = AuthenticationType.None;
    public string? ApiKeyParameterName { get; set; }
    public ParameterLocation ApiKeyLocation { get; set; } = ParameterLocation.Query;
    public IDictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();
}

public enum AuthenticationType
{
    None,
    ApiKey,
    Basic,
    Bearer,
    OAuth2
}
