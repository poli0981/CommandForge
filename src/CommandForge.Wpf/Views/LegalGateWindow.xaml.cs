using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>First-run window that gates the app behind acceptance of the legal terms.</summary>
public partial class LegalGateWindow : FluentWindow
{
    public LegalGateWindow(LegalGateViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;

        // Setting DialogResult closes a window opened with ShowDialog().
        viewModel.CloseRequested += result => DialogResult = result;

        // Start in the language list: the terms below are read in the language chosen there.
        Loaded += (_, _) => LanguageBox.Focus();
    }
}
