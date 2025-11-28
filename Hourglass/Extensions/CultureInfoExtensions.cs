// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CultureInfoExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Extensions;

using System;
using System.Globalization;
using System.Text.RegularExpressions;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Provides extensions methods for the <see cref="CultureInfo"/> class and the related <see
/// cref="IFormatProvider"/> interface.
/// </summary>
public static class CultureInfoExtensions
{
    /// <param name="provider">An <see cref="IFormatProvider"/>.</param>
    extension(IFormatProvider provider)
    {
        /// <summary>
        /// Returns a value indicating whether an <see cref="IFormatProvider"/> prefers the month-day-year ordering in
        /// date representations.
        /// </summary>
        /// <returns>A value indicating whether the specified <see cref="IFormatProvider"/> prefers the month-day-year
        /// ordering in date representations.</returns>
        public bool IsMonthFirst =>
            Regex.IsMatch(provider.GetShortDatePattern(), @"^.*M.*d.*y.*$");

        /// <summary>
        /// Returns a value indicating whether an <see cref="IFormatProvider"/> prefers the year-month-day ordering in
        /// date representations.
        /// </summary>
        /// <returns>A value indicating whether the specified <see cref="IFormatProvider"/> prefers the year-month-day
        /// ordering in date representations.</returns>
        public bool IsYearFirst =>
            Regex.IsMatch(provider.GetShortDatePattern(), @"^.*y.*M.*d.*$");

        private string GetShortDatePattern() =>
            ((DateTimeFormatInfo?)provider.GetFormat(typeof(DateTimeFormatInfo)))!.ShortDatePattern;
    }
}