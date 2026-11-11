namespace MultiCloud.Application.Web.Controllers;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiCloud.Application.Web.Errors;
using MultiCloud.Application.Web.Models;

/// <summary>
/// The two routes that fail on purpose, to prove that error handling and error logging work end to end. The setting
/// <c>Diagnostics:ErrorRoutesEnabled</c> turns them off, and then both answer 404 and throw nothing.
/// </summary>
[ApiController]
[Route("healthz/error")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly DiagnosticsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsController"/> class.
    /// </summary>
    /// <param name="options">Says whether the error routes throw.</param>
    public DiagnosticsController(DiagnosticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this._options = options;
    }

    /// <summary>
    /// Throws an ordinary exception, which the global exception handler answers with a 500 and logs as an error.
    /// </summary>
    /// <returns>404 when the error routes are turned off; otherwise nothing, because it throws.</returns>
    [HttpGet("500")]
    public IActionResult Error500()
    {
        if (!this._options.ErrorRoutesEnabled)
        {
            return this.NotFound();
        }

        throw new InvalidOperationException("This route fails on purpose, to prove that errors are handled and logged.");
    }

    /// <summary>
    /// Throws the application's own exception that carries 403, which the global exception handler answers with a 403
    /// and logs as a warning. Its <c>X-Session-Id</c> header tells it apart from a 403 a firewall or proxy wrote.
    /// </summary>
    /// <returns>404 when the error routes are turned off; otherwise nothing, because it throws.</returns>
    [HttpGet("403")]
    public IActionResult Error403()
    {
        if (!this._options.ErrorRoutesEnabled)
        {
            return this.NotFound();
        }

        throw new StatusCodeException(
            StatusCodes.Status403Forbidden,
            "This route refuses on purpose, to prove that the application's own refusal can be told from a proxy's.");
    }
}
