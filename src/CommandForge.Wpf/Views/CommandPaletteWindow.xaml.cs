using System.Windows;
using System.Windows.Input;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>Command Palette overlay (Ctrl+K). Returns the chosen command id via <see cref="ChosenCommandId"/>.</summary>
public partial class CommandPaletteWindow : Window
{
    private readonly CommandPaletteViewModel _viewModel;

    public CommandPaletteWindow(CommandPaletteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CommandChosen += OnCommandChosen;
        Loaded += (_, _) => SearchBox.Focus();
        Deactivated += (_, _) => Close();
    }

    /// <summary>The command id the user accepted, or <see langword="null"/> if dismissed.</summary>
    public string? ChosenCommandId { get; private set; }

    private void OnCommandChosen(string commandId)
    {
        ChosenCommandId = commandId;
        Close();
    }

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e) => _viewModel.AcceptCommand.Execute(null);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
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
        _viewModel.SelectedResult = _viewModel.Results[next];
        ResultsList.ScrollIntoView(_viewModel.SelectedResult);
    }
}
