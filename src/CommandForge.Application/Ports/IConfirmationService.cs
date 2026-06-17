using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>The user's choice from a confirmation dialog.</summary>
public sealed record ConfirmationOutcome(bool CreateRestorePoint);

/// <summary>
/// Asks the user to confirm a risky command. Caution requires a simple confirmation;
/// Dangerous additionally requires typing the command name (golden rule #8).
/// </summary>
public interface IConfirmationService
{
    /// <summary>Returns the outcome, or <see langword="null"/> if the user cancelled.</summary>
    public Task<ConfirmationOutcome?> ConfirmAsync(CommandDefinition command, bool defaultCreateRestorePoint);
}
