using System.Windows;
using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Abstractions;

namespace CommandForge.Desktop.Views;

/// <summary>
/// Supplies the navigation view's pages: one instance per page for the app's lifetime (so lists
/// keep their selection and scroll position), each bound to its page view-model. Pages live in the
/// navigation view's Frame, which doesn't pass the window's text color down: each page root sets
/// its own Foreground (checked by a test).
/// </summary>
internal sealed class ShellPageProvider : INavigationViewPageProvider
{
    private readonly MainViewModel _viewModel;
    private readonly Dictionary<Type, FrameworkElement> _pages = [];

    public ShellPageProvider(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
    }

    public object? GetPage(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        if (_pages.TryGetValue(pageType, out var page))
        {
            return page;
        }

        page = Create(pageType);
        if (page is not null)
        {
            _pages[pageType] = page;
        }

        return page;
    }

    /// <summary>The page already created for <typeparamref name="TPage"/>, if any.</summary>
    public TPage? Find<TPage>()
        where TPage : FrameworkElement
        => _pages.TryGetValue(typeof(TPage), out var page) ? (TPage)page : null;

    private FrameworkElement? Create(Type pageType)
    {
        if (pageType == typeof(HomeView))
        {
            return new HomeView { DataContext = _viewModel.Home };
        }

        if (pageType == typeof(CatalogView))
        {
            return new CatalogView { DataContext = _viewModel.Catalog };
        }

        if (pageType == typeof(HistoryView))
        {
            return new HistoryView { DataContext = _viewModel.History };
        }

        if (pageType == typeof(RecipesView))
        {
            return new RecipesView { DataContext = _viewModel.Recipes };
        }

        if (pageType == typeof(UserCommandsView))
        {
            return new UserCommandsView { DataContext = _viewModel.UserCommands };
        }

        if (pageType == typeof(SettingsView))
        {
            return new SettingsView { DataContext = _viewModel.Settings };
        }

        if (pageType == typeof(LogViewerView))
        {
            return new LogViewerView { DataContext = _viewModel.LogViewer };
        }

        if (pageType == typeof(DebugView))
        {
            return new DebugView { DataContext = _viewModel.Debug };
        }

        return null;
    }
}
