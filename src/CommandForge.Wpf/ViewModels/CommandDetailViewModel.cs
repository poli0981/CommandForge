using CommandForge.Application;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Domain;
using CommandForge.Infrastructure.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>Detail panel for the selected command: metadata, the exact command-line preview, and actions.</summary>
public sealed partial class CommandDetailViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private readonly IShellService _shell;
    private readonly INotificationService _notifications;

    public CommandDetailViewModel(
        CommandDefinition command,
        string title,
        string description,
        string categoryTitle,
        IClipboardService clipboard,
        IShellService shell,
        INotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(notifications);

        Command = command;
        Title = title;
        Description = description;
        CategoryTitle = categoryTitle;
        _clipboard = clipboard;
        _shell = shell;
        _notifications = notifications;

        // Show exactly what will run on this PC: path tokens filled in, as the executor does.
        PreviewCommandLine = CommandPreview.Build(command, SystemPathTokens.Values);
    }

    public CommandDefinition Command { get; }

    public string Title { get; }

    public string Description { get; }

    public string CategoryTitle { get; }

    /// <summary>The exact command line that would run (display only).</summary>
    public string PreviewCommandLine { get; }

    public DangerLevel DangerLevel => Command.DangerLevel;

    public bool RequiresAdmin => Command.RequiresAdmin;

    public bool RequiresRestart => Command.Restart != RestartPolicy.No;

    public bool HasRevert => Command.RevertCommandId is not null;

    public bool HasDoc => !string.IsNullOrWhiteSpace(Command.DocUrl);

    /// <summary>The rough duration, in words.</summary>
    public string EstimatedDurationText => Strings.Get($"Estimate_{Command.EstimatedDuration}");

    [RelayCommand]
    private void Copy()
    {
        if (_clipboard.TrySetText(PreviewCommandLine))
        {
            _notifications.Show(Strings.Get("Snack_Copied"), NotificationKind.Success);
        }
        else
        {
            _notifications.Show(Strings.Get("Snack_CopyFailed"), NotificationKind.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(HasDoc))]
    private void OpenDoc()
    {
        if (Command.DocUrl is { } url)
        {
            _shell.TryOpen(url);
        }
    }
}
