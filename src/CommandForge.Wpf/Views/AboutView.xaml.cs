using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CommandForge.Application;
using CommandForge.Desktop.Services;

namespace CommandForge.Desktop.Views;

/// <summary>The About content: app name, version, privacy note and links.</summary>
public partial class AboutView : UserControl
{
    private readonly IShellService _shell;
    private readonly Action _showTerms;

    public AboutView(IShellService shell, Action showTerms)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(showTerms);
        InitializeComponent();
        _shell = shell;
        _showTerms = showTerms;

        var version = typeof(AboutView).Assembly.GetName().Version;
        VersionText.Text = version is null
            ? "—"
            : string.Format(CultureInfo.CurrentCulture, "v{0}.{1}.{2}", version.Major, version.Minor, version.Build);
    }

    private void OnLicenseClick(object sender, RoutedEventArgs e) => _shell.TryOpen(ProductLinks.License);

    private void OnGitHubClick(object sender, RoutedEventArgs e) => _shell.TryOpen(ProductLinks.Repository);

    private void OnUserGuideClick(object sender, RoutedEventArgs e) => _shell.TryOpen(ProductLinks.UserGuide);

    private void OnTermsClick(object sender, RoutedEventArgs e) => _showTerms();
}
