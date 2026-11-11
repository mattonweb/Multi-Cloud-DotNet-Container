namespace MultiCloud.Application.Web.Hosting;

using System;
using System.Threading;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Says whether this instance should receive traffic: once the host has finished starting, and until it begins
/// stopping.
/// </summary>
/// <remarks>
/// Phase 1 has no dependency to check, so readiness is the host's own state. Soon after the stop signal the server
/// stops accepting connections, so the window in which <c>/healthz/ready</c> answers 503 is short; a platform that
/// needs a longer drain gets it from a platform setting, never from a delay here (docs/SDD.md, decision D5).
/// </remarks>
public sealed class HostReadiness
{
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostReadiness"/> class.
    /// </summary>
    /// <param name="lifetime">The host's lifetime, which says when it has started and when it begins stopping.</param>
    public HostReadiness(IHostApplicationLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        this._lifetime = lifetime;
    }

    /// <summary>
    /// Says whether the host has finished starting and has not begun stopping.
    /// </summary>
    /// <returns><see langword="true"/> when this instance should receive traffic.</returns>
    public bool IsReady()
    {
        CancellationToken started = this._lifetime.ApplicationStarted;
        CancellationToken stopping = this._lifetime.ApplicationStopping;
        return started.IsCancellationRequested && !stopping.IsCancellationRequested;
    }
}
