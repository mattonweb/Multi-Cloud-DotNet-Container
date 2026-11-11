namespace MultiCloud.Application.Web.Models;

using MultiCloud.Adapter.Platform.Contracts;

/// <summary>
/// What the home page shows: where the application thinks it is running, which run this is, and the request as the
/// application sees it.
/// </summary>
public sealed record HomePage
{
    /// <summary>
    /// Gets this run of the application.
    /// </summary>
    required public ApplicationSession Session { get; init; }

    /// <summary>
    /// Gets the application's version.
    /// </summary>
    required public ApplicationVersion Version { get; init; }

    /// <summary>
    /// Gets the host environment's name, such as Development or Production.
    /// </summary>
    required public string EnvironmentName { get; init; }

    /// <summary>
    /// Gets the platform, as the platform adapter describes it.
    /// </summary>
    required public PlatformDescription Platform { get; init; }

    /// <summary>
    /// Gets the request's scheme as the application sees it. Behind a proxy that ends HTTPS, this is https only when
    /// forwarded headers are honored.
    /// </summary>
    required public string RequestScheme { get; init; }

    /// <summary>
    /// Gets the request's host as the application sees it.
    /// </summary>
    required public string RequestHost { get; init; }

    /// <summary>
    /// Gets the caller's address as the application sees it, or <see langword="null"/> when the connection has none.
    /// Behind a proxy, this is the caller's only when forwarded headers are honored.
    /// </summary>
    public string? RemoteAddress { get; init; }
}
