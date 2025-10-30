using System.CommandLine;
using Lfm.Interface.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Lfm.Interface.Cli.CommandBuilders;

public static class CacheStatusCommandBuilder
{
    public static Command Build(IServiceProvider services)
    {
        var command = new Command("cache-status", "Show cache statistics and configuration");

        command.SetHandler(async () =>
        {
            var cacheStatusCommand = services.GetRequiredService<CacheStatusCommand>();
            await cacheStatusCommand.ExecuteAsync();
        });

        return command;
    }
}