namespace MultiCloud.Adapter.Notes.Configuration;

using System;

/// <summary>
/// The name of an in-memory database. Connections that use the same name reach the same database.
/// </summary>
public readonly record struct DatabaseName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseName"/> class.
    /// </summary>
    /// <param name="value">The name; it must contain something other than white space.</param>
    public DatabaseName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Value { get; }
}
