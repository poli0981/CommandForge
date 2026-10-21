using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests routing by <c>RequiresAdmin</c>, and that a run always ends (fault, cancel) with the output channel completed.</summary>
public sealed class RunCommandUseCaseTests
{
    [Fact]
    public async Task AdminCommand_RoutesToElevation()
    {
        var executor = new Recording();
        var elevation = new RecordingElevation();
        var useCase = new RunCommandUseCase(executor, elevation);

        await useCase.RunAsync(Command(requiresAdmin: true), Channel.CreateUnbounded<OutputLine>().Writer, TestContext.Current.CancellationToken);

        Assert.True(elevation.Called);
        Assert.False(executor.Called);
    }

    [Fact]
    public async Task NonAdminCommand_RoutesToExecutor()
    {
        var executor = new Recording();
        var elevation = new RecordingElevation();
        var useCase = new RunCommandUseCase(executor, elevation);

        await useCase.RunAsync(Command(requiresAdmin: false), Channel.CreateUnbounded<OutputLine>().Writer, TestContext.Current.CancellationToken);

        Assert.True(executor.Called);
        Assert.False(elevation.Called);
    }

    [Fact(Timeout = 30_000)]
    public async Task ExecutorFault_StillCompletesTheOutputChannel()
    {
        // The UI reads output until the channel completes; a crash must not leave it waiting forever.
        var useCase = new RunCommandUseCase(new Throwing(), new RecordingElevation());
        var channel = Channel.CreateUnbounded<OutputLine>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.RunAsync(Command(requiresAdmin: false), channel.Writer, TestContext.Current.CancellationToken));

        await channel.Reader.Completion.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 30_000)]
    public async Task CancelWhileWaitingForElevation_ReturnsCancelled()
    {
        // e.g. the user cancels while the UAC prompt is still open.
        var useCase = new RunCommandUseCase(new Recording(), new HangingElevation());
        var channel = Channel.CreateUnbounded<OutputLine>();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var run = useCase.RunAsync(Command(requiresAdmin: true), channel.Writer, cancel.Token);
        await cancel.CancelAsync();
        var result = await run;

        Assert.True(result.Cancelled);
        Assert.False(result.Success);
        await channel.Reader.Completion.WaitAsync(TestContext.Current.CancellationToken);
    }

    private static CommandDefinition Command(bool requiresAdmin) => new()
    {
        Id = "x",
        CategoryId = "c",
        TitleKey = "t",
        DescriptionKey = "d",
        Executable = "x",
        RequiresAdmin = requiresAdmin,
    };

    private static ExecutionResult Ok() => new() { ExitCode = 0, Success = true, Duration = TimeSpan.Zero };

    private sealed class Recording : ICommandExecutor
    {
        public bool Called { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken = default)
        {
            Called = true;
            output.TryComplete();
            return Task.FromResult(Ok());
        }
    }

    private sealed class RecordingElevation : IElevationService
    {
        public bool Called { get; private set; }

        public bool IsSessionActive => false;

        public event EventHandler? SessionChanged
        {
            add
            {
                // A fake session never starts or ends.
            }

            remove
            {
                // A fake session never starts or ends.
            }
        }

        public Task EndSessionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ExecutionResult> RunElevatedAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken = default)
        {
            Called = true;
            output.TryComplete();
            return Task.FromResult(Ok());
        }
    }

    private sealed class Throwing : ICommandExecutor
    {
        public Task<ExecutionResult> ExecuteAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("executor failed");
    }

    private sealed class HangingElevation : IElevationService
    {
        public bool IsSessionActive => false;

        public event EventHandler? SessionChanged
        {
            add
            {
                // A fake session never starts or ends.
            }

            remove
            {
                // A fake session never starts or ends.
            }
        }

        public Task EndSessionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task<ExecutionResult> RunElevatedAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Ok();
        }
    }
}
