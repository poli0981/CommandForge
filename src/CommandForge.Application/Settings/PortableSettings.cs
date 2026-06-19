using CommandForge.Application.Logging;

namespace CommandForge.Application.Settings;

/// <summary>
/// The machine-portable subset of settings — appearance, behavior/safety preferences and favorites —
/// used for import/export and named profiles. Excludes machine-specific state (window placement,
/// accepted terms version, recent commands, execution history).
/// </summary>
public sealed record PortableSettings
{
    /// <summary>Schema version of the portable payload (for forward-compat).</summary>
    public int Version { get; init; } = 1;

    public AppTheme Theme { get; init; } = AppTheme.System;

    public string Language { get; init; } = "";

    public FontScale FontSize { get; init; } = FontScale.Medium;

    public bool CollapseSidebarByDefault { get; init; }

    public bool ShowAdminRestartBadges { get; init; } = true;

    public bool ConfirmCaution { get; init; } = true;

    public bool AutoCreateRestorePoint { get; init; } = true;

    public bool AutoScrollConsole { get; init; } = true;

    public bool WarnOnCancel { get; init; } = true;

    public bool AutoCheckForUpdates { get; init; } = true;

    public LogLevel LogLevel { get; init; } = LogLevel.Information;

    /// <summary>Pinned favorite command ids, in pin order.</summary>
    public IReadOnlyList<string> FavoriteCommandIds { get; init; } = [];
}
