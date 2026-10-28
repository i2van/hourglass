using System;
using System.Collections.Generic;

using Hourglass.Parsing;
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

    #region Offset

    /// <summary>
    /// Tests that a time span <see cref="TimerStart"/> followed by a positive offset ends at the sum of both time spans.
    /// </summary>
    [TestMethod]
    public void OffsetAddsToTimeSpan()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("5m+3m")!;
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        bool actual = timerStart.TryGetEndTime(start, out DateTime end);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(new DateTime(2024, 3, 13, 10, 8, 0), end);
    }

    /// <summary>
    /// Tests that a date-time <see cref="TimerStart"/> followed by a negative offset ends at the offset time before the
    /// date-time, and that its <see cref="TimerStart.Type"/> is <see cref="TimerStartType.DateTime"/>.
    /// </summary>
    [TestMethod]
    public void OffsetSubtractsFromDateTime()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("until 10:00 - 5m")!;
        DateTime start = new(2024, 3, 13, 9, 0, 0);

        // Act
        bool actual = timerStart.TryGetEndTime(start, out DateTime end);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(new DateTime(2024, 3, 13, 9, 55, 0), end);
        Assert.AreEqual(TimerStartType.DateTime, timerStart.Type);
    }

    /// <summary>
    /// Tests that a date followed by an offset is parsed as the date with the offset applied, and that the original
    /// input, including the offset, is kept for the timer window editor.
    /// </summary>
    [TestMethod]
    public void OffsetAppliesToDateInputAndKeepsOriginalInput()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("Oct 30 -10")!;
        DateTime start = new(2024, 10, 1, 9, 0, 0);

        // Act
        bool actual = timerStart.TryGetEndTime(start, out DateTime end);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(new DateTime(2024, 10, 29, 23, 50, 0), end);
        Assert.AreEqual("Oct 30 -10", timerStart.OriginalInput);
    }

    /// <summary>
    /// Tests that a leading <c>+</c> is parsed as the time span without the sign.
    /// </summary>
    [TestMethod]
    public void LeadingPlusIsParsedAsTimeSpan()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("+3m")!;
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        bool actual = timerStart.TryGetEndTime(start, out DateTime end);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(start.AddMinutes(3), end);
        Assert.AreEqual(TimerStartType.TimeSpan, timerStart.Type);
    }

    /// <summary>
    /// Tests that a leading <c>-</c> is not a valid timer start on its own.
    /// </summary>
    [TestMethod]
    public void LeadingMinusIsInvalid()
    {
        Assert.IsNull(TimerStart.FromString("-3m"));
    }

    /// <summary>
    /// Tests that chained offsets are applied from left to right.
    /// </summary>
    [TestMethod]
    [DataRow("5m+3m+1m")]
    [DataRow("5+3m +1m")]
    public void ChainedOffsetsAreAppliedLeftToRight(string input)
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString(input)!;
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        bool actual = timerStart.TryGetEndTime(start, out DateTime end);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(start.AddMinutes(9), end);
        Assert.AreEqual(TimerStartType.TimeSpan, timerStart.Type);
    }

    /// <summary>
    /// Tests that the start time of a timer with chained offsets is recovered from its end time.
    /// </summary>
    [TestMethod]
    public void ChainedOffsetsStartTimeIsRecoveredFromEndTime()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("5m+3m-1m")!;
        DateTime start = new(2024, 3, 13, 10, 0, 0);
        DateTime end = start.AddMinutes(7);

        // Act
        bool actual = timerStart.TryGetStartTime(end, out DateTime recoveredStart);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(start, recoveredStart);
    }

    /// <summary>
    /// Tests that a negative offset that would end the timer before it starts fails to compute an end time.
    /// </summary>
    [TestMethod]
    public void NegativeOffsetBeyondTimeSpanFailsToGetEndTime()
    {
        // Arrange
        TimerStart timerStart = TimerStart.FromString("5m-10m")!;
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        bool actual = timerStart.TryGetEndTime(start, out _);

        // Assert
        Assert.IsFalse(actual);
    }

    /// <summary>
    /// Tests that <see cref="TimerStartToken.TryParseLeadingOffsets"/> splits a chain of signed time spans, and rejects
    /// input that is not a chain of time spans.
    /// </summary>
    [TestMethod]
    public void TryParseLeadingOffsetsSplitsSignedTimeSpanChain()
    {
        Assert.IsTrue(TimerStartToken.TryParseLeadingOffsets("+3m-1m", out IReadOnlyList<Offset> offsets));
        Assert.AreEqual(2, offsets.Count);
        Assert.IsFalse(offsets[0] is NegativeOffset);
        Assert.IsTrue(offsets[1] is NegativeOffset);

        Assert.IsFalse(TimerStartToken.TryParseLeadingOffsets("3m", out _));
        Assert.IsFalse(TimerStartToken.TryParseLeadingOffsets("+until 10:00", out _));
    }

    #endregion
}
