using CommandForge.Domain;

namespace CommandForge.Application;

/// <summary>
/// Builds the exact command line shown to the user before running (transparency —
/// see docs/Security.md §1). Display only; execution uses an argument array, not this string.
/// </summary>
public static class CommandPreview
{
    /// <summary>Returns <c>executable args</c>, or just the executable when there are no args.</summary>
    public static string Build(CommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var args = command.ArgsTemplate.Trim();
        return args.Length == 0 ? command.Executable : $"{command.Executable} {args}";
    }
}
