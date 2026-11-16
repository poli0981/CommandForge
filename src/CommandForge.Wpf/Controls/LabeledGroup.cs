using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace CommandForge.Desktop.Controls;

/// <summary>
/// A <see cref="StackPanel"/> that screen readers announce as a named group, like an HTML fieldset:
/// set <c>AutomationProperties.Name</c> and Narrator says "Theme, group" when focus moves into one of
/// its radio buttons. A plain panel has no automation peer, so its name would never be heard.
/// </summary>
public sealed class LabeledGroup : StackPanel
{
    protected override AutomationPeer OnCreateAutomationPeer() => new LabeledGroupAutomationPeer(this);

    private sealed class LabeledGroupAutomationPeer(LabeledGroup owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetClassNameCore() => nameof(LabeledGroup);
    }
}
