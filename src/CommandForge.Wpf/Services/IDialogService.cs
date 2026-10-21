namespace CommandForge.Desktop.Services;

/// <summary>
/// Modal prompts used by view-models. Behind an interface so view-models stay testable and the
/// presentation (message boxes today, Fluent dialogs later) can change without touching them.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Asks a yes/no question. "No" is the default button and what Esc returns, so the safe answer
    /// is always the easy one.
    /// </summary>
    public Task<bool> ConfirmAsync(string title, string message, DialogKind kind = DialogKind.Warning);

    /// <summary>Shows a message with a single OK button.</summary>
    public Task ShowMessageAsync(string title, string message, DialogKind kind = DialogKind.Information);
}
