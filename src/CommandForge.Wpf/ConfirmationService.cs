using CommandForge.Application.Ports;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using CommandForge.Desktop.Views;
using CommandForge.Domain;
using Serilog;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop;

/// <summary><see cref="IConfirmationService"/> that shows the in-window <see cref="ConfirmationDialog"/>.</summary>
public sealed class ConfirmationService : IConfirmationService
{
    private readonly InWindowDialogs _dialogs;

    public ConfirmationService(InWindowDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);
        _dialogs = dialogs;
    }

    public async Task<ConfirmationOutcome?> ConfirmAsync(CommandDefinition command, bool defaultCreateRestorePoint)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Without a way to ask, the answer is "no": a risky command never runs unconfirmed.
        if (!_dialogs.CanShow)
        {
            Log.Warning("Confirmation for {CommandId} could not be shown; the command was not run.", command.Id);
            return null;
        }

        var viewModel = new ConfirmationViewModel(
            command,
            Strings.Get(command.TitleKey),
            Strings.Get(command.DescriptionKey),
            defaultCreateRestorePoint);

        var result = await _dialogs.ShowAsync(new ConfirmationDialog(viewModel));
        return result == ContentDialogResult.Primary && viewModel.CanRun
            ? new ConfirmationOutcome(viewModel.ShowRestorePointOption && viewModel.CreateRestorePoint)
            : null;
    }
}
