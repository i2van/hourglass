// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Settings.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Properties;

using System;
using System.Collections.Generic;
using System.Linq;

using Serialization;
using Timing;
using Windows;

/// <summary>
/// Application settings.
/// </summary>
#if PORTABLE
[System.Configuration.SettingsProvider(typeof(PortableSettingsProvider))]
#endif
internal sealed partial class Settings
{
    private const int MaxTimeoutSeconds = 60*60;

    /// <summary>
    /// Gets or sets the most recent <see cref="TimerOptions"/>.
    /// </summary>
    public TimerOptions MostRecentOptions
    {
        get => TimerOptions.FromTimerOptionsInfo(MostRecentOptionsInfo) ?? new();
        set => MostRecentOptionsInfo = TimerOptionsInfo.FromTimerOptions(value);
    }

    /// <summary>
    /// Gets or sets the <see cref="Timer"/>s.
    /// </summary>
    public IList<Timer> Timers
    {
        get => [..(TimerInfos ?? []).Select(Timer.FromTimerInfo).OfType<Timer>()];
        set => TimerInfos = [..value.Select(TimerInfo.FromTimer).OfType<TimerInfo>()];
    }

    /// <summary>
    /// Gets or sets the <see cref="TimerStart"/>s.
    /// </summary>
    public IList<TimerStart> TimerStarts
    {
        get => [..(TimerStartInfos ?? []).Select(TimerStart.FromTimerStartInfo).OfType<TimerStart>()];
        set => TimerStartInfos = [..value.Select(TimerStartInfo.FromTimerStart).OfType<TimerStartInfo>()];
    }

    /// <summary>
    /// Gets or sets the collection of the themes defined by the user.
    /// </summary>
    public IList<Theme> UserProvidedThemes
    {
        get => [..(UserProvidedThemeInfos ?? []).Select(Theme.FromThemeInfo).OfType<Theme>()];
        set => UserProvidedThemeInfos = [..value.Select(ThemeInfo.FromTheme).OfType<ThemeInfo>()];
    }

    /// <summary>
    /// Gets or sets the <see cref="WindowSize"/>.
    /// </summary>
    public WindowSize? WindowSize
    {
        get => WindowSize.FromWindowSizeInfo(WindowSizeInfo);
        set => WindowSizeInfo = WindowSizeInfo.FromWindowSize(value);
    }

    public TimeSpan? MinimizeWhenExpiredTimeout =>
        GetWhenExpiredTimeout(MinimizeWhenExpiredSeconds);

    public TimeSpan? CloseWhenExpiredTimeout =>
        GetWhenExpiredTimeout(CloseWhenExpiredSeconds);

    public TimeSpan? ShutDownWhenExpiredTimeout =>
        GetWhenExpiredTimeout(ShutDownWhenExpiredSeconds);

    private static TimeSpan? GetWhenExpiredTimeout(int timeout) =>
        timeout is > 0 and <= MaxTimeoutSeconds
            ? TimeSpan.FromSeconds(timeout)
            : null;
}