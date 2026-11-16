using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using Serilog;

namespace CommandForge.Desktop.Services;

/// <summary>
/// Says short status messages to screen readers (Narrator, NVDA, JAWS) through a UI Automation
/// notification event raised on the active window. WPF does not raise anything for
/// <c>AutomationProperties.LiveSetting</c> by itself, so changes that appear on screen (a run's
/// result, a search's result count, a snackbar) are announced through here.
/// </summary>
public static class ScreenReader
{
    // One activity: a newer message replaces one the screen reader has not started to read yet.
    private const string ActivityId = "CommandForge.Status";

    /// <summary>
    /// Announces <paramref name="message"/>. <paramref name="important"/> messages (a finished run,
    /// "restart required") interrupt what is being read; others wait their turn. Safe from any
    /// thread; a no-op without an application, a window or a listening screen reader.
    /// </summary>
    public static void Announce(string? message, bool important = false)
    {
        if (string.IsNullOrWhiteSpace(message) || System.Windows.Application.Current is not { } app)
        {
            return;
        }

        app.Dispatcher.InvokeAsync(() => RaiseOnActiveWindow(app.Windows.OfType<Window>(), app.MainWindow, message, important));
    }

    /// <summary>
    /// When a screen reader listens, raises the notification on the active window of
    /// <paramref name="windows"/>, else on <paramref name="mainWindow"/>. False when nothing was raised.
    /// </summary>
    public static bool RaiseOnActiveWindow(IEnumerable<Window> windows, Window? mainWindow, string message, bool important)
    {
        ArgumentNullException.ThrowIfNull(windows);
        return AutomationPeer.ListenerExists(AutomationEvents.Notification)
            && Raise(windows.FirstOrDefault(w => w.IsActive) ?? mainWindow, message, important);
    }

    /// <summary>
    /// Raises the notification on <paramref name="window"/>'s automation peer, on its thread. False when
    /// there is no window or no peer, or UI Automation refused it.
    /// </summary>
    public static bool Raise(Window? window, string message, bool important)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (window is null)
        {
            return false;
        }

        try
        {
            var peer = UIElementAutomationPeer.FromElement(window) ?? UIElementAutomationPeer.CreatePeerForElement(window);
            if (peer is null)
            {
                return false;
            }

            peer.RaiseNotificationEvent(
                important ? AutomationNotificationKind.ActionCompleted : AutomationNotificationKind.Other,
                important ? AutomationNotificationProcessing.ImportantMostRecent : AutomationNotificationProcessing.MostRecent,
                message,
                ActivityId);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            Log.Debug(ex, "Could not raise a screen-reader notification.");
            return false;
        }
    }
}
