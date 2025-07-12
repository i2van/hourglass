// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EmptyDateToken.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Parsing;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Represents an unspecified date.
/// </summary>
public sealed class EmptyDateToken : DateToken
{
    /// <inheritdoc />
    public override bool IsValid => true;

    /// <inheritdoc />
    public override DateTime ToDateTime(DateTime minDate, bool inclusive)
    {
        ThrowIfNotValid();

        return inclusive ? minDate.Date : minDate.Date.AddDays(1);
    }

    /// <inheritdoc />
    public override string ToString(IFormatProvider provider)
    {
        return string.Empty;
    }

    /// <summary>
    /// Parses <see cref="EmptyDateToken"/> strings.
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
        public override bool IsCompatibleWith(TimeToken.Parser timeTokenParser)
        {
            return timeTokenParser is not EmptyTimeToken.Parser;
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetPatterns(IFormatProvider provider)
        {
            yield return string.Empty;
        }

        /// <inheritdoc />
        protected override DateToken ParseInternal(Match match, IFormatProvider provider)
        {
            return new EmptyDateToken();
        }
    }
}