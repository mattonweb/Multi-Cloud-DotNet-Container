namespace MultiCloud.Application.Web.Models;

using System;

/// <summary>
/// One run of one instance of the application, created once when the process starts. Every response header, log line
/// and heartbeat from the run carries its identifier, which is how one run is told from the next.
/// </summary>
public sealed record ApplicationSession
{
    /// <summary>
    /// Gets the session's identifier.
    /// </summary>
    required public SessionId Id { get; init; }

    /// <summary>
    /// Gets when the session started, in UTC.
    /// </summary>
    required public DateTimeOffset StartedOn { get; init; }
}
