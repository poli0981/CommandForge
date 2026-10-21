using System.Windows;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>Read-only viewer for the legal documents (reuses <see cref="LegalDocumentsView"/>).</summary>
public partial class LegalViewerWindow : FluentWindow
{
    public LegalViewerWindow() => InitializeComponent();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
