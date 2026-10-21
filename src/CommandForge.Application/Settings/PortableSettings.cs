using CommandForge.Application.Logging;

namespace CommandForge.Application.Settings;

/// <summary>
/// The machine-portable subset of settings — appearance, behavior/safety preferences and favorites —
/// used for import/export and named profiles. Excludes machine-specific state (window placement,
/// accepted terms version, recent commands, execution history). Properties are settable so the
/// source-generated serializer keeps these defaults for keys a file omits (init-only properties
/// would be reset to false/0 — e.g. silently turning off Caution confirmations).
/// </summary>
public sealed record PortableSettings
{
    /// <summary>Schema version of the portable payload (for forward-compat).</summary>
    public int Version { get; set; } = 1;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public string Language { get; set; } = "";

    public FontScale FontSize { get; set; } = FontScale.Medium;

    public bool CollapseSidebarByDefault { get; set; }

    public bool ShowAdminRestartBadges { get; set; } = true;

    public bool ConfirmCaution { get; set; } = true;

    public bool AutoCreateRestorePoint { get; set; } = true;

    public bool AutoScrollConsole { get; set; } = true;

    public bool WarnOnCancel { get; set; } = true;

    public bool AutoCheckForUpdates { get; set; } = true;

    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>Pinned favorite command ids, in pin order.</summary>
    public IReadOnlyList<string> FavoriteCommandIds { get; set; } = [];

    /// <summary>
    /// Repairs what an imported or hand-edited file may contain: null lists or language, enum
    /// values outside the defined range, blank or duplicate favorite ids.
    /// </summary>
    public PortableSettings Normalized() => this with
    {
        Language = Language ?? string.Empty,
        Theme = Enum.IsDefined(Theme) ? Theme : AppTheme.System,
        FontSize = Enum.IsDefined(FontSize) ? FontSize : FontScale.Medium,
        LogLevel = Enum.IsDefined(LogLevel) ? LogLevel : LogLevel.Information,
        FavoriteCommandIds = FavoriteCommandIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList() ?? [],
    };
}
