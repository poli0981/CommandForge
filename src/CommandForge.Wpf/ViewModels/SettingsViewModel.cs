using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommandForge.Application;
using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.Theming;
using CommandForge.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// Drives the Settings page (appearance, behavior &amp; safety, updates, profiles, logs, advanced,
/// about). Appearance changes apply live (theme/font/language) and every change is persisted via
/// <see cref="ISettingsService"/>.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IFontScaleService _fonts;
    private readonly IUpdateDialogService _updateDialog;
    private readonly ILogLevelController _logLevel;
    private readonly ILogMaintenance _maintenance;
    private readonly ISettingsFile _settingsFile;
    private readonly IProfileService _profiles;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly IShellService _shell;

    public SettingsViewModel(
        ISettingsService settings,
        IThemeService theme,
        IFontScaleService fonts,
        IUpdateDialogService updateDialog,
        ILogLevelController logLevel,
        ILogMaintenance maintenance,
        ISettingsFile settingsFile,
        IProfileService profiles,
        IUpdateService updates,
        IDialogService dialogs,
        IFileDialogService files,
        IShellService shell)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(updateDialog);
        ArgumentNullException.ThrowIfNull(logLevel);
        ArgumentNullException.ThrowIfNull(maintenance);
        ArgumentNullException.ThrowIfNull(settingsFile);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(updates);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(shell);

        _settings = settings;
        _theme = theme;
        _fonts = fonts;
        _updateDialog = updateDialog;
        _logLevel = logLevel;
        _maintenance = maintenance;
        _settingsFile = settingsFile;
        _profiles = profiles;
        _dialogs = dialogs;
        _files = files;
        _shell = shell;

        Languages = LanguageOption.CreateAll();

        // Initialize from persisted settings directly (field assignment skips the apply/save hooks).
        _selectedTheme = settings.Theme;
        _selectedFontSize = settings.FontSize;
        _selectedLanguage = Languages.FirstOrDefault(l => l.Code == settings.Language) ?? Languages[0];
        _collapseSidebarByDefault = settings.CollapseSidebarByDefault;
        _showAdminRestartBadges = settings.ShowAdminRestartBadges;
        _confirmCaution = settings.ConfirmCaution;
        _autoCreateRestorePoint = settings.AutoCreateRestorePoint;
        _autoScrollConsole = settings.AutoScrollConsole;
        _warnOnCancel = settings.WarnOnCancel;
        _autoCheckForUpdates = settings.AutoCheckForUpdates;
        _selectedLogLevel = settings.LogLevel;
        _logsSizeText = FormatSize(maintenance.GetLogsSizeBytes());

        RunModeText = updates.IsUpdateSupported
            ? Strings.Get("Settings_RunModeInstalled")
            : Strings.Get("Settings_RunModePortable");
        ConfigPathText = AppPaths.ConfigFilePath;

        var version = typeof(SettingsViewModel).Assembly.GetName().Version;
        CurrentVersionText = version is null ? "—" : $"{version.Major}.{version.Minor}.{version.Build}";

        RefreshProfiles();
    }

    /// <summary>
    /// Raised after import / apply-profile changes the live settings (incl. favorites), so the shell
    /// can re-sync favorite stars and dashboards. Appearance/language apply themselves via the
    /// observable-property setters.
    /// </summary>
    public event Action? PortableSettingsApplied;

    // ---- Appearance ----

    [ObservableProperty]
    private AppTheme _selectedTheme;

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _theme.Apply(value);
        _settings.Theme = value;
        _settings.Save();
    }

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value is null)
        {
            return;
        }

        LocalizationManager.Instance.SetCulture(
            string.IsNullOrEmpty(value.Code) ? LocalizationManager.Instance.SystemCulture : CultureInfo.GetCultureInfo(value.Code));
        _settings.Language = value.Code;
        _settings.Save();
    }

    [ObservableProperty]
    private FontScale _selectedFontSize;

    partial void OnSelectedFontSizeChanged(FontScale value)
    {
        _fonts.Apply(value);
        _settings.FontSize = value;
        _settings.Save();
    }

    [ObservableProperty]
    private bool _collapseSidebarByDefault;

    partial void OnCollapseSidebarByDefaultChanged(bool value) => Persist(() => _settings.CollapseSidebarByDefault = value);

    [ObservableProperty]
    private bool _showAdminRestartBadges;

    partial void OnShowAdminRestartBadgesChanged(bool value) => Persist(() => _settings.ShowAdminRestartBadges = value);

    // ---- Behavior & Safety ----

    [ObservableProperty]
    private bool _confirmCaution;

    partial void OnConfirmCautionChanged(bool value) => Persist(() => _settings.ConfirmCaution = value);

    /// <summary>Confirming Dangerous commands is a fixed safety floor — always on, shown as a locked toggle.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Bound from XAML, which needs an instance property.")]
    public bool ConfirmDangerous => true;

    [ObservableProperty]
    private bool _autoCreateRestorePoint;

    partial void OnAutoCreateRestorePointChanged(bool value) => Persist(() => _settings.AutoCreateRestorePoint = value);

    [ObservableProperty]
    private bool _autoScrollConsole;

    partial void OnAutoScrollConsoleChanged(bool value) => Persist(() => _settings.AutoScrollConsole = value);

    [ObservableProperty]
    private bool _warnOnCancel;

    partial void OnWarnOnCancelChanged(bool value) => Persist(() => _settings.WarnOnCancel = value);

    // ---- Updates ----

    [ObservableProperty]
    private bool _autoCheckForUpdates;

    partial void OnAutoCheckForUpdatesChanged(bool value) => Persist(() => _settings.AutoCheckForUpdates = value);

    [ObservableProperty]
    private string _lastCheckedText = "—";

    [RelayCommand]
    private async Task CheckNowAsync()
    {
        // Only a check that actually reached GitHub counts as "last checked".
        if (await _updateDialog.ShowAsync(startedFromStartup: false))
        {
            LastCheckedText = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        }
    }

    // ---- Profiles (import/export + named profiles) ----

    /// <summary>Saved profile names, for the picker.</summary>
    public ObservableCollection<string> ProfileNames { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    private string? _selectedProfile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]
    private string _newProfileName = string.Empty;

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        var title = Strings.Get("Settings_ExportSettings");
        var path = _files.PickSaveFile(title, Strings.Get("FileFilter_Json"), $"commandforge-settings-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        if (path is null)
        {
            return;
        }

        try
        {
            _settingsFile.Write(path, SettingsTransfer.Capture(_settings));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowMessageAsync(title, string.Format(CultureInfo.CurrentCulture, Strings.Get("File_SaveFailed"), ex.Message), DialogKind.Error);
            return;
        }

        await _dialogs.ShowMessageAsync(
            Strings.Get("Settings_Profiles"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Settings_ExportSuccess"), path));
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        var path = _files.PickOpenFile(Strings.Get("Settings_ImportSettings"), Strings.Get("FileFilter_Json"));
        if (path is null)
        {
            return;
        }

        var imported = _settingsFile.Read(path);
        if (imported is null)
        {
            await _dialogs.ShowMessageAsync(Strings.Get("Settings_Profiles"), Strings.Get("Settings_ImportInvalid"), DialogKind.Warning);
            return;
        }

        if (!await _dialogs.ConfirmAsync(Strings.Get("Settings_ImportSettings"), Strings.Get("Settings_ImportConfirm")))
        {
            return;
        }

        ApplyPortable(imported);
        await _dialogs.ShowMessageAsync(Strings.Get("Settings_Profiles"), Strings.Get("Settings_ImportSuccess"));
    }

    [RelayCommand(CanExecute = nameof(CanSaveProfile))]
    private void SaveProfile()
    {
        var name = NewProfileName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        _profiles.Save(name, SettingsTransfer.Capture(_settings));
        NewProfileName = string.Empty;
        RefreshProfiles();
        SelectedProfile = name;
    }

    private bool CanSaveProfile() => !string.IsNullOrWhiteSpace(NewProfileName);

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private async Task ApplyProfileAsync()
    {
        if (SelectedProfile is not { } name || _profiles.Get(name) is not { } portable)
        {
            return;
        }

        ApplyPortable(portable);
        await _dialogs.ShowMessageAsync(
            Strings.Get("Settings_Profiles"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Settings_ProfileApplied"), name));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is not { } name)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            Strings.Get("Settings_DeleteProfile"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Settings_DeleteProfileConfirm"), name));
        if (!confirmed)
        {
            return;
        }

        _profiles.Delete(name);
        RefreshProfiles();
    }

    private bool HasSelectedProfile() => SelectedProfile is not null;

    private void RefreshProfiles()
    {
        ProfileNames.Clear();
        foreach (var name in _profiles.GetNames())
        {
            ProfileNames.Add(name);
        }
    }

    /// <summary>
    /// Applies a portable payload to the live app: appearance/behavior via the observable setters
    /// (which apply + persist), favorites straight to settings, then notifies the shell to re-sync.
    /// </summary>
    private void ApplyPortable(PortableSettings portable)
    {
        portable = portable.Normalized();
        SelectedTheme = portable.Theme;
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == portable.Language) ?? Languages[0];
        SelectedFontSize = portable.FontSize;
        CollapseSidebarByDefault = portable.CollapseSidebarByDefault;
        ShowAdminRestartBadges = portable.ShowAdminRestartBadges;
        ConfirmCaution = portable.ConfirmCaution;
        AutoCreateRestorePoint = portable.AutoCreateRestorePoint;
        AutoScrollConsole = portable.AutoScrollConsole;
        WarnOnCancel = portable.WarnOnCancel;
        AutoCheckForUpdates = portable.AutoCheckForUpdates;
        SelectedLogLevel = portable.LogLevel;

        // Favorites have no observable property here — write them to settings + notify the shell.
        _settings.FavoriteCommandIds = portable.FavoriteCommandIds;
        _settings.Save();
        PortableSettingsApplied?.Invoke();
    }

    // ---- Logs ----

    public IReadOnlyList<LogLevel> LogLevels { get; } = [LogLevel.Information, LogLevel.Debug, LogLevel.Verbose];

    [ObservableProperty]
    private LogLevel _selectedLogLevel;

    partial void OnSelectedLogLevelChanged(LogLevel value)
    {
        _logLevel.Current = value;
        Persist(() => _settings.LogLevel = value);
    }

    [ObservableProperty]
    private string _logsSizeText;

    [RelayCommand]
    private void OpenLogFolder() => OpenPath(_maintenance.LogsDirectoryPath);

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        var title = Strings.Get("Settings_ExportLogs");
        var path = _files.PickSaveFile(title, Strings.Get("FileFilter_Zip"), $"commandforge-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        if (path is null)
        {
            return;
        }

        try
        {
            await _maintenance.ExportZipAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowMessageAsync(title, string.Format(CultureInfo.CurrentCulture, Strings.Get("File_SaveFailed"), ex.Message), DialogKind.Error);
            return;
        }

        await _dialogs.ShowMessageAsync(
            Strings.Get("Settings_Logs"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Log_ExportSuccess"), path));
    }

    [RelayCommand]
    private async Task ClearOldLogsAsync()
    {
        if (!await _dialogs.ConfirmAsync(Strings.Get("Settings_ClearOldLogs"), Strings.Get("Settings_ClearLogsConfirm")))
        {
            return;
        }

        _maintenance.DeleteRolledLogFiles();
        LogsSizeText = FormatSize(_maintenance.GetLogsSizeBytes());
    }

    // ---- Advanced ----

    public string RunModeText { get; }

    public string ConfigPathText { get; }

    [RelayCommand]
    private void OpenConfigFolder() => OpenPath(AppPaths.DataDirectory);

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        if (!await _dialogs.ConfirmAsync(Strings.Get("Settings_ResetDefaults"), Strings.Get("Settings_ResetConfirm")))
        {
            return;
        }

        // Assigning the observable properties re-applies (theme/font/language/log level) and persists.
        // AcceptedTermsVersion is intentionally untouched and log files are kept.
        SelectedTheme = AppTheme.System;
        SelectedLanguage = Languages[0];
        SelectedFontSize = FontScale.Medium;
        CollapseSidebarByDefault = false;
        ShowAdminRestartBadges = true;
        ConfirmCaution = true;
        AutoCreateRestorePoint = true;
        AutoScrollConsole = true;
        WarnOnCancel = true;
        AutoCheckForUpdates = true;
        SelectedLogLevel = LogLevel.Information;
    }

    // ---- About ----

    public string CurrentVersionText { get; }

    [RelayCommand]
    private void OpenRepo() => OpenUrl(ProductLinks.Repository);

    [RelayCommand]
    private void OpenLicense() => OpenUrl(ProductLinks.License);

    private void Persist(Action apply)
    {
        apply();
        _settings.Save();
    }

    private void OpenUrl(string url) => _shell.TryOpen(url);

    private void OpenPath(string path) => _shell.TryOpen(path);

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", size, units[unit]);
    }
}

