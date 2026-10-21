using System.ComponentModel;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// The main shell: which page is showing (<see cref="CurrentSection"/>, mirrored by the navigation
/// pane through <see cref="NavigationKey"/>), the page view-models, and app-wide actions (updates,
/// bug report, restore point, the elevated session).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IUpdateDialogService _updateDialog;
    private readonly IReportBugDialogService _reportBug;
    private readonly IElevationService _elevation;
    private readonly IShellService _shell;
    private readonly INotificationService _notifications;

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
        IElevationService elevation,
        SettingsViewModel settingsViewModel,
        LogViewerViewModel logViewer,
        DebugViewModel debug,
        IDialogService dialogs,
        IClipboardService clipboard,
        IShellService shell,
        INotificationService notifications)
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
        ArgumentNullException.ThrowIfNull(elevation);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(logViewer);
        ArgumentNullException.ThrowIfNull(debug);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(notifications);

        _updateDialog = updateDialog;
        _reportBug = reportBug;
        _elevation = elevation;
        _shell = shell;
        _notifications = notifications;
        Execution = execution;
        Settings = settingsViewModel;
        LogViewer = logViewer;
        Debug = debug;

        Catalog = new CatalogViewModel(
            catalog,
            systemInfo.GetStatus().OsBuild,
            execution,
            settingsViewModel,
            confirmation,
            settings,
            history,
            clock,
            registry,
            clipboard,
            shell,
            notifications);
        Home = new HomeViewModel(Catalog.ItemsById, settings, systemInfo, item => SelectCommandById(item.Command.Id));
        History = new HistoryViewModel(Catalog.ItemsById, history, SelectCommandById, RunCommandByIdAsync, execution, dialogs);
        Recipes = new RecipesViewModel(Catalog.ItemsById, execution, recipeStore, history, clock, confirmation, dialogs, settings, Catalog.RestorePointCommand);
        UserCommands = new UserCommandsViewModel(userCommandStore, execution, dialogs);

        _isPaneOpen = !settings.CollapseSidebarByDefault;
        Execution.AutoScroll = settings.AutoScrollConsole;
        _isAdminSessionActive = elevation.IsSessionActive;

        Execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
            {
                CreateRestorePointCommand.NotifyCanExecuteChanged();
                EndAdminSessionCommand.NotifyCanExecuteChanged();
            }
        };

        // The pane mirrors the catalog's browse state (Favorites / a category / everything).
        Catalog.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(CatalogViewModel.IsFavoritesMode) or nameof(CatalogViewModel.SelectedCategory))
            {
                OnPropertyChanged(nameof(NavigationKey));
            }
        };

        Settings.PropertyChanged += OnSettingsChanged;
        Settings.PortableSettingsApplied += OnPortableSettingsApplied;

        // Elevated-session changes arrive on a broker thread.
        _elevation.SessionChanged += (_, _) => System.Windows.Application.Current?.Dispatcher.InvokeAsync(
            () => IsAdminSessionActive = _elevation.IsSessionActive);

        // Live language switching: re-resolve display strings in place (keeps selection/scroll).
        LocalizationManager.Instance.CultureChanged += OnCultureChanged;

        Home.Refresh();
    }

    public HomeViewModel Home { get; }

    public CatalogViewModel Catalog { get; }

    public HistoryViewModel History { get; }

    public RecipesViewModel Recipes { get; }

    public UserCommandsViewModel UserCommands { get; }

    public SettingsViewModel Settings { get; }

    public LogViewerViewModel LogViewer { get; }

    public DebugViewModel Debug { get; }

    public ExecutionViewModel Execution { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationKey))]
    private ShellSection _currentSection = ShellSection.Home;

    /// <summary>Whether the navigation pane is expanded (Ctrl+B toggles it).</summary>
    [ObservableProperty]
    private bool _isPaneOpen;

    /// <summary>Whether an elevated helper is running (admin commands run without a new UAC prompt).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndAdminSessionCommand))]
    private bool _isAdminSessionActive;

    /// <summary>The navigation target that matches the current state (see <see cref="NavigationKeys"/>).</summary>
    public string NavigationKey => CurrentSection switch
    {
        ShellSection.Home => NavigationKeys.Home,
        ShellSection.Catalog when Catalog.IsFavoritesMode => NavigationKeys.Favorites,
        ShellSection.Catalog when Catalog.SelectedCategory?.Id is { } id => NavigationKeys.Category(id),
        ShellSection.Catalog => NavigationKeys.Catalog,
        ShellSection.History => NavigationKeys.History,
        ShellSection.Recipes => NavigationKeys.Recipes,
        ShellSection.UserCommands => NavigationKeys.UserCommands,
        ShellSection.Settings => NavigationKeys.Settings,
        ShellSection.LogViewer => NavigationKeys.Logs,
        ShellSection.Debug => NavigationKeys.Debug,
        _ => NavigationKeys.Home,
    };

    /// <summary>Goes to the page a navigation key names (from the navigation pane).</summary>
    public void Navigate(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.Equals(key, NavigationKey, StringComparison.Ordinal))
        {
            return;
        }

        if (NavigationKeys.CategoryIdOf(key) is { } categoryId)
        {
            Catalog.ShowCategory(categoryId);
            CurrentSection = ShellSection.Catalog;
            return;
        }

        switch (key)
        {
            case NavigationKeys.Favorites:
                ShowFavorites();
                break;
            case NavigationKeys.Catalog:
                ShowCatalog();
                break;
            case NavigationKeys.History:
                ShowHistory();
                break;
            case NavigationKeys.Recipes:
                ShowRecipes();
                break;
            case NavigationKeys.UserCommands:
                ShowUserCommands();
                break;
            case NavigationKeys.Settings:
                ShowSettings();
                break;
            case NavigationKeys.Logs:
                ShowLogViewer();
                break;
            case NavigationKeys.Debug:
                ShowDebug();
                break;
            default:
                ShowHome();
                break;
        }
    }

    [RelayCommand]
    private void ToggleSidebar() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    private void ShowHome()
    {
        Home.Refresh();
        CurrentSection = ShellSection.Home;
    }

    [RelayCommand]
    private void ShowCatalog()
    {
        Catalog.ShowAll();
        CurrentSection = ShellSection.Catalog;
    }

    [RelayCommand]
    private void ShowFavorites()
    {
        Catalog.ShowFavorites();
        CurrentSection = ShellSection.Catalog;
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
    private void ShowLogViewer() => CurrentSection = ShellSection.LogViewer;

    [RelayCommand]
    private void ShowDebug() => CurrentSection = ShellSection.Debug;

    [RelayCommand]
    private void OpenUserGuide() => _shell.TryOpen(ProductLinks.UserGuide);

    [RelayCommand]
    private Task ReportBugAsync() => _reportBug.ShowAsync();

    [RelayCommand]
    private Task CheckForUpdatesAsync() => _updateDialog.ShowAsync(startedFromStartup: false);

    /// <summary>Creates a System Restore Point by running the vetted catalog command (elevated via the broker).</summary>
    [RelayCommand(CanExecute = nameof(CanCreateRestorePoint))]
    private Task CreateRestorePointAsync()
    {
        if (Catalog.RestorePointCommand is not { } command)
        {
            return Task.CompletedTask;
        }

        CurrentSection = ShellSection.Catalog; // its console output shows on the catalog page
        return Catalog.RunCommandByIdAsync(command.Id);
    }

    private bool CanCreateRestorePoint() => Catalog.RestorePointCommand is not null && !Execution.IsRunning;

    /// <summary>Closes the elevated helper; the next admin command asks for permission again.</summary>
    [RelayCommand(CanExecute = nameof(CanEndAdminSession))]
    private async Task EndAdminSessionAsync()
    {
        await _elevation.EndSessionAsync();
        IsAdminSessionActive = _elevation.IsSessionActive;
        _notifications.Show(Strings.Get("Admin_SessionEnded"));
    }

    private bool CanEndAdminSession() => IsAdminSessionActive && !Execution.IsRunning;

    /// <summary>Re-runs a command by id (History's "Run again" / "Undo"); ignores unknown ids.</summary>
    public Task RunCommandByIdAsync(string commandId)
    {
        CurrentSection = ShellSection.Catalog;
        return Catalog.RunCommandByIdAsync(commandId);
    }

    /// <summary>Shows a command in the catalog (from the palette, Home and History).</summary>
    public void SelectCommandById(string commandId)
    {
        Catalog.SelectCommandById(commandId);
        CurrentSection = ShellSection.Catalog;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.AutoScrollConsole))
        {
            Execution.AutoScroll = Settings.AutoScrollConsole;
        }
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        Catalog.RefreshLanguage();
        Home.Refresh();
        History.Refresh();
    }

    private void OnPortableSettingsApplied()
    {
        // Favorites may have changed (import / apply-profile). Theme, font and language already
        // applied themselves through the Settings view-model's property setters.
        Catalog.SyncFavorites();
        Home.Refresh();
        History.Refresh();
    }
}
