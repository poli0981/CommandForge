using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace CommandForge.Wpf.Views;

/// <summary>The About dialog: app name, version, links, and a Terms &amp; Privacy viewer.</summary>
public partial class AboutDialog : Window
{
    private const string RepoUrl = "https://github.com/poli0981/CommandForge";
    private const string WikiUrl = "https://github.com/poli0981/CommandForge/wiki";
    private const string LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html";

    public AboutDialog()
    {
        InitializeComponent();
        var version = typeof(AboutDialog).Assembly.GetName().Version;
        VersionText.Text = version is null
            ? "—"
            : string.Format(CultureInfo.CurrentCulture, "v{0}.{1}.{2}", version.Major, version.Minor, version.Build);
    }

    private void OnLicenseClick(object sender, RoutedEventArgs e) => Open(LicenseUrl);

    private void OnGitHubClick(object sender, RoutedEventArgs e) => Open(RepoUrl);

    private void OnWikiClick(object sender, RoutedEventArgs e) => Open(WikiUrl);

    private void OnTermsClick(object sender, RoutedEventArgs e)
        => new LegalViewerWindow { Owner = this }.ShowDialog();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private static void Open(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
