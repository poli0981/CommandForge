using System.Text.Json.Serialization;
using CommandForge.Application.Settings;

namespace CommandForge.Infrastructure.Portability;

/// <summary>One named profile entry stored in <c>profiles.json</c>.</summary>
internal sealed record ProfileEntry
{
    public required string Name { get; init; }

    public required PortableSettings Settings { get; init; }
}

/// <summary>Persisted shape of <c>profiles.json</c>: a schema version plus the named profiles.</summary>
internal sealed record ProfilesFile
{
    public int Version { get; init; } = 1;

    public IReadOnlyList<ProfileEntry> Profiles { get; init; } = [];
}

/// <summary>
/// System.Text.Json source-generation context for portable settings: import/export files
/// (<see cref="PortableSettings"/>) and the profile store (<see cref="ProfilesFile"/>).
/// camelCase, enums as strings, indented.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(PortableSettings))]
[JsonSerializable(typeof(ProfilesFile))]
internal sealed partial class PortableSettingsJsonContext : JsonSerializerContext;
