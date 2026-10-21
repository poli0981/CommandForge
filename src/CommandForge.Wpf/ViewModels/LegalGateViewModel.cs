using System.Globalization;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// View-model for the first-run Legal Gate. "Continue" is enabled only once the user ticks the
/// agreement checkbox; accepting records the current terms version. The language can be picked
/// here first, so the terms are read in it.
/// </summary>
public partial class LegalGateViewModel : ObservableObject
{
    private readonly LegalGateService _legalGate;
    private readonly ISettingsService _settings;

    public LegalGateViewModel(LegalGateService legalGate, ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(legalGate);
        ArgumentNullException.ThrowIfNull(settings);
        _legalGate = legalGate;
        _settings = settings;

        Languages = LanguageOption.CreateAll();
        _selectedLanguage = Languages.FirstOrDefault(l => l.Code == settings.Language) ?? Languages[0];
    }

    /// <summary>Raised when the window should close; the argument is the acceptance result.</summary>
    public event Action<bool>? CloseRequested;

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    private bool _isAgreed;

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        LocalizationManager.Instance.SetCulture(
            string.IsNullOrEmpty(value.Code) ? LocalizationManager.Instance.SystemCulture : CultureInfo.GetCultureInfo(value.Code));
        _settings.Language = value.Code;
        _settings.Save();
    }

    [RelayCommand(CanExecute = nameof(CanAccept))]
    private void Accept()
    {
        _legalGate.AcceptCurrentTerms();
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(false);

    private bool CanAccept() => IsAgreed;
}
