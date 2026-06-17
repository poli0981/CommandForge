using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CommandForge.Wpf.Resources;

namespace CommandForge.Wpf.Views;

/// <summary>The catalog browse/run surface (search + command list + detail + console).</summary>
public partial class CatalogView : UserControl
{
    public CatalogView()
    {
        InitializeComponent();
    }

    private void OnRestartClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            System.Windows.Window.GetWindow(this),
            Strings.Get("Restart_ConfirmMessage"),
            Strings.Get("Restart_ConfirmTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.OK)
        {
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { UseShellExecute = false, CreateNoWindow = true });
        }
    }
}
