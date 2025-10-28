using Lfm.Shared.Models.Results;

namespace Lfm.Core.Configuration;

/// <summary>
/// Validates configuration settings to ensure they are valid before use
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates the configuration and returns detailed error information if invalid
    /// </summary>
    Result<LfmConfig> Validate(LfmConfig config);
}
