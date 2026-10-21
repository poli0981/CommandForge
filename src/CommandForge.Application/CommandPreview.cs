using CommandForge.Domain;

namespace CommandForge.Application;

/// <summary>
/// Builds the exact command line shown to the user before running (transparency —
/// see docs/Security.md §1). The executor passes the same argument string to the process.
/// </summary>
public static class CommandPreview
{
    /// <summary>Returns <c>executable args</c> with path tokens left as placeholders.</summary>
    public static string Build(CommandDefinition command)
        => Build(command, new Dictionary<string, string>());

    /// <summary>Returns <c>executable args</c> with <see cref="ArgumentTokens"/> replaced by <paramref name="tokens"/>.</summary>
    public static string Build(CommandDefinition command, IReadOnlyDictionary<string, string> tokens)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(tokens);

        var args = ArgumentTokens.Expand(command.ArgsTemplate, tokens).Trim();
        return args.Length == 0 ? command.Executable : $"{command.Executable} {args}";
    }
}
