namespace CommandForge.Domain;

/// <summary>
/// An immutable, vetted command from the embedded catalog. Display strings are resolved
/// from resources via <see cref="TitleKey"/> / <see cref="DescriptionKey"/>; the command
/// line itself (<see cref="Executable"/> + <see cref="ArgsTemplate"/>) is fixed and never
/// built from uncontrolled user input.
/// </summary>
public sealed record CommandDefinition
{
    /// <summary>Stable, unique id in the form <c>&lt;group&gt;.&lt;action&gt;</c> (e.g. <c>dism.restorehealth</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Id of the owning <see cref="CommandCategory"/>.</summary>
    public required string CategoryId { get; init; }

    /// <summary>Resource key for the command title.</summary>
    public required string TitleKey { get; init; }

    /// <summary>Resource key for the command description.</summary>
    public required string DescriptionKey { get; init; }

    /// <summary>Optional Material Design icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Executable token: <c>cmd</c> | <c>powershell</c> | <c>winget</c> | <c>dism</c> | a direct path.</summary>
    public required string Executable { get; init; }

    /// <summary>Fixed argument template (no uncontrolled interpolation).</summary>
    public string ArgsTemplate { get; init; } = string.Empty;

    /// <summary>How the command is run: capture console output, or launch a GUI/shell target.</summary>
    public ExecutionMode ExecutionMode { get; init; } = ExecutionMode.Capture;

    /// <summary>Whether the command must run elevated (through the Elevator broker).</summary>
    public bool RequiresAdmin { get; init; }

    /// <summary>Risk level driving confirmation UI.</summary>
    public DangerLevel DangerLevel { get; init; } = DangerLevel.Safe;

    /// <summary>Whether to show a confirmation dialog before running.</summary>
    public bool ConfirmBeforeRun { get; init; }

    /// <summary>Whether to offer creating a System Restore Point before running.</summary>
    public bool CreatesRestorePoint { get; init; }

    /// <summary>How a required restart is detected.</summary>
    public RestartPolicy Restart { get; init; } = RestartPolicy.No;

    /// <summary>Regex matched against output when <see cref="Restart"/> is <see cref="RestartPolicy.FromOutputRegex"/>.</summary>
    public string? RestartRegex { get; init; }

    /// <summary>Id of the command that reverts this one, if reversible.</summary>
    public string? RevertCommandId { get; init; }

    /// <summary>Rough duration hint for the UI.</summary>
    public EstimatedDuration EstimatedDuration { get; init; } = EstimatedDuration.Short;

    /// <summary>Optional documentation URL (wiki page) for the command.</summary>
    public string? DocUrl { get; init; }

    /// <summary>Free-form search tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
