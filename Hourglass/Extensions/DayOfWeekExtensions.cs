// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Extensions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Properties;

/// <summary>
/// Provides extensions methods for the <see cref="DayOfWeek"/> enumeration.
/// </summary>
public static class DayOfWeekExtensions
{
    /// <summary>
    /// Parses a string into a <see cref="DayOfWeek"/>.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    /// <returns>The <see cref="DayOfWeek"/> parsed from the string.</returns>
    /// <exception cref="FormatException">If <paramref name="str"/> is not a supported representation of a day of the week.</exception>
    public static DayOfWeek ParseDayOfWeek(string str, IFormatProvider provider)
    {
        KeyValuePair<DayOfWeek, string>[] matches = GetDayOfWeekStrings(provider)
            .Where(e => e.Value.StartsWith(str, true /* ignoreCase */, (CultureInfo)provider))
            .ToArray();

        if (matches.Length != 1)
        {
            throw new FormatException();
        }

        return matches[0].Key;
    }

    /// <summary>
    /// Returns a string that represents the <see cref="DayOfWeek"/>.
    /// </summary>
    /// <param name="dayOfWeek">A <see cref="DayOfWeek"/>.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> to use.</param>
    /// <returns>A string that represents the current object.</returns>
    public static string ToLocalizedString(this DayOfWeek dayOfWeek, IFormatProvider provider)
    {
        IDictionary<DayOfWeek, string> dayOfWeekStrings = GetDayOfWeekStrings(provider);
        return dayOfWeekStrings[dayOfWeek];
    }

    /// <summary>
    /// Returns a string that represents the <see cref="Nullable{DayOfWeek}"/>.
    /// </summary>
    /// <param name="dayOfWeek">A <see cref="Nullable{DayOfWeek}"/>.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> to use.</param>
    /// <returns>A string that represents the current object.</returns>
    public static string ToLocalizedString(this DayOfWeek? dayOfWeek, IFormatProvider provider)
    {
        return dayOfWeek is not null ? dayOfWeek.Value.ToLocalizedString(provider) : string.Empty;
    }

    /// <summary>
    /// Returns a dictionary mapping <see cref="DayOfWeek"/> values to their localized string representations.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    /// <returns>A dictionary mapping <see cref="DayOfWeek"/> values to their localized string representations.</returns>
    private static IDictionary<DayOfWeek, string> GetDayOfWeekStrings(IFormatProvider provider)
    {
        return new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday]    = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsMonday)),
            [DayOfWeek.Tuesday]   = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsTuesday)),
            [DayOfWeek.Wednesday] = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsWednesday)),
            [DayOfWeek.Thursday]  = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsThursday)),
            [DayOfWeek.Friday]    = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsFriday)),
            [DayOfWeek.Saturday]  = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsSaturday)),
            [DayOfWeek.Sunday]    = GetDayOfWeek(nameof(Resources.DayOfWeekExtensionsSunday))
        };

        string GetDayOfWeek(string name) =>
            Resources.ResourceManager.GetString(name, provider);
    }
}