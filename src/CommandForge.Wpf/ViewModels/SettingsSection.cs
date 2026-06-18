namespace CommandForge.Wpf.ViewModels;

/// <summary>A section of the Settings screen (left nav). Logs/Advanced come in Phase 5b.</summary>
public enum SettingsSection
{
    /// <summary>Theme, language, font size, sidebar/badge preferences.</summary>
    Appearance,

    /// <summary>Confirmation, restore point, console behavior.</summary>
    Behavior,

    /// <summary>Update preferences and manual check.</summary>
    Updates,

    /// <summary>Log level, size, open/export/clear.</summary>
    Logs,

    /// <summary>Run mode, config path, debug panel, reset.</summary>
    Advanced,

    /// <summary>Version, license, links.</summary>
    About,
}
