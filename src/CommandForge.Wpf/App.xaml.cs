using System.Globalization;
using System.IO;
using System.Windows;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.DependencyInjection;
using CommandForge.Infrastructure.Logging;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.Theming;
using CommandForge.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Velopack;

namespace CommandForge.Wpf;

/// <summary>
/// Application entry point. Builds the Generic Host (DI + Serilog), then routes to the
/// Legal Gate or the main window depending on whether the current terms were accepted.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack install/update lifecycle hooks. Must run before the host/UI; may exit the process.
        // Called directly in the main assembly so the vpk packager can verify its presence.
        VelopackApp.Build().Run();

        base.OnStartup(e);

        // The Legal Gate is shown as a modal dialog before the main window exists.
        // Without explicit shutdown control, closing that dialog would quit the app
        // (OnLastWindowClose) before we get a chance to show the main window.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        AppPaths.EnsureCreated();

        var logStore = new InMemoryLogStore();
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Async(sink => sink.File(
                Path.Combine(AppPaths.LogsDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day))
            .WriteTo.Sink(logStore)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddCommandForgeInfrastructure(logStore, levelSwitch);
                services.AddCommandForgeWpf();
            })
            .Build();

        Log.Information("CommandForge starting up");

        _host.Services.GetRequiredService<CrashHandlingService>().Hook();

        ApplyUserPreferences();

        var legalGate = _host.Services.GetRequiredService<LegalGateService>();
        if (legalGate.HasAcceptedCurrentTerms())
        {
            ShowMainWindow();
        }
        else
        {
            ShowLegalGate();
        }
    }

    /// <summary>Applies the persisted language, font size and theme before any window is shown.</summary>
    private void ApplyUserPreferences()
    {
        var settings = _host!.Services.GetRequiredService<ISettingsService>();
        LocalizationManager.Instance.SetCulture(ResolveCulture(settings.Language));
        _host.Services.GetRequiredService<IFontScaleService>().Apply(settings.FontSize);
        _host.Services.GetRequiredService<IThemeService>().Apply(settings.Theme);
        _host.Services.GetRequiredService<ILogLevelController>().Current = settings.LogLevel;
    }

    private static CultureInfo ResolveCulture(string language)
        => string.IsNullOrEmpty(language) ? LocalizationManager.Instance.SystemCulture : new CultureInfo(language);

    private void ShowLegalGate()
    {
        var window = _host!.Services.GetRequiredService<LegalGateWindow>();
        var accepted = window.ShowDialog() == true;
        if (accepted)
        {
            ShowMainWindow();
        }
        else
        {
            Shutdown();
        }
    }

    private void ShowMainWindow()
    {
        var window = _host!.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();

        // Non-blocking startup update check (configurable; no-op in dev/unpackaged builds).
        _ = TryAutoCheckForUpdatesAsync();
    }

    private async Task TryAutoCheckForUpdatesAsync()
    {
        try
        {
            var settings = _host!.Services.GetRequiredService<ISettingsService>();
            var updates = _host.Services.GetRequiredService<CheckForUpdateUseCase>();
            if (!settings.AutoCheckForUpdates || !updates.IsSupported)
            {
                return;
            }

            var dialog = _host.Services.GetRequiredService<IUpdateDialogService>();
            await dialog.ShowAsync(startedFromStartup: true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Startup update check failed.");
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CommandForge shutting down");
        Log.CloseAndFlush();
        _host?.Dispose();
        base.OnExit(e);
    }
}
