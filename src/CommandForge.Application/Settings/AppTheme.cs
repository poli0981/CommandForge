namespace CommandForge.Application.Settings;

/// <summary>The UI color theme. <see cref="System"/> follows the OS light/dark setting.</summary>
public enum AppTheme
{
    /// <summary>Follow the Windows light/dark setting (and react to changes).</summary>
    System,

    /// <summary>Always light.</summary>
    Light,

    /// <summary>Always dark.</summary>
    Dark,
}
