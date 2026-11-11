namespace MultiCloud.Adapter.Platform.Contracts;

/// <summary>
/// Where the application is running, as far as the platform says. A value the platform does not supply is absent,
/// never guessed.
/// </summary>
public sealed record PlatformDescription
{
    /// <summary>
    /// Gets the platform's name for people, which the home page and the <c>X-Platform</c> header show.
    /// </summary>
    required public PlatformName Name { get; init; }

    /// <summary>
    /// Gets OpenTelemetry's well-known value for the cloud provider, or <see langword="null"/> when the platform has
    /// none, as for a cluster that could be anyone's Kubernetes.
    /// </summary>
    public CloudProvider? CloudProvider { get; init; }

    /// <summary>
    /// Gets OpenTelemetry's well-known value for the cloud platform, or <see langword="null"/> when the platform has
    /// none.
    /// </summary>
    public CloudPlatform? CloudPlatform { get; init; }

    /// <summary>
    /// Gets the region, or <see langword="null"/> when the platform does not say.
    /// </summary>
    public CloudRegion? Region { get; init; }

    /// <summary>
    /// Gets the name of the deployed service, or <see langword="null"/> when the platform does not say.
    /// </summary>
    public DeployedServiceName? Service { get; init; }

    /// <summary>
    /// Gets the name of the deployed revision, or <see langword="null"/> when the platform does not say.
    /// </summary>
    public RevisionName? Revision { get; init; }

    /// <summary>
    /// Gets the name of this instance, or <see langword="null"/> when the platform does not say.
    /// </summary>
    public InstanceName? Instance { get; init; }
}
