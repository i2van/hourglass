// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerOptions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Timing;

using System;
using System.ComponentModel;
using System.Windows;

using Extensions;
using Serialization;
using Windows;

/// <summary>
/// Modes indicating what information to display in the timer window title.
/// </summary>
public enum WindowTitleMode
{
    /// <summary>
    /// This mode is obsolete. When it is specified, the <see cref="TimerOptions.HideWindowTitleBar"/> option is
    /// set instead, and the timer window title is set to show the application name.
    /// </summary>
    None,

    /// <summary>
    /// The timer window title is set to show the application name.
    /// </summary>
    ApplicationName,

    /// <summary>
    /// The timer window title is set to show the time left.
    /// </summary>
    TimeLeft,

    /// <summary>
    /// The timer window title is set to show the time elapsed.
    /// </summary>
    TimeElapsed,

    /// <summary>
    /// The timer window title is set to show the timer title.
    /// </summary>
    TimerTitle,

    /// <summary>
    /// The timer window title is set to show the time left then the timer title.
    /// </summary>
    TimeLeftPlusTimerTitle,

    /// <summary>
    /// The timer window title is set to show the time elapsed then the timer title.
    /// </summary>
    TimeElapsedPlusTimerTitle,

    /// <summary>
    /// The timer window title is set to show the timer title then the time left.
    /// </summary>
    TimerTitlePlusTimeLeft,

    /// <summary>
    /// The timer window title is set to show the timer title then the time elapsed.
    /// </summary>
    TimerTitlePlusTimeElapsed
}

/// <summary>
/// Configuration data for a timer.
/// </summary>
public sealed class TimerOptions : INotifyPropertyChanged
{
    /// <summary>
    /// The minimum timer window opacity, in percent.
    /// </summary>
    public const int MinOpacity = 10;

    /// <summary>
    /// The maximum timer window opacity, in percent. The timer window is opaque.
    /// </summary>
    public const int MaxOpacity = 100;

