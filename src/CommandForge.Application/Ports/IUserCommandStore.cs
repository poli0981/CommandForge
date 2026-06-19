using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Stores user-defined commands locally, fully separate from the vetted embedded catalog
/// (golden rule #1). These commands always run without elevation.
/// </summary>
public interface IUserCommandStore
{
    /// <summary>All user-defined commands, in stored order.</summary>
    public IReadOnlyList<UserCommand> GetAll();

    /// <summary>Creates or overwrites a command (matched by id) and persists.</summary>
    public void Save(UserCommand command);

    /// <summary>Deletes the command <paramref name="id"/> (no-op if absent) and persists.</summary>
    public void Delete(string id);
}
