namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The cloud provider, as OpenTelemetry's well-known value for the <c>cloud.provider</c> attribute, such as <c>gcp</c>.
/// </summary>
public readonly record struct CloudProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudProvider"/> class.
    /// </summary>
    /// <param name="value">The well-known value; it must contain something other than white space.</param>
    public CloudProvider(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
