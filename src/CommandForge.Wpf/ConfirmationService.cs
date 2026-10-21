using CommandForge.Application.Ports;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.ViewModels;
using CommandForge.Wpf.Views;

namespace CommandForge.Wpf;

/// <summary><see cref="IConfirmationService"/> that shows the modal <see cref="ConfirmationDialog"/>.</summary>
public sealed class ConfirmationService : IConfirmationService
{
    public Task<ConfirmationOutcome?> ConfirmAsync(CommandDefinition command, bool defaultCreateRestorePoint)
    {
        ArgumentNullException.ThrowIfNull(command);

        var viewModel = new ConfirmationViewModel(
            command,
            Strings.Get(command.TitleKey),
            Strings.Get(command.DescriptionKey),
            defaultCreateRestorePoint);

        var dialog = new ConfirmationDialog(viewModel);
        if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner)
        {
            dialog.Owner = owner;
        }

        var confirmed = dialog.ShowDialog() == true;
        return Task.FromResult<ConfirmationOutcome?>(
            confirmed ? new ConfirmationOutcome(viewModel.ShowRestorePointOption && viewModel.CreateRestorePoint) : null);
    }
}
