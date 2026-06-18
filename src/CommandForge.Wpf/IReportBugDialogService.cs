namespace CommandForge.Wpf;

/// <summary>Shows the modal "Report a bug" dialog (optionally seeded from a crash).</summary>
public interface IReportBugDialogService
{
    /// <summary>Shows the dialog, optionally seeded with a crash error code/details.</summary>
    public void Show(string? errorCode = null, string? errorDetails = null);
}
