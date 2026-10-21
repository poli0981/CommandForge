using System.Text;
using CommandForge.Domain;

namespace CommandForge.Application;

/// <summary>
/// The confirmation and restore-point rules, in one place so single runs, re-runs from History,
/// reverts and recipes all apply them identically (golden rule #8).
/// </summary>
public static class RunSafety
{
    /// <summary>The vetted catalog command that creates a System Restore Point.</summary>
    public const string RestorePointCommandId = "system.restorepoint";

    /// <summary>
    /// Exit code of <see cref="RestorePointCommandId"/> when Windows created nothing because another
    /// restore point was made recently (it allows one per 24 hours) — not a failure.
    /// </summary>
    public const int RestorePointSkippedExitCode = 2;

    /// <summary>Dangerous commands always ask; Caution ones ask unless the user turned that off.</summary>
    public static bool NeedsConfirmation(CommandDefinition command, bool confirmCaution)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.DangerLevel == DangerLevel.Dangerous
            || (command.DangerLevel == DangerLevel.Caution && confirmCaution);
    }

    /// <summary>Dangerous commands require typing the title (or id) — never just a click.</summary>
    public static bool NeedsTypedConfirmation(CommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.DangerLevel == DangerLevel.Dangerous;
    }

    /// <summary>
    /// Whether a restore point is offered before <paramref name="command"/>: for commands the
    /// catalog flags, and for risky admin commands. Never for per-user (non-admin) tweaks — creating
    /// a restore point needs administrator rights, so it would add a UAC prompt they don't need.
    /// </summary>
    public static bool OffersRestorePoint(CommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.Equals(command.Id, RestorePointCommandId, StringComparison.Ordinal))
        {
            return false;
        }

        return command.CreatesRestorePoint
            || (command.RequiresAdmin && command.DangerLevel >= DangerLevel.Caution);
    }

    /// <summary>The initial state of the restore-point checkbox.</summary>
    public static bool DefaultCreateRestorePoint(CommandDefinition command, bool autoCreateRestorePoint)
    {
        ArgumentNullException.ThrowIfNull(command);
        return OffersRestorePoint(command) && (command.CreatesRestorePoint || autoCreateRestorePoint);
    }

    /// <summary>
    /// Whether <paramref name="input"/> confirms a Dangerous command: the title as displayed, or the
    /// command id. Unicode-normalized (Vietnamese input methods may type decomposed accents) and
    /// case-insensitive, so what looks identical always matches.
    /// </summary>
    public static bool IsTypedConfirmationValid(string? input, string title, string commandId)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(commandId);
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var typed = Normalize(input);
        return string.Equals(typed, Normalize(title), StringComparison.OrdinalIgnoreCase)
            || string.Equals(typed, commandId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
        => value.Trim().Normalize(NormalizationForm.FormC);
}
