using System;
using System.Linq;

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
}
