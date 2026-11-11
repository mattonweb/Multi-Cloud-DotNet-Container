namespace MultiCloud.Application.Web.Errors;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// The one global exception handler. It answers every exception a request throws with problem details, and logs it.
/// </summary>
/// <remarks>
/// A <see cref="StatusCodeException"/> is the application refusing a request on purpose: it is answered with its own
/// status and logged as a warning. Anything else is a failure: it is answered 500, with no detail that could leak how
/// the application works, and logged as an error with the exception. The problem's session identifier is added for
/// every problem the application writes (see <see cref="SessionProblemDetails"/>).
/// </remarks>
internal sealed class ApplicationExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApplicationExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationExceptionHandler"/> class.
    /// </summary>
    /// <param name="problemDetails">Writes the problem details.</param>
    /// <param name="logger">Where each exception is logged.</param>
    public ApplicationExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApplicationExceptionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(logger);
        this._problemDetails = problemDetails;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);
        ProblemDetails problem = exception is StatusCodeException refusal
            ? this.AnswerRefusal(httpContext, refusal)
            : this.AnswerFailure(httpContext, exception);
        HttpResponse response = httpContext.Response;
        response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        ProblemDetailsContext context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        };
        return this._problemDetails.TryWriteAsync(context);
    }

    /// <summary>
    /// Logs a refusal as a warning, and describes it with its own status and message.
    /// </summary>
    /// <param name="httpContext">The request that was refused.</param>
    /// <param name="refusal">The refusal.</param>
    /// <returns>The problem to answer with.</returns>
    private ProblemDetails AnswerRefusal(HttpContext httpContext, StatusCodeException refusal)
    {
        HttpRequest request = httpContext.Request;
        this._logger.LogWarning(
            "Request {RequestMethod:l} {RequestPath:l} refused with {StatusCode}: {Reason:l}",
            request.Method,
            request.Path,
            refusal.StatusCode,
            refusal.Message);
        return new ProblemDetails
        {
            Status = refusal.StatusCode,
            Detail = refusal.Message,
        };
    }

    /// <summary>
    /// Logs a failure as an error, with the exception, and describes it as a 500 with no detail.
    /// </summary>
    /// <param name="httpContext">The request that failed.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>The problem to answer with.</returns>
    private ProblemDetails AnswerFailure(HttpContext httpContext, Exception exception)
    {
        HttpRequest request = httpContext.Request;
        this._logger.LogError(
            exception,
            "Request {RequestMethod:l} {RequestPath:l} failed with an unexpected exception",
            request.Method,
            request.Path);
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
        };
    }
}
