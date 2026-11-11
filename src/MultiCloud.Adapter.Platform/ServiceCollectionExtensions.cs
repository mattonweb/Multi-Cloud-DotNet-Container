namespace MultiCloud.Adapter.Platform;

using System;
using Microsoft.Extensions.DependencyInjection;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Adapter.Platform.Services;

/// <summary>
/// Registers the platform adapter: the one public way into it, besides its contracts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IPlatformDetector"/>, which reads the real process environment. The adapter takes no
    /// options: everything it reads, it reads from the environment.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <returns>The same service collection, so registrations can follow one another.</returns>
    public static IServiceCollection AddPlatformAdapter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IProcessEnvironment, ProcessEnvironment>();
        services.AddSingleton<IPlatformDetector, PlatformDetector>();
        return services;
    }
}
