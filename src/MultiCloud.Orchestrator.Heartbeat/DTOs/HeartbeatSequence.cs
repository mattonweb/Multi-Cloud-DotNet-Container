namespace MultiCloud.Orchestrator.Heartbeat.DTOs;

using System;

/// <summary>
/// A heartbeat's number within its session, from 0. A gap in the numbers for one session is a dropped log line.
/// </summary>
internal readonly record struct HeartbeatSequence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatSequence"/> class.
    /// </summary>
    /// <param name="value">The number; it must be 0 or more.</param>
    public HeartbeatSequence(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        this.Value = value;
    }

    /// <summary>
    /// Gets the number.
    /// </summary>
    public long Value { get; }
}
