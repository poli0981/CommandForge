using CommandForge.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// View-model for the first-run Legal Gate. "Continue" is enabled only once the user
/// ticks the agreement checkbox; accepting records the current terms version.
/// </summary>
public partial class LegalGateViewModel : ObservableObject
{
    private readonly LegalGateService _legalGate;

    public LegalGateViewModel(LegalGateService legalGate)
    {
        _legalGate = legalGate;
    }

    /// <summary>Raised when the window should close; the argument is the acceptance result.</summary>
    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    private bool _isAgreed;

    /// <summary>The terms version being presented.</summary>
    public string TermsVersion => LegalGateService.CurrentTermsVersion;

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
