// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerStart.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

using Hourglass.Parsing;
using Hourglass.Properties;
using Hourglass.Serialization;

namespace Hourglass.Timing;

/// <summary>
/// Represents the <see cref="TimerStart"/> type.
/// </summary>
public enum TimerStartType
{
    /// <summary>
    /// Represents a <see cref="TimerStart"/> that starts a timer counting down for a specified timer interval.
    /// </summary>
    TimeSpan,

    /// <summary>
    /// Represents a <see cref="TimerStart"/> that starts a timer counting down until a specified instant in time.
    /// </summary>
    DateTime
}

/// <summary>
/// Specifies a set of values used to start a timer.
/// </summary>
public sealed class TimerStart
{
    /// <summary>
    /// The maximum number of occurrences examined when searching for the latest occurrence of a <see
    /// cref="TimerStart"/>.
    /// </summary>
    private const int MaxOccurrenceSearchSteps = 10000;

    /// <summary>
    /// A <see cref="TimerStartToken"/>.
    /// </summary>
    private readonly TimerStartToken _timerStartToken;

    /// <summary>
    /// Gets or sets the user-specified title saved with this input.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The original timer input.
    /// </summary>
    public string OriginalInput =>
        string.IsNullOrWhiteSpace(_timerStartToken.OriginalInput)
            ? _timerStartToken.ToString()
            : _timerStartToken.OriginalInput!;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerStart"/> class.
    /// </summary>
    /// <param name="timerStartToken">A <see cref="TimerStartToken"/>.</param>
    private TimerStart(TimerStartToken timerStartToken)
    {
        _timerStartToken = timerStartToken ?? throw new ArgumentNullException(nameof(timerStartToken));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerStart"/> class.
    /// </summary>
    /// <param name="timerStartInfo">A <see cref="TimerStartInfo"/>.</param>
    private TimerStart(TimerStartInfo timerStartInfo)
    {
        if (timerStartInfo is null)
        {
            throw new ArgumentNullException(nameof(timerStartInfo));
        }

        _timerStartToken = timerStartInfo.TimerStartToken;
        Title = timerStartInfo.Title;
    }

    /// <summary>
    /// Gets the default <see cref="TimerStart"/> object.
    /// </summary>
    public static TimerStart Default => FromString(Resources.TimerStartDefault)!;

    /// <summary>
    /// Gets the zero-length <see cref="TimerStart"/> object.
    /// </summary>
    public static TimerStart Zero => FromString(Resources.TimerStartZero)!;

    /// <summary>
    /// Gets a value indicating whether the <see cref="TimerStart"/> can be used to start a timer now.
    /// </summary>
    public bool IsCurrent
    {
        get
        {
            DateTime now = DateTime.Now;
            return _timerStartToken.TryGetEndTime(now, out var endTime) && endTime >= now;
        }
    }

    /// <summary>
    /// Gets the <see cref="TimerStart"/> type.
    /// </summary>
    public TimerStartType Type => _timerStartToken is DateTimeToken ? TimerStartType.DateTime : TimerStartType.TimeSpan;

    /// <summary>
    /// Returns a <see cref="TimerStart"/> for a string.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <returns>The <see cref="TimerStart"/> for the string, or <see langword="null"/> if the string is not a supported
    /// representation of a <see cref="TimerStart"/>.</returns>
    public static TimerStart? FromString(string str)
    {
        TimerStartToken? timerStartToken = TimerStartToken.FromString(str);

        return timerStartToken is null ? null : new(timerStartToken);
    }

    /// <summary>
    /// Returns a <see cref="TimerStart"/> for a <see cref="TimerStartInfo"/>.
    /// </summary>
    /// <param name="timerStartInfo">A <see cref="TimerStartInfo"/>.</param>
    /// <returns>The <see cref="TimerStart"/> for the <see cref="TimerStartInfo"/>.</returns>
    public static TimerStart? FromTimerStartInfo(TimerStartInfo? timerStartInfo)
    {
        return timerStartInfo is null ? null : new(timerStartInfo);
    }

    /// <summary>
    /// Returns the end time for a timer started with this <see cref="TimerStart"/> at a specified time.
    /// </summary>
    /// <param name="startTime">The time the timer is started.</param>
    /// <param name="endTime">The end time for a timer started with this <see cref="TimerStart"/> at the specified
    /// time if the end time could be computed, or <see cref="DateTime.MinValue"/> otherwise.</param>
    /// <returns><see langword="true"/> if the end time could be computed, or <see langword="false"/> otherwise.</returns>
    public bool TryGetEndTime(DateTime startTime, out DateTime endTime)
    {
        return _timerStartToken.TryGetEndTime(startTime, out endTime);
    }

    /// <summary>
    /// Returns the time a timer started with this <see cref="TimerStart"/> counts from, given the current time. For a
    /// date-time, this is the latest occurrence not after the specified time; for a time span, this is the specified
    /// time minus the time span.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="start">The start time if it could be computed, or <see cref="DateTime.MinValue"/> otherwise.</param>
    /// <returns><see langword="true"/> if the start time could be computed, or <see langword="false"/> otherwise.</returns>
    public bool TryGetStartTime(DateTime now, out DateTime start)
    {
        if (_timerStartToken is TimeSpanToken timeSpanToken)
        {
            return timeSpanToken.TryGetStartTime(now, out start);
        }

        return TryGetLatestOccurrence(now, out start);
    }

    /// <summary>
    /// Returns the latest occurrence of a <see cref="DateTime"/> <see cref="TimerStart"/> that is not after a specified
    /// time. Only occurrences within the two years before the specified time are considered.
    /// </summary>
    /// <param name="time">The time to search up to.</param>
    /// <param name="occurrence">The latest occurrence not after the specified time if one was found, or <see
    /// cref="DateTime.MinValue"/> otherwise.</param>
    /// <returns><see langword="true"/> if an occurrence was found, or <see langword="false"/> otherwise.</returns>
    public bool TryGetLatestOccurrence(DateTime time, out DateTime occurrence)
    {
        occurrence = DateTime.MinValue;

        if (Type != TimerStartType.DateTime)
        {
            return false;
        }

        DateTime probe = time.AddYears(-2);
        for (int i = 0; i < MaxOccurrenceSearchSteps; i++)
        {
            if (!_timerStartToken.TryGetEndTime(probe, out DateTime next) || next <= probe || next > time)
            {
                break;
            }

            occurrence = next;
            probe = next;
        }

        return occurrence != DateTime.MinValue;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _timerStartToken.ToString();
    }

    /// <summary>
    /// Returns the representation of the <see cref="TimerStart"/> used for XML serialization.
    /// </summary>
    /// <returns>The representation of the <see cref="TimerStart"/> used for XML serialization.</returns>
    public TimerStartInfo ToTimerStartInfo()
    {
        return new()
        {
            TimerStartToken = _timerStartToken,
            Title = Title
        };
    }
}