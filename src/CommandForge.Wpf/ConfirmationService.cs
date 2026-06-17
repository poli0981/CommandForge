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

        var dialog = new ConfirmationDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        var confirmed = dialog.ShowDialog() == true;
        return Task.FromResult<ConfirmationOutcome?>(
            confirmed ? new ConfirmationOutcome(viewModel.CreateRestorePoint) : null);
    }
}
