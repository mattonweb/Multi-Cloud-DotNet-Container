namespace MultiCloud.Test.Unit.Orchestrator.Heartbeat.Services;

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using MultiCloud.Orchestrator.Heartbeat.DTOs;
using MultiCloud.Orchestrator.Heartbeat.Services;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HeartbeatWriter"/> class.
/// </summary>
[TestClass]
public sealed class HeartbeatWriterTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a heartbeat is one information line carrying its kind, its number, the seconds since
    /// the session started, the configured wait, and the runtime and operating system, each under its own name so a
    /// log search can find it.
    /// </summary>
    [TestMethod]
    public void Write_StartedAtSessionStart_WritesEveryValueAsItsOwnProperty()
    {
        // Arrange (Given)
        FakeTimeProvider clock = HeartbeatTestParts.CreateClock();
        FakeLogCollector collector = new FakeLogCollector();
        HeartbeatWriter writer = HeartbeatTestParts.CreateWriter(HeartbeatTestParts.CreateOptions(600), clock, collector);

        // Act (When)
        writer.Write(HeartbeatKind.Started, new HeartbeatSequence(0));

        // Assert (Then)
        FakeLogRecord line = collector.LatestRecord;
        Assert.AreEqual(
            LogLevel.Information,
            line.Level,
            "A heartbeat should be written at information level.");
        Assert.AreEqual(
            "Started",
            line.GetStructuredStateValue("HeartbeatKind"),
            "The kind should be its own property.");
        Assert.AreEqual(
            "0",
            line.GetStructuredStateValue("HeartbeatSequence"),
            "The number should be its own property.");
        Assert.AreEqual(
            "0",
            line.GetStructuredStateValue("SecondsSinceStart"),
            "A heartbeat at the session's start should report no seconds since it.");
        Assert.AreEqual(
            "600",
            line.GetStructuredStateValue("WaitInSeconds"),
            "The configured wait should be its own property.");
        Assert.AreEqual(
            ".NET 10.0.13",
            line.GetStructuredStateValue("RuntimeVersion"),
            "The runtime should be its own property.");
        Assert.AreEqual(
            "Ubuntu 24.04.3 LTS",
            line.GetStructuredStateValue("OperatingSystem"),
            "The operating system should be its own property.");
    }

    /// <summary>
    /// Unit test to verify that the seconds since the session started are read from the clock when the heartbeat is
    /// written.
    /// </summary>
    [TestMethod]
    public void Write_NinetySecondsAfterStart_ReportsNinetySeconds()
    {
        // Arrange (Given)
        FakeTimeProvider clock = HeartbeatTestParts.CreateClock();
        FakeLogCollector collector = new FakeLogCollector();
        HeartbeatWriter writer = HeartbeatTestParts.CreateWriter(HeartbeatTestParts.CreateOptions(30), clock, collector);
        clock.Advance(TimeSpan.FromSeconds(90));

        // Act (When)
        writer.Write(HeartbeatKind.Running, new HeartbeatSequence(3));

        // Assert (Then)
        FakeLogRecord line = collector.LatestRecord;
        Assert.AreEqual(
            "90",
            line.GetStructuredStateValue("SecondsSinceStart"),
            "The seconds since the session started should come from the clock.");
        Assert.AreEqual(
            "Running",
            line.GetStructuredStateValue("HeartbeatKind"),
            "The kind given should be the kind written.");
    }

    /// <summary>
    /// Unit test to verify that a writer cannot be built without its options.
    /// </summary>
    [TestMethod]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        HeartbeatOptions? options = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = HeartbeatTestParts.CreateWriter(options!, HeartbeatTestParts.CreateClock(), new FakeLogCollector());
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Null options should be refused with ArgumentNullException.");
    }

    #endregion Public Methods
}
