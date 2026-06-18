using System.IO.Compression;
using CommandForge.Application.Ports;

namespace CommandForge.Infrastructure.Logging;

/// <summary><see cref="ILogMaintenance"/> over the local rolling log files. Nothing leaves the machine.</summary>
public sealed class LogMaintenance : ILogMaintenance
{
    private const string LogFilePattern = "log-*.txt";

    /// <inheritdoc />
    public string LogsDirectoryPath => AppPaths.LogsDirectory;

    /// <inheritdoc />
    public long GetLogsSizeBytes()
    {
        if (!Directory.Exists(LogsDirectoryPath))
        {
            return 0;
        }

        long total = 0;
        foreach (var file in Directory.EnumerateFiles(LogsDirectoryPath, LogFilePattern))
        {
            try
            {
                total += new FileInfo(file).Length;
            }
            catch (IOException)
            {
                // File vanished/locked — skip.
            }
        }

        return total;
    }

    /// <inheritdoc />
    public async Task ExportZipAsync(string destinationZipPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationZipPath);

        // Stream each log file in with shared read — today's rolling file is held open by the
        // async file sink, so ZipFile.CreateFromDirectory would throw IOException.
        await using var zipStream = new FileStream(destinationZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        if (!Directory.Exists(LogsDirectoryPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(LogsDirectoryPath, LogFilePattern))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var entry = archive.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await source.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // A locked/removed file shouldn't fail the whole export — skip it.
            }
        }
    }

    /// <inheritdoc />
    public void DeleteRolledLogFiles()
    {
        if (!Directory.Exists(LogsDirectoryPath))
        {
            return;
        }

        var todaysFile = $"log-{DateTime.Now:yyyyMMdd}.txt";
        foreach (var file in Directory.EnumerateFiles(LogsDirectoryPath, LogFilePattern))
        {
            if (string.Equals(Path.GetFileName(file), todaysFile, StringComparison.OrdinalIgnoreCase))
            {
                continue; // keep today's active file
            }

            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Locked — skip.
            }
        }
    }
}
