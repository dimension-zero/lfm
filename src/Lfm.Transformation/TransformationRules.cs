namespace Lfm.Transformation;

/// <summary>
/// Represents the complete set of transformation rules for converting API data to MCP-friendly format.
/// Based on the API2EF2MCP architecture plan.
/// </summary>
public class TransformationRules
{
    /// <summary>
    /// Property flattening: nested objects → flat conversational format
    /// </summary>
    public Dictionary<string, FlatteningRule> PropertyFlattening { get; set; } = new();

    /// <summary>
    /// Token optimization: fields to exclude from MCP output
    /// </summary>
    public List<string> TokenOptimizationExcludes { get; set; } = new();

    /// <summary>
    /// Equivalence groups: name variations that should match
    /// </summary>
    public Dictionary<string, HashSet<string>> EquivalenceGroups { get; set; } = new();

    /// <summary>
    /// Navigation inlining: how to embed related entities
    /// </summary>
    public Dictionary<string, NavigationInliningRule> NavigationInlining { get; set; } = new();

    /// <summary>
    /// Metadata about these rules
    /// </summary>
    public string ApiName { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public RuleGenerationMode Mode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Custom annotations for extensibility
    /// </summary>
    public Dictionary<string, object?> Annotations { get; set; } = new();
}

/// <summary>
/// Defines how transformation rules were created
/// </summary>
public enum RuleGenerationMode
{
    /// <summary>
    /// Fully inferred from API schema
    /// </summary>
    Automatic,

    /// <summary>
    /// Auto-generated, then user-refined through testing
    /// </summary>
    Hybrid,

    /// <summary>
    /// User-written with templates
    /// </summary>
    Manual,

    /// <summary>
    /// Created through interactive wizard
    /// </summary>
    Interactive
}

/// <summary>
/// Defines how to flatten a nested property path to a conversational name
/// </summary>
public class FlatteningRule
{
    /// <summary>
    /// Source path in the nested object (e.g., "Track.Artist.Name")
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Target property name for conversational output (e.g., "artistName")
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include in MCP tool output
    /// </summary>
    public bool IncludeInConversation { get; set; } = true;

    /// <summary>
    /// Optional description of what this flattening achieves
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Defines how to inline a navigation property's data
/// </summary>
public class NavigationInliningRule
{
    /// <summary>
    /// Name of the navigation property (e.g., "Artist", "Album")
    /// </summary>
    public string NavigationProperty { get; set; } = string.Empty;

    /// <summary>
    /// Which properties to inline from the related entity
    /// </summary>
    public List<string> InlineProperties { get; set; } = new();

    /// <summary>
    /// Format template for inlined data (e.g., "{Name}" or "{CompanyName} ({Id})")
    /// </summary>
    public string Format { get; set; } = "{Name}";

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
