namespace MultiCloud.Test.Unit.Application.Web.Logging;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Logging;
using MultiCloud.Application.Web.Models;
using Serilog.Events;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="TelemetryEnricher"/> class: every log line gets
/// OpenTelemetry's names for the session, version, environment, platform and host, and an absent platform value is
/// left off.
/// </summary>
[TestClass]
public sealed class TelemetryEnricherTests
{
    private static readonly SessionId Session = new SessionId(new Guid("019a6b2c-1d2e-7f30-8a41-b52c63d74e85"));

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the session, version, environment and host are added under their dotted names.
    /// </summary>
    [TestMethod]
    public void Enrich_UnidentifiedContainer_AddsTheServiceAndHostProperties()
    {
        // Arrange (Given)
        PlatformDescription platform = new PlatformDescription
        {
            Name = new PlatformName("Container, platform not identified"),
        };
        LogEvent logEvent = CreateLogEvent();

        // Act (When)
        CreateEnricher(platform).Enrich(logEvent, propertyFactory: null!);

        // Assert (Then)
        IReadOnlyDictionary<string, LogEventPropertyValue> properties = logEvent.Properties;
        Assert.AreEqual(
            "multi-cloud-dotnet-container",
            ReadText(properties, "service.name"),
            "service.name should be the application's name.");
        Assert.AreEqual(
            "0.1.0",
            ReadText(properties, "service.version"),
            "service.version should be the application's version.");
        Assert.AreEqual(
            Session.ToString(),
            ReadText(properties, "service.instance.id"),
            "service.instance.id should be the session's identifier.");
        Assert.AreEqual(
            "Production",
            ReadText(properties, "deployment.environment.name"),
            "deployment.environment.name should be the host environment's name.");
        Assert.AreEqual(
            "3f2a9c1b7e4d",
            ReadText(properties, "host.name"),
            "host.name should be the machine's name.");
        Assert.IsFalse(
            properties.ContainsKey("cloud.provider"),
            "A platform with no cloud provider should leave cloud.provider off the line.");
    }

    /// <summary>
    /// Unit test to verify that a platform's well-known cloud values are added when the platform has them.
    /// </summary>
    [TestMethod]
    public void Enrich_CloudRun_AddsTheCloudProperties()
    {
        // Arrange (Given)
        PlatformDescription platform = new PlatformDescription
        {
            Name = new PlatformName("Google Cloud Run"),
            CloudProvider = new CloudProvider("gcp"),
            CloudPlatform = new CloudPlatform("gcp_cloud_run"),
        };
        LogEvent logEvent = CreateLogEvent();

        // Act (When)
        CreateEnricher(platform).Enrich(logEvent, propertyFactory: null!);

        // Assert (Then)
        IReadOnlyDictionary<string, LogEventPropertyValue> properties = logEvent.Properties;
        Assert.AreEqual(
            "gcp",
            ReadText(properties, "cloud.provider"),
            "cloud.provider should be OpenTelemetry's value.");
        Assert.AreEqual(
            "gcp_cloud_run",
            ReadText(properties, "cloud.platform"),
            "cloud.platform should be OpenTelemetry's value.");
        Assert.IsFalse(
            properties.ContainsKey("cloud.region"),
            "A region the platform did not supply should be left off the line.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates an enricher for a session in production, on a known host, on the platform given.
    /// </summary>
    /// <param name="platform">The platform.</param>
    /// <returns>The enricher.</returns>
    private static TelemetryEnricher CreateEnricher(PlatformDescription platform)
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
        return new TelemetryEnricher(session, identity, platform, environment);
    }

    /// <summary>
    /// Creates a log line with no properties of its own.
    /// </summary>
    /// <returns>The log line.</returns>
    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(DateTimeOffset.UnixEpoch, LogEventLevel.Information, null, MessageTemplate.Empty, []);
    }

    /// <summary>
    /// Reads a property's text.
    /// </summary>
    /// <param name="properties">The log line's properties.</param>
    /// <param name="name">The property's name.</param>
    /// <returns>The property's text.</returns>
    private static object? ReadText(IReadOnlyDictionary<string, LogEventPropertyValue> properties, string name)
    {
        ScalarValue value = (ScalarValue)properties[name];
        return value.Value;
    }

    #endregion Private Methods
}
