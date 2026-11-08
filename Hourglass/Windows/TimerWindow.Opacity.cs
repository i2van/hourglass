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
    /// The <c>WS_EX_LAYERED</c> extended style of a layered window.
    /// </summary>
    private const int WS_EX_LAYERED = 0x00080000;

    /// <summary>
    /// The <c>LWA_ALPHA</c> flag that makes <see cref="SetLayeredWindowAttributes"/> set the opacity of the whole
    /// layered window.
    /// </summary>
    private const int LWA_ALPHA = 0x00000002;

    /// <summary>
    /// The <c>RDW_INVALIDATE</c> flag that makes <see cref="RedrawWindow"/> invalidate the window.
    /// </summary>
    private const int RDW_INVALIDATE = 0x0001;

    /// <summary>
    /// The <c>RDW_ERASE</c> flag that makes <see cref="RedrawWindow"/> erase the window background.
    /// </summary>
    private const int RDW_ERASE = 0x0004;

    /// <summary>
    /// The <c>RDW_ALLCHILDREN</c> flag that makes <see cref="RedrawWindow"/> repaint the child windows too.
    /// </summary>
    private const int RDW_ALLCHILDREN = 0x0080;

    /// <summary>
    /// The <c>RDW_FRAME</c> flag that makes <see cref="RedrawWindow"/> repaint the non-client area, including the
    /// title bar.
    /// </summary>
    private const int RDW_FRAME = 0x0400;

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
        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        bool isLayered = (exStyle & WS_EX_LAYERED) != 0;

        if (_isTranslucent)
        {
            if (!isLayered)
            {
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
            }

            byte alpha = (byte)Math.Round(Options.Opacity * byte.MaxValue / (double)TimerOptions.MaxOpacity);

            SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);
            return;
        }

        if (!isLayered)
        {
            return;
        }

        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);
        RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_FRAME);
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
        if (msg != NativeMethods.WM_STYLECHANGING || wParam != (IntPtr)NativeMethods.GWL_EXSTYLE || !_isTranslucent)
        {
            return IntPtr.Zero;
        }

        STYLESTRUCT styleStruct = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
        styleStruct.styleNew |= WS_EX_LAYERED;
        Marshal.StructureToPtr(styleStruct, lParam, false);

        // Prevents WPF from seeing the message, as the public hooks run before the WPF ones.
        handled = true;

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);
}
