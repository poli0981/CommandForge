namespace CommandForge.Domain;

/// <summary>
/// The kind of executable a command runs through. Maps the <c>executable</c> field of the catalog.
/// </summary>
public enum ExecutableKind
{
    /// <summary>cmd.exe</summary>
    Cmd,

    /// <summary>powershell.exe / pwsh</summary>
    PowerShell,

    /// <summary>winget</summary>
    Winget,

    /// <summary>dism.exe</summary>
    Dism,

    /// <summary>A direct path to an executable.</summary>
    Direct,
}
