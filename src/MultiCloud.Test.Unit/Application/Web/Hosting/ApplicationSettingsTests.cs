namespace MultiCloud.Test.Unit.Application.Web.Hosting;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.DTOs;
using MultiCloud.Application.Web.Hosting;
using MultiCloud.Application.Web.Models;
using MultiCloud.Orchestrator.Heartbeat.Configuration;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ApplicationSettings"/> class: each setting is read,
/// and a setting that would leave the application in a bad state stops it at startup with a message that names it.
/// </summary>
[TestClass]
public sealed class ApplicationSettingsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the heartbeat's wait is read as configured.
    /// </summary>
    [TestMethod]
    public void ReadHeartbeatWait_FifteenSeconds_ReturnsFifteenSeconds()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Heartbeat:WaitInSeconds", "15");

        // Act (When)
        HeartbeatWait wait = settings.ReadHeartbeatWait();

        // Assert (Then)
        Assert.AreEqual(
            15,
            wait.Seconds,
            "The wait should be read as configured.");
    }

    /// <summary>
    /// Unit test to verify that a wait below 1 stops the application at startup with a message that names the setting,
    /// so the person who set it knows which one to fix.
    /// </summary>
    [TestMethod]
    public void ReadHeartbeatWait_ZeroSeconds_ThrowsNamingTheSetting()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Heartbeat:WaitInSeconds", "0");
        InvalidOperationException? caughtException = null;

        // Act (When)
        try
        {
            settings.ReadHeartbeatWait();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A wait of 0 should stop the application with InvalidOperationException.");
        StringAssert.Contains(
            caughtException.Message,
            "Heartbeat:WaitInSeconds",
            "The message should name the setting that is wrong.");
    }

    /// <summary>
    /// Unit test to verify that a wait that is not a whole number stops the application at startup.
    /// </summary>
    [TestMethod]
    public void ReadHeartbeatWait_NotANumber_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Heartbeat:WaitInSeconds", "ten");
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            settings.ReadHeartbeatWait();
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A wait that is not a whole number should stop the application.");
    }

    /// <summary>
    /// Unit test to verify that a setting with no value stops the application, since appsettings.json gives every
    /// setting its default and there is no second, silent one.
    /// </summary>
    [TestMethod]
    public void ReadHeartbeatWait_Missing_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Unrelated:Setting", "1");
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            settings.ReadHeartbeatWait();
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A missing wait should stop the application.");
    }

    /// <summary>
    /// Unit test to verify that the error routes can be turned off.
    /// </summary>
    [TestMethod]
    public void ReadDiagnostics_ErrorRoutesFalse_TurnsThemOff()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Diagnostics:ErrorRoutesEnabled", "false");

        // Act (When)
        DiagnosticsOptions diagnostics = settings.ReadDiagnostics();

        // Assert (Then)
        Assert.IsFalse(
            diagnostics.ErrorRoutesEnabled,
            "False should turn the error routes off.");
    }

    /// <summary>
    /// Unit test to verify that the shutdown timeout is read in seconds.
    /// </summary>
    [TestMethod]
    public void ReadShutdownTimeout_EightSeconds_ReturnsEightSeconds()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Host:ShutdownTimeoutInSeconds", "8");

        // Act (When)
        TimeSpan timeout = settings.ReadShutdownTimeout();

        // Assert (Then)
        Assert.AreEqual(
            TimeSpan.FromSeconds(8),
            timeout,
            "The timeout should be read in seconds.");
    }

    /// <summary>
    /// Unit test to verify that the number of trusted forwarding proxies is read as configured, as GKE's deployment
    /// sets it.
    /// </summary>
    [TestMethod]
    public void ReadForwardLimit_Two_ReturnsTwo()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("ForwardedHeaders:ForwardLimit", "2");

        // Act (When)
        ForwardLimit limit = settings.ReadForwardLimit();

        // Assert (Then)
        Assert.AreEqual(
            2,
            limit.Value,
            "The limit should be read as configured.");
    }

    /// <summary>
    /// Unit test to verify that a limit below 1 stops the application at startup with a message that names the
    /// setting.
    /// </summary>
    [TestMethod]
    public void ReadForwardLimit_Zero_ThrowsNamingTheSetting()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("ForwardedHeaders:ForwardLimit", "0");
        InvalidOperationException? caughtException = null;

        // Act (When)
        try
        {
            settings.ReadForwardLimit();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A limit of 0 should stop the application with InvalidOperationException.");
        StringAssert.Contains(
            caughtException.Message,
            "ForwardedHeaders:ForwardLimit",
            "The message should name the setting that is wrong.");
    }

    /// <summary>
    /// Unit test to verify that no port is chosen when the platform sets none, so the framework's default applies.
    /// </summary>
    [TestMethod]
    public void ReadPort_NotSet_ReturnsNull()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("Unrelated:Setting", "1");

        // Act (When)
        ListenPort? port = settings.ReadPort();

        // Assert (Then)
        Assert.IsNull(
            port,
            "With no PORT, no port should be chosen.");
    }

    /// <summary>
    /// Unit test to verify that the port a platform sets is read.
    /// </summary>
    [TestMethod]
    public void ReadPort_Set_ReturnsThePort()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("PORT", "9090");

        // Act (When)
        ListenPort? port = settings.ReadPort();

        // Assert (Then)
        Assert.AreEqual(
            new ListenPort(9090),
            port,
            "The platform's port should be read.");
    }

    /// <summary>
    /// Unit test to verify that a port out of range stops the application at startup with a message that names PORT.
    /// </summary>
    [TestMethod]
    public void ReadPort_OutOfRange_ThrowsNamingTheSetting()
    {
        // Arrange (Given)
        ApplicationSettings settings = CreateSettings("PORT", "70000");
        InvalidOperationException? caughtException = null;

        // Act (When)
        try
        {
            settings.ReadPort();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A port out of range should stop the application with InvalidOperationException.");
        StringAssert.Contains(
            caughtException.Message,
            "PORT",
            "The message should name the setting that is wrong.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates settings over a configuration that holds one value.
    /// </summary>
    /// <param name="key">The setting's name.</param>
    /// <param name="value">The setting's value.</param>
    /// <returns>The settings.</returns>
    private static ApplicationSettings CreateSettings(string key, string value)
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>
        {
            [key] = value,
        };
        ConfigurationBuilder builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfiguration configuration = builder.Build();
        return new ApplicationSettings(configuration);
    }

    #endregion Private Methods
}
