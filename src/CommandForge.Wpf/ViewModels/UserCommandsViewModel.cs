using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommandForge.Application.Ports;
using CommandForge.Application.UserCommands;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// "My commands" screen: create and run user-defined commands. These are kept entirely separate
/// from the vetted catalog and ALWAYS run without elevation (golden rule #1) — the run path uses
/// <see cref="UserCommandFactory.ToDefinition"/>, which forces RequiresAdmin = false.
/// </summary>
public sealed partial class UserCommandsViewModel : ObservableObject
{
    private readonly IUserCommandStore _store;
    private readonly ExecutionViewModel _execution;
    private string? _editingId;

    public UserCommandsViewModel(IUserCommandStore store, ExecutionViewModel execution)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(execution);

        _store = store;
        _execution = execution;

        _execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
            {
                RunCommand.NotifyCanExecuteChanged();
            }
        };

        Refresh();
    }

    /// <summary>The shared execution view-model — drives the console output panel.</summary>
    public ExecutionViewModel Execution => _execution;

    /// <summary>Saved user commands.</summary>
    public ObservableCollection<UserCommand> Commands { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private UserCommand? _selectedCommand;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _editName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _editExecutable = string.Empty;

    [ObservableProperty]
    private string _editArguments = string.Empty;

    /// <summary>Reloads the saved-command list from the store.</summary>
    public void Refresh()
    {
        Commands.Clear();
        foreach (var command in _store.GetAll())
        {
            Commands.Add(command);
        }
    }

    partial void OnSelectedCommandChanged(UserCommand? value)
    {
        if (value is null)
        {
            return;
        }

        _editingId = value.Id;
        EditName = value.Name;
        EditExecutable = value.Executable;
        EditArguments = value.Arguments;
    }

    [RelayCommand]
    private void New()
    {
        SelectedCommand = null;
        _editingId = null;
        EditName = string.Empty;
        EditExecutable = string.Empty;
        EditArguments = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var name = EditName.Trim();
        var executable = EditExecutable.Trim();
        if (name.Length == 0 || executable.Length == 0)
        {
            return;
        }

        var id = _editingId ?? Guid.NewGuid().ToString("N");
        _store.Save(new UserCommand { Id = id, Name = name, Executable = executable, Arguments = EditArguments.Trim() });
        _editingId = id;
        Refresh();
        SelectedCommand = Commands.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(EditName) && !string.IsNullOrWhiteSpace(EditExecutable);

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private void Delete()
    {
        if (SelectedCommand is not { } command)
        {
            return;
        }

        if (MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.Get("UserCmd_DeleteConfirm"), command.Name),
                Strings.Get("UserCmd_Delete"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _store.Delete(command.Id);
        New();
        Refresh();
    }

    private bool HasSelected() => SelectedCommand is not null;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        var executable = EditExecutable.Trim();
        if (executable.Length == 0 || _execution.IsRunning)
        {
            return;
        }

        var userCommand = new UserCommand
        {
            Id = _editingId ?? "unsaved",
            Name = string.IsNullOrWhiteSpace(EditName) ? executable : EditName.Trim(),
            Executable = executable,
            Arguments = EditArguments.Trim(),
        };

        // Transparency (Security.md §3): show the exact command line that will run (including the
        // cmd /c normalization applied by ToDefinition) and that it runs without admin.
        var effectiveArgs = UserCommandFactory.NormalizeArguments(executable, userCommand.Arguments);
        var commandLine = string.IsNullOrEmpty(effectiveArgs)
            ? executable
            : executable + " " + effectiveArgs;
        if (MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.Get("UserCmd_RunConfirm"), commandLine),
                Strings.Get("UserCmd_RunTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        // SAFETY: ToDefinition forces RequiresAdmin = false → runs asInvoker, never via the broker.
        await _execution.RunAsync(UserCommandFactory.ToDefinition(userCommand));
    }

    private bool CanRun() => !string.IsNullOrWhiteSpace(EditExecutable) && !_execution.IsRunning;
}
