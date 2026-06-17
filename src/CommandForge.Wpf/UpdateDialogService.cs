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

    public UpdateDialogService(CheckForUpdateUseCase useCase)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        _useCase = useCase;
    }

    /// <inheritdoc />
    public async Task ShowAsync(bool startedFromStartup)
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
                return;
            }

            if (!prechecked.HasUpdate)
            {
                return;
            }
        }

        var viewModel = new UpdateViewModel(_useCase) { IsCommandRunning = BuildCommandRunningGuard() };
        var dialog = new UpdateDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

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
    }

    private static Func<bool>? BuildCommandRunningGuard()
        => System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel main
            ? () => main.Execution.IsRunning
            : null;
}
