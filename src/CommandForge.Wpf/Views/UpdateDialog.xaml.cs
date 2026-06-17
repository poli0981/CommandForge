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
    }
}
