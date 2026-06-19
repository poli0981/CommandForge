using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Infrastructure.Elevation;
using CommandForge.Infrastructure.Execution;
using CommandForge.Infrastructure.Logging;
using CommandForge.Infrastructure.SystemInfo;
using CommandForge.Infrastructure.Updates;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

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
        InMemoryLogStore? logStore = null,
        LoggingLevelSwitch? levelSwitch = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(logStore ?? new InMemoryLogStore());
        services.AddSingleton<ILogReader>(sp => sp.GetRequiredService<InMemoryLogStore>());
        services.AddSingleton(levelSwitch ?? new LoggingLevelSwitch());
        services.AddSingleton<ILogLevelController, SerilogLevelController>();
        services.AddSingleton<ILogMaintenance, LogMaintenance>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IExecutionHistoryService, History.JsonExecutionHistoryService>();
        services.AddSingleton<ISettingsFile, Portability.JsonSettingsFile>();
        services.AddSingleton<IProfileService, Portability.JsonProfileService>();
        services.AddSingleton<ISystemInfoService, WindowsSystemInfoService>();
        services.AddSingleton<ICatalogProvider, JsonCatalogProvider>();
        services.AddSingleton<IProcessRunner, SystemProcessRunner>();
        services.AddSingleton<ICommandExecutor, ProcessCommandExecutor>();
        services.AddSingleton<IElevationService, BrokerElevationService>();
        services.AddSingleton<IUpdateService, VelopackUpdateService>();
        services.AddSingleton<RunCommandUseCase>();
        services.AddSingleton<CheckForUpdateUseCase>();

        services.AddSingleton<LegalGateService>();

        return services;
    }
}
