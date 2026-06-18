using System.Windows;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>Modal friendly crash dialog shown by the global exception handler.</summary>
public partial class ErrorDialog : Window
{
    public ErrorDialog(ErrorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }
}
