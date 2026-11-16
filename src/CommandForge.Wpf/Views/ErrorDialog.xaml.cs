using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>Friendly crash window shown by the global exception handler.</summary>
public partial class ErrorDialog : FluentWindow
{
    public ErrorDialog(ErrorViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += Close;

        // Start on Close, the default answer, so the keyboard is in the window at once.
        Loaded += (_, _) => CloseButton.Focus();
    }
}
