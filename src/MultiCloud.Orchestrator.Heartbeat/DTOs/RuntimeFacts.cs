namespace MultiCloud.Orchestrator.Heartbeat.DTOs;

/// <summary>
/// What every heartbeat reports about the process it runs in. Read once, at registration, and the same for the whole
/// session.
/// </summary>
internal sealed record RuntimeFacts
{
    /// <summary>
    /// Gets the .NET runtime the application runs on.
    /// </summary>
    required public RuntimeVersion Runtime { get; init; }

    /// <summary>
    /// Gets the operating system the application runs on.
    /// </summary>
    required public OperatingSystemDescription OperatingSystem { get; init; }
}
