namespace MultiCloud.Adapter.Platform.Services;

using System;
using System.Linq;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Adapter.Platform.DTOs;

/// <summary>
/// The platform table in docs/SDD.md section 5, in the order its rows are checked. A new platform is a new row here.
/// </summary>
/// <remarks>
/// The variable names were written from memory, not checked against each platform. Phase 1 can prove only the
/// container row; each cloud phase checks its own row against what the platform actually sets, and corrects this
/// table and the design together.
/// </remarks>
internal static class PlatformSignatures
{
    /// <summary>
    /// The variable Cloud Run sets to the service's name; it both identifies Cloud Run and names the service.
    /// </summary>
    private static readonly EnvironmentVariableName CloudRunService = new EnvironmentVariableName("K_SERVICE");

    /// <summary>
    /// The variable a container sets to its host name, which Kubernetes sets to the pod's name.
    /// </summary>
    private static readonly EnvironmentVariableName HostName = new EnvironmentVariableName("HOSTNAME");

    /// <summary>
    /// The rows, first checked first. The container row comes last because the .NET base image sets its variable on
    /// every platform above it as well.
    /// </summary>
    private static readonly PlatformSignature[] InOrder =
    [
        new PlatformSignature
        {
            IdentifiedBy = CloudRunService,
            Name = new PlatformName("Google Cloud Run"),
            CloudProvider = new CloudProvider("gcp"),
            CloudPlatform = new CloudPlatform("gcp_cloud_run"),
            ServiceVariable = CloudRunService,
            RevisionVariable = new EnvironmentVariableName("K_REVISION"),
        },
        new PlatformSignature
        {
            IdentifiedBy = new EnvironmentVariableName("ECS_CONTAINER_METADATA_URI_V4"),
            Name = new PlatformName("AWS ECS"),
            CloudProvider = new CloudProvider("aws"),
            CloudPlatform = new CloudPlatform("aws_ecs"),
            RegionVariable = new EnvironmentVariableName("AWS_REGION"),
        },
        new PlatformSignature
        {
            // Any cluster sets this, not only Google's, so the row claims no cloud provider or platform.
            IdentifiedBy = new EnvironmentVariableName("KUBERNETES_SERVICE_HOST"),
            Name = new PlatformName("Kubernetes"),
            InstanceVariable = HostName,
        },
        new PlatformSignature
        {
            IdentifiedBy = new EnvironmentVariableName("DOTNET_RUNNING_IN_CONTAINER"),
            Name = new PlatformName("Container, platform not identified"),
            InstanceVariable = HostName,
        },
    ];

    /// <summary>
    /// Finds the first row whose identifying variable is set to something other than white space.
    /// </summary>
    /// <param name="environment">The environment to read.</param>
    /// <returns>The row, or <see langword="null"/> when no row matches, which means a local process.</returns>
    internal static PlatformSignature? FindFirstMatch(IProcessEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return InOrder.FirstOrDefault(signature => IsSet(environment, signature.IdentifiedBy));
    }

    /// <summary>
    /// Says whether a variable is set to something other than white space.
    /// </summary>
    /// <param name="environment">The environment to read.</param>
    /// <param name="name">The variable's name.</param>
    /// <returns><see langword="true"/> when the variable has a value.</returns>
    private static bool IsSet(IProcessEnvironment environment, EnvironmentVariableName name)
    {
        string? value = environment.Read(name);
        return !string.IsNullOrWhiteSpace(value);
    }
}
