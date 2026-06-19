using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Application.UserCommands;
using CommandForge.Domain;
using CommandForge.Infrastructure.UserCommands;

namespace CommandForge.Tests;

/// <summary>
/// Safety tests for user-defined commands (golden rule #1): they must never elevate, carry the
/// namespaced id (so the Elevator rejects them), and persist locally.
/// </summary>
public sealed class UserCommandsTests
{
    private static UserCommand Sample(string id = "abc") => new()
    {
        Id = id,
        Name = "My tool",
        Executable = "cmd",
        Arguments = "/c echo hi",
    };

    [Fact]
    public void ToDefinition_NeverRequiresAdmin()
        => Assert.False(UserCommandFactory.ToDefinition(Sample()).RequiresAdmin);

    [Fact]
    public void ToDefinition_NamespacesId_SoElevatorRejectsIt()
    {
        var def = UserCommandFactory.ToDefinition(Sample("abc"));

        Assert.StartsWith(UserCommandFactory.IdPrefix, def.Id, StringComparison.Ordinal);
        Assert.Equal("user:abc", def.Id);
    }

    [Fact]
    public async Task UserCommand_RoutesToExecutor_NotElevation()
    {
        var executor = new RecordingExecutor();
        var elevation = new RecordingElevation();
        var useCase = new RunCommandUseCase(executor, elevation);

        await useCase.RunAsync(
            UserCommandFactory.ToDefinition(Sample()),
            Channel.CreateUnbounded<OutputLine>().Writer);

        Assert.True(executor.Called);
        Assert.False(elevation.Called); // never elevated
    }

    [Fact]
    public void Store_Save_Get_Delete_Persists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-usercmd-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonUserCommandStore(path);
            store.Save(Sample("a"));
            store.Save(Sample("b") with { Name = "Second" });

            Assert.Equal(2, store.GetAll().Count);

            var reloaded = new JsonUserCommandStore(path);
            Assert.Equal(2, reloaded.GetAll().Count);
            Assert.Equal("cmd", reloaded.GetAll()[0].Executable);

            reloaded.Delete("a");
            Assert.Single(reloaded.GetAll());
            Assert.DoesNotContain(new JsonUserCommandStore(path).GetAll(), c => c.Id == "a");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class RecordingExecutor : ICommandExecutor
    {
        public bool Called { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken)
        {
            Called = true;
            output.TryComplete();
            return Task.FromResult(new ExecutionResult { ExitCode = 0, Success = true, Duration = TimeSpan.Zero });
        }
    }

    private sealed class RecordingElevation : IElevationService
    {
        public bool Called { get; private set; }

        public bool IsElevationAvailable => true;

        public Task<ExecutionResult> RunElevatedAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken)
        {
            Called = true;
            output.TryComplete();
            return Task.FromResult(new ExecutionResult { ExitCode = 0, Success = true, Duration = TimeSpan.Zero });
        }
    }
}
