using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using CommandForge.Desktop.Behaviors;
using CommandForge.Desktop.Controls;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace CommandForge.Tests;

/// <summary>
/// The accessibility fixes that live in WPF itself, run on real controls with WPF-UI's own styles
/// and templates (on an STA thread, no window shown): the names given to WPF-UI's template parts,
/// the glyphs kept out of the automation tree, the decorative peers, the Ctrl+Q scope and the
/// screen-reader notification. A WPF-UI update that renames a template part fails here.
/// </summary>
public sealed class WpfAccessibilityRuntimeTests
{
    [Fact]
    public void TheNavigationPanesToggleAndListsAreNamedAndItsFrameIsNoTabStop() => Sta.Run(() =>
    {
        var navigation = new NavigationView { PaneDisplayMode = NavigationViewPaneDisplayMode.Left };
        Host(navigation);

        WpfUiAccessibility.Apply(navigation);

        Assert.Equal(Strings.Get("MenuToggleSidebar"), NameOf(Part(navigation, "PART_ToggleButton")));
        Assert.Equal(Strings.Get("MenuToggleSidebar"), Part(navigation, "PART_ToggleButton").ToolTip);
        Assert.Equal(Strings.Get("A11y_NavigationPages"), NameOf(Part(navigation, "PART_MenuItemsItemsControl")));
        Assert.Equal(Strings.Get("A11y_NavigationFooter"), NameOf(Part(navigation, "PART_FooterMenuItemsItemsControl")));
        Assert.False(Part(navigation, "PART_NavigationViewContentPresenter").Focusable);
    });

    [Fact]
    public void TheTitleBarsCaptionButtonsAreNamed() => Sta.Run(() =>
    {
        var titleBar = new TitleBar();
        Host(titleBar);

        WpfUiAccessibility.Apply(titleBar);

        Assert.Equal(Strings.Get("A11y_Minimize"), NameOf(Part(titleBar, "PART_MinimizeButton")));
        Assert.Equal(Strings.Get("A11y_Maximize"), NameOf(Part(titleBar, "PART_MaximizeButton")));
        Assert.Equal(Strings.Get("A11y_Close"), NameOf(Part(titleBar, "PART_CloseButton")));

        // One button for both: it draws Restore down while the window is maximized.
        titleBar.SetValue(TitleBar.IsMaximizedProperty, true);
        Assert.Equal(Strings.Get("A11y_Restore"), NameOf(Part(titleBar, "PART_MaximizeButton")));
    });

    [Fact]
    public void ATextBoxsClearButtonIsNamedOnce() => Sta.Run(() =>
    {
        var box = new Wpf.Ui.Controls.TextBox { ClearButtonEnabled = true, Text = "uptime" };
        Host(box);

        WpfUiAccessibility.Apply(box);
        WpfUiAccessibility.Apply(box);

        Assert.Equal(Strings.Get("A11y_ClearText"), NameOf(Part(box, "ClearButton")));
    });

    [Fact]
    public void ACardExpandersHeaderSpeaksTheExpandersName() => Sta.Run(() =>
    {
        var expander = new CardExpander { Header = new StackPanel() };
        AutomationProperties.SetName(expander, "Log size:");
        Host(expander);

        WpfUiAccessibility.Apply(expander);
        WpfUiAccessibility.Apply(expander);

        Assert.Equal("Log size:", NameOf(Part(expander, "ExpanderToggleButton")));
    });

