using System.Text.Json.Serialization;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.History;

/// <summary>
/// Persisted shape of <c>history.json</c>: a schema <see cref="Version"/> (for forward-compat)
/// plus the records, newest first.
/// </summary>
internal sealed record HistoryFile
{
    /// <summary>Schema version of the file.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Recorded executions, newest first.</summary>
    public IReadOnlyList<ExecutionRecord> Records { get; init; } = [];
}

/// <summary>
/// System.Text.Json source-generation context for <c>history.json</c> (camelCase, indented).
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true)]
[JsonSerializable(typeof(HistoryFile))]
internal sealed partial class ExecutionHistoryJsonContext : JsonSerializerContext;
