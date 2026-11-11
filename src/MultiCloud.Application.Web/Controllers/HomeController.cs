namespace MultiCloud.Application.Web.Controllers;

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Models;

/// <summary>
/// The home page, which says where the application thinks it is running.
/// </summary>
[Route("")]
public sealed class HomeController : Controller
{
    private readonly ApplicationSession _session;
    private readonly ApplicationIdentity _identity;
    private readonly PlatformDescription _platform;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeController"/> class.
    /// </summary>
    /// <param name="session">This run of the application.</param>
    /// <param name="identity">The application's version and the machine's name.</param>
    /// <param name="platform">The platform, as the platform adapter describes it.</param>
    /// <param name="environment">The host environment, which names the environment.</param>
    public HomeController(
        ApplicationSession session,
        ApplicationIdentity identity,
        PlatformDescription platform,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(environment);
        this._session = session;
        this._identity = identity;
        this._platform = platform;
        this._environment = environment;
    }

    /// <summary>
    /// Shows the platform, the session, the version and environment, and the request as the application sees it.
    /// </summary>
    /// <returns>The home page.</returns>
    [HttpGet]
    public ViewResult Index()
    {
        HttpRequest request = this.Request;
        ConnectionInfo connection = this.HttpContext.Connection;
        IPAddress? remoteAddress = connection.RemoteIpAddress;
        HomePage page = new HomePage
        {
            Session = this._session,
            Version = this._identity.Version,
            EnvironmentName = this._environment.EnvironmentName,
            Platform = this._platform,
            RequestScheme = request.Scheme,
            RequestHost = request.Host.ToString(),
            RemoteAddress = remoteAddress?.ToString(),
        };
        return this.View(page);
    }
}
