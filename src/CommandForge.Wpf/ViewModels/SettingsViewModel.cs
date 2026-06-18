using System.Diagnostics;
using System.Globalization;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    public SettingsViewModel(
        ISettingsService settings,
        IThemeService theme,
        IFontScaleService fonts,
        IUpdateDialogService updateDialog)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(updateDialog);

        _settings = settings;
        _theme = theme;
        _fonts = fonts;
        _updateDialog = updateDialog;

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
}
