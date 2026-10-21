using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>In-window "Check for updates" dialog (check → download → apply &amp; restart).</summary>
public partial class UpdateDialog : ContentDialog
{
    private readonly UpdateViewModel _viewModel;

    public UpdateDialog(UpdateViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();

        // Implicit styles match the exact type only: take ContentDialog's template explicitly.
        SetResourceReference(StyleProperty, typeof(ContentDialog));
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += () => Hide(ContentDialogResult.None);

        // Closing in any way (Close, Esc) must stop a download too; otherwise it would finish in the
        // background and restart the app, possibly in the middle of a command.
        Closing += (_, _) => viewModel.CancelPending();
    }

    protected override void OnButtonClick(ContentDialogButton button)
    {
        if (button == ContentDialogButton.Primary)
        {
            // Stay open to show the download progress; the view-model restarts the app when done.
            _ = _viewModel.DownloadAndRestartCommand.ExecuteAsync(null);
            return;
        }

        base.OnButtonClick(button);
    }
}
