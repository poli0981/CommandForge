using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Provides the vetted, read-only command catalog (categories + commands).
/// </summary>
public interface ICatalogProvider
{
    /// <summary>Returns all command categories, in display order.</summary>
    public IReadOnlyList<CommandCategory> GetCategories();

    /// <summary>Returns all vetted commands.</summary>
    public IReadOnlyList<CommandDefinition> GetCommands();

    /// <summary>Catalog validation errors detected at load (empty when valid). Surfaced in the Debug panel.</summary>
    public IReadOnlyList<string> ValidationErrors { get; }
}
