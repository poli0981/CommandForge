using System.Diagnostics;
using System.Globalization;
using System.Windows;
using CommandForge.Application;

namespace CommandForge.Wpf.Views;

/// <summary>The About dialog: app name, version, links, and a Terms &amp; Privacy viewer.</summary>
public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        var version = typeof(AboutDialog).Assembly.GetName().Version;
        VersionText.Text = version is null
            ? "—"
            : string.Format(CultureInfo.CurrentCulture, "v{0}.{1}.{2}", version.Major, version.Minor, version.Build);
    }

    private void OnLicenseClick(object sender, RoutedEventArgs e) => Open(ProductLinks.License);

    private void OnGitHubClick(object sender, RoutedEventArgs e) => Open(ProductLinks.Repository);

    private void OnWikiClick(object sender, RoutedEventArgs e) => Open(ProductLinks.UserGuide);

    private void OnTermsClick(object sender, RoutedEventArgs e)
        => new LegalViewerWindow { Owner = this }.ShowDialog();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private static void Open(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
