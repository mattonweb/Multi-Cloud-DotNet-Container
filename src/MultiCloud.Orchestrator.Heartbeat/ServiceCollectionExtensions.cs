namespace MultiCloud.Orchestrator.Heartbeat;

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using MultiCloud.Orchestrator.Heartbeat.DTOs;
using MultiCloud.Orchestrator.Heartbeat.Services;

/// <summary>
/// Registers the heartbeat orchestrator: the one public way into it, besides its options.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the heartbeat as a hosted service, so it starts and stops with the host.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="options">What the host supplies: the wait, and when the session started.</param>
    /// <returns>The same service collection, so registrations can follow one another.</returns>
    public static IServiceCollection AddHeartbeatOrchestrator(this IServiceCollection services, HeartbeatOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // The system clock, unless the host or a test has registered another one first.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddSingleton(ReadRuntimeFacts());
        services.AddSingleton<HeartbeatWriter>();
        services.AddHostedService<HeartbeatService>();
        return services;
    }

    /// <summary>
    /// Reads, once, what every heartbeat reports about the process.
    /// </summary>
    /// <returns>The runtime and the operating system.</returns>
    private static RuntimeFacts ReadRuntimeFacts()
    {
        return new RuntimeFacts
        {
            Runtime = new RuntimeVersion(RuntimeInformation.FrameworkDescription),
            OperatingSystem = new OperatingSystemDescription(RuntimeInformation.OSDescription),
        };
    }
}
