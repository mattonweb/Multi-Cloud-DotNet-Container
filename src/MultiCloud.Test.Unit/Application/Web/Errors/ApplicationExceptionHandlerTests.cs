namespace MultiCloud.Test.Unit.Application.Web.Errors;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.Errors;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ApplicationExceptionHandler"/> class: a
/// status-carrying exception is answered with its status and logged as a warning; anything else is a 500 logged as an
/// error with the exception.
/// </summary>
[TestClass]
public sealed class ApplicationExceptionHandlerTests
{
    private StubProblemDetailsService? _problems;
    private FakeLogCollector? _collector;
    private ApplicationExceptionHandler? _handler;
    private DefaultHttpContext? _httpContext;

    /// <summary>
    /// Builds the handler over a problem writer and a logger the tests read, and a request to a failing route.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        this._problems = new StubProblemDetailsService();
        this._collector = new FakeLogCollector();
        FakeLogger<ApplicationExceptionHandler> logger = new FakeLogger<ApplicationExceptionHandler>(this._collector);
        this._handler = new ApplicationExceptionHandler(this._problems, logger);
        this._httpContext = new DefaultHttpContext();
        HttpRequest request = this._httpContext.Request;
        request.Method = HttpMethods.Get;
        request.Path = "/healthz/error";
    }

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the application's own refusal is answered with its status and its message, and logged
    /// as a warning without the stack.
    /// </summary>
    [TestMethod]
    public async Task TryHandleAsync_StatusCodeException_AnswersItsStatusAndLogsWarning()
    {
        // Arrange (Given)
        StatusCodeException refusal = new StatusCodeException(StatusCodes.Status403Forbidden, "Refused by a test.");

        // Act (When)
        bool handled = await this._handler!
            .TryHandleAsync(this._httpContext!, refusal, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        ProblemDetails problem = this.ReadOnlyProblem();
        FakeLogRecord line = this._collector!.LatestRecord;
        Assert.IsTrue(
            handled,
            "The handler should handle the refusal.");
        Assert.AreEqual(
            StatusCodes.Status403Forbidden,
            this.ReadStatusCode(),
            "The response should carry the exception's status.");
        Assert.AreEqual(
            StatusCodes.Status403Forbidden,
            problem.Status,
            "The problem should carry the exception's status.");
        Assert.AreEqual(
            "Refused by a test.",
            problem.Detail,
            "The problem's detail should be the refusal's reason.");
        Assert.AreEqual(
            LogLevel.Warning,
            line.Level,
            "A refusal should be logged as a warning.");
        Assert.IsNull(
            line.Exception,
            "A refusal's warning should not carry the stack.");
    }

    /// <summary>
    /// Unit test to verify that any other exception is answered 500 with no detail, and logged as an error with the
    /// exception.
    /// </summary>
    [TestMethod]
    public async Task TryHandleAsync_OrdinaryException_Answers500AndLogsErrorWithException()
    {
        // Arrange (Given)
        InvalidOperationException failure = new InvalidOperationException("Failed in a test.");

        // Act (When)
        bool handled = await this._handler!
            .TryHandleAsync(this._httpContext!, failure, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        ProblemDetails problem = this.ReadOnlyProblem();
        FakeLogRecord line = this._collector!.LatestRecord;
        Assert.IsTrue(
            handled,
            "The handler should handle the failure.");
        Assert.AreEqual(
            StatusCodes.Status500InternalServerError,
            this.ReadStatusCode(),
            "The response should be a 500.");
        Assert.IsNull(
            problem.Detail,
            "A failure's problem should not say how the application works.");
        Assert.AreEqual(
            LogLevel.Error,
            line.Level,
            "A failure should be logged as an error.");
        Assert.AreSame(
            failure,
            line.Exception,
            "A failure's error should carry the exception.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Reads the one problem the handler wrote.
    /// </summary>
    /// <returns>The problem.</returns>
    private ProblemDetails ReadOnlyProblem()
    {
        Assert.HasCount(
            1,
            this._problems!.Written,
            "The handler should write exactly one problem.");
        ProblemDetailsContext context = this._problems.Written[0];
        return context.ProblemDetails;
    }

    /// <summary>
    /// Reads the status the handler set on the response.
    /// </summary>
    /// <returns>The status.</returns>
    private int ReadStatusCode()
    {
        HttpResponse response = this._httpContext!.Response;
        return response.StatusCode;
    }

    #endregion Private Methods
}
