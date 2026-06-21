using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommandForge.Application.Ports;
using CommandForge.Application.Registry;
using CommandForge.Application.Search;
using CommandForge.Application.Settings;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Main shell view-model: in-shell section switching (Catalog / Settings), catalog browse
/// (sidebar categories + list), fuzzy search, the selected-command detail, and execution.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const string RestorePointCommandId = "system.restorepoint";
    private const string WikiUrl = "https://github.com/poli0981/CommandForge/wiki";

    private readonly ICatalogProvider _catalog;
    private readonly IConfirmationService _confirmation;
    private readonly IUpdateDialogService _updateDialog;
    private readonly IReportBugDialogService _reportBug;
    private readonly ISettingsService _settings;
    private readonly IExecutionHistoryService _history;
    private readonly IClock _clock;
    private readonly IRegistryService _registry;
    private readonly Dictionary<string, CommandItemViewModel> _itemsById;
    private readonly CommandDefinition? _restorePointCommand;
    private readonly int _osBuild;
    private IReadOnlyList<SearchableCommand> _searchable;
    private IReadOnlyDictionary<string, string> _categoryTitles;

    public MainViewModel(
        ICatalogProvider catalog,
        ExecutionViewModel execution,
        IConfirmationService confirmation,
        IUpdateDialogService updateDialog,
        IReportBugDialogService reportBug,
        ISettingsService settings,
        IExecutionHistoryService history,
        IClock clock,
        IRecipeStore recipeStore,
        IUserCommandStore userCommandStore,
        IRegistryService registry,
        ISystemInfoService systemInfo,
        SettingsViewModel settingsViewModel,
        LogViewerViewModel logViewer,
        DebugViewModel debug)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(updateDialog);
        ArgumentNullException.ThrowIfNull(reportBug);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(recipeStore);
        ArgumentNullException.ThrowIfNull(userCommandStore);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(systemInfo);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(logViewer);
        ArgumentNullException.ThrowIfNull(debug);

        _catalog = catalog;
        _confirmation = confirmation;
        _updateDialog = updateDialog;
        _reportBug = reportBug;
        _settings = settings;
        _history = history;
        _clock = clock;
        _registry = registry;
        Settings = settingsViewModel;
        LogViewer = logViewer;
        Debug = debug;
        Execution = execution;
        Execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
            {
                RunSelectedCommand.NotifyCanExecuteChanged();
                RevertSelectedCommand.NotifyCanExecuteChanged();
                CreateRestorePointCommand.NotifyCanExecuteChanged();
            }
        };

        _osBuild = systemInfo.GetStatus().OsBuild;
        var vms = CatalogViewModelBuilder.Build(catalog, _osBuild);
        _searchable = vms.Searchable;
        _categoryTitles = vms.CategoryTitles;
        _itemsById = vms.Items.ToDictionary(i => i.Command.Id, StringComparer.Ordinal);
        _restorePointCommand = _itemsById.GetValueOrDefault(RestorePointCommandId)?.Command;

        // Reflect persisted favorites on the shared item view-models (drives the star icon everywhere).
        var favoriteIds = new HashSet<string>(settings.FavoriteCommandIds, StringComparer.Ordinal);
        foreach (var item in _itemsById.Values)
        {
            item.IsFavorite = favoriteIds.Contains(item.Command.Id);
        }

        Home = new HomeViewModel(_itemsById, settings, systemInfo, item => SelectCommandById(item.Command.Id));
        History = new HistoryViewModel(_itemsById, history, SelectCommandById, RunCommandByIdAsync);
        Recipes = new RecipesViewModel(_itemsById, execution, recipeStore, history, clock);
        UserCommands = new UserCommandsViewModel(userCommandStore, execution);

        Categories.Add(new CategoryViewModel(null, "Sidebar_AllCommands", "ViewGridOutline", vms.Items.Count));
        foreach (var category in catalog.GetCategories())
        {
            var count = vms.Items.Count(i => string.Equals(i.Command.CategoryId, category.Id, StringComparison.Ordinal));
            Categories.Add(new CategoryViewModel(category.Id, category.TitleKey, category.Icon, count));
        }

        _isSidebarCollapsed = settings.CollapseSidebarByDefault;
        SelectedCategory = Categories[0];

        // Open on the Home dashboard (the SelectedCategory assignment above forces Catalog, so set last).
        Home.Refresh();
        CurrentSection = ShellSection.Home;

        // Live language switching: re-resolve catalog display strings in place (keeps selection/scroll).
        LocalizationManager.Instance.CultureChanged += OnCultureChanged;

        // Import / apply-profile can change favorites — re-sync stars and dashboards when it does.
        Settings.PortableSettingsApplied += OnPortableSettingsApplied;
    }

    /// <summary>The Home dashboard view-model (shown when <see cref="CurrentSection"/> is Home).</summary>
    public HomeViewModel Home { get; }

    /// <summary>The execution-history view-model (shown when <see cref="CurrentSection"/> is History).</summary>
    public HistoryViewModel History { get; }

    /// <summary>The recipes view-model (shown when <see cref="CurrentSection"/> is Recipes).</summary>
    public RecipesViewModel Recipes { get; }

    /// <summary>The user-defined commands view-model (shown when <see cref="CurrentSection"/> is UserCommands).</summary>
    public UserCommandsViewModel UserCommands { get; }

    /// <summary>The Settings screen view-model (shown when <see cref="CurrentSection"/> is Settings).</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>The Log Viewer view-model (shown when <see cref="CurrentSection"/> is LogViewer).</summary>
    public LogViewerViewModel LogViewer { get; }

    /// <summary>The Debug panel view-model (shown when <see cref="CurrentSection"/> is Debug).</summary>
    public DebugViewModel Debug { get; }

    public ExecutionViewModel Execution { get; }

    public ObservableCollection<CategoryViewModel> Categories { get; } = [];

    public ObservableCollection<CommandItemViewModel> FilteredCommands { get; } = [];

    /// <summary>Whether the command list is filtered by the selected category or by Favorites.</summary>
    private enum BrowseMode
    {
        Category,
        Favorites,
    }

    private BrowseMode _browseMode = BrowseMode.Category;

    [ObservableProperty]
    private ShellSection _currentSection = ShellSection.Home;

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private CategoryViewModel? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertSelectedCommand))]
    private CommandItemViewModel? _selectedCommand;

    [ObservableProperty]
    private CommandDetailViewModel? _detail;

    [ObservableProperty]
    private bool _hasResults;

    /// <summary>A natural-language search keyword suggestion for the current query (null if none).</summary>
    [ObservableProperty]
    private string? _searchSuggestion;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    [RelayCommand]
    private void ShowHome()
    {
        Home.Refresh();
        CurrentSection = ShellSection.Home;
    }

    [RelayCommand]
    private void ShowHistory()
    {
        History.Refresh();
        CurrentSection = ShellSection.History;
    }

    [RelayCommand]
    private void ShowRecipes()
    {
        Recipes.Refresh();
        CurrentSection = ShellSection.Recipes;
    }

    [RelayCommand]
    private void ShowUserCommands()
    {
        UserCommands.Refresh();
        CurrentSection = ShellSection.UserCommands;
    }

    [RelayCommand]
    private void ShowSettings() => CurrentSection = ShellSection.Settings;

    [RelayCommand]
    private void OpenWiki()
        => Process.Start(new ProcessStartInfo(WikiUrl) { UseShellExecute = true });

    [RelayCommand]
    private void ShowCatalog() => CurrentSection = ShellSection.Catalog;

    /// <summary>Shows the catalog filtered to pinned favorites.</summary>
    [RelayCommand]
    private void ShowFavorites()
    {
        _browseMode = BrowseMode.Favorites;
        SearchText = string.Empty;
        SelectedCategory = null; // clears the category highlight; stays in Favorites mode (see OnSelectedCategoryChanged)
        CurrentSection = ShellSection.Catalog;
        ApplyFilter();
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

        // When viewing the Favorites filter, drop a just-unpinned command from the list immediately.
        if (_browseMode == BrowseMode.Favorites)
        {
            ApplyFilter();
        }
    }

    [RelayCommand]
    private void ShowLogViewer() => CurrentSection = ShellSection.LogViewer;

    [RelayCommand]
    private void ShowDebug() => CurrentSection = ShellSection.Debug;

    [RelayCommand]
    private void ReportBug() => _reportBug.Show();

    [RelayCommand]
    private Task CheckForUpdatesAsync() => _updateDialog.ShowAsync(startedFromStartup: false);

    /// <summary>Creates a System Restore Point by running the vetted catalog command (elevated via the broker).</summary>
    [RelayCommand(CanExecute = nameof(CanCreateRestorePoint))]
    private Task CreateRestorePointAsync()
    {
        if (_restorePointCommand is null)
        {
            return Task.CompletedTask;
        }

        SelectCommandById(_restorePointCommand.Id); // show it in the catalog so its console output is visible
        return ConfirmAndRunAsync(_restorePointCommand);
    }

    private bool CanCreateRestorePoint() => _restorePointCommand is not null && !Execution.IsRunning;

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
    /// Confirms a command per the user's settings (Dangerous always confirms; Caution honors
    /// <see cref="ISettingsService.ConfirmCaution"/>), optionally creating a restore point, then runs it.
    /// </summary>
    private async Task ConfirmAndRunAsync(CommandDefinition command)
    {
        CommandDefinition? restorePoint = null;
        var needsConfirm = command.DangerLevel == DangerLevel.Dangerous
            || (command.DangerLevel == DangerLevel.Caution && _settings.ConfirmCaution);

        if (needsConfirm)
        {
            var outcome = await _confirmation.ConfirmAsync(command, defaultCreateRestorePoint: _settings.AutoCreateRestorePoint);
            if (outcome is null)
            {
                return;
            }

            if (outcome.CreateRestorePoint)
            {
                restorePoint = _restorePointCommand;
            }
        }
        else if (command.DangerLevel == DangerLevel.Caution && _settings.AutoCreateRestorePoint)
        {
            // Caution confirmation skipped by setting, but still honor the restore-point preference.
            restorePoint = _restorePointCommand;
        }

        // Read-only before snapshot of any registry values this command may change.
        var registryBefore = command.AffectedRegistryValues.Count > 0
            ? RegistrySnapshot.Capture(_registry, command.AffectedRegistryValues)
            : null;

        await Execution.RunAsync(command, restorePoint);

        // Record the run for the Home "recent commands" list and the execution history
        // (skip user-cancelled runs, matching the recent-commands behaviour).
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

    /// <summary>Re-runs a command by id (used by the History view's "Run again"); ignores unknown ids.</summary>
    public Task RunCommandByIdAsync(string commandId)
    {
        if (!_itemsById.TryGetValue(commandId, out var item))
        {
            return Task.CompletedTask;
        }

        SelectCommandById(commandId);
        return ConfirmAndRunAsync(item.Command);
    }

    /// <summary>Selects a command by id (used by the palette and Home), clearing filters so it is visible.</summary>
    public void SelectCommandById(string commandId)
    {
        _browseMode = BrowseMode.Category;
        CurrentSection = ShellSection.Catalog;
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

    partial void OnSearchTextChanged(string value)
    {
        // Suggest a catalog keyword for natural-language queries (local; no LLM/network).
        SearchSuggestion = NaturalLanguageSearch.Suggest(value);
        ApplyFilter();
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

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        // Picking a category leaves Favorites mode; clearing the selection (value is null) keeps the
        // current mode, so ShowFavorites can deselect the category without flipping back to Category.
        if (value is not null)
        {
            _browseMode = BrowseMode.Category;
        }

        CurrentSection = ShellSection.Catalog;
        ApplyFilter();
    }

    partial void OnSelectedCommandChanged(CommandItemViewModel? value) => UpdateDetail();

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        // Rebuild the search corpus + category-title map in the new language, and refresh the
        // already-displayed item/category/detail view-models in place.
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

        UpdateDetail();
        Home.Refresh();
        History.Refresh();
    }

    private void OnPortableSettingsApplied()
    {
        // Favorites may have changed (import / apply-profile); re-sync the shared item view-models
        // (drives the star icon everywhere) and the dashboards. Theme/font/language already applied
        // live via the Settings observable-property setters.
        var favoriteIds = new HashSet<string>(_settings.FavoriteCommandIds, StringComparer.Ordinal);
        foreach (var item in _itemsById.Values)
        {
            item.IsFavorite = favoriteIds.Contains(item.Command.Id);
        }

        Home.Refresh();
        History.Refresh();
        if (_browseMode == BrowseMode.Favorites)
        {
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        var inFavorites = _browseMode == BrowseMode.Favorites;
        var favoriteIds = inFavorites
            ? new HashSet<string>(_settings.FavoriteCommandIds, StringComparer.Ordinal)
            : null;
        var filter = !inFavorites && SelectedCategory?.Id is { } categoryId
            ? new CommandSearchFilter { CategoryId = categoryId }
            : null;
        var hits = SearchCommandsUseCase.Search(_searchable, SearchText, filter);

        FilteredCommands.Clear();
        foreach (var hit in hits)
        {
            if (favoriteIds is not null && !favoriteIds.Contains(hit.Command.Id))
            {
                continue;
            }

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
