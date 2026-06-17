using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// Creates the broker's named pipe. The UI (medium integrity) owns the pipe server and the
/// elevated Elevator connects as client (high → medium integrity is allowed, avoiding the
/// integrity barrier of an elevated server). The DACL is restricted to the current user.
/// </summary>
internal static class NamedPipeFactory
{
    public static NamedPipeServerStream CreateServer(string pipeName)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User
            ?? throw new InvalidOperationException("Unable to resolve the current user SID for the pipe ACL.");

        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            pipeSecurity: security);
    }
}
