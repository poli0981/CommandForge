using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Stores user-defined recipes (named command chains) locally (golden rule: no telemetry).
/// Recipes reference only vetted catalog command ids — never raw command lines.
/// </summary>
public interface IRecipeStore
{
    /// <summary>All saved recipes, in stored order.</summary>
    public IReadOnlyList<Recipe> GetAll();

    /// <summary>Creates or overwrites a recipe (matched by name, case-insensitive) and persists.</summary>
    public void Save(Recipe recipe);

    /// <summary>Deletes the recipe <paramref name="name"/> (no-op if absent) and persists.</summary>
    public void Delete(string name);
}
