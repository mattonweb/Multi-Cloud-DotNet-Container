namespace MultiCloud.Adapter.Platform.DTOs;

using MultiCloud.Adapter.Platform.Contracts;

/// <summary>
/// One row of the platform table in docs/SDD.md section 5: the variable that identifies a platform, what the platform
/// is called, and which variables supply its other values.
/// </summary>
internal sealed record PlatformSignature
{
    /// <summary>
    /// Gets the variable whose presence identifies the platform.
    /// </summary>
    required public EnvironmentVariableName IdentifiedBy { get; init; }

    /// <summary>
    /// Gets the platform's name for people.
    /// </summary>
    required public PlatformName Name { get; init; }

    /// <summary>
    /// Gets OpenTelemetry's well-known cloud provider, or <see langword="null"/> when there is none for this row.
    /// </summary>
    public CloudProvider? CloudProvider { get; init; }

    /// <summary>
    /// Gets OpenTelemetry's well-known cloud platform, or <see langword="null"/> when there is none for this row.
    /// </summary>
    public CloudPlatform? CloudPlatform { get; init; }

    /// <summary>
    /// Gets the variable that holds the region, or <see langword="null"/> when this platform supplies none.
    /// </summary>
    public EnvironmentVariableName? RegionVariable { get; init; }

    /// <summary>
    /// Gets the variable that holds the deployed service's name, or <see langword="null"/> when this platform
    /// supplies none.
    /// </summary>
    public EnvironmentVariableName? ServiceVariable { get; init; }

    /// <summary>
    /// Gets the variable that holds the deployed revision's name, or <see langword="null"/> when this platform
    /// supplies none.
    /// </summary>
    public EnvironmentVariableName? RevisionVariable { get; init; }

    /// <summary>
    /// Gets the variable that holds this instance's name, or <see langword="null"/> when this platform supplies none.
    /// </summary>
    public EnvironmentVariableName? InstanceVariable { get; init; }
}
