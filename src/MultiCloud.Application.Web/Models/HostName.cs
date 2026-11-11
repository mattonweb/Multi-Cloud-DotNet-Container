namespace MultiCloud.Application.Web.Models;

using System;

/// <summary>
/// The machine's name, which in a container is the container's host name. Every log line carries it as
/// <c>host.name</c>.
/// </summary>
public readonly record struct HostName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public HostName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Value { get; }
}
