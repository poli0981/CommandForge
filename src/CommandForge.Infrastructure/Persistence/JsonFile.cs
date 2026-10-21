using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Serilog;

namespace CommandForge.Infrastructure.Persistence;

/// <summary>
/// Crash-safe JSON persistence shared by every local store (settings, history, recipes, profiles,
/// user commands). Writes go to a temporary file that atomically replaces the original, so a power
/// loss mid-write can't leave half a file; unreadable or corrupt files never crash startup, and a
/// corrupt file is kept aside for recovery instead of being silently overwritten.
/// </summary>
internal static class JsonFile
{
    /// <summary>
    /// Reads <paramref name="path"/>: null when it doesn't exist, can't be read (locked, denied)
    /// or isn't valid JSON. With <paramref name="keepCorruptCopy"/> an invalid file is first renamed
    /// to <c>name.corrupt-yyyyMMddHHmmss.json</c> so the next save can't overwrite the user's data.
    /// </summary>
    public static T? Read<T>(string path, JsonTypeInfo<T> typeInfo, bool keepCorruptCopy = true)
        where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(typeInfo);

        if (!File.Exists(path))
        {
            return null;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Could not read {Path}; using defaults for this session.", path);
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (JsonException ex)
        {
            if (!keepCorruptCopy)
            {
                Log.Information(ex, "{Path} is not valid JSON.", path);
                return null;
            }

            var backup = KeepCorruptCopy(path);
            Log.Warning(ex, "{Path} is not valid JSON; kept it as {Backup} and started from defaults.", path, backup);
            return null;
        }
    }

    /// <summary>Writes atomically; returns false (and logs) instead of throwing when the disk refuses.</summary>
    public static bool TryWrite<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        try
        {
            Write(path, value, typeInfo);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Could not save {Path}; the change is kept in memory only.", path);
            return false;
        }
    }

    /// <summary>Writes atomically (temporary file in the same folder, then replace); throws on I/O errors.</summary>
    public static void Write<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(typeInfo);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new IOException($"'{path}' has no parent directory.");
        Directory.CreateDirectory(directory);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temp, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private static string? KeepCorruptCopy(string path)
    {
        var backup = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(path)}.corrupt-{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(path)}");
        try
        {
            File.Move(path, backup, overwrite: true);
            return backup;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A stray .tmp file is harmless: its name is unique and nothing ever loads it.
        }
    }
}
