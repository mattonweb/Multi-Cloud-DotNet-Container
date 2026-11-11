namespace MultiCloud.Adapter.Platform.DTOs;

using System;

/// <summary>
/// The name of an environment variable, such as <c>K_SERVICE</c>.
/// </summary>
internal readonly record struct EnvironmentVariableName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public EnvironmentVariableName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the value as the platform or the table states it.
    /// </summary>
    public string Value { get; }
}
