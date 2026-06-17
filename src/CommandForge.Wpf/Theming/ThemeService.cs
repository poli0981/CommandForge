using CommandForge.Application.Settings;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace CommandForge.Wpf.Theming;

/// <summary>
/// <see cref="IThemeService"/> backed by MaterialDesign's <see cref="PaletteHelper"/>. Swaps the base
/// theme at runtime (all brushes are DynamicResource, so the UI recolors live). In System mode it
/// reads the OS light/dark setting and tracks changes via <see cref="SystemEvents"/>.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private AppTheme _current = AppTheme.System;
    private bool _subscribed;

    /// <inheritdoc />
    public void Apply(AppTheme theme)
    {
        _current = theme;

        var baseTheme = theme switch
        {
            AppTheme.Light => BaseTheme.Light,
            AppTheme.Dark => BaseTheme.Dark,
            _ => DetectOsBaseTheme(),
        };

        var palette = new PaletteHelper();
        var current = palette.GetTheme();
        current.SetBaseTheme(baseTheme);
        palette.SetTheme(current);

        if (theme == AppTheme.System)
        {
            Subscribe();
        }
        else
        {
            Unsubscribe();
        }
    }

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _subscribed = false;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && _current == AppTheme.System)
        {
            // SystemEvents fires off the UI thread; marshal the theme swap onto it.
            System.Windows.Application.Current?.Dispatcher.Invoke(() => Apply(AppTheme.System));
        }
    }

    private static BaseTheme DetectOsBaseTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0
            ? BaseTheme.Dark
            : BaseTheme.Light;
    }
}
