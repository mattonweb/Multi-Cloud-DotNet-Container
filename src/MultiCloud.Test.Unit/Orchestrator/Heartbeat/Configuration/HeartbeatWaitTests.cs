namespace MultiCloud.Test.Unit.Orchestrator.Heartbeat.Configuration;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Orchestrator.Heartbeat.Configuration;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HeartbeatWait"/> struct.
/// </summary>
[TestClass]
public sealed class HeartbeatWaitTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a wait of less than one second is refused, as the setting must be 1 or more.
    /// </summary>
    [TestMethod]
    public void Constructor_ZeroSeconds_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new HeartbeatWait(0);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A wait of 0 seconds should be refused with ArgumentOutOfRangeException.");
    }

    /// <summary>
    /// Unit test to verify that the shortest allowed wait keeps its seconds and gives the timer the same span.
    /// </summary>
    [TestMethod]
    public void Constructor_OneSecond_SetsSecondsAndInterval()
    {
        // Arrange (Given)

        // Act (When)
        HeartbeatWait wait = new HeartbeatWait(1);

        // Assert (Then)
        Assert.AreEqual(
            1,
            wait.Seconds,
            "The seconds should be kept as given.");
        Assert.AreEqual(
            TimeSpan.FromSeconds(1),
            wait.Interval,
            "The interval should be the same number of seconds.");
    }

    #endregion Public Methods
}
