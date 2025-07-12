// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EmptyTimeToken.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Parsing;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Represents an unspecified time.
/// </summary>
public sealed class EmptyTimeToken : TimeToken
{
    /// <inheritdoc />
    public override bool IsValid => true;

    /// <inheritdoc />
    public override DateTime ToDateTime(DateTime minDate, DateTime datePart)
    {
        ThrowIfNotValid();

        return datePart.Date;
    }

    /// <inheritdoc />
    public override string ToString(IFormatProvider provider)
    {
        return string.Empty;
    }

    /// <summary>
    /// Parses <see cref="EmptyTimeToken"/> strings.
    /// </summary>
    public new sealed class Parser : TimeToken.Parser
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
        public override bool IsCompatibleWith(DateToken.Parser dateTokenParser)
        {
            return dateTokenParser is not EmptyDateToken.Parser;
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetPatterns(IFormatProvider provider)
        {
            yield return string.Empty;
        }

        /// <inheritdoc />
        protected override TimeToken ParseInternal(Match match, IFormatProvider provider)
        {
            return new EmptyTimeToken();
        }
    }
}