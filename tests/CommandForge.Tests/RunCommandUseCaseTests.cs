using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests that commands route to the right executor based on <c>RequiresAdmin</c>.</summary>
public sealed class RunCommandUseCaseTests
{
    [Fact]
    public async Task AdminCommand_RoutesToElevation()
    {
        var executor = new Recording();
        var elevation = new RecordingElevation();
        var useCase = new RunCommandUseCase(executor, elevation);

        await useCase.RunAsync(Command(requiresAdmin: true), Channel.CreateUnbounded<OutputLine>().Writer);

        Assert.True(elevation.Called);
        Assert.False(executor.Called);
    }

    [Fact]
    public async Task NonAdminCommand_RoutesToExecutor()
    {
        var executor = new Recording();
        var elevation = new RecordingElevation();
        var useCase = new RunCommandUseCase(executor, elevation);

        await useCase.RunAsync(Command(requiresAdmin: false), Channel.CreateUnbounded<OutputLine>().Writer);

        Assert.True(executor.Called);
        Assert.False(elevation.Called);
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

        public Task<ExecutionResult> ExecuteAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken)
        {
            Called = true;
            output.TryComplete();
            return Task.FromResult(Ok());
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
            return Task.FromResult(Ok());
        }
    }
}
