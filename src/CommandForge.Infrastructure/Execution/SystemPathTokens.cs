using CommandForge.Application;

namespace CommandForge.Infrastructure.Execution;

/// <summary>Values for <see cref="ArgumentTokens"/>, taken from <see cref="SystemPaths"/> (trusted sources only).</summary>
public static class SystemPathTokens
{
    /// <summary>Token → value map used by the executor and by the command preview.</summary>
    public static IReadOnlyDictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ArgumentTokens.SystemDrive] = SystemPaths.SystemDrive,
        [ArgumentTokens.SystemRoot] = SystemPaths.WindowsDirectory,
        [ArgumentTokens.UserProfile] = SystemPaths.UserProfile,
    };
}
