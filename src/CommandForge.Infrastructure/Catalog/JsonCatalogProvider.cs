using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Catalog;

/// <summary>Result of loading + validating the embedded catalog.</summary>
public sealed record CatalogLoadResult(
    IReadOnlyList<CommandCategory> Categories,
    IReadOnlyList<CommandDefinition> Commands,
    IReadOnlyList<string> Errors);

/// <summary>
/// Reads the embedded <c>categories.json</c> / <c>commands.json</c>, validates them, and maps to
/// Domain entities. A record that fails validation is reported and left out — never loaded half
/// right. The cross-check that title/desc resource keys exist lives in the test project (it owns
/// the .resx).
/// </summary>
public static class CatalogLoader
{
    private const string ResourcePrefix = "CommandForge.Infrastructure.Catalog.";

    // A catalog executable is a bare program name ("dism", "services.msc"); the resolver pins it to
    // the system directories. Paths, quotes and spaces would sidestep that.
    private static readonly SearchValues<char> NotInProgramName = SearchValues.Create(" \t\"/\\");

    /// <summary>Loads and validates the catalog embedded in this assembly.</summary>
    public static CatalogLoadResult LoadEmbedded()
    {
        CategoryDto[] categories;
        CommandDto[] commands;
        try
        {
            categories = Deserialize("categories.json", CatalogJsonContext.Default.CategoryDtoArray);
            commands = Deserialize("commands.json", CatalogJsonContext.Default.CommandDtoArray);
        }
        catch (JsonException ex)
        {
            // Tests make this unreachable for a released build; still, an empty catalog plus an error
            // in the log and the Debug panel beats an app (or Elevator) that cannot start at all.
            return new CatalogLoadResult([], [], [$"The embedded catalog is not valid JSON: {ex.Message}"]);
        }

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
        var categories = BuildCategories(categoryDtos, errors);
        var categoryIds = categories.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        var commands = new List<CommandDefinition>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dto in commandDtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) { errors.Add("A command has an empty id."); continue; }
            if (!seenIds.Add(dto.Id)) { errors.Add($"Duplicate command id '{dto.Id}'."); continue; }

            var problem = FindStructuralProblem(dto, categoryIds) ?? FindBehaviorProblem(dto);
            if (problem is not null) { errors.Add($"Command '{dto.Id}' {problem}"); continue; }