    [Fact]
    public void ACheckBoxsTickAndANavigationItemsArrowAreLeftOutOfTheTree() => Sta.Run(() =>
    {
        var box = new CheckBox { Content = "Auto-scroll", IsChecked = true };
        var item = new NavigationViewItem { Content = "Command catalog" };
        item.MenuItems.Add(new NavigationViewItem { Content = "Network" });

        // A navigation item gets its template from the navigation view it is in, as in the app.
        var navigation = new NavigationView { PaneDisplayMode = NavigationViewPaneDisplayMode.Left };
        navigation.MenuItems.Add(item);
        Host(new StackPanel { Children = { box, navigation } });

        WpfUiAccessibility.Apply(box);
        WpfUiAccessibility.Apply(item);
        WpfUiAccessibility.Apply(item);

        foreach (var (control, part) in new (Control, string)[] { (box, "ControlIcon"), (item, "ChevronIcon") })
        {
            var icon = Part(control, part);
            Assert.IsType<GlyphHost>(Assert.Single(((Panel)VisualTreeHelper.GetChild(icon, 0)).Children));
            Assert.DoesNotContain(TextsIn(UIElementAutomationPeer.CreatePeerForElement(control)), IsGlyph);
        }
    });

    [Fact]
    public void AnInfoBarIsReadThroughItsSeverityIcon() => Sta.Run(() =>
    {
        var bar = new InfoBar { Title = "Not vetted.", Message = "Runs as you.", Severity = InfoBarSeverity.Warning, IsOpen = true };
        Host(bar);

        WpfUiAccessibility.Apply(bar);
        WpfUiAccessibility.Apply(bar);

        // WPF-UI draws the glyph with its own TextBlock, a subclass of WPF's.
        var glyph = Assert.IsType<TextBlock>(Assert.Single(((Panel)VisualTreeHelper.GetChild(Part(bar, "SymbolIcon"), 0)).Children), exactMatch: false);
        var pattern = Strings.Get("A11y_InfoBar");
        Assert.Equal(WpfUiAccessibility.InfoBarText(Strings.Get("A11y_SeverityWarning"), pattern, "Not vetted.", "Runs as you."), NameOf(glyph));

        bar.Severity = InfoBarSeverity.Error;
        Assert.Equal(WpfUiAccessibility.InfoBarText(Strings.Get("A11y_SeverityError"), pattern, "Not vetted.", "Runs as you."), NameOf(glyph));
        bar.Severity = InfoBarSeverity.Success;
        Assert.Equal(WpfUiAccessibility.InfoBarText(Strings.Get("A11y_SeveritySuccess"), pattern, "Not vetted.", "Runs as you."), NameOf(glyph));
        bar.Severity = InfoBarSeverity.Informational;
        Assert.Equal(WpfUiAccessibility.InfoBarText(Strings.Get("A11y_SeverityInfo"), pattern, "Not vetted.", "Runs as you."), NameOf(glyph));
    });

    [Fact]
    public void OtherElementsAreLeftAlone() => Sta.Run(() =>
    {
        WpfUiAccessibility.Register();
        WpfUiAccessibility.Register();
        var button = new System.Windows.Controls.Button();

        WpfUiAccessibility.Apply(button);
        WpfUiAccessibility.Apply(null);

        Assert.Equal(string.Empty, NameOf(button));
    });

