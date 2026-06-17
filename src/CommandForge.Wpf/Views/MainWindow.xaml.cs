using System.Windows;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Wpf.Views;

/// <summary>The main application shell (menu bar + collapsible sidebar + content panes).</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();
}
