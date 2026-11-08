// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using KPreisser.UI;

using Hourglass.Managers;
using Hourglass.Windows;

namespace Hourglass.Extensions;

/// <summary>
/// Specifies options for restoring a <see cref="WindowSize"/> to a <see cref="Window"/>.
/// </summary>
[Flags]
public enum RestoreOptions
{
    /// <summary>
    /// Specifies that no options are set.
    /// </summary>
    None,

    /// <summary>
    /// Allows restoring a window to a minimized state.
    /// </summary>
    AllowMinimized
}

/// <summary>
/// Provides extensions methods for the <see cref="Window"/> class, and helper methods for manipulating the size,
/// position, and state of windows through the <see cref="WindowSize"/> class.
/// </summary>
public static class WindowExtensions
{
    /// <summary>
    /// Sets the Desktop Window Manager (DWM) non-client rendering attributes on the window to use (or not use)
    /// immersive dark mode. This method has no effect on versions of windows that do not support it.
    /// </summary>
    /// <param name="window">A <see cref="TimerWindow"/>.</param>
    /// <param name="useImmersiveDarkMode">A value indicating whether to use immersive dark mode.</param>
    public static void SetImmersiveDarkMode(this TimerWindow window, bool useImmersiveDarkMode)
    {
        if (!EnvironmentExtensions.IsWindows10BuildOrNewer(17763))
        {
            return;
        }

        IntPtr handle = new WindowInteropHelper(window).Handle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        DwmWindowAttribute attribute = EnvironmentExtensions.IsWindows10BuildOrNewer(18985)
            ? DwmWindowAttribute.UseImmersiveDarkMode
            : DwmWindowAttribute.UseImmersiveDarkModeBefore20H1;

        if (NativeMethods.DwmGetWindowAttribute(handle, attribute, out bool prevUseImmersiveDarkMode, sizeof(int)) != 0)
        {
            return;
        }

        if (prevUseImmersiveDarkMode == useImmersiveDarkMode)
        {
            return;
        }

        if (NativeMethods.DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) != 0)
        {
            return;
        }

