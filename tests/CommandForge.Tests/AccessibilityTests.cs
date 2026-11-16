using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CommandForge.Tests;

/// <summary>
/// What a screen reader is given, checked in the XAML itself (ACCESSIBILITY.md, "How it is checked"):
/// a control that shows no text has a name, a list's items are named by what they show (WPF reads an
/// unnamed item as its view-model's type name), every page has a level-1 heading, splitters are
/// keyboard-reachable, no colour is written into a screen, and the app follows each screen's scaling.
/// </summary>
public sealed partial class AccessibilityTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace WpfUi = "http://schemas.lepo.co/wpfui/2022/xaml";
    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>Controls a screen reader announces by name; without text or a name it says only the type ("button").</summary>
    private static readonly HashSet<string> NamedControls = new(StringComparer.Ordinal)
    {
        "Button", "ToggleButton", "ToggleSwitch", "CheckBox", "RadioButton", "ComboBox", "TextBox", "NumberBox",
        "PasswordBox", "Slider", "HyperlinkButton", "CardAction", "DropDownButton", "GridSplitter", "ProgressBar",
    };

    /// <summary>Controls whose own text is typed by the user, never a label: they always need a name.</summary>
    private static readonly HashSet<string> NoLabelOfTheirOwn = new(StringComparer.Ordinal)
    {
        "ComboBox", "TextBox", "NumberBox", "PasswordBox", "Slider", "GridSplitter", "ProgressBar",
    };

    private static readonly HashSet<string> ColourProperties = new(StringComparer.Ordinal)
    {
        "Foreground", "Background", "BorderBrush", "Fill", "Stroke", "Color", "CaretBrush", "SelectionBrush",
    };

    private static readonly string[] Pages =
        ["HomeView", "CatalogView", "HistoryView", "RecipesView", "UserCommandsView", "SettingsView", "LogViewerView", "DebugView"];

    [Fact]
    public void EveryControlThatShowsNoTextHasAnAutomationName()
    {
        var unnamed = new List<string>();
        foreach (var (file, xaml) in AppXaml())
        {
            var namingStyles = StylesThatSetAName(xaml);
            foreach (var element in xaml.Descendants().Where(e => NamedControls.Contains(e.Name.LocalName)))
            {
                var needsName = NoLabelOfTheirOwn.Contains(element.Name.LocalName) || !HasVisibleText(element);
                if (needsName && !HasAutomationName(element, namingStyles) && !IsDecorative(element))
                {
                    unnamed.Add($"{file}:{Line(element)} <{element.Name.LocalName}>");
                }
            }
        }

        Assert.True(unnamed.Count == 0, "Controls with no text and no AutomationProperties.Name (from .resx):\n" + string.Join('\n', unnamed));
    }

    [Fact]
    public void ListsNameTheirItemsByWhatTheyShow()
    {
        var unnamed = new List<string>();
        foreach (var (file, xaml) in AppXaml())
        {
            var namingStyles = StylesThatSetAName(xaml);
            foreach (var list in xaml.Descendants().Where(e => e.Name.LocalName is "ListBox" or "ComboBox" or "ItemsControl"))
            {
                // A list of strings is read by its strings; one that shows a property or a template is
                // read by its items' type name unless its item containers are named.
                var showsObjects = list.Attribute("DisplayMemberPath") is not null
                    || list.Attribute("ItemTemplate") is not null
                    || list.Elements().Any(child => child.Name.LocalName.EndsWith(".ItemTemplate", StringComparison.Ordinal));
                if (showsObjects && !NamesItemContainers(list, namingStyles))
                {
                    unnamed.Add($"{file}:{Line(list)} <{list.Name.LocalName}>");
                }
            }
        }

        Assert.True(unnamed.Count == 0, "Lists whose items have no AutomationProperties.Name in their ItemContainerStyle:\n" + string.Join('\n', unnamed));
    }

    [Fact]
    public void EveryPageHasALevelOneHeading()
    {
        var views = Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf", "Views");
        foreach (var page in Pages)
        {
            var xaml = File.ReadAllText(Path.Combine(views, page + ".xaml"));
            Assert.Contains("Style=\"{StaticResource PageTitleTextStyle}\"", xaml, StringComparison.Ordinal);
        }

        var styles = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf", "Theming", "AppStyles.xaml"));
        Assert.Matches(PageTitleHeading(), styles);
    }

    [Fact]
    public void SplittersCanBeReachedWithTheKeyboard()
    {
        var unreachable = AppXaml()
            .SelectMany(doc => doc.Xaml.Descendants()
                .Where(e => e.Name.LocalName == "GridSplitter" && (string?)e.Attribute("Focusable") == "False")
                .Select(e => $"{doc.File}:{Line(e)}"))
            .ToList();

        Assert.True(unreachable.Count == 0, "Splitters the keyboard can't reach:\n" + string.Join('\n', unreachable));
    }

    [Fact]
    public void NoXamlHardCodesAColour()
    {
        var literal = new List<string>();
        foreach (var (file, xaml) in AppXaml())
        {
            foreach (var element in xaml.Descendants())
            {
                foreach (var attribute in element.Attributes().Where(a => ColourProperties.Contains(a.Name.LocalName)))
                {
                    var value = attribute.Value;
                    if (!value.StartsWith('{') && value != "Transparent")
                    {
                        literal.Add($"{file}:{Line(element)} {attribute.Name.LocalName}=\"{value}\"");
                    }
                }

                // <Setter Property="Foreground" Value="#..."/> in a style.
                if (element.Name.LocalName == "Setter"
                    && ColourProperties.Contains((string?)element.Attribute("Property") ?? string.Empty)
                    && element.Attribute("Value") is { } setterValue
                    && !setterValue.Value.StartsWith('{')
                    && setterValue.Value != "Transparent")
                {
                    literal.Add($"{file}:{Line(element)} Setter {element.Attribute("Property")!.Value}=\"{setterValue.Value}\"");
                }
            }
        }

        Assert.True(literal.Count == 0, "Colours must come from the theme (they would break Windows contrast themes):\n" + string.Join('\n', literal));
    }

    /// <summary>
    /// WPF-UI draws a font icon's glyph (a private-use character) with a plain TextBlock, which UI
    /// Automation lists as text; DecorativeIcon keeps it out of the tree, so the app uses no other.
    /// </summary>
    [Fact]
    public void IconsStayOutOfTheAccessibilityTree()
    {
        var plain = AppXaml()
            .SelectMany(doc => doc.Xaml.Descendants()
                .Where(e => e.Name.Namespace == WpfUi && e.Name.LocalName is "SymbolIcon" or "FontIcon")
                .Select(e => $"{doc.File}:{Line(e)} <ui:{e.Name.LocalName}>"))
            .ToList();
        var app = Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf");
        plain.AddRange(Directory.EnumerateFiles(app, "*.cs", SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(file) != "DecorativeIcon.cs" && !IsBuildOutput(app, file))
            .Where(file => PlainIcon().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(app, file)));

        Assert.True(plain.Count == 0, "Use ctl:DecorativeIcon (or new DecorativeIcon) instead of WPF-UI's SymbolIcon/FontIcon:\n" + string.Join('\n', plain));
    }

    [Fact]
    public void TheAppFollowsEachScreensScaling()
    {
        var manifest = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf", "app.manifest"));
        Assert.Contains(">PerMonitorV2</dpiAwareness>", manifest, StringComparison.Ordinal);
    }

    private static IEnumerable<(string File, XDocument Xaml)> AppXaml()
    {
        var app = Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf");
        foreach (var file in Directory.EnumerateFiles(app, "*.xaml", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(app, file))
            {
                continue;
            }

            yield return (Path.GetRelativePath(app, file), XDocument.Load(file, LoadOptions.SetLineInfo));
        }
    }

    private static bool IsBuildOutput(string app, string file)
        => Path.GetRelativePath(app, file).Split(Separators).Any(part => part is "bin" or "obj");

    private static bool HasVisibleText(XElement element)
    {
        if (element.Attribute("Content") is not null || element.Attribute("Header") is not null)
        {
            return true;
        }

        // Content as a child element, other than a property element such as <ui:Button.Icon> — and an
        // icon alone is not text.
        return element.Elements()
            .Where(child => !child.Name.LocalName.Contains('.', StringComparison.Ordinal))
            .Any(child => child.Name.LocalName != "SymbolIcon" && child.DescendantsAndSelf().Any(d => d.Name.LocalName == "TextBlock"));
    }

    private static bool HasAutomationName(XElement element, HashSet<string> namingStyles)
    {
        if (element.Attributes().Any(a => a.Name.LocalName is "AutomationProperties.Name" or "AutomationProperties.LabeledBy")
            || element.Elements().Any(child => child.Name.LocalName == "AutomationProperties.Name"))
        {
            return true;
        }

        return StaticResourceKey((string?)element.Attribute("Style")) is { } key && namingStyles.Contains(key);
    }

    // A control that is neither focusable nor shown to the user as something to operate (the locked,
    // display-only switch has its card's name).
    private static bool IsDecorative(XElement element)
        => (string?)element.Attribute("Focusable") == "False" && (string?)element.Attribute("IsHitTestVisible") == "False";

    private static bool NamesItemContainers(XElement list, HashSet<string> namingStyles)
    {
        if (StaticResourceKey((string?)list.Attribute("ItemContainerStyle")) is { } key && namingStyles.Contains(key))
        {
            return true;
        }

        return list.Elements()
            .Where(child => child.Name.LocalName.EndsWith(".ItemContainerStyle", StringComparison.Ordinal))
            .SelectMany(child => child.Descendants())
            .Any(IsNameSetter);
    }

    private static HashSet<string> StylesThatSetAName(XDocument xaml)
        => xaml.Descendants()
            .Where(e => e.Name.LocalName == "Style" && e.Attribute(X + "Key") is not null && e.Elements().Any(IsNameSetter))
            .Select(e => e.Attribute(X + "Key")!.Value)
            .ToHashSet(StringComparer.Ordinal);

    private static bool IsNameSetter(XElement element)
        => element.Name.LocalName == "Setter" && (string?)element.Attribute("Property") == "AutomationProperties.Name";

    private static string? StaticResourceKey(string? value)
    {
        var match = value is null ? Match.Empty : StaticResource().Match(value);
        return match.Success ? match.Groups["key"].Value : null;
    }

    private static int Line(XObject node) => node is System.Xml.IXmlLineInfo info ? info.LineNumber : 0;

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CommandForge.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (CommandForge.slnx) above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"^\{StaticResource (?<key>[A-Za-z0-9_]+)\}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex StaticResource();

    [GeneratedRegex(@"new (SymbolIcon|FontIcon)\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex PlainIcon();

    [GeneratedRegex(@"x:Key=""PageTitleTextStyle""[^<]*>(?:\s*<Setter[^>]*>)*?\s*<Setter Property=""AutomationProperties\.HeadingLevel"" Value=""Level1"" />", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex PageTitleHeading();
}
