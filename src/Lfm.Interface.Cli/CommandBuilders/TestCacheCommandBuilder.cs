using System.CommandLine;
using Lfm.Interface.Cli.Commands;
using Lfm.Shared.Configuration;
using Lfm.Core.Configuration;
using Lfm.Data.Direct.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lfm.Interface.Cli.CommandBuilders;

/// <summary>
/// Temporary command builder for testing cache directory functionality.
/// This will be removed once caching is fully implemented.
/// </summary>
public static class TestCacheCommandBuilder
{
    public static Command Build(IServiceProvider serviceProvider)
    {
        var command = new Command("test-cache", "Test cache directory functionality (temporary command)")
        {
            IsHidden = true // Hide from normal help output
        };

        command.SetHandler(() =>
        {
            var cacheDirectoryHelper = serviceProvider.GetRequiredService<ICacheDirectoryHelper>();
            var cacheStorage = serviceProvider.GetRequiredService<ICacheStorage>();
            var keyGenerator = serviceProvider.GetRequiredService<ICacheKeyGenerator>();
            var logger = serviceProvider.GetRequiredService<ILogger<TestCacheCommand>>();
            
            var testCommand = new TestCacheCommand(cacheDirectoryHelper, cacheStorage, keyGenerator, logger);
            testCommand.Execute();
        });

        return command;
    }
}