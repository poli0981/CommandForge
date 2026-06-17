using CommandForge.Application;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Confirmation prompt for a Caution/Dangerous command. Dangerous commands require the user
/// to type the exact command title before the Run button enables (golden rule #8).
/// </summary>
public sealed partial class ConfirmationViewModel : ObservableObject
{
    public ConfirmationViewModel(CommandDefinition command, string title, string description, bool defaultCreateRestorePoint)
    {
        ArgumentNullException.ThrowIfNull(command);
        Title = title;
        Description = description;
        CommandLine = CommandPreview.Build(command);
        IsDangerous = command.DangerLevel == DangerLevel.Dangerous;
        DangerText = Strings.Get($"Danger_{command.DangerLevel}");
        _createRestorePoint = defaultCreateRestorePoint;
    }

    public string Title { get; }

    public string Description { get; }

    public string CommandLine { get; }

    public bool IsDangerous { get; }

    public string DangerText { get; }

    /// <summary>Raised when the dialog should close; the argument is whether the user confirmed.</summary>
    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    private bool _createRestorePoint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    private string _confirmationInput = string.Empty;

    /// <summary>Whether Run is allowed: always for Caution, only when the title is typed for Dangerous.</summary>
    public bool CanRun => !IsDangerous || string.Equals(ConfirmationInput.Trim(), Title, StringComparison.Ordinal);

    [RelayCommand]
    private void Run() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
