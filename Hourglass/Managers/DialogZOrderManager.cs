using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Hourglass.Managers;

/// <summary>
/// Keeps the dialogs on top of all windows, including the timer windows that are always on top, while the dialogs are
/// in the foreground. A dialog that loses the foreground is placed under the foreground window, so it does not stay on
/// top of other windows.
/// </summary>
public sealed class DialogZOrderManager : Manager
{
    /// <summary>
    /// Singleton instance of the <see cref="DialogZOrderManager"/> class.
    /// </summary>
    public static readonly DialogZOrderManager Instance = new();

    private const uint EventSystemForeground = 0x0003;
    private const uint WinEventOutOfContext = 0x0000;

    private const int GwlExStyle = -20;
    private const int WsExTopmost = 0x00000008;

    private const uint GaRootOwner = 3;

    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpAsyncWindowPos = 0x4000;

    /// <summary>
    /// The handles of the dialogs, which may be shown on different threads.
    /// </summary>
    private readonly HashSet<IntPtr> _dialogs = [];

    /// <summary>
    /// The foreground change callback, which is kept referenced while the hook is set.
    /// </summary>
    private readonly WinEventProc _winEventProc;

    /// <summary>
    /// The foreground change hook handle, or <see cref="IntPtr.Zero"/> if the hook is not set.
    /// </summary>
    private IntPtr _hook;

    /// <summary>
    /// Prevents a default instance of the <see cref="DialogZOrderManager"/> class from being created.
    /// </summary>
    private DialogZOrderManager()
    {
        _winEventProc = OnForegroundChanged;
    }

    /// <summary>
    /// Sets the foreground change hook. The hook callback is invoked on the calling thread, which must be the UI
    /// thread.
    /// </summary>
    public override void Initialize()
    {
        if (_hook == IntPtr.Zero)
        {
            _hook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero, _winEventProc, 0, 0, WinEventOutOfContext);
        }
    }

    /// <summary>
    /// Registers the dialog <see cref="Window"/> until it is closed. Call this method when the window handle is
    /// created.
    /// </summary>
    /// <param name="window">The dialog <see cref="Window"/>.</param>
    public void Register(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;

        Register(handle);

        window.Closed += OnClosed;

        void OnClosed(object sender, EventArgs e)
        {
            window.Closed -= OnClosed;
            Unregister(handle);
        }
    }

    /// <summary>
    /// Registers the dialog, or the owner of the dialog, on any thread.
    /// </summary>
    /// <param name="handle">The handle of the dialog or the owner of the dialog.</param>
    public void Register(IntPtr handle)
    {
        lock (_dialogs)
        {
            _dialogs.Add(handle);
        }
    }

    /// <summary>
    /// Unregisters the dialog, or the owner of the dialog, on any thread.
    /// </summary>
    /// <param name="handle">The handle of the dialog or the owner of the dialog.</param>
    public void Unregister(IntPtr handle)
    {
        lock (_dialogs)
        {
            _dialogs.Remove(handle);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }

        base.Dispose(disposing);
    }

    private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        HashSet<IntPtr> dialogs;

        lock (_dialogs)
        {
            if (_dialogs.Count == 0)
            {
                return;
            }

            dialogs = [.._dialogs];
        }

        // The owned windows, such as the file dialog or the message it shows, make their root owner the foreground dialog.
        IntPtr foreground = hwnd == IntPtr.Zero ? IntPtr.Zero : GetAncestor(hwnd, GaRootOwner);

        // The windows of the dialogs, including the owned windows, which are not always moved with their owner.
        List<IntPtr> foregroundWindows = [];
        List<IntPtr> backgroundWindows = [];

        EnumWindows((window, _) =>
        {
            IntPtr rootOwner = GetAncestor(window, GaRootOwner);

            if (rootOwner == foreground && dialogs.Contains(rootOwner))
            {
                foregroundWindows.Add(window);
            }
            else if (dialogs.Contains(rootOwner) && IsTopmost(window))
            {
                backgroundWindows.Add(window);
            }

            return true;
        }, IntPtr.Zero);

        // The dialogs are placed under the foreground window, unless it is always on top or it is the desktop, which
        // is at the bottom of the z-order.
        IntPtr insertAfter = foreground == IntPtr.Zero || IsTopmost(foreground) || IsDesktop(foreground)
            ? IntPtr.Zero
            : foreground;

        const uint flags = SwpNoMove | SwpNoSize | SwpNoActivate | SwpNoOwnerZOrder | SwpAsyncWindowPos;

        // The windows are enumerated from the top of the z-order, so they are moved from the bottom to keep their order.
        foreach (IntPtr window in Enumerable.Reverse(foregroundWindows))
        {
            SetWindowPos(window, HwndTopmost, 0, 0, 0, 0, flags);
        }

        foreach (IntPtr window in Enumerable.Reverse(backgroundWindows))
        {
            SetWindowPos(window, HwndNoTopmost, 0, 0, 0, 0, flags);

            if (insertAfter != IntPtr.Zero)
            {
                SetWindowPos(window, insertAfter, 0, 0, 0, 0, flags);
            }
        }

        static bool IsTopmost(IntPtr hwnd) =>
            (NativeMethods.GetWindowLong(hwnd, GwlExStyle) & WsExTopmost) != 0;

        static bool IsDesktop(IntPtr hwnd)
        {
            if (hwnd == GetShellWindow())
            {
                return true;
            }

            StringBuilder className = new(16);

            return GetClassName(hwnd, className, className.Capacity) > 0 && className.ToString() is "WorkerW" or "Progman";
        }
    }

    private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);

    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc pfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
