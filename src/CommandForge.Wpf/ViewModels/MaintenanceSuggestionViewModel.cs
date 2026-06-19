namespace CommandForge.Wpf.ViewModels;

/// <summary>One Home maintenance suggestion: a reason and an optional catalog command to open.</summary>
public sealed class MaintenanceSuggestionViewModel
{
    public MaintenanceSuggestionViewModel(string reason, CommandItemViewModel? command)
    {
        Reason = reason;
        Command = command;
    }

    /// <summary>Localized reason text.</summary>
    public string Reason { get; }

    /// <summary>The catalog command this suggestion can open, or null if it's informational only.</summary>
    public CommandItemViewModel? Command { get; }

    /// <summary>Whether there is a command to open.</summary>
    public bool CanOpen => Command is not null;
}