    [Fact]
    public void ADecorativeIconIsAnImageWithoutChildrenAndOutOfTheControlViewUntilNamed() => Sta.Run(() =>
    {
        var icon = new DecorativeIcon(SymbolRegular.Warning24, 16);
        _ = new DecorativeIcon();
        var peer = UIElementAutomationPeer.CreatePeerForElement(icon);

        Assert.Null(peer.GetChildren());
        Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
        Assert.Equal(nameof(DecorativeIcon), peer.GetClassName());
        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());

        // Named, it would be an image to read: still not in the control view while it is not shown.
        AutomationProperties.SetName(icon, "Warning");
        Assert.Equal("Warning", peer.GetName());
        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
    });

    [Fact]
    public void AGlyphHostHidesTheGlyphItHolds() => Sta.Run(() =>
    {
        var host = new GlyphHost { Child = new TextBlock { Text = "" } };
        var peer = UIElementAutomationPeer.CreatePeerForElement(host);

        Assert.Null(peer.GetChildren());
        Assert.Equal(nameof(GlyphHost), peer.GetClassName());
        Assert.False(peer.IsControlElement());
    });

    [Fact]
    public void ALabeledGroupIsAGroup() => Sta.Run(() =>
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(new LabeledGroup());

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal(nameof(LabeledGroup), peer.GetClassName());
    });

    [Fact]
    public void CtrlQWaitsWhileTextIsBeingEdited() => Sta.Run(() =>
    {
        var editing = new TextBox();
        var readOnly = new TextBox { IsReadOnly = true };
        var search = new TextBox();
        ShortcutScope.SetAllowExit(search, true);

        Assert.True(ShortcutScope.CanExitFrom(null));
        Assert.True(ShortcutScope.CanExitFrom(new System.Windows.Controls.Button()));
        Assert.False(ShortcutScope.CanExitFrom(editing));
        Assert.True(ShortcutScope.CanExitFrom(readOnly));
        Assert.True(ShortcutScope.CanExitFrom(search));
        Assert.True(ShortcutScope.GetAllowExit(search));
        Assert.Throws<ArgumentNullException>(() => ShortcutScope.GetAllowExit(null!));
        Assert.Throws<ArgumentNullException>(() => ShortcutScope.SetAllowExit(null!, true));
    });

    [Fact]
    public void AnEditableComboBoxsTextBoxAnswersForItsComboBox() => Sta.Run(() =>
    {
        var combo = new ComboBox { IsEditable = true };
        Host(combo);
        var inner = Assert.IsType<TextBox>(combo.Template.FindName("PART_EditableTextBox", combo), exactMatch: false);

        Assert.False(ShortcutScope.CanExitFrom(inner));
        ShortcutScope.SetAllowExit(combo, true);
        Assert.True(ShortcutScope.CanExitFrom(inner));
    });

    [Fact]
    public void ANotificationIsRaisedOnTheWindowsPeer() => Sta.Run(() =>
    {
        // No application in a test: announcing is a no-op, never an error.
        ScreenReader.Announce(null);
        ScreenReader.Announce("  ");
        ScreenReader.Announce("Running System uptime");

        Assert.False(ScreenReader.Raise(null, "Running System uptime", important: false));
        Assert.True(ScreenReader.Raise(new Window(), "System uptime: Success", important: true));
        Assert.True(ScreenReader.Raise(new Window(), "3 commands found", important: false));
        Assert.Throws<ArgumentException>(() => ScreenReader.Raise(new Window(), " ", important: false));

        // Whether a screen reader listens depends on the PC; either way nothing fails.
        _ = ScreenReader.RaiseOnActiveWindow([new Window()], new Window(), "Waiting for administrator permission", important: false);
        Assert.Throws<ArgumentNullException>(() => ScreenReader.RaiseOnActiveWindow(null!, null, "x", important: false));
    });

    /// <summary>Lays <paramref name="content"/> out under WPF-UI's styles, as App.xaml merges them.</summary>
    private static void Host(UIElement content)
    {
        _ = System.Windows.Application.Current; // registers the pack://application: resources
        var root = new Border { Width = 900, Height = 700 };
        root.Resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Light });
        root.Resources.MergedDictionaries.Add(new ControlsDictionary());
        root.Child = content;
        root.Measure(new Size(900, 700));
        root.Arrange(new Rect(0, 0, 900, 700));
        root.UpdateLayout();
    }

    private static FrameworkElement Part(Control control, string name)
        => Assert.IsAssignableFrom<FrameworkElement>(control.Template?.FindName(name, control));

    private static string NameOf(DependencyObject element) => AutomationProperties.GetName(element);

    private static IEnumerable<string> TextsIn(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? [])
        {
            if (child.GetAutomationControlType() == AutomationControlType.Text)
            {
                yield return child.GetName();
            }

            foreach (var text in TextsIn(child))
            {
                yield return text;
            }
        }
    }

    // A character of the icon fonts' private-use range, which a screen reader can't say.
    private static bool IsGlyph(string text) => text.Any(c => c is >= '' and <= '');
}
