using System;

using Hourglass.Timing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hourglass.Test;

/// <summary>
/// Contains tests for the <see cref="TimerStart"/> class.
/// </summary>
[TestClass]
public sealed class TimerStartTest
{
    #region TryGetLatestOccurrence

    /// <summary>
    /// Tests that <see cref="TimerStart.TryGetLatestOccurrence(DateTime,out DateTime)"/> returns <see langword="false"/> for a
    /// time span <see cref="TimerStart"/>.
    /// </summary>
    [TestMethod]
    public void TryGetLatestOccurrenceWithTimeSpanReturnsFalse()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("2h")!;
        DateTime time = new(2024, 3, 13, 10, 0, 0);

        // Act
        bool actual = timerStart.TryGetLatestOccurrence(time, out DateTime occurrence);

        // Assert
        Assert.IsFalse(actual);
        Assert.AreEqual(DateTime.MinValue, occurrence);
    }

    /// <summary>
    /// Tests that <see cref="TimerStart.TryGetLatestOccurrence(DateTime,out DateTime)"/> returns the most recent
    /// occurrence of a weekday that is not after the specified time.
    /// </summary>
    [TestMethod]
    public void TryGetLatestOccurrenceWithWeekdayReturnsMostRecentOccurrence()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("Monday")!;
        DateTime time = new(2024, 3, 13, 10, 0, 0); // Wednesday

        // Act
        bool actual = timerStart.TryGetLatestOccurrence(time, out DateTime occurrence);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(DayOfWeek.Monday, occurrence.DayOfWeek);
        Assert.IsTrue(occurrence <= time);
        Assert.IsTrue(occurrence > time.AddDays(-7));
    }

    /// <summary>
    /// Tests that <see cref="TimerStart.TryGetLatestOccurrence(DateTime,out DateTime)"/> returns the time itself when
    /// the specified time is an occurrence.
    /// </summary>
    [TestMethod]
    public void TryGetLatestOccurrenceAtOccurrenceReturnsSameTime()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("Monday")!;
        DateTime time = new(2024, 3, 13, 10, 0, 0);
        Assert.IsTrue(timerStart.TryGetLatestOccurrence(time, out DateTime occurrence));

        // Act
        bool actual = timerStart.TryGetLatestOccurrence(occurrence, out DateTime sameOccurrence);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(occurrence, sameOccurrence);
    }

    #endregion

    #region TryGetStartTime

    /// <summary>
    /// Tests that <see cref="TimerStart.TryGetStartTime(DateTime,out DateTime)"/> returns the specified time minus the
    /// time span for a time span <see cref="TimerStart"/>.
    /// </summary>
    [TestMethod]
    public void TryGetStartTimeWithTimeSpanReturnsTimeBefore()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("2h")!;
        DateTime now = new(2024, 3, 13, 10, 0, 0);

        // Act
        bool actual = timerStart.TryGetStartTime(now, out DateTime start);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(new DateTime(2024, 3, 13, 8, 0, 0), start);
    }

    /// <summary>
    /// Tests that <see cref="TimerStart.TryGetStartTime(DateTime,out DateTime)"/> returns the end time of a timer
    /// started at the computed start time as the specified time for a time span <see cref="TimerStart"/>.
    /// </summary>
    [TestMethod]
    public void TryGetStartTimeWithTimeSpanRoundTripsToEndTime()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("5h")!;
        DateTime now = new(2024, 3, 13, 10, 0, 0);
        Assert.IsTrue(timerStart.TryGetStartTime(now, out DateTime start));

        // Act
        bool actual = timerStart.TryGetEndTime(start, out DateTime end);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(now, end);
    }

    /// <summary>
    /// Tests that <see cref="TimerStart.TryGetStartTime(DateTime,out DateTime)"/> returns the latest occurrence for a
    /// date-time <see cref="TimerStart"/>.
    /// </summary>
    [TestMethod]
    public void TryGetStartTimeWithWeekdayReturnsLatestOccurrence()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("Monday")!;
        DateTime now = new(2024, 3, 13, 10, 0, 0); // Wednesday

        // Act
        bool actual = timerStart.TryGetStartTime(now, out DateTime start);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(DayOfWeek.Monday, start.DayOfWeek);
        Assert.IsTrue(start <= now);
    }

    #endregion
}
