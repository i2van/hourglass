// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerWindow.TitleBar.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

using Hourglass.Extensions;
using Hourglass.Timing;

namespace Hourglass.Windows;

/// <summary>
/// Shows and hides the <see cref="TimerWindow"/> title bar.
/// </summary>
public sealed partial class TimerWindow
{
    /// <summary>
    /// The <c>WS_OVERLAPPEDWINDOW</c> style of a resizable window with a title bar.
    /// </summary>
    private const int WsOverlappedWindow = 0x00CF0000;

    /// <summary>
    /// The <see cref="System.Windows.Shell.WindowChrome"/> applied to the window while its title bar is hidden.
    /// </summary>
    private WindowChrome? _hiddenTitleBarWindowChrome;

    /// <summary>
    /// Gets a value indicating whether the window title bar is hidden.
    /// </summary>
    private bool IsTitleBarHidden =>
        Options.HideWindowTitleBar;

    /// <summary>
    /// Resets the window size to the default one, keeping the size of the window client area independent of
    /// whether the title bar is shown or hidden.
    /// </summary>
    public void ResetSize()
    {
        Width  = InterfaceScaler.BaseWindowWidth;
        Height = InterfaceScaler.BaseWindowHeight;

        if (!IsTitleBarHidden)
        {
            return;
        }

        Size titleBarFrameSize = GetTitleBarFrameSize();

        Width  = Math.Max(Width  - titleBarFrameSize.Width,  0);
        Height = Math.Max(Height - titleBarFrameSize.Height, 0);
    }

    /// <summary>
    /// Shows or hides the window title bar depending on the <see cref="TimerOptions.HideWindowTitleBar"/> and on
    /// whether the window is in full-screen mode.
    /// </summary>
    /// <param name="keepClientArea">A value indicating whether the position and the size of the window client area
    /// are kept unchanged by moving and resizing the window by the size of the title bar and the window border being
    /// shown or hidden.</param>
    private void UpdateTitleBar(bool keepClientArea = false)
    {
        bool isTitleBarHidden = IsTitleBarHidden;

        WindowStyle windowStyle = isTitleBarHidden || IsFullScreen
            ? WindowStyle.None
            : WindowStyle.SingleBorderWindow;

        WindowChrome? windowChrome = isTitleBarHidden
            ? _hiddenTitleBarWindowChrome ??= new()
            {
                CaptionHeight = 0,
                UseAeroCaptionButtons = false
            }
            : null;

        if (WindowStyle == windowStyle &&
            ReferenceEquals(WindowChrome.GetWindowChrome(this), windowChrome))
        {
            return;
        }

        // The window rectangle is left intact when the title bar is shown or hidden, so the client area moves and
        // grows or shrinks by the size of the title bar and the window border unless the window is moved and resized
        // by the very same size.
        Rect clientBounds = keepClientArea && !IsFullScreen && WindowState == WindowState.Normal
            ? GetClientBounds()
            : Rect.Empty;

        WindowStyle = windowStyle;
        WindowChrome.SetWindowChrome(this, windowChrome);

        // Removing the window chrome resets the window background color.
        UpdateOpacity();

        if (!clientBounds.IsEmpty)
        {
            Rect newClientBounds = GetClientBounds();

            if (!newClientBounds.IsEmpty)
            {
                Left   += clientBounds.Left   - newClientBounds.Left;
                Top    += clientBounds.Top    - newClientBounds.Top;
                Width  += clientBounds.Width  - newClientBounds.Width;
                Height += clientBounds.Height - newClientBounds.Height;
            }
        }

        if (isTitleBarHidden)
        {
            return;
        }

        // A window with a title bar cannot be smaller than the minimum tracking size.
        Width  = Math.Max(Width,  this.GetMinTrackWidth());
        Height = Math.Max(Height, this.GetMinTrackHeight());
    }

    /// <summary>
    /// Returns the bounds of the window client area in screen coordinates in device-independent units, or <see
    /// cref="Rect.Empty"/> if the window has no handle yet.
    /// </summary>
    /// <returns>The bounds of the window client area in screen coordinates in device-independent units, or <see
    /// cref="Rect.Empty"/> if the window has no handle yet.</returns>
    private Rect GetClientBounds()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;

        if (handle == IntPtr.Zero || !GetClientRect(handle, out RECT clientRect))
        {
            return Rect.Empty;
        }

        POINT topLeft     = new() { x = clientRect.left,  y = clientRect.top    };
        POINT bottomRight = new() { x = clientRect.right, y = clientRect.bottom };

        if (!ClientToScreen(handle, ref topLeft) || !ClientToScreen(handle, ref bottomRight))
        {
            return Rect.Empty;
        }

        // The rectangle is normalized, as the client corners are swapped horizontally if the window layout is mirrored.
        Rect clientBounds = new(new Point(topLeft.x, topLeft.y), new Point(bottomRight.x, bottomRight.y));
        clientBounds.Transform(GetTransformFromDevice());

        return clientBounds;
    }

    /// <summary>
    /// Returns the size that the title bar and the window border add to the size of the window client area in
    /// device-independent units.
    /// </summary>
    /// <returns>The size that the title bar and the window border add to the size of the window client area in
    /// device-independent units.</returns>
    private Size GetTitleBarFrameSize()
    {
        RECT frameRect = default;

        return AdjustWindowRectEx(ref frameRect, WsOverlappedWindow, false, 0)
            ? GetDeviceIndependentSize(frameRect)
            : new(0, 0);
    }

    /// <summary>
    /// Converts the size of the specified rectangle from device pixels to device-independent units.
    /// </summary>
    /// <param name="rect">A rectangle in device pixels.</param>
    /// <returns>The size of the specified rectangle in device-independent units.</returns>
    private Size GetDeviceIndependentSize(RECT rect)
    {
        Vector size = GetTransformFromDevice().Transform(
            new Vector(rect.right - rect.left, rect.bottom - rect.top));

        return new(size.X, size.Y);
    }

    /// <summary>
    /// Returns the matrix that transforms the coordinates from device pixels to device-independent units.
    /// </summary>
    /// <returns>The matrix that transforms the coordinates from device pixels to device-independent units.</returns>
    private Matrix GetTransformFromDevice() =>
        PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);
}
