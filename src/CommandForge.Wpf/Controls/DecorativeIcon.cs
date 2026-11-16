using System.Windows.Automation.Peers;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Controls;

/// <summary>
/// WPF-UI's <see cref="SymbolIcon"/> for an icon that decorates the text beside it, as every icon in
/// this app does. Its glyph is a character from the icon font's private Unicode range, drawn by a plain
/// TextBlock, so UI Automation exposed it as text and a screen reader could read that character. This
/// icon's automation peer has no children, which keeps the glyph out of the tree. An icon that means
/// something on its own gets an <c>AutomationProperties.Name</c> and is then an image with that name.
/// </summary>
public sealed class DecorativeIcon : SymbolIcon
{
    public DecorativeIcon()
    {
    }

    public DecorativeIcon(SymbolRegular symbol, double fontSize = 14, bool filled = false)
        : base(symbol, fontSize, filled)
    {
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeAutomationPeer(this);
}
