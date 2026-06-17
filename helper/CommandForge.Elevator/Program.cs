// CommandForge.Elevator — privileged broker helper (runs elevated, requireAdministrator).
//
// Connects to the UI's named pipe and serves ONLY admin commands from its OWN embedded catalog
// (golden rules #1/#2). It never builds a command line from pipe input — it accepts a commandId,
// looks it up, validates requiresAdmin, and runs it.

using System.IO.Pipes;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Infrastructure.Elevation;
using CommandForge.Infrastructure.Execution;

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
{
    return 2;
}

var pipeName = args[0];
AppPaths.EnsureCreated();
var logPath = Path.Combine(AppPaths.LogsDirectory, "elevator.log");

void Log(string message) =>
    File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");

try
{
    Log($"Starting; connecting to pipe {pipeName}");

    using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    await client.ConnectAsync(30_000);
    Log("Connected to UI");

    var server = new ElevationServer(new JsonCatalogProvider(), new ProcessCommandExecutor(new SystemProcessRunner()));
    await server.RunAsync(client, CancellationToken.None);

    Log("Pipe closed; exiting");
    return 0;
}
catch (Exception ex)
{
    Log($"Failed: {ex}");
    return 1;
}
