using System.Globalization;
using System.Windows;
using CommandForge.Application.Logging;
using CommandForge.Application.Settings;
using CommandForge.Desktop.Behaviors;
using CommandForge.Desktop.Converters;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.Theming;
using CommandForge.Desktop.ViewModels;
using CommandForge.Desktop.Views;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>
/// The pure parts of what screen readers hear and how the keyboard and text size behave. Expected
/// texts are built from the same resources, so the tests hold in any UI language.
/// </summary>
public sealed class AccessibilityHelpersTests
{
    [Theory]
    [InlineData(-1, 3, false, 0)] // F6 from outside every region: the first
    [InlineData(-1, 3, true, 2)] // Shift+F6 from outside: the last
    [InlineData(0, 3, false, 1)]
    [InlineData(2, 3, false, 0)] // wraps forward
    [InlineData(0, 3, true, 2)] // wraps backward
    [InlineData(0, 1, false, 0)] // a single region stays put
    public void F6CyclesThroughTheRegions(int current, int count, bool backwards, int expected)
        => Assert.Equal(expected, FocusCycle.Next(current, count, backwards));

    [Theory]
    [InlineData(100, 1.0)]
    [InlineData(150, 1.5)]
    [InlineData(225, 2.25)]
    [InlineData(400, 2.25)] // never beyond what Windows offers
    [InlineData(50, 1.0)]
    public void WindowsTextSizeIsReadAsAClampedFactor(int percent, double factor)
        => Assert.Equal(factor, WindowsTextScale.FromRegistryValue(percent), 3);

    [Fact]
    public void AMissingOrOddTextSizeMeansNoEnlargement()
    {
        Assert.Equal(1.0, WindowsTextScale.FromRegistryValue(null));
        Assert.Equal(1.0, WindowsTextScale.FromRegistryValue("150"));
        Assert.Equal(1.0, WindowsTextScale.Clamp(double.NaN));
    }

    [Theory]
    [InlineData(FontScale.Small, 1.0, 12)]
    [InlineData(FontScale.Medium, 1.0, 14)]
    [InlineData(FontScale.Large, 1.0, 16)]
    [InlineData(FontScale.Medium, 1.5, 21)]
    [InlineData(FontScale.Large, 2.25, 36)]
    public void TheFontSizeIsTheChoiceTimesWindowsTextSize(FontScale scale, double textScale, double body)
        => Assert.Equal(body, FontScaleService.BodySize(scale, textScale));

    [Fact]
    public void ASnackbarNamingAPathStaysLongerToBeRead()
    {
        Assert.True(SnackbarNotificationService.DurationFor(@"Saved to C:\Users\me\output.txt") > SnackbarNotificationService.DurationFor("Copied"));
        Assert.True(SnackbarNotificationService.DurationFor(@"Saved to \\server\share\a.txt") > SnackbarNotificationService.DurationFor("Copied"));
    }

    [Fact]
    public void ACommandsStatusSaysWhatTheDotAndBadgesShow()
    {
        var status = CommandItemViewModel.StatusText(DangerLevel.Caution, requiresAdmin: true, requiresRestart: true, revertable: false, favorite: true);

        Assert.StartsWith(Strings.Get("Danger_Caution"), status, StringComparison.Ordinal);
        Assert.Contains(Strings.Get("Detail_RequiresAdmin"), status, StringComparison.Ordinal);
        Assert.Contains(Strings.Get("Detail_MayRestart"), status, StringComparison.Ordinal);
        Assert.Contains(Strings.Get("A11y_Favorite"), status, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Get("A11y_CanUndo"), status, StringComparison.Ordinal);
        Assert.Equal(Strings.Get("Danger_Safe"), CommandItemViewModel.StatusText(DangerLevel.Safe, false, false, false, false));
    }

    [Fact]
    public void ARepeatedRowButtonSaysItsLabelFirstThenItsRow()
    {
        var name = RowActionNameConverter.Format("Undo", "Flush DNS cache");

        Assert.StartsWith("Undo", name, StringComparison.Ordinal); // the visible label leads (WCAG 2.5.3)
        Assert.Contains("Flush DNS cache", name, StringComparison.Ordinal);
        Assert.Equal("Undo", RowActionNameConverter.Format("Undo", string.Empty));
    }

    [Fact]
    public void ARunsResultIsReadWithItsStatusCodeTimeAndRestart()
    {
        var plain = ExecutionViewModel.ResultAnnouncement("Flush DNS cache", "OK", 0, "0.4 s", requiresRestart: false);
        var restart = ExecutionViewModel.ResultAnnouncement("Reset Windows Update", "OK", 3010, "12 s", requiresRestart: true);

        Assert.Contains("Flush DNS cache", plain, StringComparison.Ordinal);
        Assert.Contains("0.4 s", plain, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Get("A11y_RestartRequired"), plain, StringComparison.Ordinal);
        Assert.Contains(3010.ToString(CultureInfo.CurrentCulture), restart, StringComparison.Ordinal);
        Assert.EndsWith(Strings.Get("A11y_RestartRequired"), restart, StringComparison.Ordinal);
    }

