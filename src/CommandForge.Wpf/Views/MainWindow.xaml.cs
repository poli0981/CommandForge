using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Controls;
using CommandForge.Desktop.Converters;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace CommandForge.Desktop.Views;

/// <summary>
/// The main shell: Fluent title bar (search, admin session, More menu), navigation pane with the
/// catalog's categories, page host, in-window dialogs and notifications.
/// </summary>
public partial class MainWindow : FluentWindow
{
    // Keep at least this much of the window grabbable when restoring a saved position.
    private const double OnScreenMargin = 100;

    private const uint FlashAll = 0x3;
    private const uint FlashUntilForeground = 0xC;

    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settings;
    private readonly InWindowDialogs _dialogs;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notifications;
    private readonly ShellPageProvider _pages;
    private bool _closeConfirmed;

    // True while the pane is being synced to the view-model (not a user's choice of page).
    private bool _syncingNavigation;

    public MainWindow(
        MainViewModel viewModel,
        IServiceProvider services,
        ISettingsService settings,
        InWindowDialogs dialogs,
        IDialogService dialogService,
        SnackbarNotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(notifications);

        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        _settings = settings;
        _dialogs = dialogs;
        _dialogService = dialogService;
        _notifications = notifications;
        DataContext = viewModel;

        dialogs.SetHost(RootDialogHost);
        notifications.SetPresenter(RootSnackbar);

        _pages = new ShellPageProvider(viewModel);
        RootNavigation.SetPageProviderService(_pages);
        BuildNavigation();
        RootNavigation.SelectionChanged += OnNavigationSelectionChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Execution.PropertyChanged += OnExecutionPropertyChanged;
        viewModel.CommandOpened += OnCommandOpened;
        Loaded += (_, _) =>
        {
#if DEBUG
            // A developer's accessibility scan opens a given page without sending keys or clicks.
            if (Environment.GetEnvironmentVariable("COMMANDFORGE_START_PAGE") is { Length: > 0 } startPage)
            {
                _viewModel.Navigate(startPage);
            }
#endif
            SyncNavigation(force: true);

            // Start with the keyboard in the first page, not on the window frame.
            FocusPageSoon(announce: false);
        };

        ApplyAnimationPreference();
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        Closed += (_, _) => SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;

        RestorePlacement();
    }

    // ===== Navigation =====

    private void BuildNavigation()
    {
        RootNavigation.MenuItems.Add(Item("SidebarHome", SymbolRegular.Home24, NavigationKeys.Home, typeof(HomeView), "Ctrl+H"));
        RootNavigation.MenuItems.Add(Item("SidebarFavorites", SymbolRegular.Star24, NavigationKeys.Favorites, typeof(CatalogView)));
        RootNavigation.MenuItems.Add(Item("SidebarHistory", SymbolRegular.History24, NavigationKeys.History, typeof(HistoryView)));
        RootNavigation.MenuItems.Add(Item("SidebarRecipes", SymbolRegular.TaskListSquareLtr24, NavigationKeys.Recipes, typeof(RecipesView)));
        RootNavigation.MenuItems.Add(Item("SidebarUserCommands", SymbolRegular.CodeTextEdit20, NavigationKeys.UserCommands, typeof(UserCommandsView)));

        // Last, so expanding its categories never pushes the other pages out of view.
        var catalog = Item("Nav_Catalog", SymbolRegular.Apps24, NavigationKeys.Catalog, typeof(CatalogView));
        foreach (var category in _viewModel.Catalog.Categories.Where(c => c.Id is not null))
        {
            catalog.MenuItems.Add(CategoryItem(category, category.Id!));
        }

        RootNavigation.MenuItems.Add(catalog);

        RootNavigation.FooterMenuItems.Add(Item("Menu_LogViewer", SymbolRegular.DocumentText24, NavigationKeys.Logs, typeof(LogViewerView), "Ctrl+L"));
        RootNavigation.FooterMenuItems.Add(Item("Menu_DebugPanel", SymbolRegular.Bug24, NavigationKeys.Debug, typeof(DebugView)));
        RootNavigation.FooterMenuItems.Add(Item("SidebarSettings", SymbolRegular.Settings24, NavigationKeys.Settings, typeof(SettingsView), "Ctrl+,"));
    }

