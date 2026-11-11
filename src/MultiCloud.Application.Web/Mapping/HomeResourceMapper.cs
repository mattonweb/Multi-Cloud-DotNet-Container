namespace MultiCloud.Application.Web.Mapping;

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Logging;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Shapes what the application knows about itself, its platform and a request into the JSON <c>GET /</c> answers.
/// </summary>
internal static class HomeResourceMapper
{
    /// <summary>
    /// The GET routes a person can try, in the order docs/SDD.md section 4.1 lists them.
    /// </summary>
    private static readonly string[] Routes =
    [
        "/",
        "/healthz/ping",
        "/healthz/ready",
        "/healthz/error/500",
        "/healthz/error/403",
        "/api/notes",
        "/api/notes/1",
    ];

    /// <summary>
    /// Shapes the answer to <c>GET /</c>.
    /// </summary>
    /// <param name="session">This run of the application.</param>
    /// <param name="identity">The application's version and the machine's name.</param>
    /// <param name="platform">The platform, as the platform adapter describes it.</param>
    /// <param name="environmentName">The host environment's name.</param>
    /// <param name="httpContext">The request being answered.</param>
    /// <returns>The answer.</returns>
    internal static HomeResource ToResource(
        ApplicationSession session,
        ApplicationIdentity identity,
        PlatformDescription platform,
        string environmentName,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        ArgumentNullException.ThrowIfNull(httpContext);
        ApplicationVersion version = identity.Version;
        return new HomeResource
        {
            Service = TelemetryEnricher.ServiceName,
            Version = version.Value,
            Environment = environmentName,
            Session = ToSession(session),
            Platform = ToPlatform(platform),
            Request = ToRequest(httpContext),
            Routes = Routes,
        };
    }

    /// <summary>
    /// Shapes the session.
    /// </summary>
    /// <param name="session">This run of the application.</param>
    /// <returns>The session as JSON reports it.</returns>
    private static SessionResource ToSession(ApplicationSession session)
    {
        SessionId sessionId = session.Id;
        return new SessionResource
        {
            Id = sessionId.ToString(),
            StartedOn = session.StartedOn,
        };
    }

    /// <summary>
    /// Shapes the platform, leaving each value the platform does not supply as <see langword="null"/>.
    /// </summary>
    /// <param name="platform">The platform, as the platform adapter describes it.</param>
    /// <returns>The platform as JSON reports it.</returns>
    private static PlatformResource ToPlatform(PlatformDescription platform)
    {
        PlatformName name = platform.Name;
        return new PlatformResource
        {
            Name = name.Value,
            CloudProvider = platform.CloudProvider?.Value,
            CloudPlatform = platform.CloudPlatform?.Value,
            Region = platform.Region?.Value,
            Service = platform.Service?.Value,
            Revision = platform.Revision?.Value,
            Instance = platform.Instance?.Value,
        };
    }

    /// <summary>
    /// Shapes the request as the application sees it, after forwarded headers have been applied.
    /// </summary>
    /// <param name="httpContext">The request being answered.</param>
    /// <returns>The request as JSON reports it.</returns>
    private static RequestResource ToRequest(HttpContext httpContext)
    {
        HttpRequest request = httpContext.Request;
        ConnectionInfo connection = httpContext.Connection;
        IPAddress? remoteAddress = connection.RemoteIpAddress;
        return new RequestResource
        {
            Scheme = request.Scheme,
            Host = request.Host.ToString(),
            RemoteAddress = remoteAddress?.ToString(),
        };
    }
}
