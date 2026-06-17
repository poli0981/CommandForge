using System.Collections.ObjectModel;
using CommandForge.Application.Ports;
using CommandForge.Application.Search;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>Command Palette (Ctrl+K): fuzzy search across the entire catalog.</summary>
public sealed partial class CommandPaletteViewModel : ObservableObject
{
    private readonly IReadOnlyList<SearchableCommand> _searchable;
    private readonly Dictionary<string, CommandItemViewModel> _itemsById;

    public CommandPaletteViewModel(ICatalogProvider catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var vms = CatalogViewModelBuilder.Build(catalog);
        _searchable = vms.Searchable;
        _itemsById = vms.Items.ToDictionary(i => i.Command.Id, StringComparer.Ordinal);
        ApplyFilter();
    }

    public ObservableCollection<CommandItemViewModel> Results { get; } = [];

    /// <summary>Raised with the chosen command id when the user accepts a result.</summary>
    public event Action<string>? CommandChosen;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private CommandItemViewModel? _selectedResult;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void Accept()
    {
        if (SelectedResult is not null)
        {
            CommandChosen?.Invoke(SelectedResult.Command.Id);
        }
    }

    private void ApplyFilter()
    {
        var hits = SearchCommandsUseCase.Search(_searchable, SearchText);
        Results.Clear();
        foreach (var hit in hits)
        {
            var item = _itemsById[hit.Command.Id];
            item.TitleMatches = hit.TitleMatches;
            Results.Add(item);
        }

        SelectedResult = Results.Count > 0 ? Results[0] : null;
    }
}
