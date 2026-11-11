namespace MultiCloud.Orchestrator.Heartbeat.DTOs;

using System;

/// <summary>
/// The operating system the application runs on, as the runtime describes it, such as "Ubuntu 24.04.3 LTS".
/// </summary>
internal readonly record struct OperatingSystemDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperatingSystemDescription"/> class.
    /// </summary>
    /// <param name="value">The description; it must contain something other than white space.</param>
    public OperatingSystemDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Value { get; }
}
