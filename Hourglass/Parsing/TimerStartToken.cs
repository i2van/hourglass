// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerStartToken.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using Hourglass.Extensions;
using Hourglass.Properties;
using Hourglass.Timing;

namespace Hourglass.Parsing;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Represents a <see cref="TimerStart"/>.
/// </summary>
[XmlInclude(typeof(DateTimeToken))]
[XmlInclude(typeof(TimeSpanToken))]
[XmlInclude(typeof(OffsetTimerStartToken))]
public abstract class TimerStartToken
{
    /// <summary>
    /// Gets a value indicating whether the token is valid.
    /// </summary>
    protected abstract bool IsValid { get; }

    /// <summary>
    /// Gets a value indicating whether the token is valid, for use by other <see cref="TimerStartToken"/> instances.
    /// </summary>
    internal bool IsValidToken => IsValid;

    /// <summary>
    /// The original timer input.
    /// </summary>
    public string? OriginalInput { get; set; }

    /// <summary>
    /// Returns a <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/>.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <returns>A <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/>.</returns>
    public static TimerStartToken? FromString(string str)
    {
        return FromString(str, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/>.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    /// <returns>A <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/>.</returns>
    private static TimerStartToken? FromString(string? str, IFormatProvider provider)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        string originalStr = str = str!.Trim();

        if (str.StartsWith("+", StringComparison.Ordinal) && TimeSpanToken.Parser.Instance.TryParse(str.Substring(1).Trim(), provider, out TimerStartToken? leadingOffsetToken))
        {
            return leadingOffsetToken;
        }

        TimerStartToken? timerStartToken = FromStringWithoutOffset(str, provider) ?? FromOffsetString(str, provider);
        if (timerStartToken is not null)
        {
            timerStartToken.OriginalInput = originalStr;
        }

        return timerStartToken;
    }

    /// <summary>
    /// Returns a <see cref="TimerStartToken"/> for the specified string, ignoring any trailing offset, or <see
    /// langword="null"/> if the string is not a supported representation of a <see cref="TimerStartToken"/>.
    /// </summary>
    /// <param name="str">A trimmed, non-empty string.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    /// <returns>A <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/>.</returns>
    private static TimerStartToken? FromStringWithoutOffset(string str, IFormatProvider provider)
    {
        string preferDateTimePattern = Resources.ResourceManager.GetString(nameof(Resources.TimerStartTokenUseDateTimeParserPattern), provider);
        if (Regex.IsMatch(str, preferDateTimePattern, Parser.RegexOptions))
        {
            return FromDateTime(Regex.Replace(str, preferDateTimePattern, string.Empty, Parser.RegexOptions));
        }

        return FromTimeSpanOrDateTimeString(str);
    }

    /// <summary>
    /// Returns an <see cref="OffsetTimerStartToken"/> for a string of the form <c>&lt;input&gt;[+-]&lt;offset&gt;</c>,
    /// where the offset is a <see cref="TimeSpanToken"/> and the input is any other <see cref="TimerStartToken"/>,
    /// including another offset (e.g., <c>5m+3m+1m</c>, applied left to right), or <see langword="null"/> if the string
    /// is not of that form. The rightmost sign is used as the separator first.
    /// </summary>
    /// <param name="str">A trimmed, non-empty string.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    /// <returns>An <see cref="OffsetTimerStartToken"/> for the specified string, or <see langword="null"/> if the string
    /// is not of that form.</returns>
    private static TimerStartToken? FromOffsetString(string str, IFormatProvider provider)
    {
        return FromOffsetString(str, provider, new Dictionary<string, TimerStartToken?>(StringComparer.Ordinal));
    }

    /// <summary>
    /// Returns an <see cref="OffsetTimerStartToken"/> for a string, as <c>FromOffsetString</c> with a single argument
    /// does, memoizing results by string so that inputs with many signs are parsed in polynomial time.
    /// </summary>
    /// <param name="str">A trimmed, non-empty string.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    /// <param name="memo">The results computed so far, keyed by string.</param>
    /// <returns>An <see cref="OffsetTimerStartToken"/> for the specified string, or <see langword="null"/> if the string
    /// is not of that form.</returns>
    private static TimerStartToken? FromOffsetString(string str, IFormatProvider provider, Dictionary<string, TimerStartToken?> memo)
    {
        if (memo.TryGetValue(str, out TimerStartToken? cached))
        {
            return cached;
        }

        TimerStartToken? result = null;

        for (int i = str.Length - 1; i > 0 && result is null; i--)
        {
            if (str[i] is not ('+' or '-'))
            {
                continue;
            }

            string inputStr = str.Substring(0, i).Trim();
            string offsetStr = str.Substring(i + 1).Trim();

            if (inputStr.Length == 0 || !TimeSpanToken.Parser.Instance.TryParse(offsetStr, provider, out TimerStartToken? offsetToken))
            {
                continue;
            }

            TimerStartToken? inputToken = FromStringWithoutOffset(inputStr, provider) ?? FromOffsetString(inputStr, provider, memo);
            if (inputToken is null)
            {
                continue;
            }

            result = new OffsetTimerStartToken
            {
                Input = inputToken,
                Negative = str[i] == '-',
                Offset = (TimeSpanToken)offsetToken!
            };
        }

        memo[str] = result;
        return result;
    }

    /// <summary>
    /// Parses a string of the form <c>[+-]&lt;offset&gt;[+-]&lt;offset&gt;...</c>, where each offset is a
    /// <see cref="TimeSpanToken"/>.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <param name="offsets">The offsets parsed from the string in order, where a <c>-</c> sign yields a <see
    /// cref="NegativeOffset"/> and a <c>+</c> sign yields an <see cref="Offset"/>; or an empty list if the string is not
    /// of that form.</param>
    /// <returns><see langword="true"/> if the string is of the form <c>[+-]&lt;offset&gt;...</c>, or <see
    /// langword="false"/> otherwise.</returns>
    public static bool TryParseLeadingOffsets(string str, out IReadOnlyList<Offset> offsets)
    {
        offsets = Array.Empty<Offset>();

        string trimmed = str?.Trim() ?? string.Empty;
        if (trimmed.Length < 2 || trimmed[0] is not ('+' or '-'))
        {
            return false;
        }

        List<Offset> parsed = new();
        int signIndex = 0;

        for (int i = 1; i <= trimmed.Length; i++)
        {
            if (i < trimmed.Length && trimmed[i] is not ('+' or '-'))
            {
                continue;
            }

            string span = trimmed.Substring(signIndex + 1, i - signIndex - 1).Trim();
            if (!TimeSpanToken.Parser.Instance.TryParse(span, out TimerStartToken? token) || token is not TimeSpanToken timeSpanToken)
            {
                return false;
            }

            parsed.Add(trimmed[signIndex] == '-' ? new NegativeOffset(timeSpanToken) : new Offset(timeSpanToken));
            signIndex = i;
        }

        offsets = parsed;
        return true;
    }

    /// <summary>
    /// Returns the end time for a timer started with this token at a specified time.
    /// </summary>
    /// <param name="startTime">The time the timer is started.</param>
    /// <returns>The end time for a timer started with this token at the specified time.</returns>
    public abstract DateTime GetEndTime(DateTime startTime);

    /// <summary>
    /// Returns the end time for a timer started with this token at a specified time.
    /// </summary>
    /// <param name="startTime">The time the timer is started.</param>
    /// <param name="endTime">The end time for a timer started with this token at the specified time if the end
    /// time could be computed, or <see cref="DateTime.MinValue"/> otherwise.</param>
    /// <returns><see langword="true"/> if the end time could be computed, or <see langword="false"/> otherwise.</returns>
    public bool TryGetEndTime(DateTime startTime, out DateTime endTime)
    {
        try
        {
            endTime = GetEndTime(startTime);
            return true;
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            endTime = DateTime.MinValue;
            return false;
        }
    }

    /// <inheritdoc />
    public sealed override string ToString()
    {
        return ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/> to use.</param>
    /// <returns>A string that represents the current object.</returns>
    protected abstract string ToString(IFormatProvider provider);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if <see cref="IsValid"/> is <see langword="false"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">If not valid.</exception>
    protected void ThrowIfNotValid()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Returns a <see cref="TimerStartToken"/> that is a <see cref="DateTimeToken"/> for the specified string, or
    /// <see langword="null"/> if the string is not a supported representation of a <see cref="DateTimeToken"/>.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <returns>A <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="DateTimeToken"/>.</returns>
    private static TimerStartToken? FromDateTime(string str)
    {
        if (DateTimeToken.Parser.Instance.TryParse(str, out var timerStartToken))
        {
            return timerStartToken;
        }

        return null;
    }

    /// <summary>
    /// Returns a <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/> favoring a <see cref="TimeSpanToken"/> over a
    /// <see cref="DateTimeToken"/> in the case ambiguity.
    /// </summary>
    /// <param name="str">A string.</param>
    /// <returns>A <see cref="TimerStartToken"/> for the specified string, or <see langword="null"/> if the string is not a
    /// supported representation of a <see cref="TimerStartToken"/>.</returns>
    private static TimerStartToken? FromTimeSpanOrDateTimeString(string str)
    {
        if (TimeSpanToken.Parser.Instance.TryParse(str, out var timerStartToken))
        {
            return timerStartToken;
        }

        if (DateTimeToken.Parser.Instance.TryParse(str, out timerStartToken))
        {
            return timerStartToken;
        }

        return null;
    }

    /// <summary>
    /// Parses <see cref="TimerStartToken"/> strings.
    /// </summary>
    public abstract class Parser
    {
        /// <summary>
        /// The <see cref="RegexOptions"/> used when matching regular expressions.
        /// </summary>
        public static readonly RegexOptions RegexOptions =
            RegexOptions.CultureInvariant |
            RegexOptions.IgnoreCase |
            RegexOptions.IgnorePatternWhitespace;

        /// <summary>
        /// Parses a string into a <see cref="TimerStartToken"/>.
        /// </summary>
        /// <param name="str">A string representation of a <see cref="TimerStartToken"/>.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>The <see cref="TimerStartToken"/> parsed from the string.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="str"/> or <paramref name="provider"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">If <paramref name="str"/> is not a supported representation of a <see cref="TimerStartToken"/>.</exception>
        public TimerStartToken Parse(string? str, IFormatProvider provider)
        {
            if (str is null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            TimerStartToken timerStartToken = ParseInternal(str, provider);

            if (!timerStartToken.IsValid)
            {
                throw new FormatException();
            }

            timerStartToken.OriginalInput = str;

            return timerStartToken;
        }

        /// <summary>
        /// Parses a string into a <see cref="TimerStartToken"/>.
        /// </summary>
        /// <remarks>
        /// This overload uses the <see cref="CultureInfo.CurrentCulture"/> as the <see cref="IFormatProvider"/>.
        /// </remarks>
        /// <param name="str">A string representation of a <see cref="TimerStartToken"/>.</param>
        /// <param name="timerStartToken">The <see cref="TimerStartToken"/> parsed from the string, or <see langword="null"/>
        /// if the string is not a supported representation of a <see cref="TimerStartToken"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="TimerStartToken"/> was successfully parsed from <paramref
        /// name="str"/>, or <see langword="false"/> otherwise.</returns>
        public bool TryParse(string str, out TimerStartToken? timerStartToken)
        {
            return TryParse(str, CultureInfo.CurrentCulture, out timerStartToken);
        }

        /// <summary>
        /// Parses a string into a <see cref="TimerStartToken"/>.
        /// </summary>
        /// <param name="str">A string representation of a <see cref="TimerStartToken"/>.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <param name="timerStartToken">The <see cref="TimerStartToken"/> parsed from the string, or <see langword="null"/>
        /// if the string is not a supported representation of a <see cref="TimerStartToken"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="TimerStartToken"/> was successfully parsed from <paramref
        /// name="str"/>, or <see langword="false"/> otherwise.</returns>
        public bool TryParse(string str, IFormatProvider provider, out TimerStartToken? timerStartToken)
        {
            try
            {
                timerStartToken = Parse(str, provider);
                return true;
            }
            catch (Exception ex) when (ex.CanBeHandled())
            {
                timerStartToken = null;
                return false;
            }
        }

        /// <summary>
        /// Parses a string into a <see cref="TimerStartToken"/>.
        /// </summary>
        /// <param name="str">A string representation of a <see cref="TimerStartToken"/>.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
        /// <returns>The <see cref="TimerStartToken"/> parsed from the string.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="str"/> or <paramref name="provider"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">If <paramref name="str"/> is not a supported representation of a <see cref="TimerStartToken"/>.</exception>
        protected abstract TimerStartToken ParseInternal(string str, IFormatProvider provider);
    }
}