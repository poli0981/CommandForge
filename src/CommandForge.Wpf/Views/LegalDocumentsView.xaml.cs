using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CommandForge.Wpf.Views;

/// <summary>
/// The four legal documents (EULA / GPLv3 / Disclaimer / Privacy) as tabs with a summary and a
/// link to the full text on GitHub. Reused by the first-run Legal Gate and the in-app Legal Viewer.
/// </summary>
public partial class LegalDocumentsView : UserControl
{
    public LegalDocumentsView() => InitializeComponent();

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
