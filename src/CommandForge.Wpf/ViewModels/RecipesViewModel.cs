using System.Collections.ObjectModel;
using System.Windows;
using CommandForge.Application.Ports;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Recipes screen: build a named chain of catalog commands and run it sequentially (one
/// aggregated confirmation, one UAC for an all-admin chain, stop-on-error). Steps reference the
/// shared catalog <see cref="CommandItemViewModel"/> instances.
/// </summary>
public sealed partial class RecipesViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<string, CommandItemViewModel> _itemsById;
    private readonly ExecutionViewModel _execution;
    private readonly IRecipeStore _store;
    private readonly IExecutionHistoryService _history;
    private readonly IClock _clock;

    public RecipesViewModel(
        IReadOnlyDictionary<string, CommandItemViewModel> itemsById,
        ExecutionViewModel execution,
        IRecipeStore store,
        IExecutionHistoryService history,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(clock);

        _itemsById = itemsById;
        _execution = execution;
        _store = store;
        _history = history;
        _clock = clock;

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
        foreach (var id in recipe.CommandIds)
        {
            if (_itemsById.TryGetValue(id, out var item))
            {
                Steps.Add(item);
            }
        }
    }

    [RelayCommand]
    private void NewRecipe()
    {
        SelectedRecipeName = null;
        RecipeName = string.Empty;
        Steps.Clear();
    }

    [RelayCommand]
    private void AddStep()
    {
        if (CommandToAdd is not null)
        {
            Steps.Add(CommandToAdd);
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

        _store.Save(new Recipe { Name = name, CommandIds = Steps.Select(s => s.Command.Id).ToList() });
        Refresh();
        SelectedRecipeName = name;
    }

    private bool CanSaveRecipe() => !string.IsNullOrWhiteSpace(RecipeName) && Steps.Count > 0;

    [RelayCommand(CanExecute = nameof(HasSelectedRecipe))]
    private void DeleteRecipe()
    {
        if (SelectedRecipeName is not { } name)
        {
            return;
        }

        if (MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.Get("Recipe_DeleteConfirm"), name),
                Strings.Get("Recipe_Delete"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
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

        if (!ConfirmRecipe())
        {
            return;
        }

        var executed = await _execution.RunRecipeAsync(steps);

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

    /// <summary>Shows a single aggregated confirmation listing all steps (a warning if any is Dangerous).</summary>
    private bool ConfirmRecipe()
    {
        var anyDangerous = Steps.Any(s => s.DangerLevel == DangerLevel.Dangerous);
        var list = string.Join(Environment.NewLine, Steps.Select((s, i) => $"{i + 1}. {s.Title}"));
        var header = string.IsNullOrWhiteSpace(RecipeName)
            ? string.Empty
            : RecipeName + Environment.NewLine + Environment.NewLine;
        var message = header + list;
        if (anyDangerous)
        {
            message += Environment.NewLine + Environment.NewLine + Strings.Get("Recipe_ContainsDangerous");
        }

        return MessageBox.Show(
            message,
            Strings.Get("Recipe_ConfirmTitle"),
            MessageBoxButton.YesNo,
            anyDangerous ? MessageBoxImage.Warning : MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
