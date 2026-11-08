// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Hourglass;

/// <summary>
/// Flags used to specify window attributes for Desktop Window Manager (DWM) non-client rendering.
/// </summary>
internal enum DWMWINDOWATTRIBUTE : uint
{
    /// <summary>
    /// The extended frame bounds of a window, which are the visible bounds without the invisible resize borders.
    /// </summary>
    DWMWA_EXTENDED_FRAME_BOUNDS = 9,

    /// <summary>
    /// Enabled immersive dark mode for a Window. Has no effect before Windows 10 Build 17763. After Windows 10
    /// Build 18985, use <see cref="DWMWA_USE_IMMERSIVE_DARK_MODE"/>.
    /// </summary>
    DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,

    /// <summary>
    /// Enabled immersive dark mode for a Window. Has no effect before Windows 10 Build 18985.
    /// </summary>
    DWMWA_USE_IMMERSIVE_DARK_MODE = 20
}

/// <summary>
/// Specifies the type of DirectWrite factory object.
/// </summary>
internal enum DWRITE_FACTORY_TYPE
{
    /// <summary>
    /// A shared factory allows for re-use of cached font data across multiple in-process components. Such factories
    /// also take advantage of cross-process font caching components for better performance.
    /// </summary>
    DWRITE_FACTORY_TYPE_SHARED,

    /// <summary>
    /// Objects created from the isolated factory do not interact with internal DirectWrite state from other
    /// components.
    /// </summary>
    DWRITE_FACTORY_TYPE_ISOLATED
}

/// <summary>
/// The thread's execution requirements.
/// </summary>
[Flags]
internal enum EXECUTION_STATE : uint
{
    /// <summary>
    /// Informs the system that the state being set should remain in effect until the next call that uses
    /// <see cref="ES_CONTINUOUS"/> and one of the other state flags is cleared.
    /// </summary>
    ES_CONTINUOUS = 0x80000000,

    /// <summary>
    /// <para>
    /// Forces the display to be on by resetting the display idle timer.
    /// </para><para>
    /// Windows 8: This flag can only keep a display turned on, it can't turn on a display that's currently
    /// off.
    /// </para>
    /// </summary>
    ES_DISPLAY_REQUIRED = 0x00000002,

    /// <summary>
    /// Forces the system to be in the working state by resetting the system idle timer.
    /// </summary>
    ES_SYSTEM_REQUIRED = 0x00000001
}

/// <summary>
/// The window sizing and positioning flags used by <see cref="NativeMethods.SetWindowPos"/>.
/// </summary>
[Flags]
internal enum SetWindowPosFlags : uint
{
    /// <summary>
    /// Retains the current size.
    /// </summary>
    SWP_NOSIZE = 0x0001,

    /// <summary>
    /// Retains the current position.
    /// </summary>
    SWP_NOMOVE = 0x0002,

    /// <summary>
    /// Retains the current Z order.
    /// </summary>
    SWP_NOZORDER = 0x0004,

    /// <summary>
    /// Does not activate the window.
    /// </summary>
    SWP_NOACTIVATE = 0x0010,

    /// <summary>
    /// Applies the new frame styles set using the <c>SetWindowLong</c> function.
    /// </summary>
    SWP_FRAMECHANGED = 0x0020,

    /// <summary>
    /// Does not change the owner window's position in the Z order.
    /// </summary>
    SWP_NOOWNERZORDER = 0x0200,

    /// <summary>
    /// Posts the request instead of waiting if the window belongs to another thread.
    /// </summary>
    SWP_ASYNCWINDOWPOS = 0x4000
}

/// <summary>
/// Specifies the window station, desktop, standard handles, and appearance of the main window for a process at
/// creation time.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct STARTUPINFO
{
    public int cb;
    public string? lpReserved;
    public string? lpDesktop;
    public string? lpTitle;
    public int dwX;
    public int dwY;
    public int dwXSize;
    public int dwYSize;
    public int dwXCountChars;
    public int dwYCountChars;
    public int dwFillAttribute;
    public int dwFlags;
    public short wShowWindow;
    public short cbReserved2;
    public IntPtr lpReserved2;
    public IntPtr hStdInput;
    public IntPtr hStdOutput;
    public IntPtr hStdError;
}

