using System.Threading.Channels;
using CommandForge.Domain;
using CommandForge.Infrastructure.Execution;

namespace CommandForge.Tests;

/// <summary>Tests for the process execution engine using a fake process runner (no real processes).</summary>
public sealed class ExecutionTests
{
    private static CommandDefinition Command(
        RestartPolicy restart = RestartPolicy.No,
        string? restartRegex = null,
        ExecutionMode mode = ExecutionMode.Capture)
        => new()
        {
            Id = "x",
            CategoryId = "c",
            TitleKey = "t",
            DescriptionKey = "d",
            Executable = "x",
            Restart = restart,
            RestartRegex = restartRegex,
            ExecutionMode = mode,
        };

    private static async Task<(ExecutionResult Result, List<OutputLine> Lines)> RunAsync(
        IProcessRunner runner,
        CommandDefinition command)
    {
        var executor = new ProcessCommandExecutor(runner);
        var output = Channel.CreateUnbounded<OutputLine>();

        var execution = executor.ExecuteAsync(command, output.Writer, CancellationToken.None);

        var lines = new List<OutputLine>();
        await foreach (var line in output.Reader.ReadAllAsync(CancellationToken.None))
        {
            lines.Add(line);
        }

        var result = await execution;
        return (result, lines);
    }

    [Fact]
    public async Task Executor_ExitZero_Succeeds()
    {
        var runner = new FakeProcessRunner(exitCode: 0, [new OutputLine("hello", false)]);

        var (result, lines) = await RunAsync(runner, Command());

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.False(result.RequiresRestart);
        Assert.Equal("hello", Assert.Single(lines).Text);
    }

    [Fact]
    public async Task Executor_StreamsLines_InOrder()
    {
        var runner = new FakeProcessRunner(exitCode: 0,
            [new OutputLine("a", false), new OutputLine("b", false), new OutputLine("c", false)]);

        var (_, lines) = await RunAsync(runner, Command());

        Assert.Equal(["a", "b", "c"], lines.Select(l => l.Text));
    }

    [Fact]
    public async Task Executor_ExitCode3010_SetsRequiresRestart()
    {
        var runner = new FakeProcessRunner(exitCode: 3010);

        var (result, _) = await RunAsync(runner, Command(restart: RestartPolicy.FromExitCode));

        Assert.True(result.RequiresRestart);
        Assert.True(result.Success); // 3010 is a success-with-reboot code
    }

    [Fact]
    public async Task Executor_RestartAlways_SetsRequiresRestart()
    {
        var runner = new FakeProcessRunner(exitCode: 0);

        var (result, _) = await RunAsync(runner, Command(restart: RestartPolicy.Always));

        Assert.True(result.RequiresRestart);
    }

    [Fact]
    public async Task Executor_FromOutputRegex_Match_SetsRequiresRestart()
    {
        var runner = new FakeProcessRunner(exitCode: 0, [new OutputLine("Please reboot now.", false)]);

        var (result, _) = await RunAsync(runner, Command(restart: RestartPolicy.FromOutputRegex, restartRegex: "reboot"));

        Assert.True(result.RequiresRestart);
    }

    [Fact]
    public async Task Executor_FromOutputRegex_NoMatch_DoesNotSetRestart()
    {
        var runner = new FakeProcessRunner(exitCode: 0, [new OutputLine("all good", false)]);

        var (result, _) = await RunAsync(runner, Command(restart: RestartPolicy.FromOutputRegex, restartRegex: "reboot"));

        Assert.False(result.RequiresRestart);
    }

    [Fact]
    public async Task Executor_LaunchMode_CallsLaunch_WithoutCapture()
    {
        var runner = new FakeProcessRunner(exitCode: 0, [new OutputLine("should not stream", false)]);

        var (result, _) = await RunAsync(runner, Command(mode: ExecutionMode.Launch));

        Assert.True(runner.Launched);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Executor_Cancel_MarksCancelled()
    {
        var runner = new FakeProcessRunner(exitCode: 0, honorCancel: true);
        var executor = new ProcessCommandExecutor(runner);
        var output = Channel.CreateUnbounded<OutputLine>();
        using var cts = new CancellationTokenSource();

        var execution = executor.ExecuteAsync(Command(), output.Writer, cts.Token);
        var drain = Task.Run(async () =>
        {
            await foreach (var _ in output.Reader.ReadAllAsync(CancellationToken.None))
            {
            }
        });

        cts.CancelAfter(50);
        var result = await execution;
        await drain;

        Assert.True(result.Cancelled);
        Assert.False(result.Success);
        Assert.True(runner.SawCancellation);
    }

    [Fact]
    public async Task Executor_RealProcess_CapturesEchoOutput()
    {
        // Smoke test of the real runner against a harmless built-in command.
        var command = new CommandDefinition
        {
            Id = "echo",
            CategoryId = "c",
            TitleKey = "t",
            DescriptionKey = "d",
            Executable = "cmd",
            ArgsTemplate = "/c echo CommandForgeTest",
        };

        var (result, lines) = await RunAsync(new SystemProcessRunner(), command);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(lines, line => line.Text.Contains("CommandForgeTest", StringComparison.Ordinal));
    }

    /// <summary>A fake <see cref="IProcessRunner"/> that emits predetermined lines and exit code.</summary>
    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly int _exitCode;
        private readonly IReadOnlyList<OutputLine> _lines;
        private readonly bool _honorCancel;

        public FakeProcessRunner(int exitCode, IReadOnlyList<OutputLine>? lines = null, bool honorCancel = false)
        {
            _exitCode = exitCode;
            _lines = lines ?? [];
            _honorCancel = honorCancel;
        }

        public bool Launched { get; private set; }

        public bool SawCancellation { get; private set; }

        public async Task<int> RunAsync(
            string fileName,
            string arguments,
            ChannelWriter<OutputLine> output,
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var line in _lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await output.WriteAsync(line, CancellationToken.None);
                }

                if (_honorCancel)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }

                return _exitCode;
            }
            catch (OperationCanceledException)
            {
                SawCancellation = true;
                throw;
            }
            finally
            {
                output.TryComplete();
            }
        }

        public void Launch(string fileName, string arguments) => Launched = true;
    }
}
