using System.Net;
using System.Net.Sockets;
using CommandForge.Application.Ports;

namespace CommandForge.Application.Updates;

/// <summary>
/// Pure mapping of an HTTP status / exception to an <see cref="UpdateError"/>. No I/O, so it is
/// unit-testable without a network. GitHub uses 403 (primary rate limit, with an
/// <c>X-RateLimit-Remaining</c> header) and 429 (secondary); there is NO 419 (that is a Laravel
/// CSRF code, not GitHub) so it must fall through to <see cref="UpdateError.Unknown"/>.
/// </summary>
public static class UpdateErrorMapper
{
    /// <summary>Maps an HTTP <paramref name="status"/> (or, when null, the <paramref name="exception"/>) to an <see cref="UpdateError"/>.</summary>
    public static UpdateError Map(HttpStatusCode? status, Exception? exception)
    {
        if (status is null)
        {
            // No status: a transport failure (no network/DNS), a socket error, or a timeout.
            return exception is HttpRequestException or SocketException or TaskCanceledException
                ? UpdateError.Offline
                : UpdateError.Unknown;
        }

        return status switch
        {
            HttpStatusCode.NotFound => UpdateError.NotFound,                                     // 404
            HttpStatusCode.Forbidden => UpdateError.RateLimited,                                 // 403
            HttpStatusCode.TooManyRequests => UpdateError.RateLimited,                           // 429
            >= HttpStatusCode.InternalServerError and < (HttpStatusCode)600 => UpdateError.ServerError, // 5xx
            _ => UpdateError.Unknown,                                                            // incl. 419
        };
    }
}