    [Fact]
    public void ASearchSaysHowManyCommandsItFound()
    {
        Assert.Equal(Strings.Get("NoSearchResults"), CatalogView.ResultCountText(0));
        Assert.Equal(Strings.Get("A11y_ResultCountOne"), CatalogView.ResultCountText(1));
        Assert.Contains(14.ToString(CultureInfo.CurrentCulture), CatalogView.ResultCountText(14), StringComparison.Ordinal);
    }

    [Fact]
    public void ThePalettesHighlightIsReadWithItsPosition()
    {
        var text = CommandPaletteWindow.HighlightText("Flush DNS cache", "Safe", 2, 14);

        Assert.StartsWith("Flush DNS cache", text, StringComparison.Ordinal);
        Assert.Contains("Safe", text, StringComparison.Ordinal);
        Assert.Contains(2.ToString(CultureInfo.CurrentCulture), text, StringComparison.Ordinal);
        Assert.Contains(14.ToString(CultureInfo.CurrentCulture), text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(10, 25, true)]
    [InlineData(25, 49, false)]
    [InlineData(49, 50, true)]
    [InlineData(75, 100, true)]
    public void ADownloadIsReadEveryQuarter(int previous, int current, bool announce)
        => Assert.Equal(announce, UpdateDialog.CrossesQuarter(previous, current));

    [Fact]
    public void LogLevelsAreShownByTheirTranslatedNames()
    {
        Assert.Equal(Strings.Get("LogLevel_Info"), LogLevelToTextConverter.Text(LogLevel.Information));
        Assert.Equal(Strings.Get("LogLevel_Debug"), LogLevelToTextConverter.Text(LogLevel.Debug));
        Assert.Equal(Strings.Get("LogLevel_Verbose"), LogLevelToTextConverter.Text(LogLevel.Verbose));
    }

    [Fact]
    public void TheFormatConverterFillsALocalizedPattern()
    {
        var converter = new FormatConverter();

        Assert.Equal("Move Flush DNS up", converter.Convert(["Move {0} up", "Flush DNS"], typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal("Move  up", converter.Convert(["Move {0} up", DependencyProperty.UnsetValue], typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal(string.Empty, converter.Convert([DependencyProperty.UnsetValue], typeof(string), null, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData(null, true)]
    [InlineData("TotalSeconds : 13157", false)]
    public void ABlankConsoleLineIsNamedAsOne(string? text, bool blank)
        => Assert.Equal(blank, new BlankToBooleanConverter().Convert(text, typeof(bool), null, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(12, 290)]
    [InlineData(14, 290)]
    [InlineData(21, 290)]
    [InlineData(31.5, 435)]
    [InlineData(36, 497)]
    public void TheNavigationPaneWidensOnlyForVeryLargeText(double body, double width)
        => Assert.Equal(width, FontScaleService.NavigationPaneWidth(body));

    [Fact]
    public void AnInfoBarIsReadWithItsSeverityTitleAndMessage()
    {
        Assert.Equal("Warning: Not vetted. Runs as you.", WpfUiAccessibility.InfoBarText("Warning", "{0}: {1}", "Not vetted.", "Runs as you."));
        Assert.Equal("Warning: Not vetted.", WpfUiAccessibility.InfoBarText("Warning", "{0}: {1}", "Not vetted.", string.Empty));
        Assert.Equal("Information", WpfUiAccessibility.InfoBarText("Information", "{0}: {1}", null, " "));
    }

    [Fact]
    public void TheFontSizesFollowTheChoiceAndWindowsTextSize()
    {
        var resources = new ResourceDictionary();
        var textScale = 1d;
        using var fonts = new FontScaleService(() => textScale, () => resources);

        fonts.Apply(FontScale.Large);
        Assert.Equal(16d, resources[FontScaleService.ResourceKey]);
        Assert.Equal(16d, resources["ControlContentThemeFontSize"]);
        Assert.Equal(14d, resources["AppFontSizeCaption"]);
        Assert.Equal(32d, resources["AppFontSizeTitle"]);
        Assert.Equal(290d, resources[FontScaleService.PaneWidthKey]);

        // Windows' Text size changes while the app runs: re-read when the app is back in front.
        textScale = 2.25;
        fonts.Refresh();
        Assert.Equal(36d, resources[FontScaleService.ResourceKey]);
        Assert.Equal(497d, resources[FontScaleService.PaneWidthKey]);

        // Unchanged: nothing is rewritten.
        resources.Clear();
        fonts.Refresh();
        Assert.False(resources.Contains(FontScaleService.ResourceKey));
    }

    [Fact]
    public void WithoutResourcesTheFontSizesAreLeftAlone()
    {
        using var fonts = new FontScaleService(() => 1.5, () => null);
        using var defaults = new FontScaleService(); // the app's resources: none in a test

        fonts.Apply(FontScale.Small);
        fonts.Refresh();
        defaults.Apply(FontScale.Medium);
        defaults.Refresh();

        Assert.Throws<ArgumentNullException>(() => new FontScaleService(null!, () => null));
        Assert.Throws<ArgumentNullException>(() => new FontScaleService(() => 1, null!));
    }

    [Fact]
    public void WindowsTextSizeIsReadAsAFactorTheAppSupports()
        => Assert.InRange(WindowsTextScale.Read(), WindowsTextScale.Minimum, 2.25);

    [Fact]
    public void AContrastThemeDrawsDimmedTextAtFullStrength()
    {
        var resources = new ResourceDictionary();

        ThemeService.ApplyDimming(resources, highContrast: true);
        Assert.Equal(1d, resources["AppDimOpacity"]);
        Assert.Equal(1d, resources["AppBadgeOpacity"]);

        ThemeService.ApplyDimming(resources, highContrast: false);
        Assert.Equal(0.78d, resources["AppDimOpacity"]);
        Assert.Equal(0.75d, resources["AppBadgeOpacity"]);
        Assert.Throws<ArgumentNullException>(() => ThemeService.ApplyDimming(null!, highContrast: true));
    }

    [Fact]
    public void ALocalizedBindingFollowsTheLanguage()
    {
        var binding = LocalizationManager.Bind("A11y_Close");

        Assert.Equal("[A11y_Close]", binding.Path.Path);
        Assert.Same(LocalizationManager.Instance, binding.Source);
        Assert.Equal(System.Windows.Data.BindingMode.OneWay, binding.Mode);
    }

    [Fact]
    public void ACommandRowsStatusIsRaisedWhenItChanges()
    {
        var command = new CommandDefinition
        {
            Id = "t.tweak",
            CategoryId = "c",
            TitleKey = "A11y_Close",
            DescriptionKey = "A11y_Close",
            Executable = "x",
            DangerLevel = DangerLevel.Caution,
            RequiresAdmin = true,
            RevertCommandId = "t.tweak.undo",
        };
        var row = new CommandItemViewModel(command, "Tweak", "A tweak.");
        var changed = new List<string?>();
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Assert.Equal(CommandItemViewModel.StatusText(DangerLevel.Caution, true, false, true, false), row.AccessibleStatus);
        Assert.Contains(Strings.Get("A11y_CanUndo"), row.AccessibleStatus, StringComparison.Ordinal);

        row.IsFavorite = true;
        Assert.Contains(nameof(CommandItemViewModel.AccessibleStatus), changed);
        Assert.Contains(Strings.Get("A11y_Favorite"), row.AccessibleStatus, StringComparison.Ordinal);

        changed.Clear();
        row.Refresh();
        Assert.Contains(nameof(CommandItemViewModel.AccessibleStatus), changed);
    }

    [Fact]
    public void ANotificationIsShownAndReadOrOnlyRead()
    {
        // No application in a test: nothing is shown or said, and nothing fails.
        var notifications = new SnackbarNotificationService(new Wpf.Ui.SnackbarService());

        notifications.Show("Saved to C:\\Temp\\output.txt", NotificationKind.Success);
        notifications.Show("Cancelled", NotificationKind.Warning, announce: false);
        notifications.Announce("System uptime: Success", important: true);

        Assert.Throws<ArgumentNullException>(() => notifications.Announce(null!));
    }

    [Fact]
    public void TheConvertersAreOneWayAndTheirOwnMarkup()
    {
        var format = new FormatConverter();
        var level = new LogLevelToTextConverter();
        var blank = new BlankToBooleanConverter();
        var rowAction = new RowActionNameConverter { LabelKey = "Favorite_Add" };

        Assert.Same(format, format.ProvideValue(null!));
        Assert.Same(level, level.ProvideValue(null!));
        Assert.Same(blank, blank.ProvideValue(null!));
        Assert.Throws<NotSupportedException>(() => format.ConvertBack("x", [typeof(string)], null, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() => level.ConvertBack("x", typeof(LogLevel), null, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() => blank.ConvertBack(true, typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() => rowAction.ConvertBack("x", typeof(string), null, CultureInfo.InvariantCulture));

        Assert.Equal(LogLevelToTextConverter.Text(LogLevel.Warning), level.Convert(LogLevel.Warning, typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal(Strings.Get("LogLevel_Error"), LogLevelToTextConverter.Text(LogLevel.Fatal));
        Assert.Equal(string.Empty, level.Convert("Warning", typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal(
            RowActionNameConverter.Format(Strings.Get("Favorite_Add"), "Flush DNS cache"),
            rowAction.Convert("Flush DNS cache", typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal(Strings.Get("Favorite_Add"), rowAction.Convert(null, typeof(string), null, CultureInfo.InvariantCulture));
    }
}
