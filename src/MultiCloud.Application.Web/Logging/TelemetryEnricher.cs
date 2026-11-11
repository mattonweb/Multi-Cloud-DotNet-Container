namespace MultiCloud.Application.Web.Logging;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.Models;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Puts the session, the version, the environment, the platform and the host on every log line, under OpenTelemetry's
/// semantic convention names, so a log system that knows the conventions recognizes them.
/// </summary>
/// <remarks>
/// The names contain dots, which a message template's property names cannot, so they are added here as properties
/// rather than written into messages. A platform value the platform adapter found absent is left off the line, never
/// written empty.
/// </remarks>
internal sealed class TelemetryEnricher : ILogEventEnricher
{
    /// <summary>
    /// The application's name in every log system, OpenTelemetry's <c>service.name</c>.
    /// </summary>
    internal const string ServiceName = "multi-cloud-dotnet-container";

    private readonly LogEventProperty[] _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryEnricher"/> class, which builds the properties once.
    /// </summary>
    /// <param name="session">This run of the application; its identifier is the instance's.</param>
    /// <param name="identity">The application's version and the machine's name.</param>
    /// <param name="platform">The platform, which supplies the cloud's values when it knows them.</param>
    /// <param name="environment">The host environment, which supplies the environment's name.</param>
    public TelemetryEnricher(
        ApplicationSession session,
        ApplicationIdentity identity,
        PlatformDescription platform,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(environment);
        SessionId sessionId = session.Id;
        ApplicationVersion version = identity.Version;
        HostName hostName = identity.HostName;
        List<LogEventProperty> properties = [];
        Add(properties, "service.name", ServiceName);
        Add(properties, "service.version", version.Value);
        Add(properties, "service.instance.id", sessionId.ToString());
        Add(properties, "deployment.environment.name", environment.EnvironmentName);
        AddWhenKnown(properties, "cloud.provider", platform.CloudProvider?.Value);
        AddWhenKnown(properties, "cloud.platform", platform.CloudPlatform?.Value);
        AddWhenKnown(properties, "cloud.region", platform.Region?.Value);
        Add(properties, "host.name", hostName.Value);
        this._properties = [.. properties];
    }

    /// <summary>
    /// Adds each property to a log line that does not already have one by that name.
    /// </summary>
    /// <param name="logEvent">The log line.</param>
    /// <param name="propertyFactory">Not used; the properties were built once, in the constructor.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        foreach (LogEventProperty property in this._properties)
        {
            logEvent.AddPropertyIfAbsent(property);
        }
    }

    /// <summary>
    /// Adds a property with a value.
    /// </summary>
    /// <param name="properties">The properties built so far.</param>
    /// <param name="name">The property's OpenTelemetry name.</param>
    /// <param name="value">The property's value.</param>
    private static void Add(List<LogEventProperty> properties, string name, string value)
    {
        properties.Add(new LogEventProperty(name, new ScalarValue(value)));
    }

    /// <summary>
    /// Adds a property only when its value is known.
    /// </summary>
    /// <param name="properties">The properties built so far.</param>
    /// <param name="name">The property's OpenTelemetry name.</param>
    /// <param name="value">The property's value, or <see langword="null"/> when the platform does not supply it.</param>
    private static void AddWhenKnown(List<LogEventProperty> properties, string name, string? value)
    {
        if (value is null)
        {
            return;
        }

        Add(properties, name, value);
    }
}
