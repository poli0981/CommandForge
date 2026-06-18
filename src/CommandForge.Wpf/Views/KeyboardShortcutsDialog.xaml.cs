using System.Windows;
using CommandForge.Wpf.Resources;

namespace CommandForge.Wpf.Views;

/// <summary>Read-only list of keyboard shortcuts (those actually wired in the shell).</summary>
public partial class KeyboardShortcutsDialog : Window
{
    public KeyboardShortcutsDialog()
    {
        InitializeComponent();
        DataContext = new[]
        {
            new Shortcut("Ctrl+K", Strings.Get("Shortcuts_Palette")),
            new Shortcut("Ctrl+H", Strings.Get("SidebarHome")),
            new Shortcut("Ctrl+B", Strings.Get("MenuToggleSidebar")),
            new Shortcut("Ctrl+,", Strings.Get("Menu_OpenSettings")),
            new Shortcut("Ctrl+L", Strings.Get("Menu_LogViewer")),
            new Shortcut("F5", Strings.Get("Menu_CheckForUpdates")),
            new Shortcut("F11", Strings.Get("Menu_FullScreen")),
            new Shortcut("Ctrl+Q", Strings.Get("MenuExit")),
            new Shortcut("↑ / ↓", Strings.Get("Shortcuts_ListNav")),
            new Shortcut("Enter", Strings.Get("Shortcuts_OpenSelected")),
            new Shortcut("Esc", Strings.Get("Shortcuts_CloseDialog")),
        };
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private sealed record Shortcut(string Key, string Action);
}
