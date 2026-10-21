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

        // For Dangerous commands start in the type-to-confirm box; otherwise focus stays on Cancel.
        if (viewModel.IsDangerous)
        {
            Loaded += (_, _) => ConfirmationInputBox.Focus();
        }
    }
}
