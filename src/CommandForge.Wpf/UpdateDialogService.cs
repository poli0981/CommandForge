using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using CommandForge.Desktop.Views;
using Serilog;

namespace CommandForge.Desktop;

/// <summary><see cref="IUpdateDialogService"/> that shows the in-window <see cref="UpdateDialog"/>.</summary>
public sealed class UpdateDialogService : IUpdateDialogService
{
    private readonly CheckForUpdateUseCase _useCase;
    private readonly ExecutionViewModel _execution;
    private readonly InWindowDialogs _dialogs;

    public UpdateDialogService(CheckForUpdateUseCase useCase, ExecutionViewModel execution, InWindowDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(dialogs);
        _useCase = useCase;
        _execution = execution;
        _dialogs = dialogs;
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
        }

        // Never pop up over a dialog the user is working in (e.g. a confirmation); the next launch or
        // a manual check offers the update again.
        if (!_dialogs.CanShow)
        {
            Log.ForContext<UpdateDialogService>().Information(
                "Update dialog not shown because another dialog is open (update found: {Version}).", prechecked?.NewVersion);
            return prechecked?.IsSuccess ?? false;
        }

        var viewModel = new UpdateViewModel(_useCase) { IsCommandRunning = () => _execution.IsRunning };
        if (prechecked is { HasUpdate: true })
        {
            viewModel.SeedAvailable(prechecked.NewVersion);
        }
        else
        {
            // Runs on the UI thread alongside the dialog.
            _ = viewModel.CheckAsync();
        }

        await _dialogs.ShowAsync(new UpdateDialog(viewModel));
        return viewModel.CheckSucceeded;
    }
}
