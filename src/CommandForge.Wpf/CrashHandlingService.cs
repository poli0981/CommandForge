using System.Windows.Threading;
using CommandForge.Wpf.ViewModels;
using CommandForge.Wpf.Views;
using Serilog;

namespace CommandForge.Wpf;

/// <summary>
/// Global crash handler: logs unhandled exceptions (structured, no sensitive data) and shows a
/// friendly <see cref="ErrorDialog"/> for recoverable (non-terminating) failures.
/// </summary>
public sealed class CrashHandlingService
{
    private readonly IReportBugDialogService _reportBug;
    private int _handling;

    public CrashHandlingService(IReportBugDialogService reportBug)
    {
        ArgumentNullException.ThrowIfNull(reportBug);
        _reportBug = reportBug;
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

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }

    private void Handle(Exception ex, bool terminating)
    {
        if (Interlocked.Exchange(ref _handling, 1) == 1)
        {
            // Already handling a crash — log and bail to avoid a secondary-crash loop.
            Log.Fatal(ex, "Secondary exception while handling a crash.");
            Log.CloseAndFlush();
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
                var viewModel = new ErrorViewModel(code, ex.ToString(), _reportBug);
                var dialog = new ErrorDialog(viewModel) { Owner = app.MainWindow };
                dialog.ShowDialog();
            });
        }
        catch (Exception dialogEx)
        {
            // Never let the crash dialog itself crash the handler.
            Log.Error(dialogEx, "Failed to show the error dialog.");
        }
    }

    private static string GenerateCode(Exception ex)
    {
        var hash = (uint)(ex.GetType().FullName ?? ex.GetType().Name).GetHashCode(StringComparison.Ordinal) & 0xFFFF;
        return $"{DateTime.UtcNow:yyMMdd-HHmmss}-{hash:X4}";
    }
}
