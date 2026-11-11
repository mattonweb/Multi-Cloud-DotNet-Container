namespace MultiCloud.Application.Web.Controllers;

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Mapping;
using MultiCloud.Application.Web.Models;

/// <summary>
/// The root route, which says, as JSON, where the application thinks it is running. The application returns no HTML
/// page.
/// </summary>
[ApiController]
[Route("")]
public sealed class HomeController : ControllerBase
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
    /// Reports the application, this run, the platform, the request as the application sees it, and the GET routes to
    /// try.
    /// </summary>
    /// <returns>200 with the report as JSON.</returns>
    [HttpGet]
    public ActionResult<HomeResource> Index()
    {
        HomeResource home = HomeResourceMapper.ToResource(
            this._session,
            this._identity,
            this._platform,
            this._environment.EnvironmentName,
            this.HttpContext);
        return this.Ok(home);
    }
}
