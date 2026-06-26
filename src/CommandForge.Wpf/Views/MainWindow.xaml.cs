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

    /// <summary>
    /// Restores the last window position only, ignoring off-screen positions. The window is a fixed
    /// non-resizable size (see <c>ResizeMode="NoResize"</c> in XAML), so size/maximized state is not restored.
    /// </summary>
    private void RestorePlacement()
    {
        if (_settings.WindowLeft is { } left && _settings.WindowTop is { } top && IsOnScreen(left, top, Width, Height))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }
    }

    /// <summary>Persists the current window position (size/maximized are fixed and not saved).</summary>
    private void SavePlacement()
    {
        // Left/Top are not yet meaningful before the window is first laid out — skip rather than save garbage.
        if (double.IsNaN(Left) || double.IsNaN(Top))
        {
            return;
        }

        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
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
        else if (e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _viewModel.CheckForUpdatesCommand.Execute(null);
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

    private void OnShowAboutClick(object sender, RoutedEventArgs e)
        => new AboutDialog { Owner = this }.ShowDialog();

    private void OnShowTermsClick(object sender, RoutedEventArgs e)
        => new LegalViewerWindow { Owner = this }.ShowDialog();

    private void OnShowKeyboardShortcutsClick(object sender, RoutedEventArgs e)
        => new KeyboardShortcutsDialog { Owner = this }.ShowDialog();

    private void OnShowPortableInfoClick(object sender, RoutedEventArgs e)
    {
        var dialog = _services.GetRequiredService<PortableInfoDialog>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }
}
