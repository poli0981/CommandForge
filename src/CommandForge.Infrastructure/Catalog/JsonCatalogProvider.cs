using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Catalog;

/// <summary>Result of loading + validating the embedded catalog.</summary>
public sealed record CatalogLoadResult(
    IReadOnlyList<CommandCategory> Categories,
    IReadOnlyList<CommandDefinition> Commands,
    IReadOnlyList<string> Errors);

/// <summary>
/// Reads the embedded <c>categories.json</c> / <c>commands.json</c>, validates them, and
/// maps to Domain entities. Structural validation only — the cross-check that title/desc
/// resource keys exist lives in the test project (it owns the .resx).
/// </summary>
public static class CatalogLoader
{
    private const string ResourcePrefix = "CommandForge.Infrastructure.Catalog.";

    /// <summary>Loads and validates the catalog embedded in this assembly.</summary>
    public static CatalogLoadResult LoadEmbedded()
    {
        var categories = Deserialize("categories.json", CatalogJsonContext.Default.CategoryDtoArray);
        var commands = Deserialize("commands.json", CatalogJsonContext.Default.CommandDtoArray);
        return Build(categories, commands);
    }

    private static T Deserialize<T>(string fileName, JsonTypeInfo<T> typeInfo)
        where T : class
    {
        var resourceName = ResourcePrefix + fileName;
        var assembly = typeof(CatalogLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded catalog resource '{resourceName}' was not found.");
        return JsonSerializer.Deserialize(stream, typeInfo)
            ?? throw new InvalidOperationException($"Catalog resource '{resourceName}' deserialized to null.");
    }

    internal static CatalogLoadResult Build(CategoryDto[] categoryDtos, CommandDto[] commandDtos)
    {
        var errors = new List<string>();

        var categories = new List<CommandCategory>();
        var categoryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dto in categoryDtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) { errors.Add("A category has an empty id."); continue; }
            if (!categoryIds.Add(dto.Id)) { errors.Add($"Duplicate category id '{dto.Id}'."); continue; }
            if (string.IsNullOrWhiteSpace(dto.TitleKey)) { errors.Add($"Category '{dto.Id}' is missing titleKey."); continue; }

            categories.Add(new CommandCategory
            {
                Id = dto.Id,
                TitleKey = dto.TitleKey,
                Icon = dto.Icon,
                Order = dto.Order,
            });
        }

        var commands = new List<CommandDefinition>();
        var commandIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dto in commandDtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) { errors.Add("A command has an empty id."); continue; }
            if (!commandIds.Add(dto.Id)) { errors.Add($"Duplicate command id '{dto.Id}'."); continue; }
            if (string.IsNullOrWhiteSpace(dto.CategoryId)) { errors.Add($"Command '{dto.Id}' is missing categoryId."); continue; }
            if (!categoryIds.Contains(dto.CategoryId)) { errors.Add($"Command '{dto.Id}' references unknown categoryId '{dto.CategoryId}'."); continue; }
            if (string.IsNullOrWhiteSpace(dto.TitleKey)) { errors.Add($"Command '{dto.Id}' is missing titleKey."); continue; }
            if (string.IsNullOrWhiteSpace(dto.DescriptionKey)) { errors.Add($"Command '{dto.Id}' is missing descriptionKey."); continue; }
            if (string.IsNullOrWhiteSpace(dto.Executable)) { errors.Add($"Command '{dto.Id}' is missing executable."); continue; }
            if (dto.Restart == RestartPolicy.FromOutputRegex && !IsValidRegex(dto.RestartRegex)) { errors.Add($"Command '{dto.Id}' has an invalid restartRegex."); continue; }

            commands.Add(new CommandDefinition
            {
                Id = dto.Id,
                CategoryId = dto.CategoryId,
                TitleKey = dto.TitleKey,
                DescriptionKey = dto.DescriptionKey,
                Icon = dto.Icon,
                Executable = dto.Executable,
                ArgsTemplate = dto.ArgsTemplate ?? string.Empty,
                ExecutionMode = dto.ExecutionMode,
                RequiresAdmin = dto.RequiresAdmin,
                DangerLevel = dto.DangerLevel,
                ConfirmBeforeRun = dto.ConfirmBeforeRun,
                CreatesRestorePoint = dto.CreatesRestorePoint,
                Restart = dto.Restart,
                RestartRegex = dto.RestartRegex,
                RevertCommandId = dto.RevertCommandId,
                EstimatedDuration = dto.EstimatedDuration,
                DocUrl = dto.DocUrl,
                Tags = dto.Tags ?? [],
            });
        }

        foreach (var command in commands)
        {
            if (!string.IsNullOrWhiteSpace(command.RevertCommandId) && !commandIds.Contains(command.RevertCommandId))
            {
                errors.Add($"Command '{command.Id}' has revertCommandId '{command.RevertCommandId}' that does not exist.");
            }
        }

        categories.Sort((a, b) => a.Order.CompareTo(b.Order));
        return new CatalogLoadResult(categories, commands, errors);
    }

    private static bool IsValidRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            _ = Regex.Match(string.Empty, pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

/// <summary>
/// <see cref="ICatalogProvider"/> backed by the embedded JSON catalog. Validation failures
/// fail fast in Debug (caught by tests) and are logged + skipped in Release.
/// </summary>
public sealed class JsonCatalogProvider : ICatalogProvider
{
    private readonly IReadOnlyList<CommandCategory> _categories;
    private readonly IReadOnlyList<CommandDefinition> _commands;

    public JsonCatalogProvider()
    {
        var result = CatalogLoader.LoadEmbedded();
        if (result.Errors.Count > 0)
        {
#if DEBUG
            throw new InvalidOperationException(
                "Invalid command catalog:" + Environment.NewLine + string.Join(Environment.NewLine, result.Errors));
#else
            foreach (var error in result.Errors)
            {
                Serilog.Log.Warning("Catalog validation: {Error}", error);
            }
#endif
        }

        _categories = result.Categories;
        _commands = result.Commands;
    }

    /// <inheritdoc />
    public IReadOnlyList<CommandCategory> GetCategories() => _categories;

    /// <inheritdoc />
    public IReadOnlyList<CommandDefinition> GetCommands() => _commands;
}
