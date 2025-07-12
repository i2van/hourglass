// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekDateToken.cs" company="Chris Dziemborowicz">
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

/// <summary>
/// Represents the relation between the day of week and a date.
/// </summary>
public enum DayOfWeekRelation
{
    /// <summary>
    /// The next date that is the specified day of the week.
    /// </summary>
    Next,

    /// <summary>
    /// The date that is one week after the next date that is the specified day of the week.
    /// </summary>
    AfterNext,

    /// <summary>
    /// A date next week that is the specified day of the week
    /// </summary>
    NextWeek
}

/// <summary>
/// Represents the date part of an instant in time specified as a day of the week.
/// </summary>
public sealed class DayOfWeekDateToken : DateToken
{
    /// <summary>
    /// Gets or sets the day of week.
    /// </summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the relation between the day of week and date.
    /// </summary>
    public DayOfWeekRelation? DayOfWeekRelation { get; set; }

    /// <inheritdoc />
    public override bool IsValid =>
        DayOfWeek.HasValue
        && Enum.IsDefined(typeof(DayOfWeek), DayOfWeek.Value)
        && DayOfWeekRelation.HasValue
        && Enum.IsDefined(typeof(DayOfWeekRelation), DayOfWeekRelation.Value);

    /// <inheritdoc />
    public override DateTime ToDateTime(DateTime minDate, bool inclusive)
    {
        ThrowIfNotValid();

        DateTime date = minDate.Date.AddDays(1);

        // Find the next date with the matching weekday
        DayOfWeek dayOfWeek = DayOfWeek ?? System.DayOfWeek.Sunday;
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        // Advance the date by a week if necessary
        DayOfWeekRelation dayOfWeekRelation = DayOfWeekRelation ?? Parsing.DayOfWeekRelation.Next;
        if (dayOfWeekRelation == Parsing.DayOfWeekRelation.AfterNext ||
            (dayOfWeekRelation == Parsing.DayOfWeekRelation.NextWeek && dayOfWeek > minDate.DayOfWeek))
        {
            date = date.AddDays(7);
        }

        return date;
    }

    /// <inheritdoc />
    public override string ToString(IFormatProvider provider)
    {
        try
        {
            ThrowIfNotValid();

            provider = Resources.ResourceManager.GetEffectiveProvider(provider);

            string formatStringName = string.Format(
                CultureInfo.InvariantCulture,
                "DayOfWeekDateToken{0}FormatString",
                DayOfWeekRelation);

            return string.Format(
                provider,
                Resources.ResourceManager.GetString(formatStringName, provider),
                DayOfWeek.ToLocalizedString(provider));
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return GetType().ToString();
        }
    }

    /// <summary>
    /// Parses <see cref="DayOfWeekDateToken"/> strings.
    /// </summary>
    public new class Parser : DateToken.Parser
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
            yield return Resources.ResourceManager.GetString(nameof(Resources.DayOfWeekDateTokenDaysOfWeekNextPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.DayOfWeekDateTokenDaysOfWeekAfterNextPattern), provider);
            yield return Resources.ResourceManager.GetString(nameof(Resources.DayOfWeekDateTokenDaysOfWeekNextWeekPattern), provider);
        }

        /// <inheritdoc />
        protected override DateToken ParseInternal(Match match, IFormatProvider provider)
        {
            provider = Resources.ResourceManager.GetEffectiveProvider(provider);

            DayOfWeekDateToken dateToken = new();

            // Parse day of week
            if (match.Groups["weekday"].Success)
            {
                dateToken.DayOfWeek = DayOfWeekExtensions.ParseDayOfWeek(match.Groups["weekday"].Value, provider);
            }

            // Parse day of week relation
            if (match.Groups["afternext"].Success)
            {
                dateToken.DayOfWeekRelation = Parsing.DayOfWeekRelation.AfterNext;
            }
            else if (match.Groups["nextweek"].Success)
            {
                dateToken.DayOfWeekRelation = Parsing.DayOfWeekRelation.NextWeek;
            }
            else
            {
                dateToken.DayOfWeekRelation = Parsing.DayOfWeekRelation.Next;
            }

            return dateToken;
        }
    }
}