/// <summary>
/// Contains information about a newly created process and its primary thread.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PROCESS_INFORMATION
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
}

/// <summary>
/// Defines the x- and y-coordinates of a point.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int x;
    public int y;
}

/// <summary>
/// Defines a rectangle by the coordinates of its upper-left and lower-right corners.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

/// <summary>
/// Contains information about a window's maximized size and position and its minimum and maximum tracking size.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MINMAXINFO
{
    public POINT ptReserved;
    public POINT ptMaxSize;
    public POINT ptMaxPosition;
    public POINT ptMinTrackSize;
    public POINT ptMaxTrackSize;
}

/// <summary>
/// Contains the styles for a window, sent with the <see cref="NativeMethods.WM_STYLECHANGING"/> message.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct STYLESTRUCT
{
    public int styleOld;
    public int styleNew;
}

/// <summary>
/// Contains information about a display monitor.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
}

/// <summary>
/// Wrapper class for calling Win32 and other unmanaged APIs.
/// </summary>
internal static class NativeMethods
{
    /// <summary>
    /// The <c>GWL_STYLE</c> index of the window styles.
    /// </summary>
    public const int GWL_STYLE = -16;

    /// <summary>
    /// The <c>GWL_EXSTYLE</c> index of the extended window styles.
    /// </summary>
    public const int GWL_EXSTYLE = -20;

    /// <summary>
    /// The <c>WS_MAXIMIZEBOX</c> style of a window that has a maximize button.
    /// </summary>
    public const int WS_MAXIMIZEBOX = 0x00010000;

    /// <summary>
    /// The <c>WS_MINIMIZEBOX</c> style of a window that has a minimize button.
    /// </summary>
    public const int WS_MINIMIZEBOX = 0x00020000;

    /// <summary>
    /// The <c>WS_THICKFRAME</c> style of a window that has a sizing border.
    /// </summary>
    public const int WS_THICKFRAME = 0x00040000;

    /// <summary>
    /// The <c>WS_EX_CONTEXTHELP</c> extended style of a window that has a help button in the title bar. The help
    /// button is not shown if the window has the <see cref="WS_MAXIMIZEBOX"/> or <see cref="WS_MINIMIZEBOX"/> style.
    /// </summary>
    public const int WS_EX_CONTEXTHELP = 0x00000400;

    /// <summary>
    /// The message sent to a window when its size or position is about to change.
    /// </summary>
    public const int WM_GETMINMAXINFO = 0x0024;

    /// <summary>
    /// The <c>WM_HELP</c> message sent to a window when the user presses F1.
    /// </summary>
    public const int WM_HELP = 0x0053;

    /// <summary>
    /// The <c>WM_STYLECHANGING</c> message sent to a window when its styles are about to change.
    /// </summary>
    public const int WM_STYLECHANGING = 0x007C;

    /// <summary>
    /// The <c>WM_SYSCOMMAND</c> message sent to a window when the user chooses a command from the window menu or
    /// clicks a title bar button.
    /// </summary>
    public const int WM_SYSCOMMAND = 0x0112;

    /// <summary>
    /// The <c>SC_CONTEXTHELP</c> command sent when the help button in the title bar is clicked.
    /// </summary>
    public const int SC_CONTEXTHELP = 0xF180;

    /// <summary>
    /// Gets a value of Desktop Window Manager (DWM) non-client rendering attributes for a window.
    /// </summary>
    /// <param name="hwnd">The handle to the window for which the attribute value is to be retrieved.</param>
    /// <param name="dwAttribute">A flag describing which value to retrieve.</param>
    /// <param name="pvAttribute">A pointer to a value which, when this function returns successfully, receives the
    /// current value of the attribute. The type of the retrieved value depends on the value of the <paramref
    /// name="dwAttribute"/> parameter.</param>
    /// <param name="cbAttribute">The size, in bytes, of the attribute value being received via the <paramref
    /// name="pvAttribute"/> parameter. The type of the retrieved value, and therefore its size in bytes, depends
    /// on the value of the <paramref name="dwAttribute"/> parameter.</param>
    /// <returns>0 if the successful, or nonzero otherwise.</returns>
    [DllImport("dwmapi.dll", ExactSpelling = true)]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

