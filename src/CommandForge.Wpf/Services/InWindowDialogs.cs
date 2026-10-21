using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Services;

/// <summary>
/// Shows WPF-UI content dialogs inside the main window, one at a time. Dialog services check
/// <see cref="CanShow"/> first and fall back (a message box, or a safe "no") when the main window
/// isn't up yet or another dialog is already open, so a dialog never stacks on another.
/// </summary>
public sealed class InWindowDialogs
{
    private readonly IContentDialogService _service;
    private bool _hostReady;
    private int _open;

    public InWindowDialogs(IContentDialogService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    /// <summary>Whether a content dialog is showing right now.</summary>
    public bool IsOpen => _open > 0;

    /// <summary>Whether a dialog can be shown now: the main window is visible and no dialog is open.</summary>
    public bool CanShow => _hostReady && !IsOpen && System.Windows.Application.Current?.MainWindow is { IsVisible: true };

    /// <summary>Called once by the main window with its dialog host.</summary>
    public void SetHost(ContentDialogHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _service.SetDialogHost(host);
        _hostReady = true;
    }

    /// <summary>Shows <paramref name="dialog"/> and waits until it closes.</summary>
    public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        _open++;
        try
        {
            return await _service.ShowAsync(dialog, cancellationToken);
        }
        finally
        {
            _open--;
        }
    }
}