    private static NavigationViewItem Item(string titleKey, SymbolRegular icon, string key, Type pageType, string? shortcut = null)
    {
        var item = new NavigationViewItem
        {
            Icon = new DecorativeIcon { Symbol = icon },
            TargetPageTag = key,
            TargetPageType = pageType,
        };
        item.SetBinding(ContentControl.ContentProperty, Localized(titleKey));
        item.SetBinding(AutomationProperties.NameProperty, Localized(titleKey));
        if (shortcut is not null)
        {
            // Screen readers announce it with the item ("Home, Ctrl+H").
            AutomationProperties.SetAcceleratorKey(item, shortcut);
        }

        return item;
    }

    private static NavigationViewItem CategoryItem(CategoryViewModel category, string categoryId)
    {
        var title = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis };
        title.SetBinding(TextBlock.TextProperty, new Binding(nameof(CategoryViewModel.Title)) { Source = category });

        var count = new TextBlock { Text = category.Count.ToString(CultureInfo.CurrentCulture), Margin = new Thickness(8, 0, 0, 0) };
        // Secondary, not tertiary: tertiary text falls below 4.5:1 in the Light theme.
        count.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

        var item = new NavigationViewItem
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { title, count } },
            Icon = new DecorativeIcon { Symbol = IconMap.Resolve(category.Icon) },
            TargetPageTag = NavigationKeys.Category(categoryId),
            TargetPageType = typeof(CatalogView),
        };
        item.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(CategoryViewModel.Title)) { Source = category });
        return item;
    }

    private static Binding Localized(string key)
        => new($"[{key}]") { Source = LocalizationManager.Instance, Mode = BindingMode.OneWay };

    private void OnNavigationSelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        MarkCurrentPage(sender.SelectedItem as NavigationViewItem);
        if (sender.SelectedItem is NavigationViewItem { TargetPageTag: { Length: > 0 } key })
        {
            _viewModel.Navigate(key);

            // A page chosen from the keyboard takes the keyboard with it; the pane item is left behind.
            if (!_syncingNavigation && InputManager.Current.MostRecentInputDevice is KeyboardDevice)
            {
                FocusPageSoon(announce: true);
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.NavigationKey))
        {
            SyncNavigation(force: false);
        }
    }

    /// <summary>Selects the pane item (and page) that matches the view-model's state.</summary>
    private void SyncNavigation(bool force)
    {
        var key = _viewModel.NavigationKey;
        if (!force
            && RootNavigation.SelectedItem is NavigationViewItem { TargetPageTag: { } current }
            && string.Equals(current, key, StringComparison.Ordinal))
        {
            return;
        }

        _syncingNavigation = true;
        try
        {
            RootNavigation.Navigate(key);
        }
        finally
        {
            _syncingNavigation = false;
        }
    }

    /// <summary>
    /// WPF UI exposes the pane's items to screen readers without their selection state, so the
    /// current page's item says so in its status ("Settings, Current page").
    /// </summary>
    private void MarkCurrentPage(NavigationViewItem? current)
    {
        foreach (var item in RootNavigation.MenuItems.OfType<NavigationViewItem>()
                     .SelectMany(i => i.MenuItems.OfType<NavigationViewItem>().Prepend(i))
                     .Concat(RootNavigation.FooterMenuItems.OfType<NavigationViewItem>()))
        {
            if (ReferenceEquals(item, current))
            {
                item.SetBinding(AutomationProperties.ItemStatusProperty, Localized("A11y_CurrentPage"));
            }
            else
            {
                BindingOperations.ClearBinding(item, AutomationProperties.ItemStatusProperty);
            }
        }
    }

    /// <summary>
    /// Moves the keyboard into the page on screen once it is laid out, so Tab continues from there and
    /// a screen reader reads where it landed; <paramref name="announce"/> also says the page's name.
    /// </summary>
    private void FocusPageSoon(bool announce)
        => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => FocusPage(announce));

    private void FocusPage(bool announce)
    {
        if (_pages.Current is not { IsVisible: true } page)
        {
            return;
        }

        if (!page.IsKeyboardFocusWithin)
        {
            page.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        if (announce && RootNavigation.SelectedItem is NavigationViewItem item)
        {
            ScreenReader.Announce(AutomationProperties.GetName(item));
        }
    }

    // A command opened from Home, History or the palette: the control that opened it is gone.
    private void OnCommandOpened(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => _pages.Find<CatalogView>()?.FocusSelectedCommand());

    /// <summary>F6 / Shift+F6: the navigation pane, the page, and the console when one is shown.</summary>
    private void CycleFocusRegion(bool backwards)
    {
        var regions = new List<FrameworkElement>(3);
        if (RootNavigation.SelectedItem is FrameworkElement selected && selected.IsVisible)
        {
            regions.Add(selected);
        }

        if (_pages.Current is { IsVisible: true } page)
        {
            regions.Add(page);
            if (FindVisibleConsole(page) is { } console)
            {
                regions.Add(console);
            }
        }

        if (regions.Count == 0)
        {
            return;
        }

        // The console sits inside the page, so the innermost region that holds the keyboard wins.
        var current = regions.FindLastIndex(r => r.IsKeyboardFocusWithin);
        var target = regions[FocusCycle.Next(current, regions.Count, backwards)];
        if (target is NavigationViewItem)
        {
            target.Focus();
        }
        else
        {
            target.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private static ConsoleView? FindVisibleConsole(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ConsoleView { IsVisible: true } console)
            {
                return console;
            }

            if (FindVisibleConsole(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    // ===== Animations =====

    /// <summary>Page transitions follow Windows' "Animation effects" setting (off: pages switch instantly).</summary>
    private void ApplyAnimationPreference()
        => RootNavigation.TransitionDuration = SystemParameters.ClientAreaAnimation ? 150 : 0;

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.ClientAreaAnimation))
        {
            Dispatcher.InvokeAsync(ApplyAnimationPreference);
        }
    }

    // ===== Keyboard =====

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Shortcuts never reach past an open dialog.
        if (_dialogs.IsOpen)
        {
            return;
        }

        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
        {
            _viewModel.CheckForUpdatesCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F6 && Keyboard.Modifiers is ModifierKeys.None or ModifierKeys.Shift)
        {
            CycleFocusRegion(backwards: Keyboard.Modifiers == ModifierKeys.Shift);
            e.Handled = true;
            return;
        }

        // Exactly Ctrl: AltGr arrives as Ctrl+Alt, so a German "@" (AltGr+Q) must not quit the app
        // and a Polish "ł" (AltGr+L) must not open the Log Viewer.
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.K:
                OpenPalette();
                break;
            case Key.F:
                FocusCatalogSearch();
                break;
            case Key.OemComma:
                _viewModel.ShowSettingsCommand.Execute(null);
                FocusPageSoon(announce: true);
                break;
            case Key.L:
                _viewModel.ShowLogViewerCommand.Execute(null);
                FocusPageSoon(announce: true);
                break;
            case Key.H:
                _viewModel.ShowHomeCommand.Execute(null);
                FocusPageSoon(announce: true);
                break;
            case Key.B:
                _viewModel.ToggleSidebarCommand.Execute(null);
                break;
            case Key.Q:
                // Not while something is being typed: a slip would discard it (search boxes opt in).
                if (!Behaviors.ShortcutScope.CanExitFrom(Keyboard.FocusedElement))
                {
                    return;
                }

                Close();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void FocusCatalogSearch()
    {
        if (_viewModel.CurrentSection != ShellSection.Catalog)
        {
            _viewModel.ShowCatalogCommand.Execute(null);
        }

        // The page may only now be created and laid out.
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () => _pages.Find<CatalogView>()?.FocusSearch());
    }

    // ===== Title bar actions =====

    private void OnOpenPaletteClick(object sender, RoutedEventArgs e) => OpenPalette();

    private void OpenPalette()
    {
        if (_dialogs.IsOpen)
        {
            return;
        }

        var palette = _services.GetRequiredService<CommandPaletteWindow>();
        palette.Owner = this;
        palette.ShowDialog();
        if (palette.ChosenCommandId is { } commandId)
        {
            _viewModel.SelectCommandById(commandId);
        }
    }

    private async void OnShowAboutClick(object sender, RoutedEventArgs e)
        => await ShowInfoDialogAsync(Strings.Get("Menu_About"), new AboutView(_services.GetRequiredService<IShellService>(), ShowTerms));

    private async void OnShowKeyboardShortcutsClick(object sender, RoutedEventArgs e)
        => await ShowInfoDialogAsync(Strings.Get("Menu_KeyboardShortcuts"), new KeyboardShortcutsView());

    private async void OnShowPortableInfoClick(object sender, RoutedEventArgs e)
        => await ShowInfoDialogAsync(Strings.Get("Menu_PortableInfo"), new PortableInfoView(_services.GetRequiredService<IUpdateService>()));

    private void OnShowTermsClick(object sender, RoutedEventArgs e) => ShowTerms();

    private void ShowTerms() => new LegalViewerWindow { Owner = this }.ShowDialog();

    private async Task ShowInfoDialogAsync(string title, object content)
    {
        if (!_dialogs.CanShow)
        {
            return;
        }

        await _dialogs.ShowAsync(new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = Strings.Get("ReportBug_Close"),
            DefaultButton = ContentDialogButton.Close,
        });
    }

    // ===== Run feedback outside the console =====

    private void OnExecutionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExecutionViewModel.IsRunning))
        {
            return;
        }

        var execution = _viewModel.Execution;
        TaskbarItemInfo.ProgressState = execution.IsRunning ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.None;
        if (!execution.IsRunning && execution.HasResult && !execution.Cancelled)
        {
            AnnounceFinished(execution);
        }
    }

    /// <summary>Tells the user a run finished when they can't see the console: a notification, and a taskbar flash if the window is in the background.</summary>
    private void AnnounceFinished(ExecutionViewModel execution)
    {
        var consoleVisible = _viewModel.CurrentSection is ShellSection.Catalog or ShellSection.Recipes or ShellSection.UserCommands;
        if (IsActive && consoleVisible)
        {
            return;
        }

        var message = string.Format(
            CultureInfo.CurrentCulture,
            Strings.Get(execution.Success ? "Run_Finished" : "Run_FinishedFailed"),
            execution.LastRunTitle);
        // Shown only: the execution view model has already read the result to screen readers.
        _notifications.Show(message, execution.Success ? NotificationKind.Success : NotificationKind.Error, announce: false);

        if (!IsActive)
        {
            var info = new FlashInfo
            {
                Size = (uint)Marshal.SizeOf<FlashInfo>(),
                Window = new WindowInteropHelper(this).Handle,
                Flags = FlashAll | FlashUntilForeground,
                Count = uint.MaxValue,
            };
            FlashWindowEx(ref info);
        }
    }

    // ===== Placement =====

    /// <summary>Restores the last size, position and maximized state, keeping the window on a visible screen.</summary>
    private void RestorePlacement()
    {
        var workArea = SystemParameters.WorkArea;
        if (_settings.WindowWidth is { } width && _settings.WindowHeight is { } height)
        {
            Width = Math.Clamp(width, MinWidth, Math.Max(MinWidth, workArea.Width));
            Height = Math.Clamp(height, MinHeight, Math.Max(MinHeight, workArea.Height));
        }
        else
        {
            Width = Math.Max(MinWidth, Math.Min(Width, workArea.Width));
            Height = Math.Max(MinHeight, Math.Min(Height, workArea.Height));
        }

        if (_settings.WindowLeft is { } left && _settings.WindowTop is { } top && IsOnScreen(left, top, Width, Height))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        if (_settings.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    /// <summary>Persists the normal (restored) bounds and whether the window is maximized.</summary>
    private void SavePlacement()
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;

        // Before the first layout the bounds are not meaningful — skip rather than save garbage.
        if (bounds.IsEmpty || double.IsNaN(bounds.Left) || double.IsNaN(bounds.Top))
        {
            return;
        }

        _settings.WindowLeft = bounds.Left;
        _settings.WindowTop = bounds.Top;
        _settings.WindowWidth = bounds.Width;
        _settings.WindowHeight = bounds.Height;
        _settings.WindowMaximized = WindowState == WindowState.Maximized;
        _settings.Save();
    }

    private static bool IsOnScreen(double left, double top, double width, double height)
        => WindowPlacement.IsOnScreen(
            left,
            top,
            width,
            height,
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight,
            OnScreenMargin);

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed || !_viewModel.Execution.IsRunning)
        {
            SavePlacement();
            return;
        }

        if (!_viewModel.Settings.WarnOnCancel)
        {
            _viewModel.Execution.CancelCommand.Execute(null);
            SavePlacement();
            return;
        }

        // Ask first (asynchronously, so the window stays open while the dialog shows).
        e.Cancel = true;
        if (!await _dialogService.ConfirmAsync(Strings.Get("Exit_RunningTitle"), Strings.Get("Exit_RunningMessage")))
        {
            return;
        }

        _viewModel.Execution.CancelCommand.Execute(null);
        _closeConfirmed = true;
        Close();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashInfo
    {
        public uint Size;
        public IntPtr Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FlashInfo info);
}