    /// <inheritdoc cref="DwmGetWindowAttribute(IntPtr, DWMWINDOWATTRIBUTE, out bool, int)"/>
    [DllImport("dwmapi.dll", ExactSpelling = true)]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

    /// <summary>
    /// Sets the value of Desktop Window Manager (DWM) non-client rendering attributes for a window.
    /// </summary>
    /// <param name="hwnd">The handle to the window for which the attribute value is to be set.</param>
    /// <param name="dwAttribute">A flag describing which value to set.</param>
    /// <param name="pvAttribute">A pointer to an object containing the attribute value to set. The type of the
    /// value set depends on the value of the <paramref name="dwAttribute"/> parameter.</param>
    /// <param name="cbAttribute">The size, in bytes, of the attribute value being set via the <paramref
    /// name="pvAttribute"/> parameter. The type of the value set, and therefore its size in bytes, depends on the
    /// value of the <paramref name="dwAttribute"/> parameter.</param>
    /// <returns>0 if the successful, or nonzero otherwise.</returns>
    [DllImport("dwmapi.dll", ExactSpelling = true, PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref bool pvAttribute, int cbAttribute);

    /// <summary>
    /// Sets the specified timer to the inactive state.
    /// </summary>
    /// <param name="hTimer">A handle to the timer object.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern bool CancelWaitableTimer(IntPtr hTimer);

    /// <summary>
    /// Converts the client-area coordinates of a specified point to screen coordinates.
    /// </summary>
    /// <param name="hWnd">A handle to the window whose client area is used for the conversion.</param>
    /// <param name="lpPoint">The client coordinates to be converted, which receives the screen coordinates.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">A valid handle to an open object.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Creates a new process and its primary thread.
    /// </summary>
    /// <param name="lpApplicationName">The name of the module to be executed, or <see langword="null"/> to use the
    /// first white space-delimited token of the <paramref name="lpCommandLine"/>.</param>
    /// <param name="lpCommandLine">The command line to be executed. The buffer may be modified.</param>
    /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure for the process.</param>
    /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure for the thread.</param>
    /// <param name="bInheritHandles">A value indicating whether the inheritable handles are inherited.</param>
    /// <param name="dwCreationFlags">The flags that control the priority class and the creation of the process.
    /// </param>
    /// <param name="lpEnvironment">A pointer to the environment block, or <see cref="IntPtr.Zero"/> to use the
    /// environment of the calling process.</param>
    /// <param name="lpCurrentDirectory">The full path to the current directory for the process.</param>
    /// <param name="lpStartupInfo">The <see cref="STARTUPINFO"/> structure.</param>
    /// <param name="lpProcessInformation">Receives identification information about the new process.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateProcess(
        string? lpApplicationName,
        StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        [In] ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    /// <summary>
    /// Creates or opens a timer object.
    /// </summary>
    /// <param name="lpTimerAttributes">A pointer to a structure that specifies a security descriptor for the new
    /// timer object and determines whether child processes can inherit the returned handle.</param>
    /// <param name="bManualReset">If this parameter is <see langword="true"/>, the timer is a manual-reset notification timer.
    /// Otherwise, the timer is a synchronization timer.</param>
    /// <param name="lpTimerName">The name of the timer object.</param>
    /// <returns>A handle to the timer object if the call succeeds, or <see cref="IntPtr.Zero"/> otherwise.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string? lpTimerName);

    /// <summary>
    /// Creates a DirectWrite factory object that is used for subsequent creation of individual DirectWrite objects.
    /// </summary>
    /// <param name="factoryType">A value that specifies whether the factory object will be shared or isolated.
    /// </param>
    /// <param name="iid">A GUID value that identifies the DirectWrite factory interface.</param>
    /// <param name="factory">An address of a pointer to the newly created DirectWrite factory object.</param>
    [DllImport("dwrite.dll", ExactSpelling = true, PreserveSig = false)]
    public static extern void DWriteCreateFactory(DWRITE_FACTORY_TYPE factoryType, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object factory);

    /// <summary>
    /// Retrieves the coordinates of a window's client area.
    /// </summary>
    /// <param name="hWnd">A handle to the window whose client coordinates are to be retrieved.</param>
    /// <param name="lpRect">Receives the client coordinates. The left and top members are zero.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Retrieves information about a display monitor.
    /// </summary>
    /// <param name="hMonitor">A handle to the display monitor of interest.</param>
    /// <param name="lpmi">The <see cref="MONITORINFO"/> structure that receives information about the display
    /// monitor. The <see cref="MONITORINFO.cbSize"/> member must be set before calling the function.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// Retrieves information about the specified window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="nIndex">The zero-based offset to the value to be retrieved, such as <c>GWL_EXSTYLE</c>.</param>
    /// <returns>The requested value, or zero if the call fails.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Changes an attribute of the specified window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="nIndex">The zero-based offset to the value to be set, such as <see cref="GWL_EXSTYLE"/>.</param>
    /// <param name="dwNewLong">The replacement value.</param>
    /// <returns>The previous value, or zero if the call fails.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>
    /// Retrieves the dimensions of the bounding rectangle of the specified window in screen coordinates.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="lpRect">Receives the screen coordinates of the upper-left and lower-right corners of the window.
    /// </param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Determines whether a window is maximized.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <returns><see langword="true"/> if the window is maximized, or <see langword="false"/> otherwise.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsZoomed(IntPtr hWnd);

    /// <summary>
    /// Retrieves a handle to the display monitor that has the largest area of intersection with the bounding
    /// rectangle of a specified window.
    /// </summary>
    /// <param name="hwnd">A handle to the window of interest.</param>
    /// <param name="dwFlags">Determines the function's return value if the window does not intersect any display
    /// monitor.</param>
    /// <returns>A handle to the display monitor, or <see cref="IntPtr.Zero"/> if none is found.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    /// <summary>
    /// Sends the specified message to a window and does not return until the window procedure has processed the
    /// message.
    /// </summary>
    /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
    /// <param name="Msg">The message to be sent.</param>
    /// <param name="wParam">Additional message-specific information.</param>
    /// <param name="lParam">Additional message-specific information.</param>
    /// <returns>The result of the message processing, which depends on the message sent.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Brings the thread that created the specified window into the foreground and activates the window.
    /// </summary>
    /// <param name="hWnd">A handle to the window that should be activated and brought to the foreground.</param>
    /// <returns><see langword="true"/> if the window was brought to the foreground, or <see langword="false"/>
    /// otherwise.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Enables an application to inform the system that it is in use, thereby preventing the system from entering
    /// sleep or turning off the display while the application is running.
    /// </summary>
    /// <param name="esFlags">The thread's execution requirements.</param>
    /// <returns>If the function succeeds, the return value is the previous thread execution state. If the function
    /// fails, the return value is <c>NULL</c>.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    /// <summary>
    /// Activates the specified timer. When the due time arrives, the timer is signaled and the thread that set the
    /// timer calls the optional completion routine.
    /// </summary>
    /// <param name="hTimer">A handle to the timer object.</param>
    /// <param name="pDueTime">The time after which the state of the timer is to be set to signaled.</param>
    /// <param name="lPeriod">The period of the timer, in milliseconds. If <paramref name="lPeriod"/> is zero, the
    /// timer is signaled once. If <paramref name="lPeriod"/> is greater than zero, the timer is periodic.</param>
    /// <param name="pfnCompletionRoutine">A pointer to an optional completion routine.</param>
    /// <param name="lpArgToCompletionRoutine">A pointer to a structure that is passed to the completion routine.
    /// </param>
    /// <param name="fResume">If this parameter is <see langword="true"/>, restores a system in suspended power conservation
    /// mode when the timer state is set to signaled. Otherwise, the system is not restored.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern bool SetWaitableTimer(IntPtr hTimer, [In] ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);

    /// <summary>
    /// Changes the size, position, and Z order of a window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="hWndInsertAfter">A handle to the window to precede the positioned window in the Z order, or one
    /// of the special Z order values.</param>
    /// <param name="x">The new position of the left side of the window, in screen coordinates.</param>
    /// <param name="y">The new position of the top of the window, in screen coordinates.</param>
    /// <param name="cx">The new width of the window, in pixels.</param>
    /// <param name="cy">The new height of the window, in pixels.</param>
    /// <param name="uFlags">The window sizing and positioning flags.</param>
    /// <returns><see langword="true"/> if the call succeeds, or <see langword="false"/> otherwise.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);
}