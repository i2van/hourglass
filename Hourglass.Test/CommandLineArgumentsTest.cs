using System;
using System.Linq;

using Hourglass.Timing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hourglass.Test;

/// <summary>
/// Contains tests for the <see cref="CommandLineArguments"/> class.
/// </summary>
[TestClass]
public sealed class CommandLineArgumentsTest
{
    /// <summary>
    /// Tests that <c>--start-time</c> is kept alongside every timer specified with <c>--multi-timers</c>.
    /// </summary>
    [TestMethod]
    public void ParseStartTimeWithMultiTimersKeepsStartTimeAndAllTimers()
    {
        // Arrange
        string[] args = ["--multi-timers", "on", "-ss", "2h", "1h", "3h", "5h"];
        DateTime now = DateTime.Now;

        // Act
        CommandLineArguments arguments = CommandLineArguments.Parse(args);

        // Assert
        Assert.IsFalse(arguments.HasParseError, arguments.ParseErrorMessage);
        Assert.IsNotNull(arguments.StartTime);
        Assert.IsTrue(arguments.StartTime.TryGetStartTime(now, out DateTime start));
        Assert.AreEqual(now.AddHours(-2), start);
        Assert.AreEqual(3, arguments.TimerStart.Count());
    }

    /// <summary>
    /// Tests that a timer input followed by a negative offset is parsed as a single timer with the offset applied.
    /// </summary>
    [TestMethod]
    public void ParseTimerStartWithNegativeOffsetAppliesOffset()
    {
        // Arrange
        string[] args = ["5m", "-3m"];
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        CommandLineArguments arguments = CommandLineArguments.Parse(args);

        // Assert
        Assert.IsFalse(arguments.HasParseError, arguments.ParseErrorMessage);
        Assert.AreEqual(1, arguments.TimerStart.Count());
        Assert.IsTrue(arguments.TimerStart.Single().TryGetEndTime(start, out DateTime end));
        Assert.AreEqual(start.AddMinutes(2), end);
    }

    /// <summary>
    /// Tests that a trailing offset is applied to every timer specified with <c>--multi-timers</c>.
    /// </summary>
    [TestMethod]
    public void ParseMultiTimersWithTrailingOffsetAppliesOffsetToEachTimer()
    {
        // Arrange
        string[] args = ["--multi-timers", "on", "5m", "10m", "+3m"];
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        CommandLineArguments arguments = CommandLineArguments.Parse(args);

        // Assert
        Assert.IsFalse(arguments.HasParseError, arguments.ParseErrorMessage);
        TimerStart[] timerStarts = arguments.TimerStart.ToArray();
        Assert.AreEqual(2, timerStarts.Length);
        Assert.IsTrue(timerStarts[0].TryGetEndTime(start, out DateTime end1));
        Assert.AreEqual(start.AddMinutes(8), end1);
        Assert.IsTrue(timerStarts[1].TryGetEndTime(start, out DateTime end2));
        Assert.AreEqual(start.AddMinutes(13), end2);
    }

    /// <summary>
    /// Tests that a trailing offset parameter containing several offsets, quoted as one argument, is applied to every timer
    /// specified with <c>--multi-timers</c>.
    /// </summary>
    [TestMethod]
    public void ParseMultiTimersWithQuotedChainedTrailingOffsetAppliesAllOffsetsToEachTimer()
    {
        // Arrange
        string[] args = ["--multi-timers", "on", "5m", "10m", "+3m -1m"];
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        CommandLineArguments arguments = CommandLineArguments.Parse(args);

        // Assert
        Assert.IsFalse(arguments.HasParseError, arguments.ParseErrorMessage);
        TimerStart[] timerStarts = arguments.TimerStart.ToArray();
        Assert.AreEqual(2, timerStarts.Length);
        Assert.IsTrue(timerStarts[0].TryGetEndTime(start, out DateTime end1));
        Assert.AreEqual(start.AddMinutes(7), end1);
        Assert.IsTrue(timerStarts[1].TryGetEndTime(start, out DateTime end2));
        Assert.AreEqual(start.AddMinutes(12), end2);
    }

    /// <summary>
    /// Tests that a trailing offset is added to a timer that already has an offset specified with <c>--multi-timers</c>.
    /// </summary>
    [TestMethod]
    public void ParseMultiTimersWithTrailingOffsetChainsWithTimerOffset()
    {
        // Arrange
        string[] args = ["--multi-timers", "on", "5m+1m", "10m", "+3m"];
        DateTime start = new(2024, 3, 13, 10, 0, 0);

        // Act
        CommandLineArguments arguments = CommandLineArguments.Parse(args);

        // Assert
        Assert.IsFalse(arguments.HasParseError, arguments.ParseErrorMessage);
        TimerStart[] timerStarts = arguments.TimerStart.ToArray();
        Assert.AreEqual(2, timerStarts.Length);
        Assert.IsTrue(timerStarts[0].TryGetEndTime(start, out DateTime end1));
        Assert.AreEqual(start.AddMinutes(9), end1);
        Assert.IsTrue(timerStarts[1].TryGetEndTime(start, out DateTime end2));
        Assert.AreEqual(start.AddMinutes(13), end2);
    }
}
