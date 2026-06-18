using System.Collections.ObjectModel;
using CommandForge.Application.Ports;
using CommandForge.Application.Search;
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

    private readonly ICatalogProvider _catalog;
    private readonly IConfirmationService _confirmation;
    private readonly IUpdateDialogService _updateDialog;
    private readonly IReportBugDialogService _reportBug;
    private readonly ISettingsService _settings;
    private readonly Dictionary<string, CommandItemViewModel> _itemsById;
    private readonly CommandDefinition? _restorePointCommand;
    private IReadOnlyList<SearchableCommand> _searchable;
    private IReadOnlyDictionary<string, string> _categoryTitles;

    public MainViewModel(
        ICatalogProvider catalog,
        ExecutionViewModel execution,
        IConfirmationService confirmation,
        IUpdateDialogService updateDialog,
        IReportBugDialogService reportBug,
        ISettingsService settings,
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
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(logViewer);
        ArgumentNullException.ThrowIfNull(debug);

        _catalog = catalog;
        _confirmation = confirmation;
        _updateDialog = updateDialog;
        _reportBug = reportBug;
        _settings = settings;
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
            }
        };

        var vms = CatalogViewModelBuilder.Build(catalog);
        _searchable = vms.Searchable;
        _categoryTitles = vms.CategoryTitles;
        _itemsById = vms.Items.ToDictionary(i => i.Command.Id, StringComparer.Ordinal);
        _restorePointCommand = _itemsById.GetValueOrDefault(RestorePointCommandId)?.Command;

        Categories.Add(new CategoryViewModel(null, "Sidebar_AllCommands", "ViewGridOutline", vms.Items.Count));
        foreach (var category in catalog.GetCategories())
        {
            var count = vms.Items.Count(i => string.Equals(i.Command.CategoryId, category.Id, StringComparison.Ordinal));
            Categories.Add(new CategoryViewModel(category.Id, category.TitleKey, category.Icon, count));
        }

        _isSidebarCollapsed = settings.CollapseSidebarByDefault;
        SelectedCategory = Categories[0];

        // Live language switching: re-resolve catalog display strings in place (keeps selection/scroll).
        LocalizationManager.Instance.CultureChanged += OnCultureChanged;
    }

    /// <summary>The Settings screen view-model (shown when <see cref="CurrentSection"/> is Settings).</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>The Log Viewer view-model (shown when <see cref="CurrentSection"/> is LogViewer).</summary>
    public LogViewerViewModel LogViewer { get; }

    /// <summary>The Debug panel view-model (shown when <see cref="CurrentSection"/> is Debug).</summary>
    public DebugViewModel Debug { get; }

    public ExecutionViewModel Execution { get; }

    public ObservableCollection<CategoryViewModel> Categories { get; } = [];

    public ObservableCollection<CommandItemViewModel> FilteredCommands { get; } = [];

    [ObservableProperty]
    private ShellSection _currentSection = ShellSection.Catalog;

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

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    [RelayCommand]
    private void ShowSettings() => CurrentSection = ShellSection.Settings;

    [RelayCommand]
    private void ShowCatalog() => CurrentSection = ShellSection.Catalog;

    [RelayCommand]
    private void ShowLogViewer() => CurrentSection = ShellSection.LogViewer;

    [RelayCommand]
    private void ShowDebug() => CurrentSection = ShellSection.Debug;

    [RelayCommand]
    private void ReportBug() => _reportBug.Show();

    [RelayCommand]
    private Task CheckForUpdatesAsync() => _updateDialog.ShowAsync(startedFromStartup: false);

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

        await Execution.RunAsync(command, restorePoint);
    }

    /// <summary>Selects a command by id (used by the palette), clearing filters so it is visible.</summary>
    public void SelectCommandById(string commandId)
    {
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

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        CurrentSection = ShellSection.Catalog;
        ApplyFilter();
    }

    partial void OnSelectedCommandChanged(CommandItemViewModel? value) => UpdateDetail();

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        // Rebuild the search corpus + category-title map in the new language, and refresh the
        // already-displayed item/category/detail view-models in place.
        var vms = CatalogViewModelBuilder.Build(_catalog);
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
    }

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
