using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Theming;
using CommandForge.Desktop.Views;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.DependencyInjection;
using CommandForge.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CommandForge.Desktop;

/// <summary>
/// The WPF application. Builds the DI container and Serilog, then routes to the Legal Gate or the
/// main window depending on whether the current terms were accepted. The process entry point is
/// <see cref="Program.Main"/>.
/// </summary>
public partial class App : System.Windows.Application
{
    private const long MaxLogFileBytes = 10 * 1024 * 1024;
    private const int RetainedLogFiles = 14;

    private readonly SingleInstanceGuard? _guard;
    private ServiceProvider? _services;

    /// <summary>Parameterless constructor for the XAML designer; the app itself starts via <see cref="Program.Main"/>.</summary>
    public App()
    {
    }

    internal App(SingleInstanceGuard guard) => _guard = guard;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The Legal Gate is shown as a modal dialog before the main window exists.
        // Without explicit shutdown control, closing that dialog would quit the app
        // (OnLastWindowClose) before we get a chance to show the main window.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            Start();
        }
        catch (Exception ex)
        {
            // Failing here with no window up would otherwise leave an invisible process holding the
            // single-instance mutex, so every later launch would silently do nothing.
            Log.Fatal(ex, "Startup failed.");
            ShowStartupError(ex);
            Shutdown(1);
        }
    }

    private void Start()
    {
        AppPaths.EnsureCreated();

        var logStore = new InMemoryLogStore();
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        Log.Logger = CreateLogger(logStore, levelSwitch);
        Log.Information("CommandForge {Version} starting up", typeof(App).Assembly.GetName().Version);

        var services = new ServiceCollection();
        services.AddCommandForgeInfrastructure(logStore, levelSwitch);
        services.AddCommandForgeWpf();
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        _services.GetRequiredService<CrashHandlingService>().Hook();
        _guard?.StartListening(OnActivationRequested);

        ApplyUserPreferences();

        var legalGate = _services.GetRequiredService<LegalGateService>();
        if (legalGate.HasAcceptedCurrentTerms())
        {
            ShowMainWindow();
        }
        else
        {
            ShowLegalGate();
        }
    }

    private static Logger CreateLogger(InMemoryLogStore logStore, LoggingLevelSwitch levelSwitch)
        => new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Async(sink => sink.File(
                Path.Combine(AppPaths.LogsDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: MaxLogFileBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: RetainedLogFiles))
            .WriteTo.Sink(logStore)
            .CreateLogger();

    /// <summary>Applies the persisted language, font size and theme before any window is shown.</summary>
    private void ApplyUserPreferences()
    {
        var services = _services!;
        var settings = services.GetRequiredService<ISettingsService>();

        // Recreate config.json with defaults if it was deleted while the app was closed.
        if (!File.Exists(AppPaths.ConfigFilePath))
        {
            settings.Save();
        }

        LocalizationManager.Instance.SetCulture(ResolveCulture(settings.Language));
        LocalizationManager.Instance.TrackWindowLanguage();
        services.GetRequiredService<IFontScaleService>().Apply(settings.FontSize);
        services.GetRequiredService<IThemeService>().Apply(settings.Theme);
        services.GetRequiredService<ILogLevelController>().Current = settings.LogLevel;
    }

    private static CultureInfo ResolveCulture(string language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return LocalizationManager.Instance.SystemCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException ex)
        {
            Log.Warning(ex, "Unknown language '{Language}' in config.json; following the system language.", language);
            return LocalizationManager.Instance.SystemCulture;
        }
    }

    private void ShowLegalGate()
    {
        var window = _services!.GetRequiredService<LegalGateWindow>();
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
        var window = _services!.GetRequiredService<MainWindow>();
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
            var services = _services!;
            var settings = services.GetRequiredService<ISettingsService>();
            var updates = services.GetRequiredService<CheckForUpdateUseCase>();
            if (!settings.AutoCheckForUpdates || !updates.IsSupported)
            {
                return;
            }

            var dialog = services.GetRequiredService<IUpdateDialogService>();
            await dialog.ShowAsync(startedFromStartup: true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Startup update check failed.");
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        string message;
        try
        {
            message = string.Format(CultureInfo.CurrentCulture, Strings.Get("Startup_FailedMessage"), ex.Message, AppPaths.LogsDirectory);
        }
        catch (Exception)
        {
            // Resources may be what failed — fall back to plain English.
            message = $"CommandForge could not start: {ex.Message}{Environment.NewLine}{Environment.NewLine}Details were written to {AppPaths.LogsDirectory}";
        }

        MessageBox.Show(message, ProductInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>Invoked (on a background thread) when a second instance asks us to surface our window.</summary>
    private void OnActivationRequested() => Dispatcher.InvokeAsync(ActivateExistingWindow);

    /// <summary>Restores and foregrounds the existing top-level window (main window, or the Legal Gate if it is still open).</summary>
    private void ActivateExistingWindow()
    {
        var window = MainWindow
            ?? System.Windows.Application.Current.Windows.Cast<Window>().LastOrDefault();
        if (window is null)
        {
            return; // Signalled before any window is shown — the primary is about to show one anyway.
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Show();
        window.Activate();
        SetForegroundWindow(new WindowInteropHelper(window).Handle);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CommandForge shutting down");

        // Disposing the container closes the elevation broker's pipe, which ends the Elevator.
        _services?.Dispose();

        // Flush last so anything logged while disposing is kept.
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
