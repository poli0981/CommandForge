using System.Text.Json.Serialization;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Recipes;

/// <summary>Persisted shape of <c>recipes.json</c>: a schema version plus the saved recipes.</summary>
internal sealed record RecipesFile
{
    public int Version { get; init; } = 1;

    public IReadOnlyList<Recipe> Recipes { get; init; } = [];
}

/// <summary>System.Text.Json source-generation context for <c>recipes.json</c> (camelCase, indented).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(RecipesFile))]
internal sealed partial class RecipeJsonContext : JsonSerializerContext;
