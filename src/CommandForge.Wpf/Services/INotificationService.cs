namespace CommandForge.Desktop.Services;

/// <summary>Short, non-blocking notifications (snackbars at the bottom of the main window).</summary>
public interface INotificationService
{
    /// <summary>Shows <paramref name="message"/>; safe to call from any thread, a no-op before the main window exists.</summary>
    public void Show(string message, NotificationKind kind = NotificationKind.Information);
}
