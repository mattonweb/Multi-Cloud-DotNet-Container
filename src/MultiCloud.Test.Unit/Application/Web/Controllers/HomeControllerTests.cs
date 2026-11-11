namespace MultiCloud.Test.Unit.Application.Web.Controllers;

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Controllers;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HomeController"/> class: the root route reports,
/// as JSON, the application, the session, the platform and the request as the application sees it.
/// </summary>
[TestClass]
public sealed class HomeControllerTests
{
    private static readonly SessionId Session = new SessionId(new Guid("019a6b2c-1d2e-7f30-8a41-b52c63d74e85"));

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the root route reports the application's version and environment, and the session by
    /// the same identifier the response header and the log lines carry.
    /// </summary>
    [TestMethod]
    public void Index_AnyRequest_ReportsTheApplicationAndSession()
    {
        // Arrange (Given)
        HomeController controller = CreateController(CreateContainerPlatform(), CreateRequest());

        // Act (When)
        HomeResource home = ReadHome(controller.Index());

        // Assert (Then)
        Assert.AreEqual(
            "multi-cloud-dotnet-container",
            home.Service,
            "The service should be the application's name, as service.name gives it.");
        Assert.AreEqual(
            "0.1.0",
            home.Version,
            "The version should be the application's version.");
        Assert.AreEqual(
            "Production",
            home.Environment,
            "The environment should be the host environment's name.");
        Assert.AreEqual(
            "019a6b2c-1d2e-7f30-8a41-b52c63d74e85",
            home.Session.Id,
            "The session should be reported by the identifier X-Session-Id carries.");
    }

    /// <summary>
    /// Unit test to verify that the platform is reported as the platform adapter describes it, with each value it does
    /// not supply left null rather than guessed.
    /// </summary>
    [TestMethod]
    public void Index_UnidentifiedContainer_ReportsThePlatformWithAbsentValuesNull()
    {
        // Arrange (Given)
        HomeController controller = CreateController(CreateContainerPlatform(), CreateRequest());

        // Act (When)
        HomeResource home = ReadHome(controller.Index());

        // Assert (Then)
        Assert.AreEqual(
            "Container, platform not identified",
            home.Platform.Name,
            "The platform's name should be the adapter's.");
        Assert.AreEqual(
            "3f2a9c1b7e4d",
            home.Platform.Instance,
            "The instance should be the adapter's.");
        Assert.IsNull(
            home.Platform.CloudProvider,
            "A cloud provider the platform does not supply should be null.");
    }

    /// <summary>
    /// Unit test to verify that the request is reported as the application sees it, so a person can tell whether a
    /// proxy's forwarded headers were honored.
    /// </summary>
    [TestMethod]
    public void Index_ForwardedHttpsRequest_ReportsSchemeHostAndRemoteAddress()
    {
        // Arrange (Given)
        HomeController controller = CreateController(CreateContainerPlatform(), CreateRequest());

        // Act (When)
        HomeResource home = ReadHome(controller.Index());

        // Assert (Then)
        Assert.AreEqual(
            "https",
            home.Request.Scheme,
            "The scheme should be the one the application sees.");
        Assert.AreEqual(
            "example.test",
            home.Request.Host,
            "The host should be the one the application sees.");
        Assert.AreEqual(
            "203.0.113.7",
            home.Request.RemoteAddress,
            "The remote address should be the one the application sees.");
        CollectionAssert.Contains(
            home.Routes,
            "/healthz/ping",
            "The routes to try should include the liveness probe.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a platform description for a container on no platform the table knows.
    /// </summary>
    /// <returns>The platform.</returns>
    private static PlatformDescription CreateContainerPlatform()
    {
        return new PlatformDescription
        {
            Name = new PlatformName("Container, platform not identified"),
            Instance = new InstanceName("3f2a9c1b7e4d"),
        };
    }

    /// <summary>
    /// Creates a request as the application sees it once a proxy's forwarded headers have been applied.
    /// </summary>
    /// <returns>The request's context.</returns>
    private static DefaultHttpContext CreateRequest()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        HttpRequest request = context.Request;
        request.Scheme = "https";
        request.Host = new HostString("example.test");
        ConnectionInfo connection = context.Connection;
        connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        return context;
    }

    /// <summary>
    /// Creates the controller in production, for the session in <see cref="Session"/>, answering a request.
    /// </summary>
    /// <param name="platform">The platform.</param>
    /// <param name="httpContext">The request being answered.</param>
    /// <returns>The controller.</returns>
    private static HomeController CreateController(PlatformDescription platform, HttpContext httpContext)
    {
        ApplicationSession session = new ApplicationSession
        {
            Id = Session,
            StartedOn = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero),
        };
        ApplicationIdentity identity = new ApplicationIdentity
        {
            Version = new ApplicationVersion("0.1.0"),
            HostName = new HostName("3f2a9c1b7e4d"),
        };
        HostingEnvironment environment = new HostingEnvironment
        {
            EnvironmentName = "Production",
        };
        HomeController controller = new HomeController(session, identity, platform, environment);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    /// <summary>
    /// Reads the report out of the action's 200 answer.
    /// </summary>
    /// <param name="result">The action's answer.</param>
    /// <returns>The report.</returns>
    private static HomeResource ReadHome(ActionResult<HomeResource> result)
    {
        OkObjectResult ok = (OkObjectResult)result.Result!;
        return (HomeResource)ok.Value!;
    }

    #endregion Private Methods
}
