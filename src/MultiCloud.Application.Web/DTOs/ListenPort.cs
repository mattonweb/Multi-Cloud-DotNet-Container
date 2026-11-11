namespace MultiCloud.Application.Web.DTOs;

using System;
using System.Globalization;

/// <summary>
/// The port a platform tells the application to listen on, through the <c>PORT</c> environment variable.
/// </summary>
internal readonly record struct ListenPort
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenPort"/> class.
    /// </summary>
    /// <param name="value">The port, from 1 to 65535.</param>
    public ListenPort(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 65535);
        this.Value = value;
    }

    /// <summary>
    /// Gets the port.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Renders the port as the number in invariant culture, as the host's settings take it.
    /// </summary>
    /// <returns>The rendering.</returns>
    public override string ToString()
    {
        return this.Value.ToString(CultureInfo.InvariantCulture);
    }
}
