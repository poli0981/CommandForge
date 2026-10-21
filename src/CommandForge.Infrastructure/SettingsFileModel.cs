using System.Text.Json.Serialization;
using CommandForge.Application.Logging;
using CommandForge.Application.Settings;

namespace CommandForge.Infrastructure;

/// <summary>
/// The on-disk shape of <c>config.json</c>. Property names stay PascalCase and enums are written as
/// names, exactly as earlier versions wrote them, so existing files keep loading. Properties are
/// settable rather than init-only: the source-generated serializer assigns init-only properties in
/// an object initializer, which would reset every key missing from the file to default(T) instead
/// of keeping the defaults below.
/// </summary>
internal sealed record SettingsFileModel
{
    public string? AcceptedTermsVersion { get; set; }

    // Default true so existing or freshly-created configs opt in to startup update checks.
    public bool AutoCheckForUpdates { get; set; } = true;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public string Language { get; set; } = ""; // follow OS

    public FontScale FontSize { get; set; } = FontScale.Medium;

    public bool CollapseSidebarByDefault { get; set; }

    public bool ShowAdminRestartBadges { get; set; } = true;

    public bool ConfirmCaution { get; set; } = true;

    public bool AutoCreateRestorePoint { get; set; } = true;

    public bool AutoScrollConsole { get; set; } = true;

    public bool WarnOnCancel { get; set; } = true;

    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public IReadOnlyList<string> FavoriteCommandIds { get; set; } = [];

    public IReadOnlyList<string> RecentCommandIds { get; set; } = [];

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public bool WindowMaximized { get; set; }

    /// <summary>
    /// Repairs values a hand-edited or older file may contain: null lists or language, enum numbers
    /// outside the defined range, blank ids.
    /// </summary>
    public SettingsFileModel Normalized() => this with
    {
        Language = Language ?? string.Empty,
        Theme = Enum.IsDefined(Theme) ? Theme : AppTheme.System,
        FontSize = Enum.IsDefined(FontSize) ? FontSize : FontScale.Medium,
        LogLevel = Enum.IsDefined(LogLevel) ? LogLevel : LogLevel.Information,
        FavoriteCommandIds = Clean(FavoriteCommandIds),
        RecentCommandIds = Clean(RecentCommandIds),
    };

    private static IReadOnlyList<string> Clean(IReadOnlyList<string>? ids)
        => ids?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList() ?? [];
}

/// <summary>Source-generated serializer for <see cref="SettingsFileModel"/>.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SettingsFileModel))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
