namespace MultiCloud.Test.Unit.Application.Web.Errors;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.Errors;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SessionProblemDetails"/> class.
/// </summary>
[TestClass]
public sealed class SessionProblemDetailsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that every problem the application writes carries the session's identifier, so a caller
    /// can find the matching log lines.
    /// </summary>
    [TestMethod]
    public void Customize_AnyProblem_AddsTheSessionIdentifier()
    {
        // Arrange (Given)
        SessionId sessionId = new SessionId(new Guid("019a6b2c-1d2e-7f30-8a41-b52c63d74e85"));
        ApplicationSession session = new ApplicationSession
        {
            Id = sessionId,
            StartedOn = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero),
        };
        ProblemDetails problem = new ProblemDetails();
        ProblemDetailsContext context = new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext(),
            ProblemDetails = problem,
        };

        // Act (When)
        new SessionProblemDetails(session).Customize(context);

        // Assert (Then)
        Assert.AreEqual(
            "019a6b2c-1d2e-7f30-8a41-b52c63d74e85",
            problem.Extensions["sessionId"],
            "The problem should carry the session's identifier, as the X-Session-Id header shows it.");
    }

    #endregion Public Methods
}
