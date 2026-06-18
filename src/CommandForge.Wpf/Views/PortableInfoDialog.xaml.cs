using System.Globalization;
using System.Windows;
using CommandForge.Application.Ports;
using CommandForge.Infrastructure;
using CommandForge.Wpf.Resources;

namespace CommandForge.Wpf.Views;

/// <summary>Shows whether the app is installed or portable, plus its data paths and version.</summary>
public partial class PortableInfoDialog : Window
{
    public PortableInfoDialog(IUpdateService updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        InitializeComponent();

        ModeValue.Text = updates.IsUpdateSupported
            ? Strings.Get("Settings_RunModeInstalled")
            : Strings.Get("Settings_RunModePortable");

        var version = typeof(PortableInfoDialog).Assembly.GetName().Version;
        VersionValue.Text = version is null
            ? "—"
            : string.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}", version.Major, version.Minor, version.Build);

        ConfigValue.Text = AppPaths.DataDirectory;
        LogValue.Text = AppPaths.LogsDirectory;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
