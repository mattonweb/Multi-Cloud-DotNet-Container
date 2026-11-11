namespace MultiCloud.Orchestrator.Heartbeat.Services;

using System;
using Microsoft.Extensions.Logging;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using MultiCloud.Orchestrator.Heartbeat.DTOs;

/// <summary>
/// Composes one heartbeat and writes it through the logger. <see cref="HeartbeatService"/> decides when.
/// </summary>
/// <remarks>
/// The message carries only what is the heartbeat's own: its kind and number, the seconds since the session started,
/// the configured wait, and the runtime and operating system. The session, version, environment and platform are on
/// every log line already, so the message does not repeat them. It never writes environment variables or
/// configuration values wholesale, which can hold secrets.
/// </remarks>
internal sealed class HeartbeatWriter
{
    /// <summary>
    /// The heartbeat's one message template. Its property names are what a log search for heartbeats uses.
    /// </summary>
    private const string Template =
        "Heartbeat {HeartbeatKind} {HeartbeatSequence}: {SecondsSinceStart} seconds into the session, "
        + "every {WaitInSeconds} seconds, on {RuntimeVersion:l} and {OperatingSystem:l}";

    private readonly HeartbeatOptions _options;
    private readonly RuntimeFacts _runtime;
    private readonly TimeProvider _clock;
    private readonly ILogger<HeartbeatWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatWriter"/> class.
    /// </summary>
    /// <param name="options">The wait, and when the session started.</param>
    /// <param name="runtime">The runtime and operating system every heartbeat reports.</param>
    /// <param name="clock">Says how long the session has run.</param>
    /// <param name="logger">Where the heartbeat is written.</param>
    public HeartbeatWriter(
        HeartbeatOptions options,
        RuntimeFacts runtime,
        TimeProvider clock,
        ILogger<HeartbeatWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        this._options = options;
        this._runtime = runtime;
        this._clock = clock;
        this._logger = logger;
    }

    /// <summary>
    /// Writes one heartbeat, at information level.
    /// </summary>
    /// <param name="kind">Which point in the session's life the heartbeat marks.</param>
    /// <param name="sequence">The heartbeat's number within the session.</param>
    public void Write(HeartbeatKind kind, HeartbeatSequence sequence)
    {
        DateTimeOffset now = this._clock.GetUtcNow();
        TimeSpan sinceStart = now - this._options.SessionStartedOn;
        long secondsSinceStart = (long)sinceStart.TotalSeconds;
        HeartbeatWait wait = this._options.Wait;
        RuntimeVersion runtime = this._runtime.Runtime;
        OperatingSystemDescription operatingSystem = this._runtime.OperatingSystem;
        this._logger.LogInformation(
            Template,
            kind,
            sequence.Value,
            secondsSinceStart,
            wait.Seconds,
            runtime.Value,
            operatingSystem.Value);
    }
}
