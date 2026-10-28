using System;
using System.Collections.Generic;

using Hourglass.Parsing;
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

    /// <summary>
    /// Tests that adjusting a running timer by a positive offset adds the offset to the time left.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerWithPositiveOffsetAddsTimeLeft()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "+3m");

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(TimerState.Running, timer.State);
        Assert.IsTrue(timer.TimeLeft > TimeSpan.FromMinutes(62.9));
        Assert.IsTrue(timer.TimeLeft <= TimeSpan.FromMinutes(63));
    }

    /// <summary>
    /// Tests that a chain of offsets on a running timer is applied in order, so <c>+3m-1m</c> adds a net 2 minutes.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerWithChainedOffsetsAppliesAllOffsets()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m-1m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "+3m-1m");
        Assert.IsTrue(timer.TimeLeft <= TimeSpan.FromMinutes(62));
    }

    /// <summary>
    /// Tests that a chain of offsets is rejected as a whole when its result would leave no time left, so the timer is unchanged.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerWithChainedOffsetsLeavingNoTimeLeftLeavesTimerUnchanged()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1m")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+1m-5m", out IReadOnlyList<Offset> offsets));
        TimeSpan before = timer.TimeLeft!.Value;

        // Act
        bool actual = timer.Adjust(offsets, "+1m-5m");
        Assert.AreEqual(TimerState.Running, timer.State);
        Assert.IsTrue(timer.TimeLeft <= before);
        Assert.IsTrue(timer.TimeLeft > before - TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that adjusting a running timer by a negative offset subtracts the offset from the time left.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerWithNegativeOffsetSubtractsTimeLeft()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("-3m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "-3m");

        // Assert
        Assert.IsTrue(actual);
        Assert.IsTrue(timer.TimeLeft > TimeSpan.FromMinutes(56.9));
        Assert.IsTrue(timer.TimeLeft <= TimeSpan.FromMinutes(57));
    }

    /// <summary>
    /// Tests that adjusting a running timer by an offset that would leave no time left fails and keeps the timer running.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerBeyondTimeLeftFails()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1m")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("-5m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "-5m");

        // Assert
        Assert.IsFalse(actual);
        Assert.AreEqual(TimerState.Running, timer.State);
    }

    /// <summary>
    /// Tests that adjusting a paused timer by an offset resumes the timer.
    /// </summary>
    [TestMethod]
    public void AdjustPausedTimerResumesTimer()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!, startPaused: true));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "+3m");

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(TimerState.Running, timer.State);
    }

    /// <summary>
    /// Tests that the total adjustment made to a running timer is saved with the timer.
    /// </summary>
    [TestMethod]
    public void AdjustedTimerSavesAdjustment()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m", out IReadOnlyList<Offset> offsets));
        Assert.IsTrue(timer.Adjust(offsets, "+3m"));

        // Act
        TimeSpan? adjustment = timer.ToTimerInfo().Adjustment;

        // Assert
        Assert.AreEqual(TimeSpan.FromMinutes(3), adjustment);
    }

    /// <summary>
    /// Tests that every adjustment made to a running timer is kept in the timer input.
    /// </summary>
    [TestMethod]
    public void AdjustedTimerKeepsEveryAdjustmentInInput()
    {
        // Arrange
        using Timer timer = new(new TimerOptions());
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("-10", out IReadOnlyList<Offset> first));
        Assert.IsTrue(timer.Adjust(first, "-10"));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m", out IReadOnlyList<Offset> second));

        // Act
        bool actual = timer.Adjust(second, "+3m");

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual("1h -10 +3m", timer.TimerStartInput);
        Assert.AreEqual("1h -10 +3m", timer.ToTimerInfo().AdjustedInput);
    }

    /// <summary>
    /// Tests that the displayed trigger time of a running timer is updated when the timer is adjusted.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerUpdatesDisplayedTriggerTime()
    {
        // Arrange
        using Timer timer = new(new TimerOptions { ShowTriggerTime = true });
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "+3m");

        // Assert
        Assert.IsTrue(actual);
        Assert.IsNotNull(timer.EndTime);
        Assert.IsTrue(timer.TimeExpiredAsString!.Contains(timer.EndTime.Value.ToLongTimeString()));
    }

    /// <summary>
    /// Tests that the displayed start input of a running timer is updated when the timer is adjusted and the trigger
    /// time is not shown.
    /// </summary>
    [TestMethod]
    public void AdjustRunningTimerUpdatesDisplayedStartInput()
    {
        // Arrange
        using Timer timer = new(new TimerOptions { ShowTriggerTime = false });
        Assert.IsTrue(timer.Start(TimerStart.FromString("1h")!));
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m", out IReadOnlyList<Offset> offsets));

        // Act
        bool actual = timer.Adjust(offsets, "+3m");

        // Assert
        Assert.IsTrue(actual);
        Assert.IsTrue(timer.ToString().Contains("1h +3m"));
    }
}
