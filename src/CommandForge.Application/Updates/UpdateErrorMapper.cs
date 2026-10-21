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
            return exception is not null && IsTransportFailure(exception)
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

    /// <summary>
    /// Maps an exception from the update library, looking through wrapper and aggregate
    /// exceptions for the HTTP status or transport failure underneath.
    /// </summary>
    public static UpdateError Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var all = Flatten(exception).ToList();
        var http = all.OfType<HttpRequestException>().FirstOrDefault(e => e.StatusCode is not null);
        if (http is not null)
        {
            return Map(http.StatusCode, http);
        }

        return all.Exists(IsTransportFailure) ? UpdateError.Offline : UpdateError.Unknown;
    }

    // HttpClient timeouts surface as TaskCanceledException (with an inner TimeoutException).
    private static bool IsTransportFailure(Exception exception)
        => exception is HttpRequestException or SocketException or TimeoutException or TaskCanceledException;

    private static IEnumerable<Exception> Flatten(Exception root)
    {
        var pending = new Stack<Exception>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    pending.Push(inner);
                }
            }
            else if (current.InnerException is { } inner)
            {
                pending.Push(inner);
            }
        }
    }
}
