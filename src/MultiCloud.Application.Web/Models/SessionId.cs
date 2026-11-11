namespace MultiCloud.Application.Web.Models;

using System;
using System.Globalization;

/// <summary>
/// Identifies one session: one run of one instance of the application. It is not a user's session.
/// </summary>
public readonly record struct SessionId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionId"/> class.
    /// </summary>
    /// <param name="value">The identifier, a version 7 UUID; it must not be empty.</param>
    public SessionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A session identifier cannot be the empty UUID.", nameof(value));
        }

        this.Value = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Renders the identifier as the response header, the log property and the problem details all show it: 32 hex
    /// digits in groups, in lower case.
    /// </summary>
    /// <returns>The rendering.</returns>
    public override string ToString()
    {
        return this.Value.ToString("D", CultureInfo.InvariantCulture);
    }
}
