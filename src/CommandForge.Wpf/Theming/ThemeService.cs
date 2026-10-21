using System.Windows;
using CommandForge.Application.Settings;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Theming;

/// <summary>
/// <see cref="IThemeService"/> backed by WPF-UI's <see cref="ApplicationThemeManager"/>: swaps the
/// Fluent theme dictionaries at runtime (all brushes are DynamicResource, so the UI recolors live)
/// and uses the system accent color. System mode follows the OS light/dark setting, and Windows
/// High Contrast always wins over the user's choice.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private AppTheme _current = AppTheme.System;
    private bool _subscribed;

    /// <inheritdoc />
    public void Apply(AppTheme theme)
    {
        _current = theme;

        if (theme == AppTheme.System || SystemParameters.HighContrast)
        {
            ApplicationThemeManager.ApplySystemTheme(updateAccent: true);
        }
        else
        {
            ApplicationThemeManager.Apply(
                theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
                WindowBackdropType.Mica,
                updateAccent: true);
        }

        // Keep listening in every mode: High Contrast can be switched on while the app runs.
        Subscribe();
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
        // General: light/dark switch; Color: accent; Accessibility: High Contrast on/off.
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility)
        {
            // SystemEvents fires off the UI thread; marshal the theme swap onto it.
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() => Apply(_current));
        }
    }
}
