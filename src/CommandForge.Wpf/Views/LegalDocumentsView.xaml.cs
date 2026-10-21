using System.Windows;
using System.Windows.Controls;
using CommandForge.Desktop.Services;

namespace CommandForge.Desktop.Views;

/// <summary>
/// The four legal documents (EULA / GPLv3 / Disclaimer / Privacy) as tabs with a summary and a
/// link to the full text on GitHub. Reused by the first-run Legal Gate and the in-app Legal Viewer.
/// </summary>
public partial class LegalDocumentsView : UserControl
{
    private readonly ShellService _shell = new();

    public LegalDocumentsView() => InitializeComponent();

    private void OnLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
        {
            _shell.TryOpen(url);
        }
    }
}
