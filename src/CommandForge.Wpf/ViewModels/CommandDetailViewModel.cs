using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using CommandForge.Application;
using CommandForge.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>Detail panel for the selected command: metadata, the exact command-line preview, and actions.</summary>
public sealed partial class CommandDetailViewModel : ObservableObject
{
    public CommandDetailViewModel(CommandDefinition command, string title, string description, string categoryTitle)
    {
        Command = command;
        Title = title;
        Description = description;
        CategoryTitle = categoryTitle;
        PreviewCommandLine = CommandPreview.Build(command);
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

    [RelayCommand]
    private void Copy()
    {
        try
        {
            Clipboard.SetText(PreviewCommandLine);
        }
        catch (COMException)
        {
            // Clipboard temporarily locked by another process — safe to ignore.
        }
    }

    [RelayCommand(CanExecute = nameof(HasDoc))]
    private void OpenDoc()
    {
        if (Command.DocUrl is { } url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
