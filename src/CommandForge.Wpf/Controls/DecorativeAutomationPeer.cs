using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace CommandForge.Desktop.Controls;

/// <summary>
/// The automation peer of an element that only decorates (<see cref="DecorativeIcon"/>,
/// <see cref="GlyphHost"/>): it has no children, so a font glyph inside it - a private-use character
/// a screen reader can't say - is not in the tree. Unnamed, it is in the raw view only; given an
/// <c>AutomationProperties.Name</c>, it is an image with that name.
/// </summary>
internal sealed class DecorativeAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    private bool IsNamed => !string.IsNullOrEmpty(AutomationProperties.GetName(Owner));

    protected override List<AutomationPeer>? GetChildrenCore() => null;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    protected override string GetClassNameCore() => Owner.GetType().Name;

    protected override bool IsControlElementCore() => IsNamed && base.IsControlElementCore();

    protected override bool IsContentElementCore() => IsNamed && base.IsContentElementCore();
}
