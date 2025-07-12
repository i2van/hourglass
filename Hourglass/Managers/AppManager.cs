// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppManager.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Managers;

using System.Linq;

/// <summary>
/// Manages the app.
/// </summary>
public sealed class AppManager : Manager
{
    /// <summary>
    /// Singleton instance of the <see cref="AppManager"/> class.
    /// </summary>
    public static readonly AppManager Instance = new();

    /// <summary>
    /// The manager class singleton instances.
    /// </summary>
    private static readonly Manager[] Managers =
    [
        VirtualDesktopManager.Instance,
        TaskDialogManager.Instance,
        ErrorManager.Instance,
        SettingsManager.Instance,
        UpdateManager.Instance,
        KeepAwakeManager.Instance,
        WakeUpManager.Instance,
        NotificationAreaIconManager.Instance,
        ThemeManager.Instance,
        SoundManager.Instance,
        TimerStartManager.Instance,
        TimerOptionsManager.Instance,
        TimerManager.Instance
    ];

    /// <summary>
    /// Prevents a default instance of the <see cref="AppManager"/> class from being created.
    /// </summary>
    private AppManager()
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        foreach (Manager manager in Managers)
        {
            manager.Initialize();
        }
    }

    /// <inheritdoc />
    public override void Persist()
    {
        foreach (Manager manager in Managers.Reverse())
        {
            manager.Persist();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (Manager manager in Managers.Reverse())
            {
                manager.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}