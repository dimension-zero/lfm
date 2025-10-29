using Lfm.Core.Configuration;
using Lfm.Shared.Models.Results;

namespace Lfm.Tests.Mocks;

/// <summary>
/// Mock IConfigurationManager for testing that doesn't persist to disk
/// </summary>
public class MockConfigurationManager : IConfigurationManager
{
    private LfmConfig _config;
    private readonly List<string> _saveLog = new();

    public MockConfigurationManager(LfmConfig? initialConfig = null)
    {
        _config = initialConfig ?? new LfmConfig();
    }

    public Task<LfmConfig> LoadAsync()
    {
        return Task.FromResult(_config);
    }

    public Task<Result<LfmConfig>> LoadWithValidationAsync()
    {
        return Task.FromResult(Result<LfmConfig>.Ok(_config));
    }

    public Task SaveAsync(LfmConfig config)
    {
        _config = config;
        _saveLog.Add($"Config saved at {DateTime.UtcNow:O}");
        return Task.CompletedTask;
    }

    public string GetConfigPath()
    {
        return "/mock/config.json";
    }

    /// <summary>
    /// Returns the log of save operations (useful for testing)
    /// </summary>
    public IReadOnlyList<string> SaveLog => _saveLog.AsReadOnly();

    /// <summary>
    /// Resets the save log
    /// </summary>
    public void ResetSaveLog()
    {
        _saveLog.Clear();
    }

    /// <summary>
    /// Gets the current config (useful for testing)
    /// </summary>
    public LfmConfig GetCurrentConfig()
    {
        return _config;
    }
}
