namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The name of a hosting platform as a person reads it, such as "Google Cloud Run".
/// </summary>
public readonly record struct PlatformName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public PlatformName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
