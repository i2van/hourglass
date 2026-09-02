// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerWindow.TitleBar.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

using Extensions;
using Timing;

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
    /// <param name="keepClientSize">A value indicating whether the size of the window client area is kept
    /// unchanged by growing or shrinking the window by the size of the title bar being shown or hidden.</param>
    private void UpdateTitleBar(bool keepClientSize = false)
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

        // The window rectangle is left intact when the title bar is shown or hidden, so the client area grows or
        // shrinks by the size of the title bar unless the window is resized by the very same size.
        Size clientSize = keepClientSize && !IsFullScreen && WindowState == WindowState.Normal
            ? GetClientSize()
            : Size.Empty;

        WindowStyle = windowStyle;
        WindowChrome.SetWindowChrome(this, windowChrome);

        if (!clientSize.IsEmpty)
        {
            Size newClientSize = GetClientSize();

            if (!newClientSize.IsEmpty)
            {
                Width  += clientSize.Width  - newClientSize.Width;
                Height += clientSize.Height - newClientSize.Height;
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
    /// Returns the size of the window client area in device-independent units, or <see cref="Size.Empty"/> if the
    /// window has no handle yet.
    /// </summary>
    /// <returns>The size of the window client area in device-independent units, or <see cref="Size.Empty"/> if the
    /// window has no handle yet.</returns>
    private Size GetClientSize()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;

        return handle != IntPtr.Zero && GetClientRect(handle, out RECT clientRect)
            ? GetDeviceIndependentSize(clientRect)
            : Size.Empty;
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
        Matrix transformFromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

        Vector size = transformFromDevice.Transform(
            new Vector(rect.right - rect.left, rect.bottom - rect.top));

        return new(size.X, size.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);
}
