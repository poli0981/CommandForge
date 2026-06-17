using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CommandForge.Tests;

/// <summary>Smoke tests that the Infrastructure DI registrations resolve.</summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddCommandForgeInfrastructure_ResolvesCoreServices()
    {
        var services = new ServiceCollection();
        services.AddCommandForgeInfrastructure();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISettingsService>());
        Assert.NotNull(provider.GetRequiredService<IClock>());
        Assert.NotNull(provider.GetRequiredService<ILogReader>());
        Assert.NotNull(provider.GetRequiredService<ICatalogProvider>());
        Assert.NotNull(provider.GetRequiredService<IUpdateService>());
        Assert.NotNull(provider.GetRequiredService<CheckForUpdateUseCase>());
        Assert.NotNull(provider.GetRequiredService<LegalGateService>());
    }
}
