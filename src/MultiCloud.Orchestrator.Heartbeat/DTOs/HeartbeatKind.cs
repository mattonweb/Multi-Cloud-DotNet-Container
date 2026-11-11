namespace MultiCloud.Orchestrator.Heartbeat.DTOs;

/// <summary>
/// Which point in a session's life a heartbeat marks. All three are written in the same shape, so one search finds a
/// session's whole life.
/// </summary>
internal enum HeartbeatKind
{
    /// <summary>
    /// Written once, as soon as the heartbeat starts, with sequence 0.
    /// </summary>
    Started,

    /// <summary>
    /// Written after every wait, with sequence 1, 2, 3 and so on.
    /// </summary>
    Running,

    /// <summary>
    /// Written once, with the next sequence number, when the host signals a polite shutdown. A container that is
    /// killed writes none.
    /// </summary>
    Stopping,
}
