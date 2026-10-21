using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.DependencyInjection;
using CommandForge.Infrastructure.History;
using CommandForge.Infrastructure.Portability;
using CommandForge.Infrastructure.Recipes;
using CommandForge.Infrastructure.UserCommands;
using Microsoft.Extensions.DependencyInjection;

namespace CommandForge.Tests;

/// <summary>Smoke tests that the Infrastructure DI registrations resolve.</summary>
public sealed class DependencyInjectionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"cf-di-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void AddCommandForgeInfrastructure_ResolvesCoreServices()
    {
        var services = new ServiceCollection();
        services.AddCommandForgeInfrastructure();

        // Later registrations win: point every store at a scratch folder so the test never reads or
        // writes the real %AppData%\CommandForge.
        services.AddSingleton<ISettingsService>(new JsonSettingsService(Path.Combine(_directory, "config.json")));
        services.AddSingleton<IExecutionHistoryService>(new JsonExecutionHistoryService(Path.Combine(_directory, "history.json")));
        services.AddSingleton<IProfileService>(new JsonProfileService(Path.Combine(_directory, "profiles.json")));
        services.AddSingleton<IRecipeStore>(new JsonRecipeStore(Path.Combine(_directory, "recipes.json")));
        services.AddSingleton<IUserCommandStore>(new JsonUserCommandStore(Path.Combine(_directory, "user-commands.json")));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.NotNull(provider.GetRequiredService<ISettingsService>());
        Assert.NotNull(provider.GetRequiredService<IClock>());
        Assert.NotNull(provider.GetRequiredService<ILogReader>());
        Assert.NotNull(provider.GetRequiredService<ICatalogProvider>());
        Assert.NotNull(provider.GetRequiredService<IUpdateService>());
        Assert.NotNull(provider.GetRequiredService<CheckForUpdateUseCase>());
        Assert.NotNull(provider.GetRequiredService<RunCommandUseCase>());
        Assert.NotNull(provider.GetRequiredService<ILogMaintenance>());
        Assert.NotNull(provider.GetRequiredService<ILogLevelController>());
        Assert.NotNull(provider.GetRequiredService<LegalGateService>());
        Assert.False(Directory.Exists(_directory)); // resolving reads; nothing is written until a change
    }
}
