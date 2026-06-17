using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CommandForge.Wpf.Views;

/// <summary>The main application shell (menu bar + collapsible sidebar + catalog browse + console).</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _services;

    public MainWindow(MainViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        DataContext = viewModel;
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void OnWindowClosing(object sender, CancelEventArgs e)
    {
        if (!_viewModel.Execution.IsRunning)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            Strings.Get("Exit_RunningMessage"),
            Strings.Get("Exit_RunningTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            _viewModel.Execution.CancelCommand.Execute(null);
        }
        else
        {
            e.Cancel = true;
        }
    }

    private void OnOpenPaletteClick(object sender, RoutedEventArgs e) => OpenPalette();

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OpenPalette();
            e.Handled = true;
        }
    }

    private void OnRestartClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            this,
            Strings.Get("Restart_ConfirmMessage"),
            Strings.Get("Restart_ConfirmTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.OK)
        {
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { UseShellExecute = false, CreateNoWindow = true });
        }
    }

    private void OpenPalette()
    {
        var palette = _services.GetRequiredService<CommandPaletteWindow>();
        palette.Owner = this;
        palette.ShowDialog();
        if (palette.ChosenCommandId is { } commandId)
        {
            _viewModel.SelectCommandById(commandId);
        }
    }
}
