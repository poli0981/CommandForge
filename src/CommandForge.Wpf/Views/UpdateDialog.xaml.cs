using System.ComponentModel;
using System.Globalization;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>
/// In-window "Check for updates" dialog (check → download → apply &amp; restart). Each state's text is
/// also read to screen readers, and the download's progress every quarter.
/// </summary>
public partial class UpdateDialog : ContentDialog
{
    private readonly UpdateViewModel _viewModel;
    private int _announcedPercent;

    public UpdateDialog(UpdateViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();

        // Implicit styles match the exact type only: take ContentDialog's template explicitly.
        SetResourceReference(StyleProperty, typeof(ContentDialog));
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += () => Hide(ContentDialogResult.None);
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Closing in any way (Close, Esc) must stop a download too; otherwise it would finish in the
        // background and restart the app, possibly in the middle of a command.
        Closing += (_, _) =>
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.CancelPending();
        };
    }

    /// <summary>Pure: whether going from <paramref name="previous"/>% to <paramref name="current"/>% crosses a quarter worth saying.</summary>
    public static bool CrossesQuarter(int previous, int current) => current / 25 > previous / 25;

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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UpdateViewModel.Message) when !string.IsNullOrWhiteSpace(_viewModel.Message):
                ScreenReader.Announce(_viewModel.Message, important: _viewModel.IsError);
                break;
            case nameof(UpdateViewModel.IsDownloading) when _viewModel.IsDownloading:
                _announcedPercent = 0;
                ScreenReader.Announce(Strings.Get("Update_Downloading"));
                break;
            case nameof(UpdateViewModel.Percent) when CrossesQuarter(_announcedPercent, _viewModel.Percent):
                _announcedPercent = _viewModel.Percent;
                ScreenReader.Announce(string.Format(CultureInfo.CurrentCulture, Strings.Get("A11y_Percent"), _viewModel.Percent));
                break;
            default:
                break;
        }
    }
}
