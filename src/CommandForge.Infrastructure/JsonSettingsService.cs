using System.Text.Json;
using System.Text.Json.Serialization;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;

namespace CommandForge.Infrastructure;

/// <summary>
/// <see cref="ISettingsService"/> backed by a local <c>config.json</c>. All data stays on
/// the machine (see Privacy policy / golden rule "no telemetry").
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }, // human-readable enum values in config.json
    };

    private readonly string _configPath;
    private SettingsModel _model;

    /// <summary>Creates a service backed by the default <see cref="AppPaths.ConfigFilePath"/>.</summary>
    public JsonSettingsService()
        : this(AppPaths.ConfigFilePath)
    {
    }

    /// <summary>Creates a service backed by an explicit config path (used in tests).</summary>
    public JsonSettingsService(string configPath)
    {
        _configPath = configPath;
        _model = Load(configPath);
    }

    /// <inheritdoc />
    public string? AcceptedTermsVersion
    {
        get => _model.AcceptedTermsVersion;
        set => _model = _model with { AcceptedTermsVersion = value };
    }

    /// <inheritdoc />
    public bool AutoCheckForUpdates
    {
        get => _model.AutoCheckForUpdates;
        set => _model = _model with { AutoCheckForUpdates = value };
    }

    /// <inheritdoc />
    public AppTheme Theme
    {
        get => _model.Theme;
        set => _model = _model with { Theme = value };
    }

    /// <inheritdoc />
    public string Language
    {
        get => _model.Language;
        set => _model = _model with { Language = value };
    }

    /// <inheritdoc />
    public FontScale FontSize
    {
        get => _model.FontSize;
        set => _model = _model with { FontSize = value };
    }

    /// <inheritdoc />
    public bool CollapseSidebarByDefault
    {
        get => _model.CollapseSidebarByDefault;
        set => _model = _model with { CollapseSidebarByDefault = value };
    }

    /// <inheritdoc />
    public bool ShowAdminRestartBadges
    {
        get => _model.ShowAdminRestartBadges;
        set => _model = _model with { ShowAdminRestartBadges = value };
    }

    /// <inheritdoc />
    public bool ConfirmCaution
    {
        get => _model.ConfirmCaution;
        set => _model = _model with { ConfirmCaution = value };
    }

    /// <inheritdoc />
    public bool AutoCreateRestorePoint
    {
        get => _model.AutoCreateRestorePoint;
        set => _model = _model with { AutoCreateRestorePoint = value };
    }

    /// <inheritdoc />
    public bool AutoScrollConsole
    {
        get => _model.AutoScrollConsole;
        set => _model = _model with { AutoScrollConsole = value };
    }

    /// <inheritdoc />
    public bool WarnOnCancel
    {
        get => _model.WarnOnCancel;
        set => _model = _model with { WarnOnCancel = value };
    }

    /// <inheritdoc />
    public void Save()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_model, SerializerOptions);
        File.WriteAllText(_configPath, json);
    }

    private static SettingsModel Load(string path)
    {
        if (!File.Exists(path))
        {
            return new SettingsModel();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsModel>(json, SerializerOptions) ?? new SettingsModel();
        }
        catch (JsonException)
        {
            // Corrupt config: fall back to defaults rather than crashing on startup.
            return new SettingsModel();
        }
    }

    private sealed record SettingsModel
    {
        public string? AcceptedTermsVersion { get; init; }

        // Default true so existing or freshly-created configs opt in to startup update checks.
        public bool AutoCheckForUpdates { get; init; } = true;

        public AppTheme Theme { get; init; } = AppTheme.System;

        public string Language { get; init; } = ""; // follow OS

        public FontScale FontSize { get; init; } = FontScale.Medium;

        public bool CollapseSidebarByDefault { get; init; }

        public bool ShowAdminRestartBadges { get; init; } = true;

        public bool ConfirmCaution { get; init; } = true;

        public bool AutoCreateRestorePoint { get; init; } = true;

        public bool AutoScrollConsole { get; init; } = true;

        public bool WarnOnCancel { get; init; } = true;
    }
}
