using CommandForge.Application.Ports;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.Theming;
using CommandForge.Desktop.ViewModels;
using CommandForge.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using ContentDialogService = Wpf.Ui.ContentDialogService;
using IContentDialogService = Wpf.Ui.IContentDialogService;
using ISnackbarService = Wpf.Ui.ISnackbarService;
using SnackbarService = Wpf.Ui.SnackbarService;

namespace CommandForge.Desktop;

/// <summary>Registers WPF view-models and windows with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the WPF presentation layer (view-models + windows).</summary>
    public static IServiceCollection AddCommandForgeWpf(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // In-window Fluent dialogs and notifications; the main window hands them its hosts.
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<InWindowDialogs>();
        services.AddSingleton<SnackbarNotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<SnackbarNotificationService>());

        services.AddSingleton<IDialogService, FluentDialogService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IShellService, ShellService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IUpdateDialogService, UpdateDialogService>();
        services.AddSingleton<IReportBugDialogService, ReportBugDialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IFontScaleService, FontScaleService>();
        services.AddSingleton<CrashHandlingService>();

        services.AddTransient<LegalGateViewModel>();
        // Singleton: a single shell instance whose CultureChanged subscription lives for the app.
        services.AddSingleton<MainViewModel>();
        // Singleton: one shared console/run state (also read by the update dialog's "command running" guard).
        services.AddSingleton<ExecutionViewModel>();
        services.AddTransient<CommandPaletteViewModel>();
        services.AddTransient<SettingsViewModel>();
        // Singletons: held by the singleton shell; one live ILogReader.EntriesChanged subscription each.
        services.AddSingleton<LogViewerViewModel>();
        services.AddSingleton<DebugViewModel>();

        services.AddTransient<LegalGateWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<CommandPaletteWindow>();

        return services;
    }
}
