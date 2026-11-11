namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The name of this instance of the application, such as a pod's name or a machine's name.
/// </summary>
public readonly record struct InstanceName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstanceName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public InstanceName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
