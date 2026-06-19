using System.Text.Json;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Recipes;

/// <summary>
/// <see cref="IRecipeStore"/> backed by a local <c>recipes.json</c>. All data stays on the
/// machine (golden rule: no telemetry). Recipe names are matched case-insensitively.
/// </summary>
public sealed class JsonRecipeStore : IRecipeStore
{
    private readonly string _path;
    private List<Recipe> _recipes;

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
    public IReadOnlyList<Recipe> GetAll() => _recipes;

    /// <inheritdoc />
    public void Save(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        _recipes.RemoveAll(r => string.Equals(r.Name, recipe.Name, StringComparison.OrdinalIgnoreCase));
        _recipes.Add(recipe);
        SaveFile();
    }

    /// <inheritdoc />
    public void Delete(string name)
    {
        if (_recipes.RemoveAll(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            SaveFile();
        }
    }

    private void SaveFile()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var file = new RecipesFile { Recipes = _recipes };
        var json = JsonSerializer.Serialize(file, RecipeJsonContext.Default.RecipesFile);
        File.WriteAllText(_path, json);
    }

    private static List<Recipe> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize(json, RecipeJsonContext.Default.RecipesFile);
            return file?.Recipes.ToList() ?? [];
        }
        catch (JsonException)
        {
            // Corrupt recipes file: start empty rather than crashing on startup.
            return [];
        }
    }
}
