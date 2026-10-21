using System.Windows.Threading;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using CommandForge.Desktop.Views;
using Serilog;

namespace CommandForge.Desktop;

/// <summary>
/// Global crash handler: logs unhandled exceptions (structured, no sensitive data) and shows a
/// friendly <see cref="ErrorDialog"/> for recoverable (non-terminating) failures.
/// </summary>
public sealed class CrashHandlingService
{
    private readonly IReportBugDialogService _reportBug;
    private readonly IClipboardService _clipboard;
    private int _handling;

    public CrashHandlingService(IReportBugDialogService reportBug, IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(reportBug);
        ArgumentNullException.ThrowIfNull(clipboard);
        _reportBug = reportBug;
        _clipboard = clipboard;
    }

    /// <summary>Wires the three global exception sources. Call once at startup after the host is built.</summary>
    public void Hook()
    {
        if (System.Windows.Application.Current is { } app)
        {
            app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Handle(e.Exception, terminating: false);
        e.Handled = true; // we logged + showed the dialog; recover rather than hard-crash
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Handle(ex, terminating: e.IsTerminating);
        }

        Log.CloseAndFlush();
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }

    private void Handle(Exception ex, bool terminating)
    {
        if (Interlocked.Exchange(ref _handling, 1) == 1)
        {
            // Already handling a crash — log and bail to avoid a secondary-crash loop. The logger
            // stays open: closing it here would silence logging for the rest of the session.
            Log.Error(ex, "Secondary exception while handling a crash.");
            return;
        }

        try
        {
            var code = GenerateCode(ex);
            if (terminating)
            {
                Log.Fatal(ex, "Unhandled (terminating) exception. Code {Code}.", code);
            }
            else
            {
                Log.Error(ex, "Unhandled exception. Code {Code}.", code);
                ShowDialog(code, ex);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _handling, 0);
        }
    }

    private void ShowDialog(string code, Exception ex)
    {
        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        try
        {
            app.Dispatcher.Invoke(() =>
            {
                var viewModel = new ErrorViewModel(code, ex.ToString(), _reportBug, _clipboard);
                var dialog = new ErrorDialog(viewModel);

                // Owner can only be a window that has been shown (a crash may happen before that).
                if (app.MainWindow is { IsVisible: true } owner)
                {
                    dialog.Owner = owner;
                }

                dialog.ShowDialog();
            });
        }
        catch (Exception dialogEx)
        {
            // Never let the crash dialog itself crash the handler.
            Log.Error(dialogEx, "Failed to show the error dialog.");
        }
    }

    /// <summary>
    /// Builds a support code: UTC timestamp plus a hash of the exception type. The hash is FNV-1a so
    /// the same failure gets the same suffix in every run (string.GetHashCode is randomized per process).
    /// </summary>
    internal static string GenerateCode(Exception ex)
    {
        var name = ex.GetType().FullName ?? ex.GetType().Name;
        var hash = 2166136261u;
        foreach (var c in name)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return $"{DateTime.UtcNow:yyMMdd-HHmmss}-{hash & 0xFFFF:X4}";
    }
}
