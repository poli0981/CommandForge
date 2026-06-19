using CommandForge.Domain;
using CommandForge.Infrastructure.History;

namespace CommandForge.Tests;

/// <summary>Verifies execution history persists to / loads from history.json, with graceful fallbacks.</summary>
public sealed class ExecutionHistoryRoundTripTests
{
    private static ExecutionRecord Rec(string id, bool success = true) => new()
    {
        CommandId = id,
        Timestamp = new DateTimeOffset(2026, 6, 19, 10, 30, 0, TimeSpan.Zero),
        ExitCode = success ? 0 : 1,
        Success = success,
        DurationMs = 1234,
        RequiresRestart = false,
    };

    [Fact]
    public void Record_Persists_And_Reloads_NewestFirst()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-history-{Guid.NewGuid():N}.json");
        try
        {
            var writer = new JsonExecutionHistoryService(path);
            writer.Record(Rec("a"));
            writer.Record(Rec("b", success: false));

            var records = new JsonExecutionHistoryService(path).GetRecent();

            Assert.Equal(["b", "a"], records.Select(r => r.CommandId));
            Assert.False(records[0].Success);
            Assert.Equal(1, records[0].ExitCode);
            Assert.Equal(1234, records[0].DurationMs);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Clear_EmptiesHistory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-history-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new JsonExecutionHistoryService(path);
            svc.Record(Rec("a"));

            svc.Clear();

            Assert.Empty(svc.GetRecent());
            Assert.Empty(new JsonExecutionHistoryService(path).GetRecent());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CorruptFile_FallsBackToEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-history-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ not valid json ");

            Assert.Empty(new JsonExecutionHistoryService(path).GetRecent());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFile_IsEmpty()
        => Assert.Empty(new JsonExecutionHistoryService(
            Path.Combine(Path.GetTempPath(), $"cf-history-{Guid.NewGuid():N}.json")).GetRecent());
}
