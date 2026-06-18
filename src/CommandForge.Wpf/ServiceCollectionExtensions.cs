using CommandForge.Application.Ports;
using CommandForge.Wpf.Theming;
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

        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IUpdateDialogService, UpdateDialogService>();
        services.AddSingleton<IReportBugDialogService, ReportBugDialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IFontScaleService, FontScaleService>();
        services.AddSingleton<CrashHandlingService>();

        services.AddTransient<LegalGateViewModel>();
        // Singleton: a single shell instance whose CultureChanged subscription lives for the app.
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ExecutionViewModel>();
        services.AddTransient<CommandPaletteViewModel>();
        services.AddTransient<SettingsViewModel>();
        // Singletons: held by the singleton shell; one live ILogReader.EntriesChanged subscription each.
        services.AddSingleton<LogViewerViewModel>();
        services.AddSingleton<DebugViewModel>();

        services.AddTransient<LegalGateWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<CommandPaletteWindow>();
        services.AddTransient<PortableInfoDialog>();

        return services;
    }
}
