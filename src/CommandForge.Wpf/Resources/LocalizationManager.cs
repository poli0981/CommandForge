using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace CommandForge.Wpf.Resources;

/// <summary>
/// Live-localization source for XAML bindings. <see cref="LocExtension"/> binds to this singleton's
/// indexer, so changing the culture via <see cref="SetCulture"/> updates every bound string with no
/// restart. Strings built in C# should subscribe to <see cref="CultureChanged"/> and re-resolve.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    /// <summary>The shared instance that XAML binds to.</summary>
    public static LocalizationManager Instance { get; } = new();

    private LocalizationManager()
    {
    }

    /// <summary>Resolves <paramref name="key"/> for the current UI culture.</summary>
    public string this[string key] => Strings.Get(key);

    /// <summary>The active UI culture.</summary>
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    /// The OS UI culture captured at first access (before any <see cref="SetCulture"/> override).
    /// Use this for the "follow OS" option — <see cref="CultureInfo.CurrentUICulture"/> can't be used
    /// because <see cref="SetCulture"/> overwrites it process-wide.
    /// </summary>
    public CultureInfo SystemCulture { get; } = CultureInfo.CurrentUICulture;

    /// <summary>Raised after the culture changes (for strings built in C#, which bindings can't refresh).</summary>
    public event EventHandler? CultureChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Switches the UI language and notifies all bindings/listeners. No-op if unchanged. Only the UI
    /// culture changes: number and date formats keep following the user's regional settings, so an
    /// English UI on a German machine still shows German dates.
    /// </summary>
    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (string.Equals(culture.Name, CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        ApplyLanguageToOpenWindows();

        // "Item[]" is WPF's signal to re-evaluate every indexer binding on this source.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Tags every window (current and future) with the UI language (xml:lang). WPF uses it to pick
    /// glyphs for Han characters, so Japanese text isn't drawn with Chinese glyph shapes.
    /// </summary>
    public void TrackWindowLanguage()
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is Window window)
                {
                    window.Language = XmlLanguage.GetLanguage(CurrentCulture.IetfLanguageTag);
                }
            }));
        ApplyLanguageToOpenWindows();
    }

    private void ApplyLanguageToOpenWindows()
    {
        if (System.Windows.Application.Current is not { } app)
        {
            return;
        }

        var language = XmlLanguage.GetLanguage(CurrentCulture.IetfLanguageTag);
        foreach (Window window in app.Windows)
        {
            window.Language = language;
        }
    }
}
