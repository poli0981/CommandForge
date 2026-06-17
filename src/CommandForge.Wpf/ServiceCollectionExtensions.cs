using CommandForge.Wpf.ViewModels;
using CommandForge.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CommandForge.Wpf;

/// <summary>Registers WPF view-models and windows with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the WPF presentation layer (view-models + windows).</summary>
    public static IServiceCollection AddCommandForgeWpf(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<LegalGateViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<CommandPaletteViewModel>();

        services.AddTransient<LegalGateWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<CommandPaletteWindow>();

        return services;
    }
}