        // This hack appears to be the only way to get the non-client area of the window to redraw correctly.
        if (window is { IsVisible: true, IsFullScreen: false })
        {
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                double width = window.Width;
                window.Width += 1;
                window.Width = width;
            }
        }
    }

    /// <summary>
    /// Returns a new <see cref="Rect"/> with the merged properties of <paramref name="rect"/> and <paramref
    /// name="otherRect"/>. Each property in the returned <see cref="Rect"/> is set from <paramref name="otherRect"/>,
    /// except where that property is <see cref="double.PositiveInfinity"/>, <see cref="double.NegativeInfinity"/>,
    /// or <see cref="double.NaN"/>, in which case the property is set from <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">A <see cref="Rect"/>.</param>
    /// <param name="otherRect">Another <see cref="Rect"/>.</param>
    /// <returns>A new <see cref="Rect"/> with the merged properties of <paramref name="rect"/> and <paramref
    /// name="otherRect"/>.</returns>
    public static Rect Merge(this Rect rect, Rect otherRect)
    {
        return new(
            IsValidDouble(otherRect.X) ? otherRect.X : rect.X,
            IsValidDouble(otherRect.Y) ? otherRect.Y : rect.Y,
            IsValidDouble(otherRect.Width) ? otherRect.Width : rect.Width,
            IsValidDouble(otherRect.Height) ? otherRect.Height : rect.Height);
    }

    /// <summary>
    /// Offsets a <see cref="WindowSize"/> slightly from its current position if <see
    /// cref="Properties.Settings.CascadeWindows"/> is <see langword="true"/>; otherwise, returns the <see cref="WindowSize"/>
    /// unchanged.
    /// </summary>
    /// <param name="windowSize">A <see cref="WindowSize"/>.</param>
    /// <returns>The offset <see cref="WindowSize"/>.</returns>
    public static WindowSize Offset(this WindowSize windowSize)
    {
        return Properties.Settings.Default.CascadeWindows
            ? new(
                windowSize.RestoreBounds.Offset(),
                windowSize.WindowState,
                windowSize.RestoreWindowState,
                windowSize.IsFullScreen)
            : windowSize;
    }

    /// <param name="window">A window.</param>
    /// <typeparam name="T">The type of the window.</typeparam>
    extension<T>(T window) where T : Window, IRestorableWindow
    {
        /// <summary>
        /// Restores the size, position, and state of a window from its persisted size, position, and state.
        /// </summary>
        /// <param name="options">Options for performing the restore. (Optional.)</param>
        public void RestoreFromSettings(RestoreOptions options = RestoreOptions.None)
        {
            WindowSize windowSize = window.PersistedSize;
            window.Restore(windowSize, options);
        }

        /// <summary>
        /// Restores the size, position, and state of a window from another window of the same type.
        /// </summary>
        /// <remarks>
        /// If <see cref="Properties.Settings.CascadeWindows"/> is <see langword="true"/>, this method restores the size,
        /// position, and state from the latest created visible window of the same type instead, if any, and offsets
        /// the window position slightly so that the windows do not overlap.
        /// </remarks>
        /// <param name="otherWindow">The window from which to copy the size, position, and state.</param>
        /// <param name="options">Options for performing the restore. (Optional.)</param>
        public void RestoreFromWindow(T otherWindow, RestoreOptions options = RestoreOptions.None)
        {
            WindowSize? windowSize = Properties.Settings.Default.CascadeWindows
                ? WindowSize.FromSiblingOfWindow(window) ?? WindowSize.FromWindow(otherWindow)
                : WindowSize.FromWindow(otherWindow);

            window.Restore(windowSize?.Offset(), options);
        }

        /// <summary>
        /// Restores the size, position, and state of a window from the latest created other visible window of the
        /// same type, or from the app settings if there is no other visible window of the same type.
        /// </summary>
        /// <remarks>
        /// This method offsets the window position slightly so that the two windows do not overlap if <see
        /// cref="Properties.Settings.CascadeWindows"/> is <see langword="true"/>.
        /// </remarks>
        /// <param name="options">Options for performing the restore. (Optional.)</param>
        public void RestoreFromSibling(RestoreOptions options = RestoreOptions.None)
        {
            WindowSize? windowSize = WindowSize.FromSiblingOfWindow(window);
            if (windowSize is not null)
            {
                window.Restore(windowSize.Offset(), options);
            }
            else
            {
                window.RestoreFromSettings(options);
            }
        }
    }

    /// <summary>
    /// Restores the size, position, and state of a window.
    /// </summary>
    /// <typeparam name="T">The type of the window.</typeparam>
    /// <param name="window">A window.</param>
    /// <param name="windowSize">The size, position, and state to restore.</param>
    /// <param name="options">Options for performing the restore. (Optional.)</param>
    public static void Restore<T>(this T? window, WindowSize? windowSize, RestoreOptions options = RestoreOptions.None)
        where T : Window, IRestorableWindow
    {
        if (window is null || windowSize is null)
        {
            return;
        }

        // Restore size and position
        window.RestoreBounds(windowSize.RestoreBounds);

        // Restore state
        if (windowSize.WindowState == WindowState.Minimized && !options.HasFlag(RestoreOptions.AllowMinimized))
        {
            window.RestoreState(
                windowSize.RestoreWindowState,
                windowSize.RestoreWindowState,
                windowSize.IsFullScreen);
        }
        else
        {
            window.RestoreState(
                windowSize.WindowState,
                windowSize.RestoreWindowState,
                windowSize.IsFullScreen);
        }

        // If the window is restored to a size or position that is not on the screen, center the window
        if (windowSize.RestoreBounds.IsEmpty || (!Properties.Settings.Default.AllowOffScreen && !window.IsOnScreen()))
        {
            window.CenterOnScreen();
        }
    }

    /// <summary>
    /// Restores the size and position of a window.
    /// </summary>
    /// <param name="window">A window.</param>
    /// <param name="restoreBounds">The size and location of the window before being either minimized or maximized.
    /// </param>
    private static void RestoreBounds(this Window window, Rect restoreBounds)
    {
        if (restoreBounds.HasLocation())
        {
            window.Left = restoreBounds.Left;
            window.Top = restoreBounds.Top;
        }

        if (restoreBounds.HasSize())
        {
            window.Width = restoreBounds.Width;
            window.Height = restoreBounds.Height;
        }
    }

    /// <summary>
    /// Restores the state of a window.
    /// </summary>
    /// <typeparam name="T">The type of the window.</typeparam>
    /// <param name="window">A window.</param>
    /// <param name="windowState">A value that indicates whether the window is restored, minimized, or maximized.
    /// </param>
    /// <param name="restoreWindowState">The window's <see cref="Window.WindowState"/> before the window was
    /// minimized.</param>
    /// <param name="isFullScreen">A value indicating whether the window is in full-screen mode.</param>
    /// <seealso cref="RestoreStateToNotVisibleWindow{T}"/>
    private static void RestoreState<T>(this T window, WindowState windowState, WindowState restoreWindowState, bool isFullScreen)
        where T : Window, IRestorableWindow
    {
        if (window.IsVisible)
        {
            RestoreStateToVisibleWindow(
                window,
                windowState,
                restoreWindowState,
                isFullScreen);
        }
        else
        {
            RestoreStateToNotVisibleWindow(
                window,
                windowState,
                restoreWindowState,
                isFullScreen);
        }
    }

    /// <summary>
    /// Restores the state of a window that is visible.
    /// </summary>
    /// <remarks>
    /// Setting the state to maximized or full-screen before the has loaded will maximize or full-screen the window
    /// on the primary display rather than the display where the window was originally maximized or full-screened,
    /// so we need to handle those cases differently.
    /// </remarks>
    /// <typeparam name="T">The type of the window.</typeparam>
    /// <param name="window">A window.</param>
    /// <param name="windowState">A value that indicates whether the window is restored, minimized, or maximized.
    /// </param>
    /// <param name="restoreWindowState">The window's <see cref="Window.WindowState"/> before the window was
    /// minimized.</param>
    /// <param name="isFullScreen">A value indicating whether the window is in full-screen mode.</param>
    /// <seealso cref="RestoreStateToNotVisibleWindow{T}"/>
    private static void RestoreStateToVisibleWindow<T>(T window, WindowState windowState, WindowState restoreWindowState, bool isFullScreen)
        where T : Window, IRestorableWindow
    {
        if (isFullScreen)
        {
            window.IsFullScreen = true;
            window.RestoreWindowState = restoreWindowState;
        }
        else
        {
            window.IsFullScreen = false;
            window.WindowState = windowState;
            window.RestoreWindowState = restoreWindowState;
        }
    }

    /// <summary>
    /// Restores the state of a window that is not visible.
    /// </summary>
    /// <remarks>
    /// Setting the state to maximized or full-screen before the has loaded will maximize or full-screen the window
    /// on the primary display rather than the display where the window was originally maximized or full-screened,
    /// so we need to handle those cases differently.
    /// </remarks>
    /// <typeparam name="T">The type of the window.</typeparam>
    /// <param name="window">A window.</param>
    /// <param name="windowState">A value that indicates whether the window is restored, minimized, or maximized.
    /// </param>
    /// <param name="restoreWindowState">The window's <see cref="Window.WindowState"/> before the window was
    /// minimized.</param>
    /// <param name="isFullScreen">A value indicating whether the window is in full-screen mode.</param>
    /// <seealso cref="RestoreStateToVisibleWindow{T}"/>
    private static void RestoreStateToNotVisibleWindow<T>(T window, WindowState windowState, WindowState restoreWindowState, bool isFullScreen)
        where T : Window, IRestorableWindow
    {
        // Remove old handlers (if any)
        window.Loaded -= FullScreenSenderWindow;
        window.Loaded -= MaximizeSenderWindow;

        if (isFullScreen)
        {
            // Handle this on window load
            window.Loaded += FullScreenSenderWindow;
            window.RestoreWindowState = restoreWindowState;
        }
        else if (windowState == WindowState.Maximized)
        {
            // Handle this on window load
            window.Loaded += MaximizeSenderWindow;
            window.RestoreWindowState = restoreWindowState;
        }
        else
        {
            // Handle this now
            window.IsFullScreen = false;
            window.WindowState = windowState;
            window.RestoreWindowState = restoreWindowState;
        }
    }

    /// <summary>
    /// Invoked when a window is laid out, rendered, and ready for interaction in order to restore it to a
    /// full-screen state.
    /// </summary>
    /// <param name="sender">A window.</param>
    /// <param name="e">The event data.</param>
    private static void FullScreenSenderWindow(object sender, RoutedEventArgs e)
    {
        Window window = (Window)sender;
        IRestorableWindow restorableWindow = (IRestorableWindow)sender;

        restorableWindow.IsFullScreen = true;
        window.Loaded -= FullScreenSenderWindow;
    }

    /// <summary>
    /// Invoked when a window is laid out, rendered, and ready for interaction in order to restore it to a
    /// maximized state.
    /// </summary>
    /// <param name="sender">A window.</param>
    /// <param name="e">The event data.</param>
    private static void MaximizeSenderWindow(object sender, RoutedEventArgs e)
    {
        Window window = (Window)sender;
        IRestorableWindow restorableWindow = (IRestorableWindow)sender;

        restorableWindow.IsFullScreen = false;
        window.WindowState = WindowState.Maximized;
        window.Loaded -= MaximizeSenderWindow;
    }

    /// <summary>
    /// Positions a <see cref="Rect"/> in the center of the screen. If the <see cref="Rect"/> is larger than the
    /// work area, the <see cref="Rect"/> is decreased to fit in the work area.
    /// </summary>
    /// <param name="rect">A <see cref="Rect"/>.</param>
    /// <returns>The centered <see cref="Rect"/>.</returns>
    private static Rect CenterOnScreen(this Rect rect)
    {
        Rect offsetRect = rect;

        if (offsetRect.HasSize())
        {
            offsetRect.Width = Math.Min(rect.Width, SystemParameters.WorkArea.Width);
            offsetRect.Height = Math.Min(rect.Height, SystemParameters.WorkArea.Height);
            offsetRect.X = (SystemParameters.WorkArea.Width - offsetRect.Width) / 2 + SystemParameters.WorkArea.Left;
            offsetRect.Y = (SystemParameters.WorkArea.Height - offsetRect.Height) / 2 + SystemParameters.WorkArea.Top;
        }

        return offsetRect;
    }

    /// <summary>
    /// Sets the maximized position and size in the <c>WM_GETMINMAXINFO</c> message data to the work area of the
    /// monitor nearest to the window, so the maximized window does not overlap the taskbar.
    /// </summary>
    /// <param name="hwnd">A handle to the window.</param>
    /// <param name="lParam">A pointer to the <see cref="MINMAXINFO"/> structure of the <c>WM_GETMINMAXINFO</c>
    /// message.</param>
    /// <param name="horizontalOnly">A value indicating whether the window is maximized horizontally only, keeping its
    /// vertical position and height.</param>
    /// <returns><see langword="true"/> if the maximized position and size are set, or <see langword="false"/>
    /// otherwise.</returns>
    internal static bool SetMaximizedBoundsToWorkArea(IntPtr hwnd, IntPtr lParam, bool horizontalOnly = false)
    {
        IntPtr hMonitor = NativeMethods.MonitorFromWindow(hwnd, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);
        if (hMonitor == IntPtr.Zero)
        {
            return false;
        }

        MONITORINFO monitorInfo = new()
        {
            cbSize = Marshal.SizeOf(typeof(MONITORINFO))
        };
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            return false;
        }

        RECT rcWork = monitorInfo.rcWork;
        RECT rcMonitor = monitorInfo.rcMonitor;

        MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

        if (horizontalOnly)
        {
            POINT clientBottomRight = new();

            if (!NativeMethods.GetWindowRect(hwnd, out RECT rcWindow) ||
                !NativeMethods.GetClientRect(hwnd, out RECT rcClient))
            {
                return false;
            }

            clientBottomRight.x = rcClient.right;
            clientBottomRight.y = rcClient.bottom;

            if (!NativeMethods.ClientToScreen(hwnd, ref clientBottomRight))
            {
                return false;
            }

            // The resize frame is hidden outside the work area when the window is maximized. The default maximized
            // position is not used for the frame size, as it is replaced by the previous maximized position.
            int frameX = rcWindow.right - clientBottomRight.x;
            int frameY = rcWindow.bottom - clientBottomRight.y;

            int left = rcWork.left - frameX;
            int width = rcWork.right - rcWork.left + 2 * frameX;
            int height = rcWindow.bottom - rcWindow.top;

            // The window keeps its vertical position, moved into the work area if needed. The window is maximized
            // already if it has the maximized bounds, as it is reported as maximized before it is resized.
            bool maximized = NativeMethods.IsZoomed(hwnd) && rcWindow.left == left && rcWindow.right - rcWindow.left == width;
            int top = maximized ? rcWindow.top + frameY : rcWindow.top;
            top = Math.Max(rcWork.top, Math.Min(top, rcWork.bottom - height + 2 * frameY));

            mmi.ptMaxPosition.x = left - rcMonitor.left;
            mmi.ptMaxPosition.y = top - frameY - rcMonitor.top;
            mmi.ptMaxSize.x = width;
            mmi.ptMaxSize.y = height;
        }
        else
        {
            mmi.ptMaxPosition.x = Math.Abs(rcWork.left - rcMonitor.left);
            mmi.ptMaxPosition.y = Math.Abs(rcWork.top - rcMonitor.top);
            mmi.ptMaxSize.x = Math.Abs(rcWork.right - rcWork.left);
            mmi.ptMaxSize.y = Math.Abs(rcWork.bottom - rcWork.top);
        }

        Marshal.StructureToPtr(mmi, lParam, true);

        return true;
    }

    /// <summary>
    /// Returns the bounds of a window and its visible bounds, which do not include the invisible resize borders.
    /// </summary>
    /// <param name="hwnd">A handle to the window.</param>
    /// <param name="bounds">The bounds of the window, in screen coordinates.</param>
    /// <param name="visibleBounds">The visible bounds of the window, in screen coordinates.</param>
    /// <returns><see langword="true"/> if the bounds are returned, or <see langword="false"/> otherwise.</returns>
    private static bool TryGetBounds(IntPtr hwnd, out RECT bounds, out RECT visibleBounds)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out bounds))
        {
            visibleBounds = default;
            return false;
        }

        if (NativeMethods.DwmGetWindowAttribute(hwnd, DwmWindowAttribute.ExtendedFrameBounds, out visibleBounds, Marshal.SizeOf(typeof(RECT))) != 0)
        {
            visibleBounds = bounds;
        }

        return true;
    }

    /// <summary>
    /// Returns the work area of the monitor nearest to a window.
    /// </summary>
    /// <param name="hwnd">A handle to the window.</param>
    /// <param name="workArea">The work area, in screen coordinates.</param>
    /// <returns><see langword="true"/> if the work area is returned, or <see langword="false"/> otherwise.</returns>
    private static bool TryGetWorkArea(IntPtr hwnd, out RECT workArea)
    {
        MONITORINFO monitorInfo = new()
        {
            cbSize = Marshal.SizeOf(typeof(MONITORINFO))
        };

        IntPtr hMonitor = NativeMethods.MonitorFromWindow(hwnd, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);
        bool result = hMonitor != IntPtr.Zero && NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo);

        workArea = monitorInfo.rcWork;
        return result;
    }

    /// <param name="window">A window.</param>
    extension(Window window)
    {
        /// <summary>
        /// Positions a window in the center of the screen. If the window is larger than the work area, the window size
        /// is decreased to fit in the work area.
        /// </summary>
        public void CenterOnScreen()
        {
            Rect windowRect = window.GetBoundsForNormalState();
            Rect centeredRect = windowRect.CenterOnScreen();

            window.RestoreBounds(centeredRect);
        }

        /// <summary>
        /// Positions a window next to the <paramref name="target"/> window, on the first side where the window fits in
        /// the work area of the <paramref name="target"/> window monitor. The right, left, bottom, and top sides are
        /// tried in this order. If the window does not fit next to the <paramref name="target"/> window, or the
        /// <paramref name="target"/> window is not shown, the window is centered in the work area.
        /// </summary>
        /// <param name="target">The window to position the window next to.</param>
        /// <returns><see langword="true"/> if the window is positioned, or <see langword="false"/> otherwise.</returns>
        public bool PositionNextTo(Window target)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            IntPtr targetHandle = new WindowInteropHelper(target).Handle;

            bool hasTarget = targetHandle != IntPtr.Zero && target.IsVisible && target.WindowState != WindowState.Minimized;
            RECT targetBounds = default;

            if (handle == IntPtr.Zero ||
                !TryGetBounds(handle, out RECT bounds, out RECT visibleBounds) ||
                (hasTarget && !TryGetBounds(targetHandle, out _, out targetBounds)) ||
                !TryGetWorkArea(hasTarget ? targetHandle : handle, out RECT workArea))
            {
                return false;
            }

            // The window is rescaled when it is moved to the monitor with a different DPI.
            double dpiScale = VisualTreeHelper.GetDpi(hasTarget ? target : window).DpiScaleX;
            double scale = dpiScale / VisualTreeHelper.GetDpi(window).DpiScaleX;

            int width = (int)Math.Round((visibleBounds.right - visibleBounds.left) * scale);
            int height = (int)Math.Round((visibleBounds.bottom - visibleBounds.top) * scale);
            int insetLeft = (int)Math.Round((visibleBounds.left - bounds.left) * scale);
            int insetTop = (int)Math.Round((visibleBounds.top - bounds.top) * scale);
            int gap = (int)Math.Round(8 * dpiScale);

            int alignedTop = Clamp(targetBounds.top, workArea.top, workArea.bottom - height);
            int alignedLeft = Clamp(targetBounds.left, workArea.left, workArea.right - width);

            (int X, int Y)[] positions = hasTarget ? [
                (targetBounds.right + gap, alignedTop),
                (targetBounds.left - gap - width, alignedTop),
                (alignedLeft, targetBounds.bottom + gap),
                (alignedLeft, targetBounds.top - gap - height)
            ] : [];

            (int X, int Y) position = positions
                .Where(p => p.X >= workArea.left && p.Y >= workArea.top && p.X + width <= workArea.right && p.Y + height <= workArea.bottom)
                .DefaultIfEmpty((
                    Math.Max(workArea.left, workArea.left + (workArea.right - workArea.left - width) / 2),
                    Math.Max(workArea.top, workArea.top + (workArea.bottom - workArea.top - height) / 2)))
                .First();

            return NativeMethods.SetWindowPos(handle, IntPtr.Zero, position.X - insetLeft, position.Y - insetTop, 0, 0,
                SetWindowPosFlags.NoSize | SetWindowPosFlags.NoZOrder | SetWindowPosFlags.NoActivate);

            static int Clamp(int value, int min, int max) =>
                Math.Max(min, Math.Min(value, max));
        }

        /// <summary>
        /// Returns the bounds of a window, or its restore bounds if it is minimized or maximized.
        /// </summary>
        /// <returns>The bounds of a window, or its restore bounds if it is minimized or maximized.</returns>
        private Rect GetBoundsForNormalState()
        {
            if (window.WindowState != WindowState.Normal && window.RestoreBounds.HasSizeAndLocation())
            {
                return window.RestoreBounds;
            }

            return new(window.Left, window.Top, window.Width, window.Height);
        }

        /// <summary>
        /// Force window layout update.
        /// </summary>
        public void ForceUpdateLayout()
        {
            try
            {
                window.Dispatcher.Invoke(EmptyAction, DispatcherPriority.Loaded);
            }
            catch(InvalidOperationException)
            {
                // Ignore.
            }
        }

        /// <summary>
        /// Open window context menu.
        /// </summary>
        public void OpenContextMenu()
        {
            try
            {
                if (window.ContextMenu is null)
                {
                    return;
                }

                var contextMenuEventArgs = (ContextMenuEventArgs)Activator.CreateInstance(
                    typeof(ContextMenuEventArgs),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [window, true],
                    null);

                contextMenuEventArgs.RoutedEvent = FrameworkElement.ContextMenuOpeningEvent;
                window.RaiseEvent(contextMenuEventArgs);

                window.ContextMenu.IsOpen = !contextMenuEventArgs.Handled;
            }
            catch
            {
                // Fallback.
                System.Windows.Forms.SendKeys.SendWait("+{F10}");
            }
        }

        public void MoveToCurrentVirtualDesktop() =>
            VirtualDesktopManager.Instance.MoveToCurrentVirtualDesktop(window);

        /// <summary>
        /// Shows the help button in the window title bar. The help button, as well as the <c>F1</c> key, opens the
        /// specified help page.
        /// </summary>
        /// <remarks>
        /// Windows does not show the help button together with the minimize and maximize buttons, so these buttons
        /// are removed. The window must have a handle, so this method is to be invoked when the <see
        /// cref="Window.SourceInitialized"/> event is raised or later, before the <see
        /// cref="RemoveDisabledSystemMenuItems"/> method.
        /// </remarks>
        /// <param name="helpUri">The help page <see cref="Uri"/>.</param>
        public void ShowHelpButton(Uri helpUri)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (HwndSource.FromHwnd(handle) is not { } hwndSource)
            {
                return;
            }

            // WPF rewrites the window styles, e.g. when the window resize mode or topmost state changes.
            hwndSource.AddHook(HelpButtonWindowProc);

            ShowHelpButton(handle);

            IntPtr HelpButtonWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                switch (msg)
                {
                    case NativeMethods.WmStyleChanging:
                        STYLESTRUCT styleStruct = Marshal.PtrToStructure<STYLESTRUCT>(lParam);

                        if (wParam == (IntPtr)NativeMethods.GwlStyle)
                        {
                            styleStruct.styleNew &= ~MinimizeMaximizeBoxes;
                        }
                        else if (wParam == (IntPtr)NativeMethods.GwlExStyle)
                        {
                            styleStruct.styleNew |= NativeMethods.WsExContextHelp;
                        }
                        else
                        {
                            break;
                        }

                        Marshal.StructureToPtr(styleStruct, lParam, false);
                        break;

                    case NativeMethods.WmSysCommand when IsContextHelpCommand(wParam):
                    case NativeMethods.WmHelp:
                        NavigateToHelp(helpUri);
                        handled = true;
                        break;
                }

                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Removes the items that are always disabled from the window system menu, as well as the separators if no
        /// more than two other items are left.
        /// </summary>
        /// <remarks>
        /// The system grays out the "Restore", "Size", "Minimize" and "Maximize" items of the system menu of a window
        /// which cannot be resized, minimized or maximized, so these items are removed depending on the window styles.
        /// The separator preceding the "Close" item is removed too if only the "Move" and "Close" items are left. The
        /// window must have a handle, so this method is to be invoked when the <see cref="Window.SourceInitialized"/>
        /// event is raised or later.
        /// </remarks>
        public void RemoveDisabledSystemMenuItems()
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            IntPtr systemMenu = handle != IntPtr.Zero ? GetSystemMenu(handle, false) : IntPtr.Zero;

            if (systemMenu == IntPtr.Zero)
            {
                return;
            }

            uint[] disabledCommands = [..GetDisabledCommands(NativeMethods.GetWindowLong(handle, NativeMethods.GwlStyle))];

            if (disabledCommands.Length == 0)
            {
                return;
            }

            foreach (uint command in disabledCommands)
            {
                DeleteMenu(systemMenu, command, MfByCommand);
            }

            RemoveMenuSeparators(systemMenu);

            static IEnumerable<uint> GetDisabledCommands(int style)
            {
                bool noMinimizeBox = (style & NativeMethods.WsMinimizeBox) == 0;
                bool noMaximizeBox = (style & NativeMethods.WsMaximizeBox) == 0;

                if (noMinimizeBox && noMaximizeBox)
                {
                    yield return ScRestore;
                }

                if ((style & NativeMethods.WsThickFrame) == 0)
                {
                    yield return ScSize;
                }

                if (noMinimizeBox)
                {
                    yield return ScMinimize;
                }

                if (noMaximizeBox)
                {
                    yield return ScMaximize;
                }
            }
        }
    }

    /// <param name="rect">A <see cref="Rect"/>.</param>
    extension(Rect rect)
    {
        /// <summary>
        /// Returns the <see cref="Point"/> at the center of a <see cref="Rect"/>.
        /// </summary>
        /// <returns>The <see cref="Point"/> at the center of a <see cref="Rect"/>.</returns>
        private Point GetCenter()
        {
            if (rect.HasSizeAndLocation())
            {
                return new(
                    (int)(rect.X + rect.Width / 2),
                    (int)(rect.Y + rect.Height / 2));
            }

            return rect.Location;
        }

        /// <summary>
        /// Returns a value indicating whether the <see cref="Rect"/> has a valid location.
        /// </summary>
        /// <returns>A value indicating whether the <see cref="Rect"/> has a valid location.</returns>
        private bool HasLocation()
        {
            return IsValidDouble(rect.X) && IsValidDouble(rect.Y);
        }

        /// <summary>
        /// Returns a value indicating whether the <see cref="Rect"/> has a valid size.
        /// </summary>
        /// <returns>A value indicating whether the <see cref="Rect"/> has a valid size.</returns>
        private bool HasSize()
        {
            return IsValidDouble(rect.Width) && IsValidDouble(rect.Height);
        }

        /// <summary>
        /// Returns a value indicating whether the <see cref="Rect"/> has a valid size and location.
        /// </summary>
        /// <returns>A value indicating whether the <see cref="Rect"/> has a valid size and location.</returns>
        private bool HasSizeAndLocation()
        {
            return rect.HasSize() && rect.HasLocation();
        }

        /// <summary>
        /// Returns a value indicating whether the size and position of the <see cref="Rect"/> are such that the center
        /// of the <see cref="Rect"/> is visible on the screen.
        /// </summary>
        /// <returns>A value indicating whether the size and position of the <see cref="Rect"/> are such that the center
        /// of the <see cref="Rect"/> is visible on the screen.</returns>
        private bool IsOnScreen()
        {
            if (rect.HasLocation())
            {
                return VirtualScreenRect.Contains(rect.GetCenter());
            }

            return true;
        }

        /// <summary>
        /// Returns the bounds of the screen area the <see cref="Rect"/> is on: the work area of the primary screen if
        /// the center of the <see cref="Rect"/> is within it; otherwise, the virtual screen.
        /// </summary>
        /// <returns>The bounds of the screen area the <see cref="Rect"/> is on.</returns>
        private Rect GetScreenRect()
        {
            Rect workAreaRect = SystemParameters.WorkArea;
            return workAreaRect.Contains(rect.GetCenter()) ? workAreaRect : VirtualScreenRect;
        }
    }

    /// <summary>
    /// Gets the bounds of the virtual screen.
    /// </summary>
    private static Rect VirtualScreenRect => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);

    /// <summary>
    /// Returns a value indicating whether the size and position of the window are such that the center of the
    /// window is visible on the screen when in its normal state.
    /// </summary>
    /// <param name="window">A window.</param>
    /// <returns>A value indicating whether the size and position of the window are such that the center of the window is visible on the screen when in its normal state.</returns>
    private static bool IsOnScreen(this Window window)
    {
        Rect windowRect = window.GetBoundsForNormalState();
        return windowRect.IsOnScreen();
    }

    /// <summary>
    /// Returns a value indicating whether a <see cref="double"/> is a value other than <see
    /// cref="double.PositiveInfinity"/>, <see cref="double.NegativeInfinity"/>, and <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="value">A <see cref="double"/>.</param>
    /// <returns>A value indicating whether a <see cref="double"/> is a value other than <see cref="double.PositiveInfinity"/>, <see cref="double.NegativeInfinity"/>, and <see cref="double.NaN"/>.</returns>
    private static bool IsValidDouble(double value)
    {
        return !double.IsInfinity(value) && !double.IsNaN(value);
    }

    /// <summary>
    /// Offsets a <see cref="Rect"/> from its current position by the system cascade step, so that the title bar of
    /// the window behind remains visible. When the offset <see cref="Rect"/> does not fit on the screen, its position
    /// wraps to the top of the next column, and then to the top-left corner of the screen.
    /// </summary>
    /// <param name="rect">A <see cref="Rect"/>.</param>
    /// <returns>The offset <see cref="Rect"/>.</returns>
    private static Rect Offset(this Rect rect)
    {
        if (!rect.HasSizeAndLocation())
        {
            return rect;
        }

        // The window title bar height including the frame, as used by Windows to cascade windows.
        double offsetAmount = SystemParameters.WindowNonClientFrameThickness.Top;

        Rect screenRect = rect.GetScreenRect();
        Rect offsetRect = rect;

        // Move the rect down and to the right
        offsetRect.X += offsetAmount;
        offsetRect.Y += offsetAmount;

        if (screenRect.Contains(offsetRect))
        {
            return offsetRect;
        }

        // Wrap the rect to the top of the next column
        offsetRect.X += offsetAmount - Math.Floor((offsetRect.Y - screenRect.Top) / offsetAmount) * offsetAmount;
        offsetRect.Y = screenRect.Top;

        if (screenRect.Contains(offsetRect))
        {
            return offsetRect;
        }

        // Wrap the rect to the top-left corner of the screen
        offsetRect.X = screenRect.Left;
        offsetRect.Y = screenRect.Top;

        if (screenRect.Contains(offsetRect))
        {
            return offsetRect;
        }

        // Center the rect as a fallback
        return rect.CenterOnScreen();
    }

    private static readonly Action EmptyAction = delegate { };

    private static TaskDialog? _taskDialogInstance;
    private static Window? _taskDialogOwner;

    /// <summary>
    /// Brings the open task dialog, if any, together with its owner window to the foreground.
    /// </summary>
    /// <returns><see langword="true"/> if a task dialog is open, or <see langword="false"/> otherwise.</returns>
    public static bool TryBringTaskDialogToFront()
    {
        if (_taskDialogInstance is null)
        {
            return false;
        }

        try
        {
            switch (_taskDialogOwner)
            {
                case TimerWindow timerWindow:
                    timerWindow.BringToFrontAndActivate(activate: false);
                    break;
                case { WindowState: WindowState.Minimized } owner:
                    owner.WindowState = WindowState.Normal;
                    break;
            }
        }
        catch (InvalidOperationException)
        {
            // This happens if the owner window is closing.
        }

        NativeMethods.SetForegroundWindow(_taskDialogInstance.Handle);

        return true;
    }

    public static MessageBoxResult ShowTaskDialog(this Window? window, string instruction, string yesText, string? noText = null, TaskDialogCheckBox? taskDialogCheckBox = null, bool allowCancel = true, Uri? helpUri = null)
    {
        if (TryBringTaskDialogToFront())
        {
            return MessageBoxResult.Cancel;
        }

        TaskDialogPage dialogPage = new()
        {
            Title = Properties.Resources.MessageBoxTitle,
            Text = Properties.Resources.WhatWouldYouDoTaskDialogText,
            Instruction = instruction,
            Icon = TaskDialogStandardIcon.Warning,
            CustomButtonStyle = TaskDialogCustomButtonStyle.CommandLinks,
            AllowCancel = allowCancel,
            CheckBox = taskDialogCheckBox
        };

        TaskDialogCustomButton yesButton = dialogPage.CustomButtons.Add(yesText);
        TaskDialogCustomButton? noButton = string.IsNullOrWhiteSpace(noText) ? null : dialogPage.CustomButtons.Add(noText);

        if (allowCancel)
        {
            dialogPage.CustomButtons.Add(Properties.Resources.BackTaskDialogCommand);
        }

        bool hasOwner = window is not null;

        TaskDialog dialog = new(dialogPage)
        {
            StartupLocation = hasOwner ? TaskDialogStartupLocation.CenterParent : TaskDialogStartupLocation.CenterScreen
        };

        dialog.Shown += OnShown;

        if (helpUri is not null)
        {
            // The Help event is raised when F1 is pressed or the help button is clicked.
            dialogPage.Help += (_, _) => NavigateToHelp(helpUri);
            dialog.Opened += (_, _) => ShowHelpButton(dialog.Handle);
        }

        IntPtr handle = hasOwner ? new WindowInteropHelper(window!).Handle : IntPtr.Zero;
        TaskDialogButton result = dialog.Show(handle);

        MessageBoxResult messageBoxResult = ReferenceEquals(result, yesButton)
            ? MessageBoxResult.Yes
            : ReferenceEquals(result, noButton)
                ? MessageBoxResult.No
                : MessageBoxResult.Cancel;

        _taskDialogInstance = null;
        _taskDialogOwner = null;

        return messageBoxResult;

        void OnShown(object sender, EventArgs e)
        {
            dialog.Shown -= OnShown;
            _taskDialogInstance = dialog;
            _taskDialogOwner = window;
        }
    }

    public static void Clean()
    {
        _taskDialogInstance?.Close();
        _taskDialogInstance = null;
        _taskDialogOwner = null;
    }

    extension(Visual visual)
    {
        public double GetMinTrackWidth() =>
            Math.Round(
                GetSystemMetrics(34 /* SM_CXMINTRACK */) /
                (PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice.M11 ?? 1));

        public double GetMinTrackHeight() =>
            Math.Round(1+
                       GetSystemMetrics(35 /* SM_CYMINTRACK */) /
                       (PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice.M22 ?? 1));
    }

    public static bool IsTextBoxView(this object o) =>
        StringComparer.Ordinal.Equals(o.GetType().FullName, "System.Windows.Controls.TextBoxView");

    /// <summary>
    /// Shows the help button in the title bar of the specified window, removing the minimize and maximize buttons
    /// as Windows does not show the help button together with them.
    /// </summary>
    /// <param name="handle">A handle to the window.</param>
    private static void ShowHelpButton(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int style = NativeMethods.GetWindowLong(handle, NativeMethods.GwlStyle);

        if ((style & MinimizeMaximizeBoxes) != 0)
        {
            NativeMethods.SetWindowLong(handle, NativeMethods.GwlStyle, style & ~MinimizeMaximizeBoxes);
        }

        int exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, exStyle | NativeMethods.WsExContextHelp);

        NativeMethods.SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
            SetWindowPosFlags.NoSize | SetWindowPosFlags.NoMove | SetWindowPosFlags.NoZOrder |
            SetWindowPosFlags.NoActivate | SetWindowPosFlags.FrameChanged);
    }

    /// <summary>
    /// Returns a value indicating whether the <see cref="NativeMethods.WmSysCommand"/> message parameter is the
    /// command sent when the help button in the title bar is clicked.
    /// </summary>
    /// <param name="wParam">The <see cref="NativeMethods.WmSysCommand"/> message wParam value.</param>
    /// <returns><see langword="true"/> if the help button in the title bar is clicked, or <see langword="false"/>
    /// otherwise.</returns>
    private static bool IsContextHelpCommand(IntPtr wParam) =>
        (wParam.ToInt64() & NativeMethods.ScCommandMask) == NativeMethods.ScContextHelp;

    /// <summary>
    /// Opens the help page in the default web browser.
    /// </summary>
    /// <param name="helpUri">The help page <see cref="Uri"/>.</param>
    private static void NavigateToHelp(Uri helpUri)
    {
        try
        {
            helpUri.Navigate();
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            // Opening the help page is not critical.
        }
    }

    /// <summary>
    /// The <see cref="NativeMethods.WsMinimizeBox"/> and <see cref="NativeMethods.WsMaximizeBox"/> styles.
    /// </summary>
    private const int MinimizeMaximizeBoxes = NativeMethods.WsMinimizeBox | NativeMethods.WsMaximizeBox;

    /// <summary>
    /// Removes the separators from the specified menu if it has no more than two other items.
    /// </summary>
    /// <param name="menu">A handle to the menu.</param>
    private static void RemoveMenuSeparators(IntPtr menu)
    {
        const int maxItemCountWithoutSeparators = 2;

        int itemCount = GetMenuItemCount(menu);

        int[] separatorPositions =
        [
            ..Enumerable.Range(0, Math.Max(itemCount, 0)).Where(position =>
            {
                uint state = GetMenuState(menu, (uint)position, MfByPosition);
                return state != uint.MaxValue && (state & MfSeparator) != 0;
            })
        ];

        if (separatorPositions.Length == 0 || itemCount - separatorPositions.Length > maxItemCountWithoutSeparators)
        {
            return;
        }

        // Deleting an item shifts the positions of the items following it, so the separators are deleted backwards.
        foreach (int position in separatorPositions.AsEnumerable().Reverse())
        {
            DeleteMenu(menu, (uint)position, MfByPosition);
        }
    }

    /// <summary>
    /// The <c>MF_BYCOMMAND</c> flag indicating that a menu item is identified by its command.
    /// </summary>
    private const uint MfByCommand = 0x00000000;

    /// <summary>
    /// The <c>MF_BYPOSITION</c> flag indicating that a menu item is identified by its zero-based position.
    /// </summary>
    private const uint MfByPosition = 0x00000400;

    /// <summary>
    /// The <c>MF_SEPARATOR</c> flag indicating that a menu item is a separator.
    /// </summary>
    private const uint MfSeparator = 0x00000800;

    /// <summary>
    /// The <c>SC_SIZE</c> command of the "Size" system menu item.
    /// </summary>
    private const uint ScSize = 0xF000;

    /// <summary>
    /// The <c>SC_MINIMIZE</c> command of the "Minimize" system menu item.
    /// </summary>
    private const uint ScMinimize = 0xF020;

    /// <summary>
    /// The <c>SC_MAXIMIZE</c> command of the "Maximize" system menu item.
    /// </summary>
    private const uint ScMaximize = 0xF030;

    /// <summary>
    /// The <c>SC_RESTORE</c> command of the "Restore" system menu item.
    /// </summary>
    private const uint ScRestore = 0xF120;

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetMenuItemCount(IntPtr hMenu);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint GetMenuState(IntPtr hMenu, uint uId, uint uFlags);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);
}