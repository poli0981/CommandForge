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

        app.Dispatcher.InvokeAsync(() => Raise(app, message, important));
    }

    private static void Raise(System.Windows.Application app, string message, bool important)
    {
        if (!AutomationPeer.ListenerExists(AutomationEvents.Notification))
        {
            return;
        }

        var window = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
        if (window is null)
        {
            return;
        }

        try
        {
            var peer = UIElementAutomationPeer.FromElement(window) ?? UIElementAutomationPeer.CreatePeerForElement(window);
            peer?.RaiseNotificationEvent(
                important ? AutomationNotificationKind.ActionCompleted : AutomationNotificationKind.Other,
                important ? AutomationNotificationProcessing.ImportantMostRecent : AutomationNotificationProcessing.MostRecent,
                message,
                ActivityId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            Log.Debug(ex, "Could not raise a screen-reader notification.");
        }
    }
}
