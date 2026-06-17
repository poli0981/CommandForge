using System.Windows;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>Modal confirmation for Caution/Dangerous commands.</summary>
public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(ConfirmationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
