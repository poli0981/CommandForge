using System.Windows;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>Modal "Report a bug" dialog (environment, export logs, open a prefilled GitHub issue).</summary>
public partial class ReportBugDialog : Window
{
    public ReportBugDialog(ReportBugViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }
}
