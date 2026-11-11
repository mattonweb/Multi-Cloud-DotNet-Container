namespace MultiCloud.Orchestrator.Heartbeat.DTOs;

using System;

/// <summary>
/// The .NET runtime the application runs on, as the runtime describes itself, such as ".NET 10.0.13".
/// </summary>
internal readonly record struct RuntimeVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeVersion"/> class.
    /// </summary>
    /// <param name="value">The description; it must contain something other than white space.</param>
    public RuntimeVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Value { get; }
}
