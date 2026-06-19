using CommandForge.Domain;

namespace CommandForge.Application.UserCommands;

/// <summary>
/// Builds an executable <see cref="CommandDefinition"/> for a user-defined command.
///
/// SAFETY (golden rule #1): the produced definition ALWAYS has <see cref="CommandDefinition.RequiresAdmin"/>
/// = false, so user commands run as the current (non-elevated) user and can never be routed to the
/// elevation broker by <c>RunCommandUseCase</c>. The id is namespaced with <see cref="IdPrefix"/> so it
/// can never collide with — or be mistaken for — a vetted catalog id; even if a bug routed it to the
/// Elevator, the Elevator validates ids against its own embedded catalog and rejects anything else.
/// </summary>
public static class UserCommandFactory
{
    /// <summary>Prefix marking an id as user-defined (never present in the vetted catalog).</summary>
    public const string IdPrefix = "user:";

    /// <summary>Converts a user command to a non-elevated executable definition.</summary>
    public static CommandDefinition ToDefinition(UserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return new CommandDefinition
        {
            Id = IdPrefix + command.Id,
            CategoryId = "user",
            TitleKey = command.Name,
            DescriptionKey = command.Name,
            Executable = command.Executable,
            ArgsTemplate = command.Arguments,
            ExecutionMode = ExecutionMode.Capture,
            RequiresAdmin = false, // INVARIANT — a user command must never elevate.
            DangerLevel = DangerLevel.Caution,
            Restart = RestartPolicy.No,
        };
    }
}
