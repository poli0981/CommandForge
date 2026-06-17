using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandForge.Wpf.ViewModels;

/// <summary>A command as shown in the list / palette: resolved text + display flags + match highlight.</summary>
public sealed partial class CommandItemViewModel : ObservableObject
{
    public CommandItemViewModel(CommandDefinition command, string title, string description)
    {
        Command = command;
        _title = title;
        _description = description;
    }

    public CommandDefinition Command { get; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _description;

    public string? Icon => Command.Icon;

    public DangerLevel DangerLevel => Command.DangerLevel;

    public bool RequiresAdmin => Command.RequiresAdmin;

    public bool RequiresRestart => Command.Restart != RestartPolicy.No;

    public bool IsRevertable => Command.RevertCommandId is not null;

    /// <summary>Indices of fuzzy-matched characters in <see cref="Title"/> for highlighting.</summary>
    [ObservableProperty]
    private IReadOnlyList<int> _titleMatches = [];

    /// <summary>Re-resolves the localized title/description (after a culture change).</summary>
    public void Refresh()
    {
        Title = Strings.Get(Command.TitleKey);
        Description = Strings.Get(Command.DescriptionKey);
    }
}
