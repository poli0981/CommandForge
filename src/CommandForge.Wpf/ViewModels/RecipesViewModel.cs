using System.Collections.ObjectModel;
using System.Globalization;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// Recipes screen: build a named chain of catalog commands and run it sequentially (an overview
/// prompt, then the same per-command confirmation a single run would get, one UAC for an all-admin
/// chain, stop-on-error). Steps reference the shared catalog <see cref="CommandItemViewModel"/> instances.
/// </summary>
public sealed partial class RecipesViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<string, CommandItemViewModel> _itemsById;
    private readonly ExecutionViewModel _execution;
    private readonly IRecipeStore _store;
    private readonly IExecutionHistoryService _history;
    private readonly IClock _clock;
    private readonly IConfirmationService _confirmation;
    private readonly IDialogService _dialogs;
    private readonly ISettingsService _settings;
    private readonly CommandDefinition? _restorePointCommand;

    // Steps of the loaded recipe that this PC doesn't offer (unknown id or another Windows build):
    // kept on save so a recipe shared between machines never silently loses them.
    private List<string> _unavailableStepIds = [];

    public RecipesViewModel(
        IReadOnlyDictionary<string, CommandItemViewModel> itemsById,
        ExecutionViewModel execution,
        IRecipeStore store,
        IExecutionHistoryService history,
        IClock clock,
        IConfirmationService confirmation,
        IDialogService dialogs,
        ISettingsService settings,
        CommandDefinition? restorePointCommand)
    {
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(settings);

        _itemsById = itemsById;
        _execution = execution;
        _store = store;
        _history = history;
        _clock = clock;
        _confirmation = confirmation;
        _dialogs = dialogs;
        _settings = settings;
        _restorePointCommand = restorePointCommand;

        AllCommands = itemsById.Values.OrderBy(i => i.Title, StringComparer.CurrentCulture).ToList();

        // Re-evaluate Save/Run availability when the step list changes.
        Steps.CollectionChanged += (_, _) =>
        {
            SaveRecipeCommand.NotifyCanExecuteChanged();
            RunRecipeCommand.NotifyCanExecuteChanged();
        };

        // Disable Run while any execution is in progress (shared console).
        _execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
            {
                RunRecipeCommand.NotifyCanExecuteChanged();
            }
        };

        Refresh();
    }

    /// <summary>The shared execution view-model — drives the recipe console output panel.</summary>
    public ExecutionViewModel Execution => _execution;

    /// <summary>Names of saved recipes.</summary>
    public ObservableCollection<string> RecipeNames { get; } = [];

    /// <summary>All catalog commands (for the add-step picker), sorted by title.</summary>
    public IReadOnlyList<CommandItemViewModel> AllCommands { get; }

    /// <summary>The steps of the recipe currently being edited, in order.</summary>
    public ObservableCollection<CommandItemViewModel> Steps { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRecipeCommand))]
    private string? _selectedRecipeName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveRecipeCommand))]
    private string _recipeName = string.Empty;

    [ObservableProperty]
    private CommandItemViewModel? _commandToAdd;

    /// <summary>Shown when the loaded recipe has steps this PC doesn't offer (null when none).</summary>
    [ObservableProperty]
    private string? _unavailableStepsNotice;

    /// <summary>Reloads the saved-recipe name list from the store.</summary>
    public void Refresh()
    {
        var previous = SelectedRecipeName;
        RecipeNames.Clear();
        foreach (var recipe in _store.GetAll())
        {
            RecipeNames.Add(recipe.Name);
        }

        if (previous is not null && RecipeNames.Contains(previous))
        {
            SelectedRecipeName = previous;
        }
    }

    partial void OnSelectedRecipeNameChanged(string? value)
    {
        if (value is null)
        {
            return;
        }

        var recipe = _store.GetAll().FirstOrDefault(r => string.Equals(r.Name, value, StringComparison.OrdinalIgnoreCase));
        if (recipe is null)
        {
            return;
        }

        RecipeName = recipe.Name;
        Steps.Clear();
        var unavailable = new List<string>();
        foreach (var id in recipe.CommandIds)
        {
            if (_itemsById.TryGetValue(id, out var item))
            {
                Steps.Add(item);
            }
            else
            {
                unavailable.Add(id);
            }
        }

        SetUnavailableSteps(unavailable);
    }

    [RelayCommand]
    private void NewRecipe()
    {
        SetUnavailableSteps([]);
        SelectedRecipeName = null;
        RecipeName = string.Empty;
        Steps.Clear();
    }

    [RelayCommand]
    private void AddStep()
    {
        // Avoid duplicate steps — a recipe runs each command once (don't run one command N times).
        if (CommandToAdd is { } command && !Steps.Contains(command))
        {
            Steps.Add(command);
        }
    }

    [RelayCommand]
    private void RemoveStep(CommandItemViewModel? step)
    {
        if (step is not null)
        {
            Steps.Remove(step);
        }
    }

    [RelayCommand]
    private void MoveStepUp(CommandItemViewModel? step)
    {
        var index = step is null ? -1 : Steps.IndexOf(step);
        if (index > 0)
        {
            Steps.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveStepDown(CommandItemViewModel? step)
    {
        var index = step is null ? -1 : Steps.IndexOf(step);
        if (index >= 0 && index < Steps.Count - 1)
        {
            Steps.Move(index, index + 1);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveRecipe))]
    private void SaveRecipe()
    {
        var name = RecipeName.Trim();
        if (name.Length == 0 || Steps.Count == 0)
        {
            return;
        }

        _store.Save(new Recipe { Name = name, CommandIds = [.. Steps.Select(s => s.Command.Id), .. _unavailableStepIds] });
        Refresh();
        SelectedRecipeName = name;
    }

    private bool CanSaveRecipe() => !string.IsNullOrWhiteSpace(RecipeName) && Steps.Count > 0;

    [RelayCommand(CanExecute = nameof(HasSelectedRecipe))]
    private async Task DeleteRecipeAsync()
    {
        if (SelectedRecipeName is not { } name)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            Strings.Get("Recipe_Delete"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Recipe_DeleteConfirm"), name));
        if (!confirmed)
        {
            return;
        }

        _store.Delete(name);
        SelectedRecipeName = null;
        NewRecipe();
        Refresh();
    }

    private bool HasSelectedRecipe() => SelectedRecipeName is not null;

    [RelayCommand(CanExecute = nameof(CanRunRecipe))]
    private async Task RunRecipeAsync()
    {
        var steps = Steps.Select(s => s.Command).ToList();
        if (steps.Count == 0 || _execution.IsRunning)
        {
            return;
        }

        // 1. Overview of every step and its risk ("No" is the default answer).
        var anyDangerous = steps.Any(s => s.DangerLevel == DangerLevel.Dangerous);
        if (!await _dialogs.ConfirmAsync(Strings.Get("Recipe_ConfirmTitle"), BuildOverview(), anyDangerous ? DialogKind.Warning : DialogKind.Information))
        {
            return;
        }

        // 2. Each risky step gets exactly the confirmation a single run would (typing the name for
        //    Dangerous): a recipe must never weaken the per-command safety gate (golden rule #8).
        var createRestorePoint = false;
        foreach (var step in steps)
        {
            if (!RunSafety.NeedsConfirmation(step, _settings.ConfirmCaution))
            {
                createRestorePoint |= RunSafety.DefaultCreateRestorePoint(step, _settings.AutoCreateRestorePoint);
                continue;
            }

            var outcome = await _confirmation.ConfirmAsync(step, RunSafety.DefaultCreateRestorePoint(step, _settings.AutoCreateRestorePoint));
            if (outcome is null)
            {
                return;
            }

            createRestorePoint |= outcome.CreateRestorePoint;
        }

        // 3. One restore point before the whole chain, then the steps (stop-on-error).
        var name = string.IsNullOrWhiteSpace(RecipeName) ? Strings.Get("Recipe_ConfirmTitle") : RecipeName.Trim();
        var executed = await _execution.RunRecipeAsync(steps, name, createRestorePoint ? _restorePointCommand : null);

        // Record each finished (non-cancelled) step in the execution history.
        foreach (var (commandId, result) in executed)
        {
            if (result.Cancelled)
            {
                continue;
            }

            _history.Record(new ExecutionRecord
            {
                CommandId = commandId,
                Timestamp = _clock.Now,
                ExitCode = result.ExitCode,
                Success = result.Success,
                DurationMs = (long)result.Duration.TotalMilliseconds,
                RequiresRestart = result.RequiresRestart,
            });
        }
    }

    private bool CanRunRecipe() => Steps.Count > 0 && !_execution.IsRunning;

    /// <summary>The overview: recipe name, then each step with its danger level (and an admin marker).</summary>
    private string BuildOverview()
    {
        var lines = Steps.Select((s, i) => string.Format(
            CultureInfo.CurrentCulture,
            "{0}. {1} \u2014 {2}{3}",
            i + 1,
            s.Title,
            Strings.Get($"Danger_{s.DangerLevel}"),
            s.Command.RequiresAdmin ? " \u00b7 " + Strings.Get("Detail_RequiresAdmin") : string.Empty));
        var header = string.IsNullOrWhiteSpace(RecipeName) ? string.Empty : RecipeName.Trim() + Environment.NewLine + Environment.NewLine;
        var message = header + string.Join(Environment.NewLine, lines);
        if (Steps.Any(s => s.DangerLevel == DangerLevel.Dangerous))
        {
            message += Environment.NewLine + Environment.NewLine + Strings.Get("Recipe_ContainsDangerous");
        }

        return message;
    }

    private void SetUnavailableSteps(List<string> ids)
    {
        _unavailableStepIds = ids;
        UnavailableStepsNotice = ids.Count == 0
            ? null
            : string.Format(CultureInfo.CurrentCulture, Strings.Get("Recipe_UnavailableSteps"), ids.Count);
    }
}
