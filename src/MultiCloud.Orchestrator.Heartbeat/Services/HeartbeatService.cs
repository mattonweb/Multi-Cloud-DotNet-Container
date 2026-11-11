namespace MultiCloud.Orchestrator.Heartbeat.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using MultiCloud.Orchestrator.Heartbeat.DTOs;

/// <summary>
/// Decides when a heartbeat is written: <c>Started</c> at once, <c>Running</c> after every wait, and <c>Stopping</c>
/// when the host signals shutdown. <see cref="HeartbeatWriter"/> composes each one.
/// </summary>
/// <remarks>
/// The timer reads the injected clock, so a test advances a fake clock instead of waiting. Nobody waits a full
/// interval for the first line: it is written before the first wait begins.
/// </remarks>
internal sealed class HeartbeatService : BackgroundService
{
    private readonly HeartbeatOptions _options;
    private readonly HeartbeatWriter _writer;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatService"/> class.
    /// </summary>
    /// <param name="options">The wait between heartbeats.</param>
    /// <param name="writer">Composes and writes each heartbeat.</param>
    /// <param name="clock">Drives the timer.</param>
    public HeartbeatService(HeartbeatOptions options, HeartbeatWriter writer, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(clock);
        this._options = options;
        this._writer = writer;
        this._clock = clock;
    }

    /// <summary>
    /// Writes the session's heartbeats until the host signals shutdown, then writes the last one.
    /// </summary>
    /// <param name="stoppingToken">Signals that the host has begun stopping.</param>
    /// <returns>A task that completes after the <c>Stopping</c> heartbeat is written.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long sequence = 0;
        HeartbeatWait wait = this._options.Wait;
        using (PeriodicTimer timer = new PeriodicTimer(wait.Interval, this._clock))
        {
            // The timer starts counting first, so the wait is measured from the Started line and no tick is missed
            // between the two.
            this._writer.Write(HeartbeatKind.Started, new HeartbeatSequence(sequence));
            while (await WaitForNextTickAsync(timer, stoppingToken).ConfigureAwait(false))
            {
                sequence++;
                this._writer.Write(HeartbeatKind.Running, new HeartbeatSequence(sequence));
            }
        }

        this._writer.Write(HeartbeatKind.Stopping, new HeartbeatSequence(sequence + 1));
    }

    /// <summary>
    /// Waits for the timer's next tick.
    /// </summary>
    /// <param name="timer">The timer.</param>
    /// <param name="stoppingToken">Signals that the host has begun stopping.</param>
    /// <returns><see langword="true"/> at the next tick; <see langword="false"/> once the host has begun stopping.</returns>
    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The host signals shutdown by cancelling the token while the timer waits. For this loop that is the
            // expected way out, not an error, so it becomes the answer that ends the loop.
            return false;
        }
    }
}
