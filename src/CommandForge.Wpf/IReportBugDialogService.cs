namespace CommandForge.Desktop;

/// <summary>Shows the "Report a bug" dialog (user-initiated; nothing is sent automatically).</summary>
public interface IReportBugDialogService
{
    /// <summary>
    /// Shows the dialog, optionally prefilled from a crash (<paramref name="errorCode"/> and
    /// <paramref name="errorDetails"/>), and completes when it closes.
    /// </summary>
    public Task ShowAsync(string? errorCode = null, string? errorDetails = null);
}
