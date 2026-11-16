using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommandForge.Desktop.Controls;
using CommandForge.Desktop.Resources;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Behaviors;

/// <summary>
/// WPF-UI gives some parts of its control templates an AutomationId but no name, so Narrator says only
/// "button" or "list": the navigation pane's toggle and its two lists, the title bar's caption buttons,
/// a text box's clear (×) button and a card expander's header. Its templates also draw icons as font
/// glyphs that UI Automation lists as text: the arrow of a navigation item that has sub-items, a check
/// box's tick and an info bar's severity icon. Registered once at startup, these class handlers name
/// those parts from the app's strings (live, so a language switch renames them), keep the arrow and
/// the tick out of the tree (the item and the check box already say expanded or checked) and make the
/// severity icon read the whole info bar, whose own text the tree leaves out.
/// </summary>
public static class WpfUiAccessibility
{
    /// <summary>The controls <see cref="Apply"/> fixes.</summary>
    private static readonly Type[] HandledTypes =
    [
        typeof(NavigationView), typeof(TitleBar), typeof(Wpf.Ui.Controls.TextBox), typeof(CardExpander),
        typeof(NavigationViewItem), typeof(CheckBox), typeof(InfoBar),
    ];

    private static bool _registered;

    /// <summary>
    /// Registers the handlers for every window the app opens. Call once, before the first window.
    /// <see cref="Apply"/> runs when one of the controls loads and whenever its size changes. Loaded
    /// alone is not enough: WPF raises it only on elements that have a Loaded handler of their own, and
    /// some WPF-UI controls (CardExpander) have none, so a class handler never hears it. SizeChanged is
    /// raised on every element after its first layout, when its template is applied.
    /// </summary>
    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        var handler = new RoutedEventHandler((sender, _) => Apply(sender));
        foreach (var type in HandledTypes)
        {
            EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, handler);
            EventManager.RegisterClassHandler(type, FrameworkElement.SizeChangedEvent, handler);
        }
    }

    /// <summary>
    /// Names the parts of <paramref name="element"/>'s template, or keeps its glyph out of the
    /// automation tree, when it is one of the WPF-UI controls this class fixes; does nothing for
    /// anything else, or for a part already fixed. Its template must be applied (it is by layout).
    /// </summary>
    public static void Apply(object? element)
    {
        switch (element)
        {
            case NavigationView navigation:
                NameNavigationParts(navigation);
                break;
            case TitleBar titleBar:
                NamePart(titleBar, "PART_MinimizeButton", "A11y_Minimize");
                NameMaximizeButton(titleBar);
                NamePart(titleBar, "PART_CloseButton", "A11y_Close");
                break;
            case Wpf.Ui.Controls.TextBox textBox:
                NamePart(textBox, "ClearButton", "A11y_ClearText");
                break;
            case CardExpander expander:
                NameExpanderHeader(expander);
                break;
            case NavigationViewItem item:
                HideGlyph(item, "ChevronIcon");
                break;
            case CheckBox box:
                HideGlyph(box, "ControlIcon");
                break;
            case InfoBar bar:
                NameSeverityIcon(bar);
                break;
        }
    }

    private static void NameNavigationParts(NavigationView navigation)
    {
        NamePart(navigation, "PART_ToggleButton", "MenuToggleSidebar", withToolTip: true);
        NamePart(navigation, "PART_MenuItemsItemsControl", "A11y_NavigationPages");
        NamePart(navigation, "PART_FooterMenuItemsItemsControl", "A11y_NavigationFooter");

        // The frame that shows the page is a Tab stop of its own with nothing to operate: Tab from the
        // pane went to an invisible, unnamed "pane" before the page's first control.
        if (FindPart(navigation, "PART_NavigationViewContentPresenter") is { Focusable: true } presenter)
        {
            presenter.Focusable = false;
        }
    }

    private static void NamePart(Control control, string partName, string key, bool withToolTip = false)
    {
        if (FindPart(control, partName) is not { } part || part.GetValue(AutomationProperties.NameProperty) is string { Length: > 0 })
        {
            return;
        }

        part.SetBinding(AutomationProperties.NameProperty, LocalizationManager.Bind(key));
        if (withToolTip)
        {
            part.SetBinding(FrameworkElement.ToolTipProperty, LocalizationManager.Bind(key));
        }
    }

    /// <summary>
    /// WPF-UI has one button for Maximize and Restore down: it draws Restore while the window is
    /// maximized, and its name follows.
    /// </summary>
    private static void NameMaximizeButton(TitleBar titleBar)
    {
        if (FindPart(titleBar, "PART_MaximizeButton") is { } button && !BindingOperations.IsDataBound(button, AutomationProperties.NameProperty))
        {
            var name = new MultiBinding { Converter = MaximizeNameConverter.Instance };
            name.Bindings.Add(new Binding(nameof(TitleBar.IsMaximized)) { Source = titleBar });
            name.Bindings.Add(LocalizationManager.Bind("A11y_Maximize"));
            name.Bindings.Add(LocalizationManager.Bind("A11y_Restore"));
            button.SetBinding(AutomationProperties.NameProperty, name);
        }
    }

    /// <summary>The header toggle speaks the expander's own name (set in XAML), so "Logs size, expanded".</summary>
    private static void NameExpanderHeader(CardExpander expander)
    {
        if (FindPart(expander, "ExpanderToggleButton") is { } toggle && !BindingOperations.IsDataBound(toggle, AutomationProperties.NameProperty))
        {
            toggle.SetBinding(AutomationProperties.NameProperty, new Binding { Path = new PropertyPath(AutomationProperties.NameProperty), Source = expander });
        }
    }

    /// <summary>Moves a template icon's glyph into a <see cref="GlyphHost"/>, out of the automation tree.</summary>
    private static void HideGlyph(Control control, string iconPart)
    {
        if (FindPart(control, iconPart) is IconElement icon && Glyph(icon) is { Parent: Panel panel } glyph)
        {
            var index = panel.Children.IndexOf(glyph);
            panel.Children.RemoveAt(index);
            panel.Children.Insert(index, new GlyphHost { Child = glyph });
        }
    }

    /// <summary>
    /// An info bar's title and message are TextBlocks of its template, which WPF leaves out of the
    /// control view, so Narrator never read them; the severity icon's glyph is the bar's one element
    /// there. It is named with the whole bar: "Warning: These commands are not part of the vetted…".
    /// </summary>
    private static void NameSeverityIcon(InfoBar bar)
    {
        if (FindPart(bar, "SymbolIcon") is IconElement icon && Glyph(icon) is { } glyph && !BindingOperations.IsDataBound(glyph, AutomationProperties.NameProperty))
        {
            var name = new MultiBinding { Converter = InfoBarNameConverter.Instance };
            name.Bindings.Add(new Binding(nameof(InfoBar.Severity)) { Source = bar });
            name.Bindings.Add(LocalizationManager.Bind("A11y_SeverityInfo"));
            name.Bindings.Add(LocalizationManager.Bind("A11y_SeveritySuccess"));
            name.Bindings.Add(LocalizationManager.Bind("A11y_SeverityWarning"));
            name.Bindings.Add(LocalizationManager.Bind("A11y_SeverityError"));
            name.Bindings.Add(LocalizationManager.Bind("A11y_InfoBar"));
            name.Bindings.Add(new Binding(nameof(InfoBar.Title)) { Source = bar });
            name.Bindings.Add(new Binding(nameof(InfoBar.Message)) { Source = bar });
            glyph.SetBinding(AutomationProperties.NameProperty, name);
        }
    }

    /// <summary>
    /// Pure: what an info bar is read as, from its translated severity, the "{0}: {1}" pattern and its
    /// title and message (either may be empty).
    /// </summary>
    public static string InfoBarText(string severity, string pattern, string? title, string? message)
    {
        var text = string.Join(' ', new[] { title, message }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return text.Length == 0 ? severity : string.Format(CultureInfo.CurrentCulture, pattern, severity, text);
    }

    /// <summary>
    /// The TextBlock that draws an icon's glyph: WPF-UI's FontIcon puts it in a panel, its only visual
    /// child. Null when it is not there (an image icon, already moved into a GlyphHost, a WPF-UI that
    /// draws icons another way), so nothing changes.
    /// </summary>
    private static System.Windows.Controls.TextBlock? Glyph(IconElement icon)
        => VisualTreeHelper.GetChildrenCount(icon) == 1
            && VisualTreeHelper.GetChild(icon, 0) is Panel { Children: [System.Windows.Controls.TextBlock glyph] }
            ? glyph
            : null;

    private static FrameworkElement? FindPart(Control control, string partName)
    {
        control.ApplyTemplate();
        return control.Template?.FindName(partName, control) as FrameworkElement;
    }

    /// <summary>The values: whether the window is maximized; the translated Maximize and Restore down.</summary>
    private sealed class MaximizeNameConverter : IMultiValueConverter
    {
        public static readonly MaximizeNameConverter Instance = new();

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [bool maximized, string maximize, string restore])
            {
                return string.Empty;
            }

            return maximized ? restore : maximize;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// The values: the bar's <see cref="InfoBarSeverity"/>; the translated names of information,
    /// success, warning and error; the pattern; the bar's title and message.
    /// </summary>
    private sealed class InfoBarNameConverter : IMultiValueConverter
    {
        public static readonly InfoBarNameConverter Instance = new();

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [InfoBarSeverity severity, string info, string success, string warning, string error, string pattern, var title, var message])
            {
                return string.Empty;
            }

            var name = severity switch
            {
                InfoBarSeverity.Success => success,
                InfoBarSeverity.Warning => warning,
                InfoBarSeverity.Error => error,
                _ => info,
            };
            return InfoBarText(name, pattern, title as string, message as string);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
