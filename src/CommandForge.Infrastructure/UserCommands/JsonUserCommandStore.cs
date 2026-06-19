using System.Text.Json;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.UserCommands;

/// <summary>
/// <see cref="IUserCommandStore"/> backed by a local <c>user-commands.json</c>, kept entirely
/// separate from the embedded catalog (golden rule #1). All data stays on the machine (no telemetry).
/// </summary>
public sealed class JsonUserCommandStore : IUserCommandStore
{
    private readonly string _path;
    private List<UserCommand> _commands;

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
    public IReadOnlyList<UserCommand> GetAll() => _commands;

    /// <inheritdoc />
    public void Save(UserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _commands.RemoveAll(c => string.Equals(c.Id, command.Id, StringComparison.Ordinal));
        _commands.Add(command);
        SaveFile();
    }

    /// <inheritdoc />
    public void Delete(string id)
    {
        if (_commands.RemoveAll(c => string.Equals(c.Id, id, StringComparison.Ordinal)) > 0)
        {
            SaveFile();
        }
    }

    private void SaveFile()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var file = new UserCommandsFile { Commands = _commands };
        var json = JsonSerializer.Serialize(file, UserCommandJsonContext.Default.UserCommandsFile);
        File.WriteAllText(_path, json);
    }

    private static List<UserCommand> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize(json, UserCommandJsonContext.Default.UserCommandsFile);
            return file?.Commands.ToList() ?? [];
        }
        catch (JsonException)
        {
            // Corrupt file: start empty rather than crashing on startup.
            return [];
        }
    }
}
