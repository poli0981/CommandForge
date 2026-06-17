using CommandForge.Application.Settings;

namespace CommandForge.Wpf.Theming;

/// <summary>Applies the UI color theme (Light/Dark/System) at runtime, with no restart.</summary>
public interface IThemeService
{
    /// <summary>Applies <paramref name="theme"/>; for <see cref="AppTheme.System"/>, follows and tracks the OS setting.</summary>
    public void Apply(AppTheme theme);
}
