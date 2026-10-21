using System.Collections.ObjectModel;
using System.Globalization;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.Registry;
using CommandForge.Application.Search;
using CommandForge.Application.Settings;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// The command catalog: browse by category or favorites, search, filter by risk, the selected
/// command's detail, and running commands through the shared safety rules (<see cref="RunSafety"/>).
/// </summary>
public sealed partial class CatalogViewModel : ObservableObject
{
    private readonly ICatalogProvider _catalog;
    private readonly IConfirmationService _confirmation;
    private readonly ISettingsService _settings;
    private readonly IExecutionHistoryService _history;
    private readonly IClock _clock;
    private readonly IRegistryService _registry;
    private readonly IClipboardService _clipboard;
    private readonly IShellService _shell;
    private readonly INotificationService _notifications;
    private readonly Dictionary<string, CommandItemViewModel> _itemsById;
    private readonly int _osBuild;
    private IReadOnlyList<SearchableCommand> _searchable;
    private IReadOnlyDictionary<string, string> _categoryTitles;

    public CatalogViewModel(
        ICatalogProvider catalog,
        int osBuild,
        ExecutionViewModel execution,
        SettingsViewModel settingsViewModel,
        IConfirmationService confirmation,
        ISettingsService settings,
        IExecutionHistoryService history,
        IClock clock,
        IRegistryService registry,
        IClipboardService clipboard,
        IShellService shell,
        INotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(notifications);

        _catalog = catalog;
        _osBuild = osBuild;
        _confirmation = confirmation;
        _settings = settings;
        _history = history;
        _clock = clock;
        _registry = registry;
        _clipboard = clipboard;
        _shell = shell;
        _notifications = notifications;
        Execution = execution;
        Settings = settingsViewModel;

        var vms = CatalogViewModelBuilder.Build(catalog, osBuild);
        _searchable = vms.Searchable;
        _categoryTitles = vms.CategoryTitles;
        _itemsById = vms.Items.ToDictionary(i => i.Command.Id, StringComparer.Ordinal);
        RestorePointCommand = _itemsById.GetValueOrDefault(RunSafety.RestorePointCommandId)?.Command;
        TotalCount = vms.Items.Count;

        foreach (var category in catalog.GetCategories())
        {
            var count = vms.Items.Count(i => string.Equals(i.Command.CategoryId, category.Id, StringComparison.Ordinal));
            Categories.Add(new CategoryViewModel(category.Id, category.TitleKey, category.Icon, count));
        }

        SyncFavorites();

        Execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
            {
                RunSelectedCommand.NotifyCanExecuteChanged();
                RevertSelectedCommand.NotifyCanExecuteChanged();
            }
        };

