using CommandForge.Application.Ports;
using CommandForge.Domain;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Infrastructure.UserCommands;

/// <summary>
/// <see cref="IUserCommandStore"/> backed by a local <c>user-commands.json</c>, kept entirely
/// separate from the embedded catalog (golden rule #1). All data stays on the machine (no telemetry).
/// </summary>
public sealed class JsonUserCommandStore : IUserCommandStore
{
    private readonly string _path;
    private readonly Lock _gate = new();
    private readonly List<UserCommand> _commands;

    /// <summary>Creates a store backed by the default <see cref="AppPaths.UserCommandsFilePath"/>.</summary>
    public JsonUserCommandStore()
        : this(AppPaths.UserCommandsFilePath)
    {
    }

    /// <summary>Creates a store backed by an explicit path (used in tests).</summary>
    public JsonUserCommandStore(string path)
    {
        _path = path;
        _commands = Load(path);
    }

    /// <inheritdoc />
    public IReadOnlyList<UserCommand> GetAll()
    {
        lock (_gate)
        {
            return [.. _commands];
        }
    }

    /// <inheritdoc />
    public void Save(UserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_gate)
        {
            _commands.RemoveAll(c => string.Equals(c.Id, command.Id, StringComparison.Ordinal));
            _commands.Add(command);
            SaveFile();
        }
    }

    /// <inheritdoc />
    public void Delete(string id)
    {
        lock (_gate)
        {
            if (_commands.RemoveAll(c => string.Equals(c.Id, id, StringComparison.Ordinal)) > 0)
            {
                SaveFile();
            }
        }
    }

    private void SaveFile()
        => JsonFile.TryWrite(_path, new UserCommandsFile { Commands = _commands }, UserCommandJsonContext.Default.UserCommandsFile);

    private static List<UserCommand> Load(string path)
        => JsonFile.Read(path, UserCommandJsonContext.Default.UserCommandsFile)?.Commands?.Where(item => item is not null).ToList() ?? [];
}
