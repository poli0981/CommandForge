using System.Windows;

namespace CommandForge.Wpf.Views;

/// <summary>Read-only viewer for the legal documents (reuses <see cref="LegalDocumentsView"/>).</summary>
public partial class LegalViewerWindow : Window
{
    public LegalViewerWindow() => InitializeComponent();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
