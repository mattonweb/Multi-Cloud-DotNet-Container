namespace MultiCloud.Adapter.Platform.Contracts;

using System;

/// <summary>
/// The cloud platform, as OpenTelemetry's well-known value for the <c>cloud.platform</c> attribute, such as <c>gcp_cloud_run</c>.
/// </summary>
public readonly record struct CloudPlatform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudPlatform"/> class.
    /// </summary>
    /// <param name="value">The well-known value; it must contain something other than white space.</param>
    public CloudPlatform(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
