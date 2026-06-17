using System.Text.Json.Serialization;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>Request sent UI → Elevator over the pipe.</summary>
internal enum ElevationRequestKind
{
    Run,
    Cancel,
}

/// <summary>Message sent Elevator → UI over the pipe.</summary>
internal enum ElevationMessageKind
{
    Output,
    Result,
}

/// <summary>A request from the UI. Only a <see cref="CommandId"/> is ever sent — never a command line.</summary>
internal sealed record ElevationRequest
{
    public ElevationRequestKind Kind { get; init; }

    public string? CommandId { get; init; }
}

/// <summary>A streamed output line or the final result.</summary>
internal sealed record ElevationMessage
{
    public ElevationMessageKind Kind { get; init; }

    public string? Text { get; init; }

    public bool IsError { get; init; }

    public int ExitCode { get; init; }

    public bool Success { get; init; }

    public bool RequiresRestart { get; init; }

    public bool Cancelled { get; init; }

    public long DurationMs { get; init; }

    public static ElevationMessage Output(string text, bool isError)
        => new() { Kind = ElevationMessageKind.Output, Text = text, IsError = isError };

    public static ElevationMessage FromResult(ExecutionResult result)
        => new()
        {
            Kind = ElevationMessageKind.Result,
            ExitCode = result.ExitCode,
            Success = result.Success,
            RequiresRestart = result.RequiresRestart,
            Cancelled = result.Cancelled,
            DurationMs = (long)result.Duration.TotalMilliseconds,
        };
}

/// <summary>Source-generated JSON for the pipe protocol.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ElevationRequest))]
[JsonSerializable(typeof(ElevationMessage))]
internal sealed partial class ElevationJsonContext : JsonSerializerContext;

/// <summary>Pipe naming helper.</summary>
internal static class ElevationProtocol
{
    /// <summary>Creates a unique, per-session pipe name.</summary>
    public static string CreatePipeName() => $"CommandForge.Elevator.{Guid.NewGuid():N}";
}
