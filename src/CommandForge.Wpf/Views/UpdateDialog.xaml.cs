using System.Windows;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>Modal "Check for updates" dialog (check → download → apply &amp; restart).</summary>
public partial class UpdateDialog : Window
{
    public UpdateDialog(UpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += Close;

        // Closing with the title-bar X must stop the download too; otherwise it would finish in the
        // background and restart the app — possibly in the middle of a command.
        Closing += (_, _) => viewModel.CancelPending();
    }
}
