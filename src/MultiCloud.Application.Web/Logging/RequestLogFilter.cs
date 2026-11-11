namespace MultiCloud.Application.Web.Logging;

using System;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Decides which requests get a line in the request log: every one except the two health probes, which a platform
/// calls every few seconds. The error routes are logged.
/// </summary>
internal static class RequestLogFilter
{
    /// <summary>
    /// The liveness probe's path.
    /// </summary>
    private static readonly PathString PingPath = new PathString("/healthz/ping");

    /// <summary>
    /// The readiness probe's path.
    /// </summary>
    private static readonly PathString ReadyPath = new PathString("/healthz/ready");

    /// <summary>
    /// Says whether a request gets a line in the request log.
    /// </summary>
    /// <param name="context">The request's context.</param>
    /// <returns><see langword="false"/> for the two health probes; <see langword="true"/> for everything else.</returns>
    internal static bool IsLogged(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        PathString path = context.Request.Path;
        bool isProbe = path.Equals(PingPath, StringComparison.OrdinalIgnoreCase)
            || path.Equals(ReadyPath, StringComparison.OrdinalIgnoreCase);
        return !isProbe;
    }
}
