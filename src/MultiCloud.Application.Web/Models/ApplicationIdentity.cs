namespace MultiCloud.Application.Web.Models;

/// <summary>
/// What the application is and where it runs: its version and the machine's name. Read once, when the process
/// starts, and the same for the whole session.
/// </summary>
public sealed record ApplicationIdentity
{
    /// <summary>
    /// Gets the application's version.
    /// </summary>
    required public ApplicationVersion Version { get; init; }

    /// <summary>
    /// Gets the machine's name, which in a container is the container's host name.
    /// </summary>
    required public HostName HostName { get; init; }
}
