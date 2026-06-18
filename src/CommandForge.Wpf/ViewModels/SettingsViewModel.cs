using System.Diagnostics;
using System.Globalization;
using System.Windows;
using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Drives the in-shell Settings screen (Appearance, Behavior &amp; Safety, Updates, About). Appearance
/// changes apply LIVE (theme/font/language) and every change is persisted via <see cref="ISettingsService"/>.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private const string RepoUrl = "https://github.com/poli0981/CommandForge";
    private const string LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html";

    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IFontScaleService _fonts;
    private readonly IUpdateDialogService _updateDialog;
    private readonly ILogLevelController _logLevel;
    private readonly ILogMaintenance _maintenance;

    public SettingsViewModel(
        ISettingsService settings,
        IThemeService theme,
        IFontScaleService fonts,
        IUpdateDialogService updateDialog,
        ILogLevelController logLevel,
        ILogMaintenance maintenance,
        IUpdateService updates)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(updateDialog);
        ArgumentNullException.ThrowIfNull(logLevel);
        ArgumentNullException.ThrowIfNull(maintenance);
        ArgumentNullException.ThrowIfNull(updates);

        _settings = settings;
        _theme = theme;
        _fonts = fonts;
        _updateDialog = updateDialog;
        _logLevel = logLevel;
        _maintenance = maintenance;

        Languages =
        [
            new LanguageOption("", Strings.Get("LanguageOption_System")),
            new LanguageOption("en", "English"),
            new LanguageOption("vi", "Tiếng Việt"),
        ];

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
    }

    [ObservableProperty]
    private SettingsSection _selectedSection = SettingsSection.Appearance;

    // ---- Appearance ----

    [ObservableProperty]
    private AppTheme _selectedTheme;

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _theme.Apply(value);
        _settings.Theme = value;
        _settings.Save();
    }

    /// <summary>Sets the theme (used by the View menu); routes through <see cref="SelectedTheme"/> to apply + persist.</summary>
    [RelayCommand]
    private void SetTheme(AppTheme theme) => SelectedTheme = theme;

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
            string.IsNullOrEmpty(value.Code) ? LocalizationManager.Instance.SystemCulture : new CultureInfo(value.Code));
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

    /// <summary>Sets the font size (used by the View menu); routes through <see cref="SelectedFontSize"/>.</summary>
    [RelayCommand]
    private void SetFontSize(FontScale size) => SelectedFontSize = size;

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
        await _updateDialog.ShowAsync(startedFromStartup: false);
        LastCheckedText = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
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
    private string _logsSizeText = string.Empty;

    [RelayCommand]
    private void OpenLogFolder() => OpenPath(_maintenance.LogsDirectoryPath);

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Get("Settings_ExportLogs"),
            Filter = "Zip archive (*.zip)|*.zip",
            FileName = $"commandforge-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _maintenance.ExportZipAsync(dialog.FileName);
        MessageBox.Show(
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Log_ExportSuccess"), dialog.FileName),
            Strings.Get("Settings_Logs"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ClearOldLogs()
    {
        var confirm = MessageBox.Show(
            Strings.Get("Settings_ClearLogsConfirm"),
            Strings.Get("Settings_ClearOldLogs"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
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
    private void ResetToDefaults()
    {
        var confirm = MessageBox.Show(
            Strings.Get("Settings_ResetConfirm"),
            Strings.Get("Settings_ResetDefaults"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
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
    private void OpenRepo() => OpenUrl(RepoUrl);

    [RelayCommand]
    private void OpenLicense() => OpenUrl(LicenseUrl);

    private void Persist(Action apply)
    {
        apply();
        _settings.Save();
    }

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static void OpenPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Path missing / shell failure — ignore.
        }
    }

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

