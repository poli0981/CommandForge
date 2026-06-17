using System.IO.Pipes;
using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Domain;
using CommandForge.Infrastructure.Elevation;

namespace CommandForge.Tests;

/// <summary>
/// Tests the broker's pipe protocol and the elevated <see cref="ElevationServer"/> over an
/// in-process named pipe (no real elevation — the spawn/UAC step is verified manually).
/// </summary>
public sealed class ElevationTests
{
    [Fact]
    public async Task PipeMessageChannel_RoundTrips_Request()
    {
        using var stream = new MemoryStream();
        using var channel = new PipeMessageChannel(stream);

        await channel.WriteAsync(
            new ElevationRequest { Kind = ElevationRequestKind.Run, CommandId = "dism.checkhealth" },
            ElevationJsonContext.Default.ElevationRequest,
            CancellationToken.None);

        stream.Position = 0;
        var read = await channel.ReadAsync(ElevationJsonContext.Default.ElevationRequest, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal(ElevationRequestKind.Run, read!.Kind);
        Assert.Equal("dism.checkhealth", read.CommandId);
    }

    [Fact]
    public async Task PipeMessageChannel_RoundTrips_Message()
    {
        using var stream = new MemoryStream();
        using var channel = new PipeMessageChannel(stream);

        await channel.WriteAsync(
            ElevationMessage.Output("hello", isError: true),
            ElevationJsonContext.Default.ElevationMessage,
            CancellationToken.None);

        stream.Position = 0;
        var read = await channel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal(ElevationMessageKind.Output, read!.Kind);
        Assert.Equal("hello", read.Text);
        Assert.True(read.IsError);
    }

    [Fact]
    public async Task ElevationServer_RunsAdminCommand_StreamsOutputThenResult()
    {
        var command = AdminCommand("adm");
        var (uiChannel, serverTask, client, server) = await StartServerAsync(
            new FakeCatalog(command),
            new FakeExecutor([new OutputLine("line-1", false)], Result(exitCode: 0, success: true)));

        await using (client)
        await using (server)
        {
            await uiChannel.WriteAsync(
                new ElevationRequest { Kind = ElevationRequestKind.Run, CommandId = "adm" },
                ElevationJsonContext.Default.ElevationRequest,
                CancellationToken.None);

            var output = await uiChannel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, CancellationToken.None);
            Assert.Equal(ElevationMessageKind.Output, output!.Kind);
            Assert.Equal("line-1", output.Text);

            var result = await uiChannel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, CancellationToken.None);
            Assert.Equal(ElevationMessageKind.Result, result!.Kind);
            Assert.True(result.Success);
            Assert.Equal(0, result.ExitCode);

            uiChannel.Dispose();
            client.Dispose();
            await serverTask;
        }
    }

    [Fact]
    public async Task ElevationServer_RejectsUnknownCommand()
    {
        var (uiChannel, serverTask, client, server) = await StartServerAsync(
            new FakeCatalog(AdminCommand("adm")),
            new FakeExecutor([], Result(0, true)));

        await using (client)
        await using (server)
        {
            await uiChannel.WriteAsync(
                new ElevationRequest { Kind = ElevationRequestKind.Run, CommandId = "does-not-exist" },
                ElevationJsonContext.Default.ElevationRequest,
                CancellationToken.None);

            var rejection = await uiChannel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, CancellationToken.None);
            Assert.Equal(ElevationMessageKind.Output, rejection!.Kind);
            Assert.True(rejection.IsError);

            var result = await uiChannel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, CancellationToken.None);
            Assert.Equal(ElevationMessageKind.Result, result!.Kind);
            Assert.False(result.Success);

            uiChannel.Dispose();
            client.Dispose();
            await serverTask;
        }
    }

    private static async Task<(PipeMessageChannel UiChannel, Task ServerTask, NamedPipeClientStream Client, NamedPipeServerStream Server)>
        StartServerAsync(ICatalogProvider catalog, ICommandExecutor executor)
    {
        var pipeName = "CommandForge.Test." + Guid.NewGuid().ToString("N");
        var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        var waitConnection = server.WaitForConnectionAsync();
        await client.ConnectAsync(5000);
        await waitConnection;

        var serverTask = new ElevationServer(catalog, executor).RunAsync(server, CancellationToken.None);
        return (new PipeMessageChannel(client), serverTask, client, server);
    }

    private static CommandDefinition AdminCommand(string id) => new()
    {
        Id = id,
        CategoryId = "c",
        TitleKey = "t",
        DescriptionKey = "d",
        Executable = "x",
        RequiresAdmin = true,
    };

    private static ExecutionResult Result(int exitCode, bool success)
        => new() { ExitCode = exitCode, Success = success, Duration = TimeSpan.Zero };

    private sealed class FakeCatalog : ICatalogProvider
    {
        private readonly IReadOnlyList<CommandDefinition> _commands;

        public FakeCatalog(params CommandDefinition[] commands) => _commands = commands;

        public IReadOnlyList<CommandCategory> GetCategories() => [];

        public IReadOnlyList<CommandDefinition> GetCommands() => _commands;
    }

    private sealed class FakeExecutor : ICommandExecutor
    {
        private readonly IReadOnlyList<OutputLine> _lines;
        private readonly ExecutionResult _result;

        public FakeExecutor(IReadOnlyList<OutputLine> lines, ExecutionResult result)
        {
            _lines = lines;
            _result = result;
        }

        public async Task<ExecutionResult> ExecuteAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken)
        {
            foreach (var line in _lines)
            {
                await output.WriteAsync(line, CancellationToken.None);
            }

            output.TryComplete();
            return _result;
        }
    }
}
