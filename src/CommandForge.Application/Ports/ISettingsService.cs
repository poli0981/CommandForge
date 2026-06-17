using CommandForge.Application.Settings;

namespace CommandForge.Application.Ports;

/// <summary>
/// Reads and persists local user settings (config.json). No data leaves the machine.
/// </summary>
public interface ISettingsService
{
    /// <summary>The terms version the user last accepted, or <see langword="null"/> if none.</summary>
    public string? AcceptedTermsVersion { get; set; }

    /// <summary>Whether to check GitHub Releases for updates on startup (default <see langword="true"/>). Local-only.</summary>
    public bool AutoCheckForUpdates { get; set; }

    /// <summary>The UI color theme (default <see cref="AppTheme.System"/>).</summary>
    public AppTheme Theme { get; set; }

    /// <summary>UI language: <c>""</c> = follow OS, otherwise a culture code such as <c>"en"</c> or <c>"vi"</c>.</summary>
    public string Language { get; set; }

    /// <summary>The overall UI font size (default <see cref="FontScale.Medium"/>).</summary>
    public FontScale FontSize { get; set; }

    /// <summary>Whether the sidebar starts collapsed (default <see langword="false"/>).</summary>
    public bool CollapseSidebarByDefault { get; set; }

    /// <summary>Whether the Admin/Restart badges show in the command list (default <see langword="true"/>).</summary>
    public bool ShowAdminRestartBadges { get; set; }

    /// <summary>Whether to confirm before running a <c>Caution</c> command (default <see langword="true"/>).</summary>
    /// <remarks>Confirming a <c>Dangerous</c> command is a fixed safety floor and is intentionally not configurable.</remarks>
    public bool ConfirmCaution { get; set; }

    /// <summary>Whether to offer creating a System Restore Point before Caution/Dangerous commands (default <see langword="true"/>).</summary>
    public bool AutoCreateRestorePoint { get; set; }

    /// <summary>Whether the console auto-scrolls to the latest output (default <see langword="true"/>).</summary>
    public bool AutoScrollConsole { get; set; }

    /// <summary>Whether to warn before closing/cancelling while a command is running (default <see langword="true"/>).</summary>
    public bool WarnOnCancel { get; set; }

    /// <summary>Persists the current settings to disk.</summary>
    public void Save();
}
