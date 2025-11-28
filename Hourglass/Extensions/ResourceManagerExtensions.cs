// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceManagerExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Extensions;

using System;
using System.Globalization;
using System.Resources;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Provides extensions methods for the <see cref="ResourceManager"/> class.
/// </summary>
public static class ResourceManagerExtensions
{
    /// <param name="resourceManager">A <see cref="ResourceManager"/>.</param>
    extension(ResourceManager resourceManager)
    {
        /// <summary>
        /// Returns an <see cref="IFormatProvider"/> for the culture that is actually loaded when retrieving resources.
        /// </summary>
        /// <returns>An <see cref="IFormatProvider"/> for the culture that is actually loaded when retrieving
        /// resources.</returns>
        public IFormatProvider GetEffectiveProvider()
        {
            string? cultureName = resourceManager.GetString(nameof(Properties.Resources.CultureName));
            return !string.IsNullOrWhiteSpace(cultureName) ? CultureInfo.GetCultureInfo(cultureName) : CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Returns an <see cref="IFormatProvider"/> for the culture that is actually loaded when retrieving resources
        /// for the culture specified by <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/> that is a <see cref="CultureInfo"/>.</param>
        /// <returns>An <see cref="IFormatProvider"/> for the culture that is actually loaded when retrieving resources
        /// for the culture specified by <paramref name="provider"/>.</returns>
        public IFormatProvider GetEffectiveProvider(IFormatProvider provider)
        {
            string? cultureName = resourceManager.GetString(nameof(Properties.Resources.CultureName), (CultureInfo)provider);
            return !string.IsNullOrWhiteSpace(cultureName) ? CultureInfo.GetCultureInfo(cultureName) : CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Returns the value of the string resource localized for a culture specified by an <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that is a <see cref="CultureInfo"/>.</param>
        /// <returns>The value of the resource localized for the specified culture, or <c>null</c> if <paramref
        /// name="name"/> cannot be found in a resource set.</returns>
        public string GetString(string name, IFormatProvider provider)
        {
            return resourceManager.GetString(name, (CultureInfo)provider)!;
        }
    }
}