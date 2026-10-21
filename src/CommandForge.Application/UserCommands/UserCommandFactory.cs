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
            // Not resource keys: the shell displays user commands by name. A name that happens to
            // equal a resource key (e.g. "AppTitle") must not be translated into that resource.
            TitleKey = IdPrefix + command.Id,
            DescriptionKey = IdPrefix + command.Id,
            Executable = command.Executable,
            ArgsTemplate = NormalizeArguments(command.Executable, command.Arguments),
            ExecutionMode = ExecutionMode.Capture,
            RequiresAdmin = false, // INVARIANT — a user command must never elevate.
            DangerLevel = DangerLevel.Caution,
            Restart = RestartPolicy.No,
        };
    }

    /// <summary>
    /// Normalizes arguments so a one-shot command actually runs and then exits. <c>cmd.exe</c>
    /// without <c>/c</c> (or <c>/k</c>) opens an interactive shell that blocks forever waiting on
    /// stdin, so for cmd we prepend <c>/c</c>. Other executables (e.g. powershell) are left as-is.
    /// </summary>
    public static string NormalizeArguments(string executable, string? arguments)
    {
        ArgumentNullException.ThrowIfNull(executable);

        var args = arguments ?? string.Empty;
        if (IsCmd(executable) && !HasCmdRunSwitch(args))
        {
            return args.Length == 0 ? "/c" : "/c " + args;
        }

        return args;
    }

    private static bool IsCmd(string executable)
        => string.Equals(Path.GetFileNameWithoutExtension(executable.Trim().Trim('"')), "cmd", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when a /c or /k switch appears among the leading switches (e.g. "/q /c dir").</summary>
    private static bool HasCmdRunSwitch(string arguments)
    {
        foreach (var token in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!token.StartsWith('/'))
            {
                return false; // switches come first; the command text starts here
            }

            if (StartsWithSwitch(token, "/c") || StartsWithSwitch(token, "/k"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool StartsWithSwitch(string text, string flag)
        => text.StartsWith(flag, StringComparison.OrdinalIgnoreCase)
            && (text.Length == flag.Length || char.IsWhiteSpace(text[flag.Length]));
}
