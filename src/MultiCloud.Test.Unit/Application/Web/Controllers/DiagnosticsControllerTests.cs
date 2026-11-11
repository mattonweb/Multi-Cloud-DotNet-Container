namespace MultiCloud.Test.Unit.Application.Web.Controllers;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.Controllers;
using MultiCloud.Application.Web.Errors;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="DiagnosticsController"/> class: the error routes
/// throw when the setting is on, and answer 404 without throwing when it is off.
/// </summary>
[TestClass]
public sealed class DiagnosticsControllerTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the 500 route throws an ordinary exception, which the global handler answers with 500.
    /// </summary>
    [TestMethod]
    public void Error500_RoutesEnabled_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        DiagnosticsController controller = CreateController(errorRoutesEnabled: true);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            controller.Error500();
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "The 500 route should throw an ordinary exception.");
    }

    /// <summary>
    /// Unit test to verify that the 403 route throws the application's own exception carrying 403.
    /// </summary>
    [TestMethod]
    public void Error403_RoutesEnabled_ThrowsStatusCodeExceptionCarrying403()
    {
        // Arrange (Given)
        DiagnosticsController controller = CreateController(errorRoutesEnabled: true);
        StatusCodeException? caughtException = null;

        // Act (When)
        try
        {
            controller.Error403();
        }
        catch (StatusCodeException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "The 403 route should throw the application's own exception.");
        Assert.AreEqual(
            StatusCodes.Status403Forbidden,
            caughtException.StatusCode,
            "The exception should carry 403.");
    }

    /// <summary>
    /// Unit test to verify that, with the setting off, the 500 route answers 404 and throws nothing.
    /// </summary>
    [TestMethod]
    public void Error500_RoutesDisabled_AnswersNotFound()
    {
        // Arrange (Given)
        DiagnosticsController controller = CreateController(errorRoutesEnabled: false);

        // Act (When)
        IActionResult result = controller.Error500();

        // Assert (Then)
        Assert.IsInstanceOfType<NotFoundResult>(
            result,
            "With the error routes off, the 500 route should answer 404.");
    }

    /// <summary>
    /// Unit test to verify that, with the setting off, the 403 route answers 404 and throws nothing.
    /// </summary>
    [TestMethod]
    public void Error403_RoutesDisabled_AnswersNotFound()
    {
        // Arrange (Given)
        DiagnosticsController controller = CreateController(errorRoutesEnabled: false);

        // Act (When)
        IActionResult result = controller.Error403();

        // Assert (Then)
        Assert.IsInstanceOfType<NotFoundResult>(
            result,
            "With the error routes off, the 403 route should answer 404.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the controller with the error routes on or off.
    /// </summary>
    /// <param name="errorRoutesEnabled">Whether the error routes throw.</param>
    /// <returns>The controller.</returns>
    private static DiagnosticsController CreateController(bool errorRoutesEnabled)
    {
        DiagnosticsOptions options = new DiagnosticsOptions
        {
            ErrorRoutesEnabled = errorRoutesEnabled,
        };
        return new DiagnosticsController(options);
    }

    #endregion Private Methods
}
