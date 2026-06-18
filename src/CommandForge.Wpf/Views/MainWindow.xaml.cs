using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CommandForge.Application.Ports;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CommandForge.Wpf.Views;

/// <summary>The main application shell (menu bar + collapsible sidebar + Home/Catalog/Settings content).</summary>
public partial class MainWindow : Window
{
    // Keep at least this much of the window grabbable when restoring a saved position.
    private const double OnScreenMargin = 100;

    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _services;
    private readonly ISettingsService _settings;

    public MainWindow(MainViewModel viewModel, IServiceProvider services, ISettingsService settings)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        _settings = settings;
        DataContext = viewModel;
        RestorePlacement();
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>Restores the last window size/position/maximized state, ignoring off-screen positions.</summary>
    private void RestorePlacement()
    {
        if (_settings.WindowWidth is { } width && width >= MinWidth)
        {
            Width = width;
        }

        if (_settings.WindowHeight is { } height && height >= MinHeight)
        {
            Height = height;
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

    /// <summary>Persists the current window placement (using restore bounds when maximized/minimized).</summary>
    private void SavePlacement()
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        // RestoreBounds is empty before the window is first laid out — skip rather than save zeros.
        if (bounds.Width <= 0 || bounds.Height <= 0)
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

    private void OnWindowClosing(object sender, CancelEventArgs e)
    {
        if (_viewModel.Execution.IsRunning)
        {
            if (_viewModel.Settings.WarnOnCancel)
            {
                var confirm = MessageBox.Show(
                    this,
                    Strings.Get("Exit_RunningMessage"),
                    Strings.Get("Exit_RunningTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return; // Close cancelled — do not persist a transient placement.
                }
            }

            _viewModel.Execution.CancelCommand.Execute(null);
        }

        // The window is really closing now — persist its placement.
        SavePlacement();
    }

    private void OnOpenPaletteClick(object sender, RoutedEventArgs e) => OpenPalette();

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OpenPalette();
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _viewModel.ShowSettingsCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.L && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _viewModel.ShowLogViewerCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.H && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _viewModel.ShowHomeCommand.Execute(null);
            e.Handled = true;
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
