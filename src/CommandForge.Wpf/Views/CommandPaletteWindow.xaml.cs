using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>
/// Command Palette overlay (Ctrl+K). Returns the chosen command id via <see cref="ChosenCommandId"/>.
/// The cursor stays in the search box while the arrow keys move the highlight, so screen readers are
/// told the highlighted command and the result count (they would hear nothing otherwise).
/// </summary>
public partial class CommandPaletteWindow : FluentWindow
{
    private static readonly TimeSpan ResultCountDelay = TimeSpan.FromMilliseconds(700);

    private readonly CommandPaletteViewModel _viewModel;
    private readonly DispatcherTimer _resultCountTimer;
    private bool _closing;

    public CommandPaletteWindow(CommandPaletteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CommandChosen += OnCommandChosen;
        _resultCountTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = ResultCountDelay };
        _resultCountTimer.Tick += OnResultCountTick;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (_, _) => SearchBox.Focus();
        // Close on focus loss — but guard against re-entrancy: Close() itself deactivates the window,
        // which would re-enter here and throw "Cannot ... while a Window is closing".
        Deactivated += (_, _) => RequestClose();
    }

    /// <summary>The command id the user accepted, or <see langword="null"/> if dismissed.</summary>
    public string? ChosenCommandId { get; private set; }

    private void OnCommandChosen(string commandId)
    {
        ChosenCommandId = commandId;
        RequestClose();
    }

    private void RequestClose()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        Close();
    }

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e) => _viewModel.AcceptCommand.Execute(null);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                RequestClose();
                e.Handled = true;
                break;
            case Key.Enter:
                _viewModel.AcceptCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            default:
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        var count = _viewModel.Results.Count;
        if (count == 0)
        {
            return;
        }

        var current = _viewModel.SelectedResult is null ? -1 : _viewModel.Results.IndexOf(_viewModel.SelectedResult);
        var next = Math.Clamp(current + delta, 0, count - 1);
        var item = _viewModel.Results[next];
        _viewModel.SelectedResult = item;
        ResultsList.ScrollIntoView(item);
        ScreenReader.Announce(HighlightText(item.Title, item.AccessibleStatus, next + 1, count));
    }

    /// <summary>Pure: "Flush DNS cache, Safe, 2 of 14" — what a screen reader hears for the highlighted result.</summary>
    public static string HighlightText(string title, string status, int position, int count)
        => string.Format(CultureInfo.CurrentCulture, Strings.Get("A11y_PaletteItem"), title, status, position, count);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommandPaletteViewModel.SearchText))
        {
            _resultCountTimer.Stop();
            _resultCountTimer.Start();
        }
    }

    private void OnResultCountTick(object? sender, EventArgs e)
    {
        _resultCountTimer.Stop();
        if (!_closing)
        {
            ScreenReader.Announce(CatalogView.ResultCountText(_viewModel.Results.Count));
        }
    }
}
