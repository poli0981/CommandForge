using CommandForge.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandForge.Wpf.ViewModels;

/// <summary>A command as shown in the list / palette: resolved text + display flags + match highlight.</summary>
public sealed partial class CommandItemViewModel : ObservableObject
{
    public CommandItemViewModel(CommandDefinition command, string title, string description)
    {
        Command = command;
        Title = title;
        Description = description;
    }

    public CommandDefinition Command { get; }

    public string Title { get; }

    public string Description { get; }

    public string? Icon => Command.Icon;

    public DangerLevel DangerLevel => Command.DangerLevel;

    public bool RequiresAdmin => Command.RequiresAdmin;

    public bool RequiresRestart => Command.Restart != RestartPolicy.No;

    public bool IsRevertable => Command.RevertCommandId is not null;

    /// <summary>Indices of fuzzy-matched characters in <see cref="Title"/> for highlighting.</summary>
    [ObservableProperty]
    private IReadOnlyList<int> _titleMatches = [];
}
