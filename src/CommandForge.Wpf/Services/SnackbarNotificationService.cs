using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Services;

/// <summary><see cref="INotificationService"/> backed by WPF-UI's snackbar in the main window.</summary>
public sealed class SnackbarNotificationService : INotificationService
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(4);

    private readonly ISnackbarService _snackbars;
    private bool _presenterReady;

    public SnackbarNotificationService(ISnackbarService snackbars)
    {
        ArgumentNullException.ThrowIfNull(snackbars);
        _snackbars = snackbars;
    }

    /// <summary>Called once by the main window with its snackbar presenter.</summary>
    public void SetPresenter(SnackbarPresenter presenter)
    {
        ArgumentNullException.ThrowIfNull(presenter);
        _snackbars.SetSnackbarPresenter(presenter);
        _presenterReady = true;
    }

    /// <inheritdoc />
    public void Show(string message, NotificationKind kind = NotificationKind.Information)
    {
        ArgumentNullException.ThrowIfNull(message);
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (!_presenterReady)
            {
                Log.Debug("Notification before the main window was shown: {Message}", message);
                return;
            }

            var (appearance, symbol) = kind switch
            {
                NotificationKind.Success => (ControlAppearance.Success, SymbolRegular.CheckmarkCircle24),
                NotificationKind.Warning => (ControlAppearance.Caution, SymbolRegular.Warning24),
                NotificationKind.Error => (ControlAppearance.Danger, SymbolRegular.ErrorCircle24),
                _ => (ControlAppearance.Secondary, SymbolRegular.Info24),
            };

            _snackbars.Show(message, string.Empty, appearance, new SymbolIcon(symbol, 20, filled: false), Duration);
        });
    }
}
