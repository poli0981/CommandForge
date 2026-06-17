using System.IO;
using System.Windows;
using CommandForge.Application;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.DependencyInjection;
using CommandForge.Infrastructure.Logging;
using CommandForge.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
        base.OnStartup(e);

        // The Legal Gate is shown as a modal dialog before the main window exists.
        // Without explicit shutdown control, closing that dialog would quit the app
        // (OnLastWindowClose) before we get a chance to show the main window.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        AppPaths.EnsureCreated();

        var logStore = new InMemoryLogStore();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(sink => sink.File(
                Path.Combine(AppPaths.LogsDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day))
            .WriteTo.Sink(logStore)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddCommandForgeInfrastructure(logStore);
                services.AddCommandForgeWpf();
            })
            .Build();

        Log.Information("CommandForge starting up");

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