    /// <summary>
    /// The timer window opacity step, in percent. The timer window opacity is always a multiple of it.
    /// </summary>
    public const int OpacityStep = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerOptions"/> class.
    /// </summary>
    public TimerOptions()
    {
        Title = string.Empty;
        AlwaysOnTop = false;
        PromptOnExit = true;
        ShowProgressInTaskbar = true;
        DoNotKeepComputerAwake = false;
        ReverseProgressBar = false;
        DigitalClockTime = false;
        ShowTimeElapsed = false;
        ShowTriggerTime = false;
        ShowIteration = false;
        ResetExpiredTimer = false;
        LoopTimer = false;
        PauseBeforeLoopTimer = false;
        StartPaused = false;
        PopUpWhenExpired = true;
        CloseWhenExpired = false;
        MinimizeWhenExpired = false;
        ShutDownWhenExpired = false;
        Theme = Theme.DefaultTheme;
        Sound = Sound.DefaultSound;
        LoopSound = false;
        WindowTitleMode = WindowTitleMode.ApplicationName;
        HideWindowTitleBar = false;
        Opacity = MaxOpacity;
        WindowSize = new(
            new(double.PositiveInfinity, double.PositiveInfinity, InterfaceScaler.BaseWindowWidth, InterfaceScaler.BaseWindowHeight),
            WindowState.Normal,
            WindowState.Normal,
            false /* isFullScreen */);
        LockInterface = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerOptions"/> class from another instance of the <see
    /// cref="TimerOptions"/> class.
    /// </summary>
    /// <param name="options">A <see cref="TimerOptions"/>.</param>
    public TimerOptions(TimerOptions options)
    {
        Set(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerOptions"/> class from a <see cref="TimerOptionsInfo"/>.
    /// </summary>
    /// <param name="info">A <see cref="TimerOptionsInfo"/>.</param>
    public TimerOptions(TimerOptionsInfo info)
    {
        Set(info);
    }

    /// <summary>
    /// Raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets a user-specified title for the timer.
    /// </summary>
    public string? Title
    {
        get =>
            LockInterface
                ? field?.StartsWith(Properties.Resources.LockInterfaceSymbol) == true
                    ? field
                    : $"{Properties.Resources.LockInterfaceSymbol}{(string.IsNullOrWhiteSpace(field) ? string.Empty : field)}"
                : field;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the timer window should always be displayed on top of other windows.
    /// </summary>
    public bool AlwaysOnTop
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to prompt the user before closing the timer window if the timer is
    /// running.
    /// </summary>
    public bool PromptOnExit
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show progress in the taskbar.
    /// </summary>
    public bool ShowProgressInTaskbar
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to keep the computer awake while the timer is running.
    /// </summary>
    public bool DoNotKeepComputerAwake
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to reverse the progress bar (count backwards).
    /// </summary>
    public bool ReverseProgressBar
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to display time in the digital clock format.
    /// </summary>
    public bool DigitalClockTime
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the time elapsed rather than the time left.
    /// </summary>
    public bool ShowTimeElapsed
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the trigger time.
    /// </summary>
    public bool ShowTriggerTime
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the iteration of the timer in the window title.
    /// </summary>
    public bool ShowIteration
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to reset the expired timer when the timer window switches to the
    /// input mode, for example when the user clicks the expired timer window.
    /// </summary>
    public bool ResetExpiredTimer
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to loop the timer continuously.
    /// </summary>
    public bool LoopTimer
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to pause before loop the timer continuously.
    /// </summary>
    public bool PauseBeforeLoopTimer
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to start the timer in a paused state.
    /// </summary>
    public bool StartPaused
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the timer window should be brought to the top of other windows when
    /// the timer expires.
    /// </summary>
    public bool PopUpWhenExpired
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the timer window should be closed when the timer expires.
    /// </summary>
    public bool CloseWhenExpired
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the timer window should be minimized when the timer expires.
    /// </summary>
    public bool MinimizeWhenExpired
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether Windows should be shut down when the timer expires.
    /// </summary>
    public bool ShutDownWhenExpired
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets the theme of the timer window.
    /// </summary>
    public Theme? Theme
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets the sound to play when the timer expires, or <see langword="null"/> if no sound is to be played.
    /// </summary>
    public Sound? Sound
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the sound that plays when the timer expires should be looped until
    /// stopped by the user.
    /// </summary>
    public bool LoopSound
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating what information to display in the timer window title.
    /// </summary>
    public WindowTitleMode WindowTitleMode
    {
        get;

        set
        {
            // The None mode is obsolete. Hide the window title bar and show the application name instead.
            if (value == WindowTitleMode.None)
            {
                HideWindowTitleBar = true;
                value = WindowTitleMode.ApplicationName;
            }

            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the timer window title bar is hidden. The timer window title itself
    /// is not affected.
    /// </summary>
    public bool HideWindowTitleBar
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets the timer window opacity, in percent. The value is clamped by <see cref="ClampOpacity"/>. The
    /// opacity applies to the whole timer window, including the title bar and the system buttons.
    /// </summary>
    public int Opacity
    {
        get;

        set
        {
            value = ClampOpacity(value);

            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets the size, position, and state of the timer window.
    /// </summary>
    public WindowSize? WindowSize
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user interface should be locked, preventing the user from taking
    /// any action until the timer expires.
    /// </summary>
    public bool LockInterface
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged.Notify(this, nameof(LockInterface), nameof(Title));
        }
    }

    /// <summary>
    /// Returns a <see cref="TimerOptions"/> for the specified <see cref="TimerOptions"/>, or <see langword="null"/> if the
    /// specified <see cref="TimerOptions"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="options">A <see cref="TimerOptions"/>.</param>
    /// <returns>A <see cref="TimerOptions"/> for the specified <see cref="TimerOptions"/>, or <see langword="null"/> if the specified <see cref="TimerOptions"/> is <see langword="null"/>.</returns>
    public static TimerOptions? FromTimerOptions(TimerOptions? options)
    {
        return options is not null ? new TimerOptions(options) : null;
    }

    /// <summary>
    /// Returns a <see cref="TimerOptions"/> for the specified <see cref="TimerOptionsInfo"/>, or <see langword="null"/> if
    /// the specified <see cref="TimerOptionsInfo"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="info">A <see cref="TimerOptionsInfo"/>.</param>
    /// <returns>A <see cref="TimerOptions"/> for the specified <see cref="TimerOptionsInfo"/>, or <see langword="null"/> if the specified <see cref="TimerOptionsInfo"/> is <see langword="null"/>.</returns>
    public static TimerOptions? FromTimerOptionsInfo(TimerOptionsInfo? info)
    {
        return info is not null ? new TimerOptions(info) : null;
    }

    /// <summary>
    /// Returns the specified timer window opacity limited to the range from <see cref="MinOpacity"/> to <see
    /// cref="MaxOpacity"/> and rounded to the nearest multiple of <see cref="OpacityStep"/>.
    /// </summary>
    /// <param name="opacity">A timer window opacity, in percent.</param>
    /// <returns>The specified timer window opacity limited to the range from <see cref="MinOpacity"/> to <see cref="MaxOpacity"/> and rounded to the nearest multiple of <see cref="OpacityStep"/>.</returns>
    public static int ClampOpacity(int opacity)
    {
        int clampedOpacity = Math.Max(MinOpacity, Math.Min(MaxOpacity, opacity));
        return (int)Math.Round(clampedOpacity / (double)OpacityStep, MidpointRounding.AwayFromZero) * OpacityStep;
    }

    /// <summary>
    /// Sets all the options from another instance of the <see cref="TimerOptions"/> class.
    /// </summary>
    /// <param name="options">A <see cref="TimerOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public void Set(TimerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        Title = options.Title;
        AlwaysOnTop = options.AlwaysOnTop;
        PromptOnExit = options.PromptOnExit;
        ShowProgressInTaskbar = options.ShowProgressInTaskbar;
        DoNotKeepComputerAwake = options.DoNotKeepComputerAwake;
        ReverseProgressBar = options.ReverseProgressBar;
        DigitalClockTime = options.DigitalClockTime;
        ShowTimeElapsed = options.ShowTimeElapsed;
        ShowTriggerTime = options.ShowTriggerTime;
        ShowIteration = options.ShowIteration;
        ResetExpiredTimer = options.ResetExpiredTimer;
        LoopTimer = options.LoopTimer;
        PauseBeforeLoopTimer = options.PauseBeforeLoopTimer;
        StartPaused = options.StartPaused;
        PopUpWhenExpired = options.PopUpWhenExpired;
        CloseWhenExpired = options.CloseWhenExpired;
        MinimizeWhenExpired = options.MinimizeWhenExpired;
        ShutDownWhenExpired = options.ShutDownWhenExpired;
        Theme = options.Theme;
        Sound = options.Sound;
        LoopSound = options.LoopSound;
        // HideWindowTitleBar must be set before WindowTitleMode, which overrides it for the obsolete None mode.
        HideWindowTitleBar = options.HideWindowTitleBar;
        WindowTitleMode = options.WindowTitleMode;
        Opacity = options.Opacity;
        WindowSize = WindowSize.FromWindowSize(options.WindowSize);
        LockInterface = options.LockInterface;

        PropertyChanged.Notify(this,
            nameof(WindowTitleMode),
            nameof(HideWindowTitleBar),
            nameof(Opacity),
            nameof(WindowSize),
            nameof(Title),
            nameof(AlwaysOnTop),
            nameof(PromptOnExit),
            nameof(ShowProgressInTaskbar),
            nameof(DoNotKeepComputerAwake),
            nameof(ReverseProgressBar),
            nameof(DigitalClockTime),
            nameof(ShowTimeElapsed),
            nameof(ResetExpiredTimer),
            nameof(LoopTimer),
            nameof(PauseBeforeLoopTimer),
            nameof(StartPaused),
            nameof(PopUpWhenExpired),
            nameof(CloseWhenExpired),
            nameof(MinimizeWhenExpired),
            nameof(ShutDownWhenExpired),
            nameof(Theme),
            nameof(Sound),
            nameof(LoopSound),
            nameof(LockInterface));
    }

    /// <summary>
    /// Sets all the options from an instance of the <see cref="TimerOptionsInfo"/> class.
    /// </summary>
    /// <param name="info">A <see cref="TimerOptionsInfo"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is <see langword="null"/></exception>
    private void Set(TimerOptionsInfo info)
    {
        if (info is null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        Title = info.Title;
        AlwaysOnTop = info.AlwaysOnTop;
        PromptOnExit = info.PromptOnExit;
        ShowProgressInTaskbar = info.ShowProgressInTaskbar;
        DoNotKeepComputerAwake = info.DoNotKeepComputerAwake;
        ReverseProgressBar = info.ReverseProgressBar;
        DigitalClockTime = info.DigitalClockTime;
        ShowTimeElapsed = info.ShowTimeElapsed;
        ShowTriggerTime = info.ShowTriggerTime;
        ShowIteration = info.ShowIteration;
        ResetExpiredTimer = info.ResetExpiredTimer;
        LoopTimer = info.LoopTimer;
        PauseBeforeLoopTimer = info.PauseBeforeLoopTimer;
        StartPaused = info.StartPaused;
        PopUpWhenExpired = info.PopUpWhenExpired;
        CloseWhenExpired = info.CloseWhenExpired;
        MinimizeWhenExpired = info.MinimizeWhenExpired;
        ShutDownWhenExpired = info.ShutDownWhenExpired;
        Theme = Theme.FromIdentifier(info.ThemeIdentifier);
        Sound = Sound.FromIdentifier(info.SoundIdentifier);
        LoopSound = info.LoopSound;
        // HideWindowTitleBar must be set before WindowTitleMode, which overrides it for the obsolete None mode.
        HideWindowTitleBar = info.HideWindowTitleBar;
        WindowTitleMode = info.WindowTitleMode;
        Opacity = info.Opacity;
        WindowSize = WindowSize.FromWindowSizeInfo(info.WindowSize);
        LockInterface = info.LockInterface;

        PropertyChanged.Notify(this,
            nameof(WindowTitleMode),
            nameof(HideWindowTitleBar),
            nameof(Opacity),
            nameof(WindowSize),
            nameof(Title),
            nameof(AlwaysOnTop),
            nameof(PromptOnExit),
            nameof(ShowProgressInTaskbar),
            nameof(DoNotKeepComputerAwake),
            nameof(ReverseProgressBar),
            nameof(DigitalClockTime),
            nameof(ShowTimeElapsed),
            nameof(ResetExpiredTimer),
            nameof(LoopTimer),
            nameof(PauseBeforeLoopTimer),
            nameof(StartPaused),
            nameof(PopUpWhenExpired),
            nameof(CloseWhenExpired),
            nameof(MinimizeWhenExpired),
            nameof(ShutDownWhenExpired),
            nameof(Theme),
            nameof(Sound),
            nameof(LoopSound),
            nameof(LockInterface));
    }

    /// <summary>
    /// Returns the representation of the <see cref="TimerOptions"/> used for XML serialization.
    /// </summary>
    /// <returns>The representation of the <see cref="TimerOptions"/> used for XML serialization.</returns>
    public TimerOptionsInfo ToTimerOptionsInfo()
    {
        return new()
        {
            Title = Title,
            AlwaysOnTop = AlwaysOnTop,
            PromptOnExit = PromptOnExit,
            ShowProgressInTaskbar = ShowProgressInTaskbar,
            DoNotKeepComputerAwake = DoNotKeepComputerAwake,
            ReverseProgressBar = ReverseProgressBar,
            DigitalClockTime = DigitalClockTime,
            ShowTimeElapsed = ShowTimeElapsed,
            ShowTriggerTime = ShowTriggerTime,
            ShowIteration = ShowIteration,
            ResetExpiredTimer = ResetExpiredTimer,
            LoopTimer = LoopTimer,
            PauseBeforeLoopTimer = PauseBeforeLoopTimer,
            StartPaused = StartPaused,
            PopUpWhenExpired = PopUpWhenExpired,
            CloseWhenExpired = CloseWhenExpired,
            MinimizeWhenExpired = MinimizeWhenExpired,
            ShutDownWhenExpired = ShutDownWhenExpired,
            ThemeIdentifier = Theme?.Identifier,
            SoundIdentifier = Sound?.Identifier,
            LoopSound = LoopSound,
            WindowTitleMode = WindowTitleMode,
            HideWindowTitleBar = HideWindowTitleBar,
            Opacity = Opacity,
            WindowSize = WindowSizeInfo.FromWindowSize(WindowSize)!,
            LockInterface = LockInterface
        };
    }
}