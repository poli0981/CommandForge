using System.Globalization;
using System.Windows.Controls;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Resources;
using CommandForge.Infrastructure;

namespace CommandForge.Desktop.Views;

/// <summary>Shows whether the app is installed or portable, plus its data paths and version.</summary>
public partial class PortableInfoView : UserControl
{
    public PortableInfoView(IUpdateService updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        InitializeComponent();

        ModeValue.Text = updates.IsUpdateSupported
            ? Strings.Get("Settings_RunModeInstalled")
            : Strings.Get("Settings_RunModeDev");

        var version = typeof(PortableInfoView).Assembly.GetName().Version;
        VersionValue.Text = version is null
            ? "—"
            : string.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}", version.Major, version.Minor, version.Build);

        ConfigValue.Text = AppPaths.DataDirectory;
        LogValue.Text = AppPaths.LogsDirectory;
    }
}
