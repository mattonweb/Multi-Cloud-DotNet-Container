namespace MultiCloud.Orchestrator.Heartbeat.Configuration;

using System;

/// <summary>
/// What the host supplies to register the heartbeat. The orchestrator reads no configuration of its own.
/// </summary>
/// <remarks>
/// The session's identifier and the platform are on every log line already, through the host's logging, so the
/// heartbeat needs only the moment the session started (docs/SDD.md, decision D2).
/// </remarks>
public sealed record HeartbeatOptions
{
    /// <summary>
    /// Gets how long to wait between <c>Running</c> heartbeats.
    /// </summary>
    required public HeartbeatWait Wait { get; init; }

    /// <summary>
    /// Gets when this run of the application, its session, started.
    /// </summary>
    required public DateTimeOffset SessionStartedOn { get; init; }
}
