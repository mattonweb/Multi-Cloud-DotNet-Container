namespace MultiCloud.Application.Web.Hosting;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Puts <c>X-Session-Id</c> and <c>X-Platform</c> on every response. A 403 or a 500 without them did not come from the
/// application, but from a firewall or proxy in front of it.
/// </summary>
/// <remarks>
/// The headers are added as the response starts rather than as the request arrives, because the exception handler
/// clears the headers of a response it replaces; a callback registered here still runs for the response it writes.
/// </remarks>
internal sealed class ResponseHeadersMiddleware
{
    /// <summary>
    /// The header that carries the session's identifier.
    /// </summary>
    internal const string SessionHeader = "X-Session-Id";

    /// <summary>
    /// The header that carries the platform's name.
    /// </summary>
    internal const string PlatformHeader = "X-Platform";

    private readonly RequestDelegate _next;
    private readonly ApplicationSession _session;
    private readonly PlatformDescription _platform;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The rest of the pipeline.</param>
    /// <param name="session">This run of the application.</param>
    /// <param name="platform">The platform the application runs on.</param>
    public ResponseHeadersMiddleware(RequestDelegate next, ApplicationSession session, PlatformDescription platform)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(platform);
        this._next = next;
        this._session = session;
        this._platform = platform;
    }

    /// <summary>
    /// Arranges for the headers to be added when the response starts, then runs the rest of the pipeline.
    /// </summary>
    /// <param name="context">The request's context.</param>
    /// <returns>A task that completes when the rest of the pipeline has.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        HttpResponse response = context.Response;
        response.OnStarting(this.AddHeaders, response);
        return this._next(context);
    }

    /// <summary>
    /// Adds the headers to a response that is about to start.
    /// </summary>
    /// <param name="state">The response.</param>
    /// <returns>A completed task.</returns>
    private Task AddHeaders(object state)
    {
        HttpResponse response = (HttpResponse)state;
        IHeaderDictionary headers = response.Headers;
        SessionId sessionId = this._session.Id;
        PlatformName platformName = this._platform.Name;
        headers[SessionHeader] = sessionId.ToString();
        headers[PlatformHeader] = platformName.Value;
        return Task.CompletedTask;
    }
}
