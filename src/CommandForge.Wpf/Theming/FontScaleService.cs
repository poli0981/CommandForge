using CommandForge.Application.Settings;
using Microsoft.Win32;

namespace CommandForge.Desktop.Theming;

/// <summary>
/// <see cref="IFontScaleService"/> that rewrites the app-level font size resources. Text binds to
/// them as DynamicResource (<c>AppFontSize</c> and its caption/subtitle/title/console companions),
/// and WPF-UI controls read <c>ControlContentThemeFontSize</c>, so the whole UI rescales live.
/// The user's Small / Medium / Large choice is multiplied by Windows' own Text size setting
/// (<see cref="WindowsTextScale"/>), re-read when Windows reports a preference change or the app
/// comes to the foreground.
/// </summary>
public sealed class FontScaleService : IFontScaleService, IDisposable
{
    /// <summary>The <see cref="System.Windows.Application.Resources"/> key holding the body font size (a <see cref="double"/>).</summary>
    public const string ResourceKey = "AppFontSize";

    /// <summary>The resource key of the navigation pane's open width (a <see cref="double"/>).</summary>
    public const string PaneWidthKey = "AppNavigationPaneWidth";

    private const double PaneWidth = 290;

    private FontScale _scale = FontScale.Medium;
    private double _textScale = WindowsTextScale.Minimum;
    private bool _subscribed;

    /// <inheritdoc />
    public void Apply(FontScale scale)
    {
        _scale = scale;
        _textScale = WindowsTextScale.Read();
        Subscribe();
        ApplyResources();
    }

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();

    /// <summary>Pure: the body font size for a choice and Windows' text size factor.</summary>
    public static double BodySize(FontScale scale, double textScale)
    {
        double baseSize = scale switch
        {
            FontScale.Small => 12d,
            FontScale.Large => 16d,
            _ => 14d,
        };

        return Math.Round(baseSize * WindowsTextScale.Clamp(textScale), 1);
    }

    /// <summary>
    /// Pure: the navigation pane's open width for a body font size. Its labels fit the usual width up
    /// to one and a half times the Medium size; beyond that the pane widens with the text, so "Command
    /// catalog" and the category names are not cut off at the largest Windows text sizes.
    /// </summary>
    public static double NavigationPaneWidth(double bodySize)
        => Math.Round(PaneWidth * Math.Max(1d, bodySize / 14d * 2d / 3d));

    private void ApplyResources()
    {
        if (System.Windows.Application.Current is not { } app)
        {
            return;
        }

        var body = BodySize(_scale, _textScale);
        var resources = app.Resources;
        resources[ResourceKey] = body;
        resources["ControlContentThemeFontSize"] = body;
        resources["AppFontSizeCaption"] = body - 2;
        resources["AppFontSizeSubtitle"] = Math.Round(body * 10 / 7);
        resources["AppFontSizeTitle"] = body * 2;
        resources["AppFontSizeConsole"] = body - 1.5;
        resources[PaneWidthKey] = NavigationPaneWidth(body);
    }

    /// <summary>Re-reads Windows' text size and rescales only when it changed.</summary>
    private void Refresh()
    {
        var factor = WindowsTextScale.Read();
        if (Math.Abs(factor - _textScale) < 0.001)
        {
            return;
        }

        _textScale = factor;
        ApplyResources();
    }

    private void Subscribe()
    {
        if (_subscribed || System.Windows.Application.Current is not { } app)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        app.Activated += OnAppActivated;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        if (System.Windows.Application.Current is { } app)
        {
            app.Activated -= OnAppActivated;
        }

        _subscribed = false;
    }

    // SystemEvents fires off the UI thread; the resources live on it.
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        => System.Windows.Application.Current?.Dispatcher.InvokeAsync(Refresh);

    private void OnAppActivated(object? sender, EventArgs e) => Refresh();
}
