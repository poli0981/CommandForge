using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Infrastructure;

/// <summary>
/// <see cref="ISettingsService"/> backed by a local <c>config.json</c>. All data stays on
/// the machine (see Privacy policy / golden rule "no telemetry"). Saving is atomic and never
/// throws; a corrupt file is kept aside and replaced by defaults.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private readonly string _configPath;
    private readonly Lock _gate = new();
    private SettingsFileModel _model;

    /// <summary>Creates a service backed by the default <see cref="AppPaths.ConfigFilePath"/>.</summary>
    public JsonSettingsService()
        : this(AppPaths.ConfigFilePath)
    {
    }

    /// <summary>Creates a service backed by an explicit config path (used in tests).</summary>
    public JsonSettingsService(string configPath)
    {
        _configPath = configPath;
        _model = (JsonFile.Read(configPath, SettingsJsonContext.Default.SettingsFileModel) ?? new SettingsFileModel()).Normalized();
    }

    /// <inheritdoc />
    public string? AcceptedTermsVersion
    {
        get => _model.AcceptedTermsVersion;
        set => Update(m => m with { AcceptedTermsVersion = value });
    }

    /// <inheritdoc />
    public bool AutoCheckForUpdates
    {
        get => _model.AutoCheckForUpdates;
        set => Update(m => m with { AutoCheckForUpdates = value });
    }

    /// <inheritdoc />
    public AppTheme Theme
    {
        get => _model.Theme;
        set => Update(m => m with { Theme = value });
    }

    /// <inheritdoc />
    public string Language
    {
        get => _model.Language;
        set => Update(m => m with { Language = value ?? string.Empty });
    }

    /// <inheritdoc />
    public FontScale FontSize
    {
        get => _model.FontSize;
        set => Update(m => m with { FontSize = value });
    }

    /// <inheritdoc />
    public bool CollapseSidebarByDefault
    {
        get => _model.CollapseSidebarByDefault;
        set => Update(m => m with { CollapseSidebarByDefault = value });
    }

    /// <inheritdoc />
    public bool ShowAdminRestartBadges
    {
        get => _model.ShowAdminRestartBadges;
        set => Update(m => m with { ShowAdminRestartBadges = value });
    }

    /// <inheritdoc />
    public bool ConfirmCaution
    {
        get => _model.ConfirmCaution;
        set => Update(m => m with { ConfirmCaution = value });
    }

    /// <inheritdoc />
    public bool AutoCreateRestorePoint
    {
        get => _model.AutoCreateRestorePoint;
        set => Update(m => m with { AutoCreateRestorePoint = value });
    }

    /// <inheritdoc />
    public bool AutoScrollConsole
    {
        get => _model.AutoScrollConsole;
        set => Update(m => m with { AutoScrollConsole = value });
    }

    /// <inheritdoc />
    public bool WarnOnCancel
    {
        get => _model.WarnOnCancel;
        set => Update(m => m with { WarnOnCancel = value });
    }

    /// <inheritdoc />
    public LogLevel LogLevel
    {
        get => _model.LogLevel;
        set => Update(m => m with { LogLevel = value });
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FavoriteCommandIds
    {
        get => _model.FavoriteCommandIds;
        set => Update(m => m with { FavoriteCommandIds = value ?? [] });
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RecentCommandIds
    {
        get => _model.RecentCommandIds;
        set => Update(m => m with { RecentCommandIds = value ?? [] });
    }

    /// <inheritdoc />
    public double? WindowWidth
    {
        get => _model.WindowWidth;
        set => Update(m => m with { WindowWidth = value });
    }

    /// <inheritdoc />
    public double? WindowHeight
    {
        get => _model.WindowHeight;
        set => Update(m => m with { WindowHeight = value });
    }

    /// <inheritdoc />
    public double? WindowLeft
    {
        get => _model.WindowLeft;
        set => Update(m => m with { WindowLeft = value });
    }

    /// <inheritdoc />
    public double? WindowTop
    {
        get => _model.WindowTop;
        set => Update(m => m with { WindowTop = value });
    }

    /// <inheritdoc />
    public bool WindowMaximized
    {
        get => _model.WindowMaximized;
        set => Update(m => m with { WindowMaximized = value });
    }

    /// <inheritdoc />
    public void Save()
    {
        lock (_gate)
        {
            JsonFile.TryWrite(_configPath, _model, SettingsJsonContext.Default.SettingsFileModel);
        }
    }

    private void Update(Func<SettingsFileModel, SettingsFileModel> change)
    {
        lock (_gate)
        {
            _model = change(_model);
        }
    }
}
