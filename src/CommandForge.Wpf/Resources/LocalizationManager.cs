using System.ComponentModel;
using System.Globalization;

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

    /// <summary>Switches the UI culture and notifies all bindings/listeners. No-op if unchanged.</summary>
    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (string.Equals(culture.Name, CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;

        // "Item[]" is WPF's signal to re-evaluate every indexer binding on this source.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }
}
