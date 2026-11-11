namespace MultiCloud.Test.Unit.Orchestrator.Heartbeat;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using MultiCloud.Orchestrator.Heartbeat.DTOs;
using MultiCloud.Orchestrator.Heartbeat.Services;

/// <summary>
/// Builds the pieces the heartbeat tests share: a session that started at a known moment, a fake clock stopped there,
/// and a writer whose lines a test can read.
/// </summary>
internal static class HeartbeatTestParts
{
    /// <summary>
    /// Gets the moment every test's session starts.
    /// </summary>
    internal static DateTimeOffset SessionStartedOn { get; } = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Gets the runtime facts every test's heartbeats report.
    /// </summary>
    internal static RuntimeFacts Runtime { get; } = new RuntimeFacts
    {
        Runtime = new RuntimeVersion(".NET 10.0.13"),
        OperatingSystem = new OperatingSystemDescription("Ubuntu 24.04.3 LTS"),
    };

    /// <summary>
    /// Creates a fake clock stopped at the moment the session starts.
    /// </summary>
    /// <returns>The clock.</returns>
    internal static FakeTimeProvider CreateClock()
    {
        return new FakeTimeProvider(SessionStartedOn);
    }

    /// <summary>
    /// Creates the options for a session that started at <see cref="SessionStartedOn"/>.
    /// </summary>
    /// <param name="waitInSeconds">The wait between heartbeats.</param>
    /// <returns>The options.</returns>
    internal static HeartbeatOptions CreateOptions(int waitInSeconds)
    {
        return new HeartbeatOptions
        {
            Wait = new HeartbeatWait(waitInSeconds),
            SessionStartedOn = SessionStartedOn,
        };
    }

    /// <summary>
    /// Creates a writer whose lines go to a collector the test reads.
    /// </summary>
    /// <param name="options">The wait, and when the session started.</param>
    /// <param name="clock">The clock the writer reads.</param>
    /// <param name="collector">Collects every line written.</param>
    /// <returns>The writer.</returns>
    internal static HeartbeatWriter CreateWriter(
        HeartbeatOptions options,
        TimeProvider clock,
        FakeLogCollector collector)
    {
        FakeLogger<HeartbeatWriter> logger = new FakeLogger<HeartbeatWriter>(collector);
        return new HeartbeatWriter(options, Runtime, clock, logger);
    }

    /// <summary>
    /// Waits, for at most five seconds of real time, until the collector holds a number of lines. The heartbeat runs on
    /// its own task, so a line it writes after the fake clock moves arrives a moment later.
    /// </summary>
    /// <param name="collector">The collector.</param>
    /// <param name="count">The number of lines to wait for.</param>
    /// <returns>A task that completes when the lines are there, or the time is up.</returns>
    internal static async Task WaitForLinesAsync(FakeLogCollector collector, int count)
    {
        Stopwatch waited = Stopwatch.StartNew();
        while (collector.Count < count && waited.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
