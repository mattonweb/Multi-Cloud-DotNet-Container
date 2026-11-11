namespace MultiCloud.Application.Web.Models;

using System;

/// <summary>
/// The application's version, as its project file sets it, the same under <c>dotnet run</c> and in the image.
/// </summary>
public readonly record struct ApplicationVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationVersion"/> class.
    /// </summary>
    /// <param name="value">The version; it must contain something other than white space.</param>
    public ApplicationVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    public string Value { get; }
}
