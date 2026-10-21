using CommandForge.Application.Ports;
using CommandForge.Domain;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Infrastructure.Recipes;

/// <summary>
/// <see cref="IRecipeStore"/> backed by a local <c>recipes.json</c>. All data stays on the
/// machine (golden rule: no telemetry). Recipe names are matched case-insensitively.
/// </summary>
public sealed class JsonRecipeStore : IRecipeStore
{
    private readonly string _path;
    private readonly Lock _gate = new();
    private readonly List<Recipe> _recipes;

    /// <summary>Creates a store backed by the default <see cref="AppPaths.RecipesFilePath"/>.</summary>
    public JsonRecipeStore()
        : this(AppPaths.RecipesFilePath)
    {
    }

    /// <summary>Creates a store backed by an explicit path (used in tests).</summary>
    public JsonRecipeStore(string path)
    {
        _path = path;
        _recipes = Load(path);
    }

    /// <inheritdoc />
    public IReadOnlyList<Recipe> GetAll()
    {
        lock (_gate)
        {
            return [.. _recipes];
        }
    }

    /// <inheritdoc />
    public void Save(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        lock (_gate)
        {
            _recipes.RemoveAll(r => string.Equals(r.Name, recipe.Name, StringComparison.OrdinalIgnoreCase));
            _recipes.Add(recipe);
            SaveFile();
        }
    }

    /// <inheritdoc />
    public void Delete(string name)
    {
        lock (_gate)
        {
            if (_recipes.RemoveAll(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                SaveFile();
            }
        }
    }

    private void SaveFile()
        => JsonFile.TryWrite(_path, new RecipesFile { Recipes = _recipes }, RecipeJsonContext.Default.RecipesFile);

    private static List<Recipe> Load(string path)
        => JsonFile.Read(path, RecipeJsonContext.Default.RecipesFile)?.Recipes?.Where(item => item is not null).ToList() ?? [];
}
