// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Hourglass.Extensions;

/// <summary>
/// Provides utility methods for the <see cref="Color"/> struct.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts a <see cref="Color"/> to an <see cref="int"/> representation.
    /// </summary>
    /// <param name="color">A <see cref="Color"/>.</param>
    /// <returns>An <see cref="int"/> for <paramref name="color"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt(this Color color)
    {
        return (color.R << 0) | (color.G << 8) | (color.B << 16);
    }

    /// <summary>
    /// Converts a <see cref="string"/> representation of a <see cref="Color"/> into a <see cref="Color"/>.
    /// </summary>
    /// <param name="colorString">A <see cref="string"/> representation of a <see cref="Color"/>.</param>
    /// <returns>A <see cref="Color"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="colorString"/> is <see langword="null"/></exception>
    public static Color FromString(string colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            throw new ArgumentNullException(nameof(colorString));
        }

        object? color = ColorConverter.ConvertFromString(colorString);

        return color is null ? throw new ArgumentNullException(nameof(colorString)) : (Color)color;
    }
}