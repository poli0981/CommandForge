// CommandForge.Elevator — privileged broker helper (runs elevated, requireAdministrator).
//
// Connects to the UI's named pipe and serves ONLY admin commands from its OWN embedded catalog
// (golden rules #1/#2). It never builds a command line from pipe input — it accepts a commandId,
// looks it up, validates requiresAdmin, and runs it. It writes no files: diagnostics go back to
// the UI over the pipe (a file in the user-writable profile could be redirected by a junction).

using System.IO.Pipes;
using System.Security.Principal;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Infrastructure.Elevation;
using CommandForge.Infrastructure.Execution;

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
{
    return 2;
}

try
{
    // Identification level: the (non-elevated) pipe server may learn who we are but cannot act as us.
    using var client = new NamedPipeClientStream(".", args[0], PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
    await client.ConnectAsync(30_000);

    if (!PeerVerifier.IsExpectedServer(client))
    {
        return 3; // someone else owns the pipe — serve nothing
    }

    // Elevated children get a clean environment (see ChildEnvironment for why).
    var executor = new ProcessCommandExecutor(new SystemProcessRunner(ChildEnvironment.Apply));
    await new ElevationServer(new JsonCatalogProvider(), executor).RunAsync(client, CancellationToken.None);
    return 0;
}
catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or OperationCanceledException)
{
    return 1;
}
