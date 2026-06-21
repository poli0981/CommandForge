using CommandForge.Domain;

namespace CommandForge.Infrastructure.Catalog;

/// <summary>JSON shape of a category record in <c>categories.json</c>.</summary>
internal sealed record CategoryDto
{
    public string? Id { get; init; }
    public string? TitleKey { get; init; }
    public string? Icon { get; init; }
    public int Order { get; init; }
}

/// <summary>JSON shape of a command record in <c>commands.json</c> (see docs/CatalogCommand.md).</summary>
internal sealed record CommandDto
{
    public string? Id { get; init; }
    public string? CategoryId { get; init; }
    public string? TitleKey { get; init; }
    public string? DescriptionKey { get; init; }
    public string? Icon { get; init; }
    public string? Executable { get; init; }
    public string? ArgsTemplate { get; init; }
    public ExecutionMode ExecutionMode { get; init; }
    public bool RequiresAdmin { get; init; }
    public DangerLevel DangerLevel { get; init; }
    public bool ConfirmBeforeRun { get; init; }
    public bool CreatesRestorePoint { get; init; }
    public RestartPolicy Restart { get; init; }
    public string? RestartRegex { get; init; }
    public string? RevertCommandId { get; init; }
    public EstimatedDuration EstimatedDuration { get; init; }
    public string? DocUrl { get; init; }
    public string[]? Tags { get; init; }
    public RegistryValueDto[]? AffectedRegistryValues { get; init; }
    public int? MinOsBuild { get; init; }
    public int? MaxOsBuild { get; init; }
}

/// <summary>JSON shape of a registry-value reference (for before/after comparison).</summary>
internal sealed record RegistryValueDto
{
    public string? Path { get; init; }
    public string? Name { get; init; }
}
