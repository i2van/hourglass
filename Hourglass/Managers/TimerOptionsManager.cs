// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerOptionsManager.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Managers;

using System;
using System.Linq;
using System.Windows;

using Properties;
using Timing;
using Windows;

/// <summary>
/// Manages <see cref="TimerOptions"/>s.
/// </summary>
public sealed class TimerOptionsManager : Manager
{
    /// <summary>
    /// Singleton instance of the <see cref="TimerOptionsManager"/> class.
    /// </summary>
    public static readonly TimerOptionsManager Instance = new();

    /// <summary>
    /// The most recent <see cref="TimerOptions"/>.
    /// </summary>
    private TimerOptions _mostRecentOptions = new();

    /// <summary>
    /// The <see cref="TimerWindow.MenuLastShown"/> date and time for the window the <see cref="_mostRecentOptions"/>
    /// were taken from.
    /// </summary>
    private DateTime _mostRecentOptionsMenuLastShown;

    /// <summary>
    /// Prevents a default instance of the <see cref="TimerOptionsManager"/> class from being created.
    /// </summary>
    private TimerOptionsManager()
    {
    }

    /// <summary>
    /// Gets the most recent <see cref="TimerOptions"/>.
    /// </summary>
    public TimerOptions MostRecentOptions
    {
        get
        {
            UpdateMostRecentOptions();
            return TimerOptions.FromTimerOptions(_mostRecentOptions)!;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _mostRecentOptions = Settings.Default.MostRecentOptions;
    }

    /// <inheritdoc />
    public override void Persist()
    {
        UpdateMostRecentOptions();
        Settings.Default.MostRecentOptions = _mostRecentOptions;
    }

    /// <summary>
    /// Updates the <see cref="MostRecentOptions"/> from the currently opened <see cref="TimerWindow"/>s.
    /// </summary>
    private void UpdateMostRecentOptions()
    {
        if (Application.Current is null)
        {
            return;
        }

        // Get the options most recently shown to the user from a window that is still open
        TimerWindow? window = Application.Current.Windows.OfType<TimerWindow>()
            .Where(static window => window.IsVisible)
            .OrderByDescending(static window => window.MenuLastShown)
            .FirstOrDefault();

        // Keep the most recent options if the window options are older, e.g., when the window is made visible only
        // to confirm the exit
        if (window is null || window.MenuLastShown < _mostRecentOptionsMenuLastShown)
        {
            return;
        }

        _mostRecentOptionsMenuLastShown = window.MenuLastShown;

        _mostRecentOptions = TimerOptions.FromTimerOptions(window.Options)!;

        // Never save a title
        _mostRecentOptions.Title = string.Empty;

        // Never save shutting down when expired, lock interface, or start paused options
        _mostRecentOptions.ShutDownWhenExpired = false;
        _mostRecentOptions.LockInterface = false;
        _mostRecentOptions.StartPaused = false;
    }
}