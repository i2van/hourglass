using System;

using Hourglass.Timing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hourglass.Test;

/// <summary>
/// Contains tests for the <see cref="Timer"/> class.
/// </summary>
[TestClass]
public sealed class TimerTest
{
    /// <summary>
    /// Tests that starting a timer at an explicit start time whose end is already past marks the timer as expired.
    /// </summary>
    [TestMethod]
    public void StartWithExplicitStartAfterEndMarksTimerExpired()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        TimerStart timerStart = TimerStart.FromString("1h")!;
        TimerStart startTime = TimerStart.FromString("2h")!;

        // Act
        bool actual = timer.Start(timerStart, false, startTime);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(TimerState.Expired, timer.State);
        Assert.AreEqual(TimeSpan.Zero, timer.TimeLeft);
    }

    /// <summary>
    /// Tests that starting a timer at an explicit start time whose end is in the future leaves the timer running with the
    /// remaining time.
    /// </summary>
    [TestMethod]
    public void StartWithExplicitStartBeforeEndLeavesTimerRunning()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        TimerStart timerStart = TimerStart.FromString("5h")!;
        TimerStart startTime = TimerStart.FromString("2h")!;

        // Act
        bool actual = timer.Start(timerStart, false, startTime);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(TimerState.Running, timer.State);
        Assert.IsTrue(timer.TimeLeft > TimeSpan.FromHours(2.9));
        Assert.IsTrue(timer.TimeLeft <= TimeSpan.FromHours(3));
    }
}
