namespace MultiCloud.Application.Web.Errors;

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Adds the session's identifier to every problem details body the application writes: a 400 for an invalid note, a
/// 404, a 403 or a 500 alike.
/// </summary>
internal sealed class SessionProblemDetails
{
    /// <summary>
    /// The name the identifier has in the body.
    /// </summary>
    internal const string SessionIdKey = "sessionId";

    private readonly ApplicationSession _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionProblemDetails"/> class.
    /// </summary>
    /// <param name="session">This run of the application.</param>
    public SessionProblemDetails(ApplicationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        this._session = session;
    }

    /// <summary>
    /// Adds the session's identifier to a problem about to be written.
    /// </summary>
    /// <param name="context">The problem and the request it answers.</param>
    public void Customize(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ProblemDetails problem = context.ProblemDetails;
        IDictionary<string, object?> extensions = problem.Extensions;
        SessionId sessionId = this._session.Id;
        extensions[SessionIdKey] = sessionId.ToString();
    }
}
