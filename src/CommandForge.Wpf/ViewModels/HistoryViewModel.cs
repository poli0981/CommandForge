using System.Collections.ObjectModel;
using CommandForge.Application.Ports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

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

    public HistoryViewModel(
        IReadOnlyDictionary<string, CommandItemViewModel> itemsById,
        IExecutionHistoryService history,
        Action<string> openCommand,
        Func<string, Task> runCommand)
    {
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(openCommand);
        ArgumentNullException.ThrowIfNull(runCommand);

        _itemsById = itemsById;
        _history = history;
        _openCommand = openCommand;
        _runCommand = runCommand;
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
            Entries.Add(new HistoryEntryViewModel(record, title, item?.Icon, canRun: item is not null));
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

    [RelayCommand]
    private Task RunAgain(HistoryEntryViewModel? entry)
        => entry is { CanRun: true } ? _runCommand(entry.CommandId) : Task.CompletedTask;

    [RelayCommand]
    private void Clear()
    {
        _history.Clear();
        Refresh();
    }
}
