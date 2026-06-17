namespace CommandForge.Wpf;

/// <summary>Shows the modal update dialog. A WPF-side presentation service called by the shell.</summary>
public interface IUpdateDialogService
{
    /// <summary>
    /// Runs the update flow. When <paramref name="startedFromStartup"/> is <see langword="true"/> a
    /// silent check runs first and the dialog is shown only if an update is available; otherwise the
    /// dialog is shown immediately and drives the check.
    /// </summary>
    public Task ShowAsync(bool startedFromStartup);
}
