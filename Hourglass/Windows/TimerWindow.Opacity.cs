// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimerWindow.Opacity.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using Hourglass.Timing;

namespace Hourglass.Windows;

/// <summary>
/// Changes the <see cref="TimerWindow"/> opacity.
/// </summary>
public sealed partial class TimerWindow
{
    /// <summary>
    /// The <c>GWL_EXSTYLE</c> index of the extended window styles.
    /// </summary>
    private const int GwlExStyle = -20;

    /// <summary>
    /// The <c>WS_EX_LAYERED</c> extended style of a layered window.
    /// </summary>
    private const int WsExLayered = 0x00080000;

    /// <summary>
    /// The <c>LWA_ALPHA</c> flag that makes <see cref="SetLayeredWindowAttributes"/> set the opacity of the whole
    /// layered window.
    /// </summary>
    private const int LwaAlpha = 0x00000002;

    /// <summary>
    /// The <c>WM_STYLECHANGING</c> message sent to a window when its styles are about to change.
    /// </summary>
    private const int WmStyleChanging = 0x007C;

    /// <summary>
    /// The <c>RDW_INVALIDATE</c>, <c>RDW_ERASE</c>, <c>RDW_ALLCHILDREN</c> and <c>RDW_FRAME</c> flags that make
    /// <see cref="RedrawWindow"/> repaint the whole window, including the title bar.
    /// </summary>
    private const int RdwRepaintWindow = 0x0001 | 0x0004 | 0x0080 | 0x0400;

    /// <summary>
    /// The <see cref="HwndSource"/> the <see cref="OpacityWindowProc"/> hook is added to.
    /// </summary>
    private HwndSource? _opacityHwndSource;

    /// <summary>
    /// A value indicating whether the window is translucent.
    /// </summary>
    private bool _isTranslucent;

    /// <summary>
    /// Makes the whole window, including the title bar and the system buttons, translucent depending on the
    /// <see cref="TimerOptions.Opacity"/>, and paints the window background with the background color of the bound
    /// theme.
    /// </summary>
    /// <remarks>
    /// The translucency relies on the layered window, so the window keeps the title bar and the system buttons
    /// drawn by Windows.
    /// </remarks>
    private void UpdateOpacity()
    {
        if (PresentationSource.FromVisual(this) is not HwndSource { CompositionTarget: { } compositionTarget } hwndSource)
        {
            return;
        }

        Color backgroundColor = Theme?.BackgroundColor ?? SystemColors.WindowColor;

        compositionTarget.BackgroundColor = Color.FromRgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);

        if (!ReferenceEquals(_opacityHwndSource, hwndSource))
        {
            hwndSource.AddHook(OpacityWindowProc);
            _opacityHwndSource = hwndSource;
        }

        _isTranslucent = Options.Opacity < TimerOptions.MaxOpacity;

        IntPtr hwnd = hwndSource.Handle;
        int exStyle = GetWindowLong(hwnd, GwlExStyle);
        bool isLayered = (exStyle & WsExLayered) != 0;

        if (_isTranslucent)
        {
            if (!isLayered)
            {
                SetWindowLong(hwnd, GwlExStyle, exStyle | WsExLayered);
            }

            byte alpha = (byte)Math.Round(Options.Opacity * byte.MaxValue / (double)TimerOptions.MaxOpacity);

            SetLayeredWindowAttributes(hwnd, 0, alpha, LwaAlpha);
            return;
        }

        if (!isLayered)
        {
            return;
        }

        SetWindowLong(hwnd, GwlExStyle, exStyle & ~WsExLayered);
        RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RdwRepaintWindow);
    }

    /// <summary>
    /// Keeps the window layered while it is translucent, as WPF removes the layered style of the window that does
    /// not allow transparency whenever the extended window styles change.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <param name="msg">The message ID.</param>
    /// <param name="wParam">The message's wParam value.</param>
    /// <param name="lParam">The message's lParam value.</param>
    /// <param name="handled">A value that indicates whether the message was handled.</param>
    /// <returns>The appropriate return value depending on the particular message.</returns>
    private IntPtr OpacityWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmStyleChanging || wParam != (IntPtr)GwlExStyle || !_isTranslucent)
        {
            return IntPtr.Zero;
        }

        STYLESTRUCT styleStruct = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
        styleStruct.styleNew |= WsExLayered;
        Marshal.StructureToPtr(styleStruct, lParam, false);

        // Prevents WPF from seeing the message, as the public hooks run before the WPF ones.
        handled = true;

        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STYLESTRUCT
    {
        public int styleOld;
        public int styleNew;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);
}
