using System.Collections.ObjectModel;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// Execution-history screen: past command runs (newest first) with re-run and clear.
/// Titles are resolved from the catalog's shared <see cref="CommandItemViewModel"/> instances;
/// runs of commands no longer in the catalog show their id and cannot be re-run.
/// </summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<string, CommandItemViewModel> _itemsById;
    private readonly IExecutionHistoryService _history;
    private readonly Action<string> _openCommand;
    private readonly Func<string, Task> _runCommand;
    private readonly ExecutionViewModel _execution;
    private readonly IDialogService _dialogs;

    public HistoryViewModel(
        IReadOnlyDictionary<string, CommandItemViewModel> itemsById,
        IExecutionHistoryService history,
        Action<string> openCommand,
        Func<string, Task> runCommand,
        ExecutionViewModel execution,
        IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(openCommand);
        ArgumentNullException.ThrowIfNull(runCommand);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(dialogs);

        _itemsById = itemsById;
        _history = history;
        _openCommand = openCommand;
        _runCommand = runCommand;
        _execution = execution;
        _dialogs = dialogs;

        // "Run again"/"Undo" are unavailable while another command runs (the console is shared).
        _execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
            {
                RunAgainCommand.NotifyCanExecuteChanged();
                UndoCommand.NotifyCanExecuteChanged();
            }
        };
    }

    /// <summary>Past runs, newest first.</summary>
    public ObservableCollection<HistoryEntryViewModel> Entries { get; } = [];

    [ObservableProperty]
    private bool _hasEntries;

    /// <summary>Rebuilds the list from the history store, resolving titles against the current catalog.</summary>
    public void Refresh()
    {
        Entries.Clear();
        foreach (var record in _history.GetRecent())
        {
            _itemsById.TryGetValue(record.CommandId, out var item);
            var title = item?.Title ?? record.CommandId;

            // Undo is offered only when the run's command has a vetted revert command in the catalog.
            var revertCommandId = item?.Command.RevertCommandId is { } revertId && _itemsById.ContainsKey(revertId)
                ? revertId
                : null;

            Entries.Add(new HistoryEntryViewModel(record, title, item?.Icon, canRun: item is not null, revertCommandId));
        }

        HasEntries = Entries.Count > 0;
    }

    [RelayCommand]
    private void Open(HistoryEntryViewModel? entry)
    {
        if (entry is { CanRun: true })
        {
            _openCommand(entry.CommandId);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartRun))]
    private Task RunAgain(HistoryEntryViewModel? entry)
        => entry is { CanRun: true } ? _runCommand(entry.CommandId) : Task.CompletedTask;

    /// <summary>Undoes a run by executing its (vetted catalog) revert command. No arbitrary writes.</summary>
    [RelayCommand(CanExecute = nameof(CanStartRun))]
    private Task Undo(HistoryEntryViewModel? entry)
        => entry?.RevertCommandId is { } revertId ? _runCommand(revertId) : Task.CompletedTask;

    private bool CanStartRun(HistoryEntryViewModel? entry) => entry is not null && !_execution.IsRunning;

    [RelayCommand]
    private async Task ClearAsync()
    {
        // Clearing can't be undone, so ask first ("No" is the default).
        if (!await _dialogs.ConfirmAsync(Strings.Get("History_Clear"), Strings.Get("History_ClearConfirm")))
        {
            return;
        }

        _history.Clear();
        Refresh();
    }
}
