namespace MultiCloud.Application.Web.Controllers;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiCloud.Application.Web.Hosting;

/// <summary>
/// The two health probes: liveness, which a platform and an outside monitor call, and readiness, which says whether
/// this instance should receive traffic. Neither is in the request log.
/// </summary>
/// <remarks>
/// The routes that fail on purpose share the <c>/healthz</c> prefix but are <see cref="DiagnosticsController"/>'s.
/// </remarks>
[ApiController]
[Route("healthz")]
public sealed class HealthController : ControllerBase
{
    private const string PlainText = "text/plain";

    private readonly HostReadiness _readiness;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="readiness">Says whether this instance should receive traffic.</param>
    public HealthController(HostReadiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        this._readiness = readiness;
    }

    /// <summary>
    /// Answers <c>up</c>, always.
    /// </summary>
    /// <remarks>
    /// This route has no dependencies and never will. It is what an outside monitor such as Dynatrace calls, and what a
    /// platform's liveness probe calls. If it checked a dependency, one failing dependency would make every instance
    /// look dead at once, and the platform would restart them all.
    /// </remarks>
    /// <returns>200 with the text <c>up</c>.</returns>
    [HttpGet("ping")]
    public ContentResult Ping()
    {
        return this.Content("up", PlainText);
    }

    /// <summary>
    /// Answers whether this instance should receive traffic.
    /// </summary>
    /// <remarks>
    /// Phase 1 has no dependencies, so the answer is the host's own state. Future work: a dependency this instance
    /// cannot serve without is checked here, and only here, never in <see cref="Ping"/>.
    /// </remarks>
    /// <returns>200 with <c>ready</c> once the host has started; 503 with <c>not ready</c> before that and once it has
    /// begun stopping.</returns>
    [HttpGet("ready")]
    public ContentResult Ready()
    {
        if (this._readiness.IsReady())
        {
            return this.Content("ready", PlainText);
        }

        ContentResult notReady = this.Content("not ready", PlainText);
        notReady.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return notReady;
    }
}
