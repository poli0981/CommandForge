using System.Net;
using CommandForge.Application.Ports;
using CommandForge.Application.Updates;

namespace CommandForge.Tests;

/// <summary>
/// Verifies the HTTP-status → <see cref="UpdateError"/> mapping (must-have test per Testing.md).
/// The key invariant: 429 is handled, and 419 (the Laravel CSRF myth) is NOT special-cased.
/// </summary>
public sealed class UpdateErrorMappingTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound, UpdateError.NotFound)]                  // 404
    [InlineData(HttpStatusCode.Forbidden, UpdateError.RateLimited)]              // 403 (primary rate limit)
    [InlineData(HttpStatusCode.TooManyRequests, UpdateError.RateLimited)]        // 429 (secondary rate limit)
    [InlineData(HttpStatusCode.InternalServerError, UpdateError.ServerError)]    // 500
    [InlineData(HttpStatusCode.ServiceUnavailable, UpdateError.ServerError)]     // 503
    [InlineData(HttpStatusCode.BadGateway, UpdateError.ServerError)]             // 502
    public void Map_HttpStatus_MapsToExpectedError(HttpStatusCode status, UpdateError expected)
    {
        var result = UpdateErrorMapper.Map(status, new HttpRequestException("boom", null, status));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_OfflineHttpRequestException_WithNoStatus_MapsToOffline()
    {
        var result = UpdateErrorMapper.Map(null, new HttpRequestException("no network"));
        Assert.Equal(UpdateError.Offline, result);
    }

    [Fact]
    public void Map_TaskCanceled_WithNoStatus_MapsToOffline()
    {
        var result = UpdateErrorMapper.Map(null, new TaskCanceledException());
        Assert.Equal(UpdateError.Offline, result);
    }

    [Fact]
    public void Map_419_IsNotSpecialCased_MapsToUnknown()
    {
        // 419 is a Laravel CSRF code, not a GitHub status — it must fall through to Unknown.
        var status = (HttpStatusCode)419;
        var result = UpdateErrorMapper.Map(status, new HttpRequestException("csrf myth", null, status));
        Assert.Equal(UpdateError.Unknown, result);
    }

    [Fact]
    public void Map_UnexpectedExceptionWithNoStatus_MapsToUnknown()
    {
        var result = UpdateErrorMapper.Map(null, new InvalidOperationException());
        Assert.Equal(UpdateError.Unknown, result);
    }
}
