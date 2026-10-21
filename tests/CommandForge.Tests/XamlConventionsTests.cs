using System.Text.RegularExpressions;

namespace CommandForge.Tests;

/// <summary>
/// Conventions the XAML must follow that the compiler can't check. Pages are shown inside the
/// navigation view's Frame, which does not pass the window's text color down, so a page that
/// doesn't set its own Foreground draws black text on the dark theme.
/// </summary>
public sealed partial class XamlConventionsTests
{
    private static readonly string[] Pages =
        ["HomeView", "CatalogView", "HistoryView", "RecipesView", "UserCommandsView", "SettingsView", "LogViewerView", "DebugView"];

    [Fact]
    public void EveryPage_SetsTheThemeTextColorOnItsRoot()
    {
        var views = Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf", "Views");
        foreach (var page in Pages)
        {
            var xaml = File.ReadAllText(Path.Combine(views, page + ".xaml"));
            var root = RootTag().Match(xaml);

            Assert.True(root.Success, $"{page}.xaml has no <UserControl> root.");
            Assert.Contains("Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"", root.Value, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ContentDialogSubclasses_TakeTheContentDialogStyle()
    {
        // Implicit styles match the exact type, so a subclass would render without the dialog frame.
        var views = Path.Combine(FindRepositoryRoot(), "src", "CommandForge.Wpf", "Views");
        foreach (var xamlPath in Directory.EnumerateFiles(views, "*.xaml"))
        {
            if (!File.ReadAllText(xamlPath).TrimStart().StartsWith("<ui:ContentDialog", StringComparison.Ordinal))
            {
                continue;
            }

            var codeBehind = File.ReadAllText(xamlPath + ".cs");
            Assert.Contains("SetResourceReference(StyleProperty, typeof(ContentDialog))", codeBehind, StringComparison.Ordinal);
        }
    }

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

    [GeneratedRegex(@"<UserControl\b[^>]*>", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex RootTag();
}
