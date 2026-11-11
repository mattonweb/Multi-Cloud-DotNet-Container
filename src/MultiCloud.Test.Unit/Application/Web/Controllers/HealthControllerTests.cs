namespace MultiCloud.Test.Unit.Application.Web.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.Controllers;
using MultiCloud.Application.Web.Hosting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HealthController"/> class: liveness always, and
/// readiness in each state of the host.
/// </summary>
/// <remarks>
/// The host's state comes from the framework's own <see cref="ApplicationLifetime"/>, moved through starting, started
/// and stopping as the host would move it.
/// </remarks>
[TestClass]
public sealed class HealthControllerTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that liveness answers 200 with exactly <c>up</c>, whatever the host's state, since it has
    /// no dependencies.
    /// </summary>
    [TestMethod]
    public void Ping_HostStillStarting_AnswersUp()
    {
        // Arrange (Given)
        ApplicationLifetime lifetime = new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance);
        HealthController controller = new HealthController(new HostReadiness(lifetime));

        // Act (When)
        ContentResult result = controller.Ping();

        // Assert (Then)
        Assert.AreEqual(
            "up",
            result.Content,
            "Liveness should answer exactly up.");
        Assert.IsNull(
            result.StatusCode,
            "Liveness should answer the default status, 200.");
    }

    /// <summary>
    /// Unit test to verify that an instance whose host has not finished starting is not ready for traffic.
    /// </summary>
    [TestMethod]
    public void Ready_HostStillStarting_AnswersNotReadyWith503()
    {
        // Arrange (Given)
        ApplicationLifetime lifetime = new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance);
        HealthController controller = new HealthController(new HostReadiness(lifetime));

        // Act (When)
        ContentResult result = controller.Ready();

        // Assert (Then)
        Assert.AreEqual(
            "not ready",
            result.Content,
            "A host that is still starting should not be ready.");
        Assert.AreEqual(
            StatusCodes.Status503ServiceUnavailable,
            result.StatusCode,
            "Not ready should be a 503.");
    }

    /// <summary>
    /// Unit test to verify that an instance whose host has started is ready for traffic.
    /// </summary>
    [TestMethod]
    public void Ready_HostStarted_AnswersReady()
    {
        // Arrange (Given)
        ApplicationLifetime lifetime = new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance);
        lifetime.NotifyStarted();
        HealthController controller = new HealthController(new HostReadiness(lifetime));

        // Act (When)
        ContentResult result = controller.Ready();

        // Assert (Then)
        Assert.AreEqual(
            "ready",
            result.Content,
            "A started host should be ready.");
        Assert.IsNull(
            result.StatusCode,
            "Ready should answer the default status, 200.");
    }

    /// <summary>
    /// Unit test to verify that an instance whose host has begun stopping is no longer ready, so a platform stops
    /// sending it requests.
    /// </summary>
    [TestMethod]
    public void Ready_HostStopping_AnswersNotReadyWith503()
    {
        // Arrange (Given)
        ApplicationLifetime lifetime = new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance);
        lifetime.NotifyStarted();
        lifetime.StopApplication();
        HealthController controller = new HealthController(new HostReadiness(lifetime));

        // Act (When)
        ContentResult result = controller.Ready();

        // Assert (Then)
        Assert.AreEqual(
            "not ready",
            result.Content,
            "A host that has begun stopping should not be ready.");
        Assert.AreEqual(
            StatusCodes.Status503ServiceUnavailable,
            result.StatusCode,
            "Not ready should be a 503.");
    }

    #endregion Public Methods
}
