using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Wpf.ViewModels;
using CommandForge.Wpf.Views;
using Serilog;

namespace CommandForge.Wpf;

/// <summary><see cref="IUpdateDialogService"/> that shows the modal <see cref="UpdateDialog"/>.</summary>
public sealed class UpdateDialogService : IUpdateDialogService
{
    private readonly CheckForUpdateUseCase _useCase;
    private readonly ExecutionViewModel _execution;

    public UpdateDialogService(CheckForUpdateUseCase useCase, ExecutionViewModel execution)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(execution);
        _useCase = useCase;
        _execution = execution;
    }

    /// <inheritdoc />
    public async Task<bool> ShowAsync(bool startedFromStartup)
    {
        UpdateCheckResult? prechecked = null;
        if (startedFromStartup)
        {
            // Silent check: only surface the dialog when there is actually an update; on error,
            // log and stay quiet (no nagging on launch).
            try
            {
                prechecked = await _useCase.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Log.ForContext<UpdateDialogService>().Warning(ex, "Startup update check failed; ignoring.");
                return false;
            }

            if (!prechecked.HasUpdate)
            {
                return prechecked.IsSuccess;
            }

            // Never pop up over a dialog the user is working in (e.g. a confirmation); the next
            // launch or a manual check will offer the update again.
            if (IsAnotherDialogOpen())
            {
                Log.ForContext<UpdateDialogService>().Information("Update {Version} found at startup; not shown because another dialog is open.", prechecked.NewVersion);
                return true;
            }
        }

        var viewModel = new UpdateViewModel(_useCase) { IsCommandRunning = () => _execution.IsRunning };
        var dialog = new UpdateDialog(viewModel);
        if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner)
        {
            dialog.Owner = owner;
        }

        if (prechecked is { HasUpdate: true })
        {
            viewModel.SeedAvailable(prechecked.NewVersion);
        }
        else
        {
            // Runs on the UI thread; awaited continuations pump during the modal ShowDialog loop.
            _ = viewModel.CheckAsync();
        }

        dialog.ShowDialog();
        return viewModel.CheckSucceeded;
    }

    private static bool IsAnotherDialogOpen()
    {
        var app = System.Windows.Application.Current;
        return app is not null
            && app.Windows.OfType<System.Windows.Window>().Any(w => w.IsVisible && !ReferenceEquals(w, app.MainWindow));
    }
}
