// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MathExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Hourglass.Extensions;

/// <summary>
/// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions
/// that extend those provided by the <see cref="Math"/> class.
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// Limits a value to a specified range.
    /// </summary>
    /// <param name="value">A value.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns><paramref name="value"/> limited to the specified range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LimitToRange(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    /// <summary>
    /// Returns the latter of two <see cref="DateTime"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="DateTime"/> to compare.</param>
    /// <param name="b">The second <see cref="DateTime"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is later.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Max(DateTime a, DateTime b)
    {
        return a > b ? a : b;
    }

    /// <summary>
    /// Returns the larger of two <see cref="TimeSpan"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="TimeSpan"/> to compare.</param>
    /// <param name="b">The second <see cref="TimeSpan"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is larger.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan Max(TimeSpan a, TimeSpan b)
    {
        return a > b ? a : b;
    }

    /// <summary>
    /// Returns the earlier of two <see cref="DateTime"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="DateTime"/> to compare.</param>
    /// <param name="b">The second <see cref="DateTime"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is earlier.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Min(DateTime a, DateTime b)
    {
        return a < b ? a : b;
    }

    /// <summary>
    /// Returns the smaller of two <see cref="TimeSpan"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="TimeSpan"/> to compare.</param>
    /// <param name="b">The second <see cref="TimeSpan"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is smaller.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan Min(TimeSpan a, TimeSpan b)
    {
        return a < b ? a : b;
    }

    // https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Double.cs,155
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(this double d)
    {
        long bits = BitConverter.DoubleToInt64Bits(d);
        return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
    }
}