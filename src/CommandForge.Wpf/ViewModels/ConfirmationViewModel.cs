using CommandForge.Application;
using CommandForge.Desktop.Resources;
using CommandForge.Domain;
using CommandForge.Infrastructure.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// Confirmation prompt for a Caution/Dangerous command. Dangerous commands require the user
/// to type the command title (or id) before Run enables (golden rule #8); see <see cref="RunSafety"/>.
/// </summary>
public sealed partial class ConfirmationViewModel : ObservableObject
{
    public ConfirmationViewModel(CommandDefinition command, string title, string description, bool defaultCreateRestorePoint)
    {
        ArgumentNullException.ThrowIfNull(command);
        Title = title;
        Description = description;
        CommandId = command.Id;
        CommandLine = CommandPreview.Build(command, SystemPathTokens.Values);
        IsDangerous = RunSafety.NeedsTypedConfirmation(command);
        DangerLevel = command.DangerLevel;
        DangerText = Strings.Get($"Danger_{command.DangerLevel}");
        ShowRestorePointOption = RunSafety.OffersRestorePoint(command);
        _createRestorePoint = ShowRestorePointOption && defaultCreateRestorePoint;
    }

    public string Title { get; }

    /// <summary>The catalog id, accepted as an alternative to typing the (possibly long, localized) title.</summary>
    public string CommandId { get; }

    /// <summary>Whether the restore-point checkbox applies to this command (risky admin commands only).</summary>
    public bool ShowRestorePointOption { get; }

    public string Description { get; }

    public string CommandLine { get; }

    public bool IsDangerous { get; }

    public DangerLevel DangerLevel { get; }

    public string DangerText { get; }

    /// <summary>Raised when the dialog should close; the argument is whether the user confirmed.</summary>
    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    private bool _createRestorePoint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _confirmationInput = string.Empty;

    /// <summary>Whether Run is allowed: always for Caution, only once the title or id is typed for Dangerous.</summary>
    public bool CanRun => !IsDangerous || RunSafety.IsTypedConfirmationValid(ConfirmationInput, Title, CommandId);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
