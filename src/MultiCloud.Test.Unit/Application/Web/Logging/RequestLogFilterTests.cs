namespace MultiCloud.Test.Unit.Application.Web.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.Logging;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="RequestLogFilter"/> class: the health probes are
/// left out of the request log, and everything else, the error routes included, is in it.
/// </summary>
[TestClass]
public sealed class RequestLogFilterTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the liveness probe, which platforms call every few seconds, is left out.
    /// </summary>
    [TestMethod]
    public void IsLogged_LivenessProbe_ReturnsFalse()
    {
        // Arrange (Given)
        HttpContext context = CreateContext("/healthz/ping");

        // Act (When)
        bool logged = RequestLogFilter.IsLogged(context);

        // Assert (Then)
        Assert.IsFalse(
            logged,
            "The liveness probe should be left out of the request log.");
    }

    /// <summary>
    /// Unit test to verify that the readiness probe is left out, however its path is capitalized.
    /// </summary>
    [TestMethod]
    public void IsLogged_ReadinessProbeInCapitals_ReturnsFalse()
    {
        // Arrange (Given)
        HttpContext context = CreateContext("/HEALTHZ/READY");

        // Act (When)
        bool logged = RequestLogFilter.IsLogged(context);

        // Assert (Then)
        Assert.IsFalse(
            logged,
            "The readiness probe should be left out of the request log in any case.");
    }

    /// <summary>
    /// Unit test to verify that the error routes, which share the probes' prefix, are logged.
    /// </summary>
    [TestMethod]
    public void IsLogged_ErrorRoute_ReturnsTrue()
    {
        // Arrange (Given)
        HttpContext context = CreateContext("/healthz/error/500");

        // Act (When)
        bool logged = RequestLogFilter.IsLogged(context);

        // Assert (Then)
        Assert.IsTrue(
            logged,
            "The error routes should be in the request log.");
    }

    /// <summary>
    /// Unit test to verify that the notes API is logged.
    /// </summary>
    [TestMethod]
    public void IsLogged_NotesRoute_ReturnsTrue()
    {
        // Arrange (Given)
        HttpContext context = CreateContext("/api/notes/1");

        // Act (When)
        bool logged = RequestLogFilter.IsLogged(context);

        // Assert (Then)
        Assert.IsTrue(
            logged,
            "The notes API should be in the request log.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a request to a path.
    /// </summary>
    /// <param name="path">The request's path.</param>
    /// <returns>The request's context.</returns>
    private static HttpContext CreateContext(string path)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        HttpRequest request = context.Request;
        request.Path = path;
        return context;
    }

    #endregion Private Methods
}
