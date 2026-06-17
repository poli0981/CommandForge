namespace CommandForge.Application.Ports;

/// <summary>
/// Creates a System Restore Point before risky operations.
/// </summary>
public interface IRestorePointService
{
    /// <summary>Creates a restore point with the given description. Returns whether it succeeded.</summary>
    public Task<bool> CreateRestorePointAsync(string description, CancellationToken cancellationToken = default);
}
