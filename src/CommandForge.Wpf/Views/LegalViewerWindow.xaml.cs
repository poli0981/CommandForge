using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>Read-only viewer for the legal documents (reuses <see cref="LegalDocumentsView"/>).</summary>
public partial class LegalViewerWindow : FluentWindow
{
    public LegalViewerWindow()
    {
        InitializeComponent();

        // Start in the documents' tabs, so the keyboard is in the window at once.
        Loaded += (_, _) => MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
