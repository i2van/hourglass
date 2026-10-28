using System;

using Hourglass.Extensions;
using Hourglass.Properties;

namespace Hourglass.Parsing;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Represents a <see cref="TimerStartToken"/> with a time span offset added to or subtracted from it (e.g.,
/// <c>"5 minutes + 3 minutes"</c>, <c>"until 10:00 - 5 minutes"</c>).
/// </summary>
public sealed class OffsetTimerStartToken : TimerStartToken
{
    /// <summary>
    /// Gets or sets the <see cref="TimerStartToken"/> the offset is applied to.
    /// </summary>
    public TimerStartToken? Input { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the offset is subtracted rather than added.
    /// </summary>
    public bool Negative { get; set; }

    /// <summary>
    /// Gets or sets the time span offset.
    /// </summary>
    public TimeSpanToken? Offset { get; set; }

    /// <inheritdoc />
    protected override bool IsValid => Input?.IsValidToken == true && Offset?.IsValidToken == true;

    /// <inheritdoc />
    public override DateTime GetEndTime(DateTime startTime)
    {
        ThrowIfNotValid();

        DateTime inputEndTime = Input!.GetEndTime(startTime);
        DateTime endTime;

        if (Negative)
        {
            if (!Offset!.TryGetStartTime(inputEndTime, out endTime))
            {
                throw new InvalidOperationException("The end time could not be computed");
            }
        }
        else
        {
            endTime = Offset!.GetEndTime(inputEndTime);
        }

        if (endTime < startTime)
        {
            throw new InvalidOperationException("The end time is less than the start time");
        }

        return endTime;
    }

    /// <summary>
    /// Returns the start time for which <see cref="GetEndTime"/> yields the specified end time, if the input is a
    /// <see cref="TimeSpanToken"/> or an <see cref="OffsetTimerStartToken"/> that can be inverted.
    /// </summary>
    /// <param name="endTime">The end time.</param>
    /// <param name="startTime">The start time if it could be computed, or <see cref="DateTime.MinValue"/> otherwise.</param>
    /// <returns><see langword="true"/> if the start time could be computed, or <see langword="false"/> otherwise.</returns>
    public bool TryGetStartTime(DateTime endTime, out DateTime startTime)
    {
        ThrowIfNotValid();

        startTime = DateTime.MinValue;

        try
        {
            DateTime inputEndTime;

            if (Negative)
            {
                inputEndTime = Offset!.GetEndTime(endTime);
            }
            else if (!Offset!.TryGetStartTime(endTime, out inputEndTime))
            {
                return false;
            }

            if (Input is TimeSpanToken inputTimeSpan)
            {
                return inputTimeSpan.TryGetStartTime(inputEndTime, out startTime);
            }

            if (Input is OffsetTimerStartToken inputOffset)
            {
                return inputOffset.TryGetStartTime(inputEndTime, out startTime);
            }

            return false;
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            startTime = DateTime.MinValue;
            return false;
        }
    }


    /// <inheritdoc />
    protected override string ToString(IFormatProvider provider)
    {
        try
        {
            ThrowIfNotValid();

            string formatString = Negative
                ? Resources.ResourceManager.GetString(nameof(Resources.OffsetTimerStartTokenNegativeFormatString), provider)
                : Resources.ResourceManager.GetString(nameof(Resources.OffsetTimerStartTokenPositiveFormatString), provider);

            return string.Format(
                Resources.ResourceManager.GetEffectiveProvider(provider),
                formatString,
                Input!.ToString(),
                Offset!.ToString());
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return GetType().ToString();
        }
    }
}