            commands.Add(ToDefinition(dto));
        }

        // Only commands that were actually loaded can be reverted to; a dangling id would offer an
        // Undo that cannot run.
        var loadedIds = commands.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < commands.Count; i++)
        {
            if (commands[i].RevertCommandId is { } revertId && !loadedIds.Contains(revertId))
            {
                errors.Add($"Command '{commands[i].Id}' has revertCommandId '{revertId}' that does not exist.");
                commands[i] = commands[i] with { RevertCommandId = null };
            }
        }

        return new CatalogLoadResult(categories, commands, errors);
    }

    private static List<CommandCategory> BuildCategories(CategoryDto[] categoryDtos, List<string> errors)
    {
        var categories = new List<CommandCategory>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dto in categoryDtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) { errors.Add("A category has an empty id."); continue; }
            if (!seenIds.Add(dto.Id)) { errors.Add($"Duplicate category id '{dto.Id}'."); continue; }
            if (string.IsNullOrWhiteSpace(dto.TitleKey)) { errors.Add($"Category '{dto.Id}' is missing titleKey."); continue; }

            categories.Add(new CommandCategory
            {
                Id = dto.Id,
                TitleKey = dto.TitleKey,
                Icon = dto.Icon,
                Order = dto.Order,
            });
        }

        categories.Sort((a, b) => a.Order.CompareTo(b.Order));
        return categories;
    }

    /// <summary>A missing or dangling field, as the end of an error sentence; <see langword="null"/> when there is none.</summary>
    private static string? FindStructuralProblem(CommandDto dto, HashSet<string> categoryIds)
    {
        if (string.IsNullOrWhiteSpace(dto.CategoryId)) { return "is missing categoryId."; }
        if (!categoryIds.Contains(dto.CategoryId)) { return $"references unknown categoryId '{dto.CategoryId}'."; }
        if (string.IsNullOrWhiteSpace(dto.TitleKey)) { return "is missing titleKey."; }
        if (string.IsNullOrWhiteSpace(dto.DescriptionKey)) { return "is missing descriptionKey."; }
        if (string.IsNullOrWhiteSpace(dto.Executable)) { return "is missing executable."; }
        if (dto.Executable.AsSpan().IndexOfAny(NotInProgramName) >= 0) { return "has an executable that is not a plain program name (arguments belong in argsTemplate)."; }
        if (dto.MinOsBuild is <= 0) { return "has a non-positive minOsBuild."; }
        if (dto.MaxOsBuild is <= 0) { return "has a non-positive maxOsBuild."; }
        if (dto.MinOsBuild is { } minBuild && dto.MaxOsBuild is { } maxBuild && maxBuild < minBuild) { return $"has maxOsBuild ({maxBuild}) less than minOsBuild ({minBuild})."; }
        return null;
    }

    /// <summary>Settings that contradict each other or the safety rules; <see langword="null"/> when there are none.</summary>
    private static string? FindBehaviorProblem(CommandDto dto)
    {
        var unknownTokens = ArgumentTokens.FindUnknown(dto.ArgsTemplate ?? string.Empty);
        if (unknownTokens.Count > 0) { return $"uses unknown argument token(s) {string.Join(", ", unknownTokens)}."; }
        if (dto.DangerLevel != DangerLevel.Safe && !dto.ConfirmBeforeRun) { return $"is {dto.DangerLevel} but does not set confirmBeforeRun."; }
        if (dto.ExecutionMode == ExecutionMode.Launch && dto.Restart is RestartPolicy.FromExitCode or RestartPolicy.FromOutputRegex)
        {
            return $"cannot use restart policy {dto.Restart} in Launch mode (its exit code and output are not captured).";
        }

        if (dto.Restart == RestartPolicy.FromOutputRegex && !IsValidRegex(dto.RestartRegex)) { return "has an invalid restartRegex."; }
        if (dto.Restart != RestartPolicy.FromOutputRegex && !string.IsNullOrEmpty(dto.RestartRegex)) { return $"has a restartRegex but its restart policy is {dto.Restart}."; }
        return null;
    }

    private static CommandDefinition ToDefinition(CommandDto dto) => new()
    {
        Id = dto.Id!,
        CategoryId = dto.CategoryId!,
        TitleKey = dto.TitleKey!,
        DescriptionKey = dto.DescriptionKey!,
        Icon = dto.Icon,
        Executable = dto.Executable!,
        ArgsTemplate = dto.ArgsTemplate ?? string.Empty,
        ExecutionMode = dto.ExecutionMode,
        RequiresAdmin = dto.RequiresAdmin,
        DangerLevel = dto.DangerLevel,
        ConfirmBeforeRun = dto.ConfirmBeforeRun,
        CreatesRestorePoint = dto.CreatesRestorePoint,
        Restart = dto.Restart,
        RestartRegex = dto.RestartRegex,
        RevertCommandId = string.IsNullOrWhiteSpace(dto.RevertCommandId) ? null : dto.RevertCommandId,
        EstimatedDuration = dto.EstimatedDuration,
        DocUrl = dto.DocUrl,
        Tags = dto.Tags ?? [],
        AffectedRegistryValues = dto.AffectedRegistryValues is null
            ? []
            : dto.AffectedRegistryValues
                .Where(r => !string.IsNullOrWhiteSpace(r.Path))
                .Select(r => new RegistryValueRef { Path = r.Path!, Name = r.Name ?? string.Empty })
                .ToList(),
        MinOsBuild = dto.MinOsBuild,
        MaxOsBuild = dto.MaxOsBuild,
    };

    private static bool IsValidRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            _ = Regex.Match(string.Empty, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
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
    private readonly IReadOnlyList<string> _validationErrors;

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
        _validationErrors = result.Errors;
    }

    /// <inheritdoc />
    public IReadOnlyList<CommandCategory> GetCategories() => _categories;

    /// <inheritdoc />
    public IReadOnlyList<CommandDefinition> GetCommands() => _commands;

    /// <inheritdoc />
    public IReadOnlyList<string> ValidationErrors => _validationErrors;
}
