namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The name the platform gives the deployed service, such as a Cloud Run service's name.
/// </summary>
public readonly record struct DeployedServiceName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeployedServiceName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public DeployedServiceName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
