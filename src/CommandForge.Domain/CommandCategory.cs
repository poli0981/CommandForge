namespace CommandForge.Domain;

/// <summary>
/// A group of related commands (e.g. system-maintenance, network, cleanup).
/// Display strings are resolved from resources via <see cref="TitleKey"/>.
/// </summary>
public sealed record CommandCategory
{
    /// <summary>Stable, unique category id (e.g. <c>system-maintenance</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Resource key for the category's display title.</summary>
    public required string TitleKey { get; init; }

    /// <summary>Optional Material Design icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Sort order in the sidebar.</summary>
    public int Order { get; init; }
}
