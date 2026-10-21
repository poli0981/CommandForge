using System.Threading.Channels;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Domain;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.History;
using CommandForge.Infrastructure.Recipes;
using CommandForge.Wpf.Services;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Tests;

/// <summary>
/// A recipe must never weaken the per-command safety gate (golden rule #8): every risky step gets the
/// confirmation a single run would, and cancelling any of them runs nothing.
/// </summary>
public sealed class RecipeConfirmationTests : IDisposable
{
    private static readonly CommandDefinition SafeStep = Step("t.safe", DangerLevel.Safe, admin: false);
    private static readonly CommandDefinition CautionStep = Step("t.caution", DangerLevel.Caution, admin: false);
    private static readonly CommandDefinition DangerousStep = Step("t.dangerous", DangerLevel.Dangerous, admin: true);
    private static readonly CommandDefinition RestorePoint = Step(RunSafety.RestorePointCommandId, DangerLevel.Safe, admin: true);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"cf-recipe-{Guid.NewGuid():N}");
    private readonly List<string> _executed = [];
    private readonly FakeDialogs _dialogs = new();
    private readonly FakeConfirmation _confirmation = new();
    private readonly JsonSettingsService _settings;

    public RecipeConfirmationTests()
    {
        Directory.CreateDirectory(_directory);
        _settings = new JsonSettingsService(Path.Combine(_directory, "config.json"));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task DecliningTheOverview_AsksNothingElseAndRunsNothing()
    {
        _dialogs.Answer = false;

        await RunRecipeAsync(SafeStep, CautionStep, DangerousStep);

        Assert.Empty(_confirmation.Asked);
        Assert.Empty(_executed);
    }

    [Fact]
    public async Task EveryRiskyStep_IsConfirmedLikeASingleRun()
    {
        await RunRecipeAsync(SafeStep, CautionStep, DangerousStep);

        Assert.Equal([CautionStep.Id, DangerousStep.Id], _confirmation.Asked);
        Assert.Equal([SafeStep.Id, CautionStep.Id, DangerousStep.Id], _executed);
    }

    [Fact]
    public async Task CancellingOneStep_RunsNoStepAtAll()
    {
        _confirmation.Cancel.Add(DangerousStep.Id);

        await RunRecipeAsync(SafeStep, CautionStep, DangerousStep);

        Assert.Empty(_executed);
    }

    [Fact]
    public async Task TurningOffCautionPrompts_StillConfirmsDangerousSteps()
    {
        _settings.ConfirmCaution = false;

        await RunRecipeAsync(CautionStep, DangerousStep);

        Assert.Equal([DangerousStep.Id], _confirmation.Asked);
    }

    [Fact]
    public async Task ARequestedRestorePoint_IsCreatedOnceBeforeTheFirstStep()
    {
        _confirmation.CreateRestorePoint = true;

        await RunRecipeAsync(CautionStep, DangerousStep);

        Assert.Equal([RestorePoint.Id, CautionStep.Id, DangerousStep.Id], _executed);
    }

    private async Task RunRecipeAsync(params CommandDefinition[] steps)
    {
        var items = new[] { SafeStep, CautionStep, DangerousStep }
            .ToDictionary(c => c.Id, c => new CommandItemViewModel(c, c.Id, string.Empty), StringComparer.Ordinal);
        var execution = new ExecutionViewModel(
            new RunCommandUseCase(new RecordingExecutor(_executed), new RecordingElevation(_executed)),
            _dialogs,
            new NoClipboard());
        var recipes = new RecipesViewModel(
            items,
            execution,
            new JsonRecipeStore(Path.Combine(_directory, "recipes.json")),
            new JsonExecutionHistoryService(Path.Combine(_directory, "history.json")),
            new SystemClock(),
            _confirmation,
            _dialogs,
            _settings,
            RestorePoint);

        foreach (var step in steps)
        {
            recipes.Steps.Add(items[step.Id]);
        }

        // Fakes answer instantly; the time limit only turns a regression that hangs into a failure.
        await recipes.RunRecipeCommand.ExecuteAsync(null).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
    }

    private static CommandDefinition Step(string id, DangerLevel level, bool admin) => new()
    {
        Id = id,
        CategoryId = "c",
        TitleKey = "Recipe_ConfirmTitle",
        DescriptionKey = "Recipe_ConfirmTitle",
        Executable = "x",
        DangerLevel = level,
        ConfirmBeforeRun = level != DangerLevel.Safe,
        RequiresAdmin = admin,
    };

    private static ExecutionResult Ok() => new() { ExitCode = 0, Success = true, Duration = TimeSpan.Zero };

    private sealed class FakeDialogs : IDialogService
    {
        public bool Answer { get; set; } = true;

        public Task<bool> ConfirmAsync(string title, string message, DialogKind kind = DialogKind.Warning) => Task.FromResult(Answer);

        public Task ShowMessageAsync(string title, string message, DialogKind kind = DialogKind.Information) => Task.CompletedTask;
    }

    private sealed class FakeConfirmation : IConfirmationService
    {
        public List<string> Asked { get; } = [];

        public HashSet<string> Cancel { get; } = [];

        public bool CreateRestorePoint { get; set; }

        public Task<ConfirmationOutcome?> ConfirmAsync(CommandDefinition command, bool defaultCreateRestorePoint)
        {
            Asked.Add(command.Id);
            return Task.FromResult(Cancel.Contains(command.Id) ? null : new ConfirmationOutcome(CreateRestorePoint));
        }
    }

    private sealed class RecordingExecutor(List<string> executed) : ICommandExecutor
    {
        public Task<ExecutionResult> ExecuteAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken = default)
        {
            lock (executed)
            {
                executed.Add(command.Id);
            }

            output.TryComplete();
            return Task.FromResult(Ok());
        }
    }

    private sealed class RecordingElevation(List<string> executed) : IElevationService
    {
        public bool IsSessionActive => false;

        public Task EndSessionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ExecutionResult> RunElevatedAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken = default)
        {
            lock (executed)
            {
                executed.Add(command.Id);
            }

            output.TryComplete();
            return Task.FromResult(Ok());
        }
    }

    private sealed class NoClipboard : IClipboardService
    {
        public bool TrySetText(string text) => true;
    }
}
