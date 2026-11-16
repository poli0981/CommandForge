using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;

namespace CommandForge.Desktop.Views;

/// <summary>The catalog browse/run page: search and risk filters, command list, detail and console.</summary>
public partial class CatalogView : UserControl
{
    // Long enough that typing a word announces once, at the end.
    private static readonly TimeSpan ResultCountDelay = TimeSpan.FromMilliseconds(800);

    private readonly DispatcherTimer _resultCountTimer;
    private CatalogViewModel? _viewModel;

    public CatalogView()
    {
        InitializeComponent();
        _resultCountTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = ResultCountDelay };
        _resultCountTimer.Tick += OnResultCountTick;
        DataContextChanged += (_, _) => Attach(DataContext as CatalogViewModel);
        CommandList.KeyDown += OnCommandListKeyDown;
    }

    /// <summary>Puts the cursor in the search box (Ctrl+F, or after navigating here from the search box).</summary>
    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    /// <summary>
    /// Moves keyboard focus to the selected command's row, so that after opening a command from Home,
    /// History or the palette a screen reader reads it and the arrow keys continue from there.
    /// </summary>
    public void FocusSelectedCommand()
    {
        if (CommandList.SelectedItem is not { } item)
        {
            FocusSearch();
            return;
        }

        CommandList.ScrollIntoView(item);
        CommandList.UpdateLayout();
        if (CommandList.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem row)
        {
            row.Focus();
        }
    }

    /// <summary>Enter on a command row opens it: the keyboard moves to its Run button in the detail pane.</summary>
    private void OnCommandListKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None || e.OriginalSource is not ListBoxItem)
        {
            return;
        }

        if (RunButton.IsVisible && RunButton.Focus())
        {
            e.Handled = true;
        }
    }

    /// <summary>Pure: what a screen reader hears after a search or a filter change.</summary>
    public static string ResultCountText(int count) => count switch
    {
        0 => Strings.Get("NoSearchResults"),
        1 => Strings.Get("A11y_ResultCountOne"),
        _ => string.Format(CultureInfo.CurrentCulture, Strings.Get("A11y_ResultCount"), count),
    };

    private void Attach(CatalogViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The list is already refiltered when these change; announce once typing pauses.
        if (e.PropertyName is nameof(CatalogViewModel.SearchText)
            or nameof(CatalogViewModel.ShowSafe)
            or nameof(CatalogViewModel.ShowCaution)
            or nameof(CatalogViewModel.ShowDangerous)
            or nameof(CatalogViewModel.AdminOnly))
        {
            _resultCountTimer.Stop();
            _resultCountTimer.Start();
        }
    }

    private void OnResultCountTick(object? sender, EventArgs e)
    {
        _resultCountTimer.Stop();
        if (IsVisible && _viewModel is not null)
        {
            ScreenReader.Announce(ResultCountText(_viewModel.FilteredCommands.Count));
        }
    }
}
