namespace CommandForge.Desktop.Services;

/// <summary>Short, non-blocking notifications (snackbars at the bottom of the main window).</summary>
public interface INotificationService
{
    /// <summary>
    /// Shows <paramref name="message"/> and, unless <paramref name="announce"/> is false (because the
    /// same news was already said), reads it to screen readers. Safe to call from any thread; a no-op
    /// before the main window exists.
    /// </summary>
    public void Show(string message, NotificationKind kind = NotificationKind.Information, bool announce = true);

    /// <summary>
    /// Reads <paramref name="message"/> to screen readers without showing anything, for a change the
    /// screen already shows (a run's result, its status). <paramref name="important"/> messages
    /// interrupt what is being read. Safe to call from any thread.
    /// </summary>
    public void Announce(string message, bool important = false);
}
