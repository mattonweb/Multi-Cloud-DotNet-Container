namespace MultiCloud.Test.Unit.Orchestrator.Heartbeat.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using MultiCloud.Orchestrator.Heartbeat.Services;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HeartbeatService"/> class.
/// </summary>
/// <remarks>
/// The service runs on its own task, as it does in the host, and the timer reads a fake clock, so a test moves time
/// instead of waiting for it. A line written after the clock moves arrives a moment later, so each test waits for the
/// lines it expects before it reads them.
/// </remarks>
[TestClass]
public sealed class HeartbeatServiceTests
{
    private const int WaitInSeconds = 15;

    private FakeTimeProvider? _clock;
    private FakeLogCollector? _collector;
    private HeartbeatService? _service;

    /// <summary>
    /// Builds a heartbeat service with a 15-second wait over a fake clock.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        this._clock = HeartbeatTestParts.CreateClock();
        this._collector = new FakeLogCollector();
        HeartbeatOptions options = HeartbeatTestParts.CreateOptions(WaitInSeconds);
        HeartbeatWriter writer = HeartbeatTestParts.CreateWriter(options, this._clock, this._collector);
        this._service = new HeartbeatService(options, writer, this._clock);
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        this._service?.Dispose();
    }

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the first heartbeat is written as soon as the service starts, so nobody waits a full
    /// interval for it.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_NoTimeHasPassed_WritesStartedWithSequenceZero()
    {
        // Arrange (Given)

        // Act (When)
        await this._service!.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 1).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Started 0" },
            this.ReadKindsAndNumbers(),
            "One Started line, numbered 0, should be written before any time passes.");
    }

    /// <summary>
    /// Unit test to verify that a Running heartbeat follows each wait on the fake clock, numbered 1, then 2.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_TwoWaitsPass_WritesRunningOneAndTwo()
    {
        // Arrange (Given)
        await this._service!.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 1).ConfigureAwait(false);

        // Act (When)
        this._clock!.Advance(TimeSpan.FromSeconds(WaitInSeconds));
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 2).ConfigureAwait(false);
        this._clock.Advance(TimeSpan.FromSeconds(WaitInSeconds));
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 3).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Started 0", "Running 1", "Running 2" },
            this.ReadKindsAndNumbers(),
            "Each wait should add one Running line, numbered in sequence.");
    }

    /// <summary>
    /// Unit test to verify that no Running heartbeat is written before a full wait has passed.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_LessThanOneWaitPasses_WritesNoRunning()
    {
        // Arrange (Given)
        await this._service!.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 1).ConfigureAwait(false);

        // Act (When)
        this._clock!.Advance(TimeSpan.FromSeconds(WaitInSeconds - 1));

        // A line that should not come cannot be waited for; a short real pause gives a wrong one time to arrive.
        await Task.Delay(200).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Started 0" },
            this.ReadKindsAndNumbers(),
            "A Running line should wait for the full interval.");
    }

    /// <summary>
    /// Unit test to verify that a polite shutdown writes a Stopping heartbeat with the next number, so a log search
    /// sees the end of the session.
    /// </summary>
    [TestMethod]
    public async Task StopAsync_AfterOneRunning_WritesStoppingWithNextSequence()
    {
        // Arrange (Given)
        await this._service!.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 1).ConfigureAwait(false);
        this._clock!.Advance(TimeSpan.FromSeconds(WaitInSeconds));
        await HeartbeatTestParts.WaitForLinesAsync(this._collector!, 2).ConfigureAwait(false);

        // Act (When)
        await this._service.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Started 0", "Running 1", "Stopping 2" },
            this.ReadKindsAndNumbers(),
            "Stopping should be the last line, numbered after the last Running line.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Describes one heartbeat as its kind and number.
    /// </summary>
    /// <param name="line">The heartbeat's log line.</param>
    /// <returns>The kind and number, such as "Running 1".</returns>
    private static string Describe(FakeLogRecord line)
    {
        string? kind = line.GetStructuredStateValue("HeartbeatKind");
        string? sequence = line.GetStructuredStateValue("HeartbeatSequence");
        return $"{kind} {sequence}";
    }

    /// <summary>
    /// Reads each heartbeat written so far as its kind and number, such as "Running 1".
    /// </summary>
    /// <returns>The heartbeats, in the order written.</returns>
    private string[] ReadKindsAndNumbers()
    {
        IReadOnlyList<FakeLogRecord> lines = this._collector!.GetSnapshot();
        return lines.Select(Describe).ToArray();
    }

    #endregion Private Methods
}
