using System.Windows.Controls;
using CommandForge.Desktop.Resources;

namespace CommandForge.Desktop.Views;

/// <summary>Read-only list of keyboard shortcuts (those actually wired in the shell).</summary>
public partial class KeyboardShortcutsView : UserControl
{
    public KeyboardShortcutsView()
    {
        InitializeComponent();
        ShortcutList.ItemsSource = new[]
        {
            new Shortcut("Ctrl+K", Strings.Get("Shortcuts_Palette")),
            new Shortcut("Ctrl+F", Strings.Get("Shortcuts_Search")),
            new Shortcut("Ctrl+H", Strings.Get("SidebarHome")),
            new Shortcut("Ctrl+B", Strings.Get("MenuToggleSidebar")),
            new Shortcut("Ctrl+,", Strings.Get("Menu_OpenSettings")),
            new Shortcut("Ctrl+L", Strings.Get("Menu_LogViewer")),
            new Shortcut("F5", Strings.Get("Menu_CheckForUpdates")),
            new Shortcut("Ctrl+Q", Strings.Get("MenuExit")),
            new Shortcut("↑ / ↓", Strings.Get("Shortcuts_ListNav")),
            new Shortcut("Enter", Strings.Get("Shortcuts_OpenSelected")),
            new Shortcut("Esc", Strings.Get("Shortcuts_CloseDialog")),
        };
    }

    private sealed record Shortcut(string Key, string Action);
}
