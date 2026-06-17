using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Infrastructure.Elevation;
using CommandForge.Infrastructure.Execution;
using CommandForge.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CommandForge.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Infrastructure adapters and the Application services that depend on them.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Infrastructure adapters (settings, clock, logging) and application
    /// services. Pass the <paramref name="logStore"/> already wired into Serilog so the
    /// Log Viewer reads the same instance; if omitted, a fresh store is created.
    /// </summary>
    public static IServiceCollection AddCommandForgeInfrastructure(
        this IServiceCollection services,
        InMemoryLogStore? logStore = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(logStore ?? new InMemoryLogStore());
        services.AddSingleton<ILogReader>(sp => sp.GetRequiredService<InMemoryLogStore>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<ICatalogProvider, JsonCatalogProvider>();
        services.AddSingleton<IProcessRunner, SystemProcessRunner>();
        services.AddSingleton<ICommandExecutor, ProcessCommandExecutor>();
        services.AddSingleton<IElevationService, BrokerElevationService>();
        services.AddSingleton<RunCommandUseCase>();

        services.AddSingleton<LegalGateService>();

        return services;
    }
}
