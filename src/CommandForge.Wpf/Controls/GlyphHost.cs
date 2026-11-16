using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace CommandForge.Desktop.Controls;

/// <summary>
/// Holds the glyph of an icon that a WPF-UI control template draws (a check box's tick, a navigation
/// item's arrow), where <see cref="DecorativeIcon"/> can't be used: <see cref="Behaviors.WpfUiAccessibility"/>
/// moves the glyph's TextBlock in here, and this element's automation peer keeps it out of the tree.
/// The control itself already says what the mark shows (checked, expanded).
/// </summary>
public sealed class GlyphHost : Decorator
{
    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativeAutomationPeer(this);
}