        ApplyFilter();
    }

    /// <summary>The shared console/run state.</summary>
    public ExecutionViewModel Execution { get; }

    /// <summary>The settings screen's view-model (the list binds to its badge preference live).</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Every command available on this Windows build, by id (shared with Home, History and Recipes).</summary>
    public IReadOnlyDictionary<string, CommandItemViewModel> ItemsById => _itemsById;

    /// <summary>The vetted command that creates a restore point, if the catalog has it.</summary>
    public CommandDefinition? RestorePointCommand { get; }

    /// <summary>Number of commands available on this Windows build.</summary>
    public int TotalCount { get; }

    public ObservableCollection<CategoryViewModel> Categories { get; } = [];

    public ObservableCollection<CommandItemViewModel> FilteredCommands { get; } = [];

    /// <summary>Whether the list shows pinned favorites instead of a category.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private bool _isFavoritesMode;

    /// <summary>The category being browsed; <see langword="null"/> for all commands.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private CategoryViewModel? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>A natural-language search keyword suggestion for the current query (null if none).</summary>
    [ObservableProperty]
    private string? _searchSuggestion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private bool _showSafe = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private bool _showCaution = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private bool _showDangerous = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveFilters))]
    private bool _adminOnly;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertSelectedCommand))]
    private CommandItemViewModel? _selectedCommand;

    [ObservableProperty]
    private CommandDetailViewModel? _detail;

    [ObservableProperty]
    private bool _hasResults;

    /// <summary>The heading above the list: all commands, the category, or Favorites.</summary>
    public string PageTitle => IsFavoritesMode
        ? Strings.Get("SidebarFavorites")
        : SelectedCategory?.Title ?? Strings.Get("Sidebar_AllCommands");

    /// <summary>Whether any risk/admin filter hides commands.</summary>
    public bool HasActiveFilters => !ShowSafe || !ShowCaution || !ShowDangerous || AdminOnly;

    /// <summary>Shows every command.</summary>
    public void ShowAll() => Browse(favorites: false, category: null);

    /// <summary>Shows one category.</summary>
    public void ShowCategory(string categoryId)
        => Browse(favorites: false, category: Categories.FirstOrDefault(c => string.Equals(c.Id, categoryId, StringComparison.Ordinal)));

    /// <summary>Shows pinned favorites.</summary>
    public void ShowFavorites() => Browse(favorites: true, category: null);

    /// <summary>Selects a command by id, clearing the search and filters so it is visible.</summary>
    public void SelectCommandById(string commandId)
    {
        SearchText = string.Empty;
        ClearFilters();
        Browse(favorites: false, category: null);
        if (_itemsById.TryGetValue(commandId, out var item))
        {
            SelectedCommand = item;
        }
    }

    /// <summary>Selects and runs a command by id (History's "Run again", the restore-point menu); ignores unknown ids.</summary>
    public Task RunCommandByIdAsync(string commandId)
    {
        if (!_itemsById.TryGetValue(commandId, out var item))
        {
            return Task.CompletedTask;
        }

        SelectCommandById(commandId);
        return ConfirmAndRunAsync(item.Command);
    }

    /// <summary>Re-resolves display strings after a language change, keeping selection and scroll.</summary>
    public void RefreshLanguage()
    {
        var vms = CatalogViewModelBuilder.Build(_catalog, _osBuild);
        _searchable = vms.Searchable;
        _categoryTitles = vms.CategoryTitles;

        foreach (var item in _itemsById.Values)
        {
            item.Refresh();
        }

        foreach (var category in Categories)
        {
            category.Refresh();
        }

        OnPropertyChanged(nameof(PageTitle));
        UpdateDetail();
    }

    /// <summary>Re-reads favorites from settings (after import / apply-profile) into the shared items.</summary>
    public void SyncFavorites()
    {
        var favoriteIds = new HashSet<string>(_settings.FavoriteCommandIds, StringComparer.Ordinal);
        foreach (var item in _itemsById.Values)
        {
            item.IsFavorite = favoriteIds.Contains(item.Command.Id);
        }

        if (IsFavoritesMode)
        {
            ApplyFilter();
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        ShowSafe = true;
        ShowCaution = true;
        ShowDangerous = true;
        AdminOnly = false;
    }

    /// <summary>Pins or unpins a command from Favorites and persists the change.</summary>
    [RelayCommand]
    private void ToggleFavorite(CommandItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _settings.FavoriteCommandIds = Favorites.Toggle(_settings.FavoriteCommandIds, item.Command.Id);

        // Update the shared item before persisting so the UI matches the in-memory list even if Save() fails.
        item.IsFavorite = _settings.FavoriteCommandIds.Contains(item.Command.Id, StringComparer.Ordinal);
        _settings.Save();

        // When viewing Favorites, drop a just-unpinned command from the list immediately.
        if (IsFavoritesMode)
        {
            ApplyFilter();
        }
    }

    /// <summary>Applies the natural-language keyword suggestion as the search query.</summary>
    [RelayCommand]
    private void ApplySuggestion()
    {
        if (SearchSuggestion is { } suggestion)
        {
            SearchText = suggestion;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunSelected))]
    private Task RunSelectedAsync()
        => SelectedCommand is null ? Task.CompletedTask : ConfirmAndRunAsync(SelectedCommand.Command);

    private bool CanRunSelected() => SelectedCommand is not null && !Execution.IsRunning;

    [RelayCommand(CanExecute = nameof(CanRevertSelected))]
    private Task RevertSelectedAsync()
    {
        if (SelectedCommand?.Command.RevertCommandId is not { } revertId
            || !_itemsById.TryGetValue(revertId, out var revertItem))
        {
            return Task.CompletedTask;
        }

        return ConfirmAndRunAsync(revertItem.Command);
    }

    private bool CanRevertSelected()
        => SelectedCommand?.Command.RevertCommandId is not null && !Execution.IsRunning;

    /// <summary>
    /// Confirms a command per <see cref="RunSafety"/> (Dangerous always confirms with typing; Caution
    /// honors <see cref="ISettingsService.ConfirmCaution"/>), optionally creating a restore point, then runs it.
    /// </summary>
    private async Task ConfirmAndRunAsync(CommandDefinition command)
    {
        if (Execution.IsRunning)
        {
            return; // one run at a time — and never record a run that didn't happen
        }

        CommandDefinition? restorePoint = null;
        if (RunSafety.NeedsConfirmation(command, _settings.ConfirmCaution))
        {
            var outcome = await _confirmation.ConfirmAsync(
                command,
                defaultCreateRestorePoint: RunSafety.DefaultCreateRestorePoint(command, _settings.AutoCreateRestorePoint));
            if (outcome is null)
            {
                return;
            }

            if (outcome.CreateRestorePoint)
            {
                restorePoint = RestorePointCommand;
            }
        }
        else if (RunSafety.DefaultCreateRestorePoint(command, _settings.AutoCreateRestorePoint))
        {
            // Caution confirmation skipped by setting, but still honor the restore-point preference.
            restorePoint = RestorePointCommand;
        }

        // Read-only before snapshot of any registry values this command may change.
        var registryBefore = command.AffectedRegistryValues.Count > 0
            ? RegistrySnapshot.Capture(_registry, command.AffectedRegistryValues)
            : null;

        if (!await Execution.RunAsync(command, restorePoint))
        {
            return;
        }

        // Record the run for Home's recent commands and the history (skip user-cancelled runs).
        if (!Execution.Cancelled)
        {
            _settings.RecentCommandIds = RecentCommands.Add(_settings.RecentCommandIds, command.Id);
            _settings.Save();

            if (Execution.LastResult is { } result)
            {
                _history.Record(new ExecutionRecord
                {
                    CommandId = command.Id,
                    Timestamp = _clock.Now,
                    ExitCode = result.ExitCode,
                    Success = result.Success,
                    DurationMs = (long)result.Duration.TotalMilliseconds,
                    RequiresRestart = result.RequiresRestart,
                });
            }

            if (registryBefore is not null)
            {
                AppendRegistryDiff(command.AffectedRegistryValues, registryBefore);
            }
        }
    }

    /// <summary>Re-reads the affected registry values and appends a before/after summary to the console.</summary>
    private void AppendRegistryDiff(
        IReadOnlyList<RegistryValueRef> references,
        IReadOnlyDictionary<RegistryValueRef, string?> before)
    {
        var after = RegistrySnapshot.Capture(_registry, references);
        var changes = RegistrySnapshot.Diff(references, before, after);

        Execution.OutputLines.Add(new OutputLine(Strings.Get("Registry_ChangesHeader"), IsError: false));
        if (changes.Count == 0)
        {
            Execution.OutputLines.Add(new OutputLine(Strings.Get("Registry_NoChanges"), IsError: false));
            return;
        }

        foreach (var change in changes)
        {
            var label = string.IsNullOrEmpty(change.Reference.Name)
                ? change.Reference.Path
                : change.Reference.Path + "\\" + change.Reference.Name;
            Execution.OutputLines.Add(new OutputLine(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.Get("Registry_ChangeFormat"),
                    label,
                    change.Before ?? Strings.Get("Registry_NotSet"),
                    change.After ?? Strings.Get("Registry_NotSet")),
                IsError: false));
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Suggest a catalog keyword for natural-language queries (local; no LLM/network).
        SearchSuggestion = NaturalLanguageSearch.Suggest(value);
        ApplyFilter();
    }

    partial void OnShowSafeChanged(bool value) => ApplyFilter();

    partial void OnShowCautionChanged(bool value) => ApplyFilter();

    partial void OnShowDangerousChanged(bool value) => ApplyFilter();

    partial void OnAdminOnlyChanged(bool value) => ApplyFilter();

    partial void OnSelectedCommandChanged(CommandItemViewModel? value) => UpdateDetail();

    private void Browse(bool favorites, CategoryViewModel? category)
    {
        IsFavoritesMode = favorites;
        SelectedCategory = category;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var levels = new HashSet<DangerLevel>();
        if (ShowSafe)
        {
            levels.Add(DangerLevel.Safe);
        }

        if (ShowCaution)
        {
            levels.Add(DangerLevel.Caution);
        }

        if (ShowDangerous)
        {
            levels.Add(DangerLevel.Dangerous);
        }

        FilteredCommands.Clear();
        if (levels.Count > 0)
        {
            var favoriteIds = IsFavoritesMode
                ? new HashSet<string>(_settings.FavoriteCommandIds, StringComparer.Ordinal)
                : null;
            var filter = new CommandSearchFilter
            {
                CategoryId = IsFavoritesMode ? null : SelectedCategory?.Id,
                DangerLevels = levels.Count == 3 ? null : levels,
                RequiresAdmin = AdminOnly ? true : null,
            };

            foreach (var hit in SearchCommandsUseCase.Search(_searchable, SearchText, filter))
            {
                if (favoriteIds is not null && !favoriteIds.Contains(hit.Command.Id))
                {
                    continue;
                }

                var item = _itemsById[hit.Command.Id];
                item.TitleMatches = hit.TitleMatches;
                FilteredCommands.Add(item);
            }
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
        Detail = new CommandDetailViewModel(
            command,
            SelectedCommand.Title,
            SelectedCommand.Description,
            categoryTitle,
            _clipboard,
            _shell,
            _notifications);
    }
}
