namespace Lfm.Core.Attributes;

/// <summary>
/// Suppresses code analysis warnings for justified cases
/// Used to exempt methods from silent failure detection when the pattern is intentional
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class SuppressMessageAttribute : Attribute
{
    /// <summary>
    /// Creates a new SuppressMessage attribute
    /// </summary>
    /// <param name="category">Category of the warning (e.g., "SilentFailure")</param>
    /// <param name="checkId">Check ID (e.g., "SF001")</param>
    /// <param name="justification">Justification for suppressing the warning</param>
    public SuppressMessageAttribute(string category, string checkId, string justification = "")
    {
        Category = category;
        CheckId = checkId;
        Justification = justification;
    }

    /// <summary>
    /// Category of the warning being suppressed
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Specific check ID being suppressed
    /// </summary>
    public string CheckId { get; }

    /// <summary>
    /// Justification for why this warning is being suppressed
    /// </summary>
    public string Justification { get; set; }
}
