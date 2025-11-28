// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NormalDateToken.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Parsing;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Extensions;
using Properties;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Represents the date part of an instant in time specified as year, month, and day.
/// </summary>
public sealed class NormalDateToken : DateToken
{
    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the month.
    /// </summary>
    public int? Month { get; set; }

    /// <summary>
    /// Gets or sets the day.
    /// </summary>
    public int? Day { get; set; }

    /// <inheritdoc />
    public override bool IsValid =>
        DateTimeExtensions.IsValid(Year, Month, Day)
        && (Year is not null || Month is not null || Day is not null)
        && !(Year is not null && Month is null && Day is not null);

    /// <inheritdoc />
    public override DateTime ToDateTime(DateTime minDate, bool inclusive)
    {
        ThrowIfNotValid();

        DateTime date;

        int year = Year ?? minDate.Year;
        int month = Month ?? (Year is null ? minDate.Month : 1);
        int day = Day ?? (Year is null && Month is null ? minDate.Day : 1);

        while (!DateTimeExtensions.TryToDateTime(year, month, day, out date)
               || date < minDate.Date
               || (date == minDate.Date && !inclusive))
        {
            // Try the next month if we only have a day
            if (Month is null && Year is null)
            {
                DateTimeExtensions.IncrementMonth(ref year, ref month);
                continue;
            }

            // Try the next year if we only have a month or a day and a month
            if (Year is null)
            {
                year++;
                continue;
            }

            // Nothing else to try
            break;
        }

        return date;
    }

    /// <inheritdoc />
    public override string ToString(IFormatProvider provider)
    {
        try
        {
            ThrowIfNotValid();

            // Day only
            if (Day is not null && Month is null && Year is null)
            {
                return string.Format(
                    Resources.ResourceManager.GetEffectiveProvider(provider),
                    Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenDayOnlyFormatString), provider),
                    DateTimeExtensions.GetOrdinalDayString(Day.Value, provider));
            }

            // Day and month
            if (Day is not null && Month is not null && Year is null)
            {
                string formatString = provider.IsMonthFirst
                    ? Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenMonthAndDayFormatString), provider)
                    : Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenDayAndMonthFormatString), provider);

                return string.Format(
                    Resources.ResourceManager.GetEffectiveProvider(provider),
                    formatString,
                    Day.Value,
                    DateTimeExtensions.GetMonthString(Month.Value, provider));
            }

            // Day, month, and year
            if (Day is not null && Month is not null && Year is not null)
            {
                string formatString = provider.IsMonthFirst
                    ? Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenMonthDayAndYearFormatString), provider)
                    : Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenDayMonthAndYearFormatString), provider);

                return string.Format(
                    Resources.ResourceManager.GetEffectiveProvider(provider),
                    formatString,
                    Day.Value,
                    DateTimeExtensions.GetMonthString(Month.Value, provider),
                    Year.Value);
            }

            // Month only
            if (Day is null && Month is not null && Year is null)
            {
                return string.Format(
                    Resources.ResourceManager.GetEffectiveProvider(provider),
                    Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenMonthOnlyFormatString), provider),
                    DateTimeExtensions.GetMonthString(Month.Value, provider));
            }

            // Month and year
            if (Day is null && Month is not null && Year is not null)
            {
                return string.Format(
                    Resources.ResourceManager.GetEffectiveProvider(provider),
                    Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenMonthAndYearFormatString), provider),
                    DateTimeExtensions.GetMonthString(Month.Value, provider),
                    Year);
            }

            // Year
            if (Day is null && Month is null && Year is not null)
            {
                return string.Format(
                    Resources.ResourceManager.GetEffectiveProvider(provider),
                    Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenYearOnlyFormatString), provider),
                    Year);
            }

            // Unsupported
            return GetType().ToString();
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return GetType().ToString();
        }
    }

    /// <summary>
    /// Parses <see cref="NormalDateToken"/> strings.
    /// </summary>
    public new sealed class Parser : DateToken.Parser
    {
        /// <summary>
        /// Singleton instance of the <see cref="Parser"/> class.
        /// </summary>
        public static readonly Parser Instance = new();

        /// <summary>
        /// Prevents a default instance of the <see cref="Parser"/> class from being created.
        /// </summary>
        private Parser()
        {
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetPatterns(IFormatProvider provider)
        {
            if (provider.IsMonthFirst)
            {
                return GetPatternsWithMonthFirst(provider);
            }

            if (provider.IsYearFirst)
            {
                return GetPatternsWithYearFirst(provider);
            }

            return GetPatternsWithDayFirst(provider);
        }

        /// <inheritdoc />
        protected override DateToken ParseInternal(Match match, IFormatProvider provider)
        {
            NormalDateToken dateToken = new();

            provider = Resources.ResourceManager.GetEffectiveProvider(provider);

            // Parse day
            if (match.Groups["day"].Success)
            {
                dateToken.Day = int.Parse(match.Groups["day"].Value, provider);
            }

            // Parse month
            if (match.Groups["month"].Success)
            {
                dateToken.Month = int.TryParse(match.Groups["month"].Value, NumberStyles.None, provider, out var month)
                    ? month
                    : DateTimeExtensions.ParseMonth(match.Groups["month"].Value, provider);
            }

            // Parse year
            if (match.Groups["year"].Success)
            {
                dateToken.Year = int.Parse(match.Groups["year"].Value, provider);

                if (dateToken.Year.Value < 100)
                {
                    dateToken.Year += 2000;
                }
            }

            return dateToken;
        }

        /// <summary>
        /// Returns a set of regular expressions for matching dates. In the case of ambiguity, these patterns favor
        /// interpreting matching strings specifying a day, month, and year in that order.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>A set of regular expressions supported by this parser.</returns>
        private static IEnumerable<string> GetPatternsWithDayFirst(IFormatProvider provider)
        {
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledDateWithDayFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledDateWithMonthFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithDayFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithMonthFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithYearFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenDayOnlyPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledMonthAndOptionalYearPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalMonthAndYearPattern), provider);
        }

        /// <summary>
        /// Returns a set of regular expressions for matching dates. In the case of ambiguity, these patterns favor
        /// interpreting matching strings specifying a month, day, and year in that order.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>A set of regular expressions supported by this parser.</returns>
        private static IEnumerable<string> GetPatternsWithMonthFirst(IFormatProvider provider)
        {
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledDateWithMonthFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledDateWithDayFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithMonthFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithDayFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithYearFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenDayOnlyPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledMonthAndOptionalYearPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalMonthAndYearPattern), provider);
        }

        /// <summary>
        /// Returns a set of regular expressions for matching dates. In the case of ambiguity, these patterns favor
        /// interpreting matching strings specifying a year, month, and day in that order.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>A set of regular expressions supported by this parser.</returns>
        private static IEnumerable<string> GetPatternsWithYearFirst(IFormatProvider provider)
        {
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledDateWithDayFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledDateWithMonthFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithYearFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithDayFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalDateWithMonthFirstPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenDayOnlyPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenSpelledMonthAndOptionalYearPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.NormalDateTokenNumericalMonthAndYearPattern), provider);
        }
    }
}