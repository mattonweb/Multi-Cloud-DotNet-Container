namespace MultiCloud.Application.Web.Errors;

using System;

/// <summary>
/// The application's own exception that carries the HTTP status to answer with. ASP.NET Core has no built-in one.
/// </summary>
/// <remarks>
/// <see cref="ApplicationExceptionHandler"/> answers it with its status and logs it as a warning, where any other
/// exception is a 500 logged as an error.
/// </remarks>
public sealed class StatusCodeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status to answer with, from 400 to 599.</param>
    /// <param name="message">Why the request was refused; the caller sees it as the problem's detail.</param>
    public StatusCodeException(int statusCode, string message)
        : base(message)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(statusCode, 400);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(statusCode, 599);
        this.StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status to answer with.
    /// </summary>
    public int StatusCode { get; }
}
