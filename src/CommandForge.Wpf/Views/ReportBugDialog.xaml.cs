using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>In-window "Report a bug" dialog (environment, export logs, open a prefilled GitHub issue).</summary>
public partial class ReportBugDialog : ContentDialog
{
    private readonly ReportBugViewModel _viewModel;

    public ReportBugDialog(ReportBugViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();

        // Implicit styles match the exact type only: take ContentDialog's template explicitly.
        SetResourceReference(StyleProperty, typeof(ContentDialog));
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += () => Hide(ContentDialogResult.None);
    }

    protected override void OnButtonClick(ContentDialogButton button)
    {
        // Opening the issue and exporting logs keep the dialog open: the user often does both.
        switch (button)
        {
            case ContentDialogButton.Primary:
                _viewModel.OpenIssueCommand.Execute(null);
                return;
            case ContentDialogButton.Secondary:
                _ = _viewModel.ExportLogCommand.ExecuteAsync(null);
                return;
            default:
                base.OnButtonClick(button);
                return;
        }
    }
}
