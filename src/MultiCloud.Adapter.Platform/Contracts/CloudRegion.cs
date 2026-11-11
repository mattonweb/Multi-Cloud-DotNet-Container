namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The cloud region the application runs in, such as <c>us-east-1</c>.
/// </summary>
public readonly record struct CloudRegion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudRegion"/> class.
    /// </summary>
    /// <param name="value">The region; it must contain something other than white space.</param>
    public CloudRegion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
