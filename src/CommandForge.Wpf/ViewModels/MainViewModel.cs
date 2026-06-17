using System.Collections.ObjectModel;
using CommandForge.Application.Ports;
using CommandForge.Application.Search;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Main shell view-model: catalog browse (sidebar categories + list), fuzzy search, and the
/// selected-command detail. Execution is wired in a later phase.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IReadOnlyList<SearchableCommand> _searchable;
    private readonly IReadOnlyDictionary<string, string> _categoryTitles;
    private readonly Dictionary<string, CommandItemViewModel> _itemsById;

    public MainViewModel(ICatalogProvider catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var vms = CatalogViewModelBuilder.Build(catalog);
        _searchable = vms.Searchable;
        _categoryTitles = vms.CategoryTitles;
        _itemsById = vms.Items.ToDictionary(i => i.Command.Id, StringComparer.Ordinal);

        Categories.Add(new CategoryViewModel(null, Strings.Get("Sidebar_AllCommands"), "ViewGridOutline", vms.Items.Count));
        foreach (var category in catalog.GetCategories())
        {
            var count = vms.Items.Count(i => string.Equals(i.Command.CategoryId, category.Id, StringComparison.Ordinal));
            Categories.Add(new CategoryViewModel(category.Id, Strings.Get(category.TitleKey), category.Icon, count));
        }

        SelectedCategory = Categories[0];
    }

    public ObservableCollection<CategoryViewModel> Categories { get; } = [];

    public ObservableCollection<CommandItemViewModel> FilteredCommands { get; } = [];

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private CategoryViewModel? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private CommandItemViewModel? _selectedCommand;

    [ObservableProperty]
    private CommandDetailViewModel? _detail;

    [ObservableProperty]
    private bool _hasResults;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    /// <summary>Selects a command by id (used by the palette), clearing filters so it is visible.</summary>
    public void SelectCommandById(string commandId)
    {
        if (Categories.Count > 0)
        {
            SelectedCategory = Categories[0];
        }

        SearchText = string.Empty;
        if (_itemsById.TryGetValue(commandId, out var item))
        {
            SelectedCommand = item;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCategoryChanged(CategoryViewModel? value) => ApplyFilter();

    partial void OnSelectedCommandChanged(CommandItemViewModel? value) => UpdateDetail();

    private void ApplyFilter()
    {
        var filter = SelectedCategory?.Id is { } categoryId
            ? new CommandSearchFilter { CategoryId = categoryId }
            : null;
        var hits = SearchCommandsUseCase.Search(_searchable, SearchText, filter);

        FilteredCommands.Clear();
        foreach (var hit in hits)
        {
            var item = _itemsById[hit.Command.Id];
            item.TitleMatches = hit.TitleMatches;
            FilteredCommands.Add(item);
        }

        HasResults = FilteredCommands.Count > 0;
        if (SelectedCommand is null || !FilteredCommands.Contains(SelectedCommand))
        {
            SelectedCommand = FilteredCommands.Count > 0 ? FilteredCommands[0] : null;
        }
    }

    private void UpdateDetail()
    {
        if (SelectedCommand is null)
        {
            Detail = null;
            return;
        }

        var command = SelectedCommand.Command;
        var categoryTitle = _categoryTitles.TryGetValue(command.CategoryId, out var title)
            ? title
            : command.CategoryId;
        Detail = new CommandDetailViewModel(command, SelectedCommand.Title, SelectedCommand.Description, categoryTitle);
    }
}
