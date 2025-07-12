// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationAreaIconManager.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Managers;

using Windows;

/// <summary>
/// Manages the <see cref="NotificationAreaIcon"/>.
/// </summary>
public sealed class NotificationAreaIconManager : Manager
{
    /// <summary>
    /// Singleton instance of the <see cref="NotificationAreaIconManager"/> class.
    /// </summary>
    public static readonly NotificationAreaIconManager Instance = new();

    /// <summary>
    /// Prevents a default instance of the <see cref="NotificationAreaIconManager"/> class from being created.
    /// </summary>
    private NotificationAreaIconManager()
    {
    }

    /// <summary>
    /// Gets the icon for the app in the notification area of the taskbar.
    /// </summary>
    public NotificationAreaIcon NotifyIcon { get; private set; } = null!;

    /// <inheritdoc />
    public override void Initialize()
    {
        NotifyIcon = new();
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
            NotifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}