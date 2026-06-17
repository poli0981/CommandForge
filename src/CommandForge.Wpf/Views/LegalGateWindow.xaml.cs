using System.Windows;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>First-run modal that gates the app behind acceptance of the legal terms.</summary>
public partial class LegalGateWindow : Window
{
    public LegalGateWindow(LegalGateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(bool result)
    {
        // Setting DialogResult closes a dialog opened with ShowDialog().
        DialogResult = result;
    }
}
