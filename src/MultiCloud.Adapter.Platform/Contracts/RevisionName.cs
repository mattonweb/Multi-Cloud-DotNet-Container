namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The name the platform gives the deployed revision of the service.
/// </summary>
public readonly record struct RevisionName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public RevisionName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
