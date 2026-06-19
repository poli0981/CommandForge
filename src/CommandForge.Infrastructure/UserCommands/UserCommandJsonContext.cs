using System.Text.Json.Serialization;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.UserCommands;

/// <summary>Persisted shape of <c>user-commands.json</c>: a schema version plus the user commands.</summary>
internal sealed record UserCommandsFile
{
    public int Version { get; init; } = 1;

    public IReadOnlyList<UserCommand> Commands { get; init; } = [];
}

/// <summary>System.Text.Json source-generation context for <c>user-commands.json</c> (camelCase, indented).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(UserCommandsFile))]
internal sealed partial class UserCommandJsonContext : JsonSerializerContext;
