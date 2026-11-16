using System.Windows.Controls;
using System.Windows.Input;
using CommandForge.Desktop.ViewModels;

namespace CommandForge.Desktop.Views;

/// <summary>The execution-history view (past runs, re-run, clear).</summary>
public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        EntriesList.KeyDown += OnEntriesKeyDown;
    }

    /// <summary>Enter on a run opens its command in the catalog (a button in the row handles its own Enter first).</summary>
    private void OnEntriesKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        if (e.OriginalSource is ListBoxItem { DataContext: HistoryEntryViewModel entry } && DataContext is HistoryViewModel viewModel)
        {
            viewModel.OpenCommand.Execute(entry);
            e.Handled = true;
        }
    }
}
