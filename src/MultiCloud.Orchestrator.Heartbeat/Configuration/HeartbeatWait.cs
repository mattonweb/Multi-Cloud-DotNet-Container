namespace MultiCloud.Orchestrator.Heartbeat.Configuration;

using System;

/// <summary>
/// How long the heartbeat waits between <c>Running</c> lines: a whole number of seconds, 1 or more.
/// </summary>
public readonly record struct HeartbeatWait
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatWait"/> class.
    /// </summary>
    /// <param name="seconds">The wait in seconds; it must be 1 or more.</param>
    public HeartbeatWait(int seconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seconds, 1);
        this.Seconds = seconds;
        this.Interval = TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Gets the wait in seconds, as the setting states it and as each heartbeat reports it.
    /// </summary>
    public int Seconds { get; }

    /// <summary>
    /// Gets the wait as a time span, for the timer.
    /// </summary>
    public TimeSpan Interval { get; }
}
