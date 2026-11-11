namespace MultiCloud.Adapter.Platform.Services;

using System;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Adapter.Platform.DTOs;

/// <summary>
/// Describes the platform from the first row of <see cref="PlatformSignatures"/> that matches the environment, or as
/// a local process when none does.
/// </summary>
internal sealed class PlatformDetector : IPlatformDetector
{
    /// <summary>
    /// The name of the platform when no row matches: the application is a process on a developer's machine.
    /// </summary>
    private static readonly PlatformName LocalProcess = new PlatformName("Local process");

    private readonly IProcessEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformDetector"/> class.
    /// </summary>
    /// <param name="environment">The environment to read.</param>
    public PlatformDetector(IProcessEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        this._environment = environment;
    }

    /// <inheritdoc/>
    public PlatformDescription Detect()
    {
        PlatformSignature? signature = PlatformSignatures.FindFirstMatch(this._environment);
        if (signature is null)
        {
            return this.DescribeLocalProcess();
        }

        return this.Describe(signature);
    }

    /// <summary>
    /// Describes a local process, whose only other value is the machine's name.
    /// </summary>
    /// <returns>The description.</returns>
    private PlatformDescription DescribeLocalProcess()
    {
        string machineName = this._environment.ReadMachineName();
        return new PlatformDescription
        {
            Name = LocalProcess,
            Instance = new InstanceName(machineName),
        };
    }

    /// <summary>
    /// Describes the platform a row identifies, reading each of the row's other variables.
    /// </summary>
    /// <param name="signature">The row that matched.</param>
    /// <returns>The description.</returns>
    private PlatformDescription Describe(PlatformSignature signature)
    {
        return new PlatformDescription
        {
            Name = signature.Name,
            CloudProvider = signature.CloudProvider,
            CloudPlatform = signature.CloudPlatform,
            Region = this.ReadOptional(signature.RegionVariable, static value => new CloudRegion(value)),
            Service = this.ReadOptional(signature.ServiceVariable, static value => new DeployedServiceName(value)),
            Revision = this.ReadOptional(signature.RevisionVariable, static value => new RevisionName(value)),
            Instance = this.ReadOptional(signature.InstanceVariable, static value => new InstanceName(value)),
        };
    }

    /// <summary>
    /// Reads a variable a row names, and wraps its value.
    /// </summary>
    /// <typeparam name="T">The type that wraps the value.</typeparam>
    /// <param name="name">The variable's name, or <see langword="null"/> when the row names none.</param>
    /// <param name="wrap">Wraps the value.</param>
    /// <returns>The wrapped value, or <see langword="null"/> when the row names no variable or it is not set.</returns>
    private T? ReadOptional<T>(EnvironmentVariableName? name, Func<string, T> wrap)
        where T : struct
    {
        if (name is not EnvironmentVariableName variable)
        {
            return null;
        }

        string? value = this._environment.Read(variable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return wrap(value);
    }
}
