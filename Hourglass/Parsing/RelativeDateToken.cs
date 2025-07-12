// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RelativeDateToken.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Parsing;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Extensions;
using Properties;

/// <summary>
/// Represents a relative date.
/// </summary>
public enum RelativeDate
{
    /// <summary>
    /// Represents today.
    /// </summary>
    Today,

    /// <summary>
    /// Represents tomorrow.
    /// </summary>
    Tomorrow
}

/// <summary>
/// Represents a relative date.
/// </summary>
public sealed class RelativeDateToken : DateToken
{
    /// <summary>
    /// A list of supported relative dates.
    /// </summary>
    private static readonly RelativeDateDefinition[] RelativeDates =
    [
        new(
            RelativeDate.Today,
            0 /* yearDelta */,
            0 /* monthDelta */,
            0 /* dayDelta */),

        new(
            RelativeDate.Tomorrow,
            0 /* yearDelta */,
            0 /* monthDelta */,
            1 /* dayDelta */)
    ];

    /// <summary>
    /// Gets or sets the <see cref="RelativeDate"/> represented by this token.
    /// </summary>
    public RelativeDate RelativeDate { get; set; }

    /// <inheritdoc />
    public override bool IsValid => GetRelativeDateDefinition() is not null;

    /// <inheritdoc />
    public override DateTime ToDateTime(DateTime minDate, bool inclusive)
    {
        ThrowIfNotValid();

        RelativeDateDefinition relativeDateDefinition = GetRelativeDateDefinition()!;

        DateTime date = minDate.Date;
        date = date.AddDays(relativeDateDefinition.DayDelta);
        date = date.AddMonths(relativeDateDefinition.MonthDelta);
        date = date.AddYears(relativeDateDefinition.YearDelta);
        return date;
    }

    /// <inheritdoc />
    public override string ToString(IFormatProvider provider)
    {
        try
        {
            ThrowIfNotValid();

            RelativeDateDefinition relativeDateDefinition = GetRelativeDateDefinition()!;
            return relativeDateDefinition.GetName(provider);
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return GetType().ToString();
        }
    }

    /// <summary>
    /// Returns the <see cref="RelativeDateDefinition"/> object for a <see cref="Match"/>.
    /// </summary>
    /// <param name="match">A <see cref="Match"/>.</param>
    /// <returns>The <see cref="RelativeDateDefinition"/> object for a <see cref="Match"/>.</returns>
#pragma warning disable S3398
    private static RelativeDateDefinition? GetRelativeDateDefinitionForMatch(Match match)
#pragma warning restore S3398
    {
        return Array.Find(RelativeDates, e => match.Groups[e.MatchGroup].Success);
    }

    /// <summary>
    /// Returns the <see cref="RelativeDateDefinition"/> object for this part.
    /// </summary>
    /// <returns>The <see cref="RelativeDateDefinition"/> object for this part.</returns>
    private RelativeDateDefinition? GetRelativeDateDefinition()
    {
        return Array.Find(RelativeDates, e => e.RelativeDate == RelativeDate);
    }

    /// <summary>
    /// Parses <see cref="RelativeDateToken"/> strings.
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
            return RelativeDates.Select(e => e.GetPattern(provider));
        }

        /// <inheritdoc />
        protected override DateToken ParseInternal(Match match, IFormatProvider provider)
        {
            RelativeDateDefinition? relativeDateDefinition = GetRelativeDateDefinitionForMatch(match);

            return relativeDateDefinition is not null
                ? new RelativeDateToken { RelativeDate = relativeDateDefinition.RelativeDate }
                : throw new FormatException();
        }
    }

    /// <summary>
    /// Defines a relative date.
    /// </summary>
    private sealed class RelativeDateDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeDateDefinition"/> class.
        /// </summary>
        /// <param name="relativeDate">The <see cref="relativeDate"/>.</param>
        /// <param name="yearDelta">The year delta.</param>
        /// <param name="monthDelta">The month delta.</param>
        /// <param name="dayDelta">The day delta.</param>
        public RelativeDateDefinition(RelativeDate relativeDate, int yearDelta, int monthDelta, int dayDelta)
        {
            RelativeDate = relativeDate;

            YearDelta = yearDelta;
            MonthDelta = monthDelta;
            DayDelta = dayDelta;

            MatchGroup = relativeDate.ToString();
        }

        /// <summary>
        /// Gets the <see cref="RelativeDate"/>.
        /// </summary>
        public RelativeDate RelativeDate { get; }

        /// <summary>
        /// Gets the year delta.
        /// </summary>
        public int YearDelta { get; }

        /// <summary>
        /// Gets the month delta.
        /// </summary>
        public int MonthDelta { get; }

        /// <summary>
        /// Gets the day delta.
        /// </summary>
        public int DayDelta { get; }

        /// <summary>
        /// Gets the name of the regular expression match group that identifies the relative date in a match.
        /// </summary>
        public string MatchGroup { get; }

        /// <summary>
        /// Returns the friendly name for the relative date.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>The friendly name for the relative date.</returns>
        public string GetName(IFormatProvider provider)
        {
            string resourceName = string.Format(
                CultureInfo.InvariantCulture,
                "RelativeDateToken{0}Name",
                RelativeDate);

            return Resources.ResourceManager.GetString(resourceName, provider);
        }

        /// <summary>
        /// Returns the regular expression that matches the relative date.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>The regular expression that matches the relative date.</returns>
        public string GetPattern(IFormatProvider provider)
        {
            string resourceName = string.Format(
                CultureInfo.InvariantCulture,
                "RelativeDateToken{0}Pattern",
                RelativeDate);

            string pattern = Resources.ResourceManager.GetString(resourceName, provider);
            return string.Format(CultureInfo.InvariantCulture, @"(?<{0}>{1})", RelativeDate, pattern);
        }
    }
}