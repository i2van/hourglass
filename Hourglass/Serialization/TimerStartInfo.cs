// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerStartInfo.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Serialization;

using Parsing;
using Timing;

/// <summary>
/// The representation of a <see cref="TimerStart"/> used for XML serialization.
/// </summary>
public sealed class TimerStartInfo
{
    /// <summary>
    /// Gets or sets the <see cref="TimerStartToken"/>.
    /// </summary>
    public TimerStartToken TimerStartToken { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user-specified title saved with this input.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Returns a <see cref="TimerStartInfo"/> for a <see cref="TimerStart"/>.
    /// </summary>
    /// <param name="timerStart">A <see cref="TimerStart"/>.</param>
    /// <returns>The <see cref="TimerStartInfo"/> for the <see cref="TimerStart"/>.</returns>
    public static TimerStartInfo? FromTimerStart(TimerStart? timerStart)
    {
        return timerStart?.ToTimerStartInfo();
    }
}