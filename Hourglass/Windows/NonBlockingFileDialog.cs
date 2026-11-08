using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

using Hourglass.Managers;
using Hourglass.Properties;

namespace Hourglass.Windows;

/// <summary>
/// A file dialog that is shown on a dedicated thread, so the UI stays responsive while the dialog is open.
/// </summary>
public abstract class NonBlockingFileDialog
{
    /// <summary>
    /// The <see cref="Dispatcher"/>s of the idle threads that show the dialogs. Each shown dialog uses its own thread,
    /// as a dialog shown on the thread of another shown dialog runs in the modal loop of that dialog, which disables
    /// that dialog until the nested dialog is closed. The idle threads are reused, as the shell does not release some
    /// resources it allocates for each thread that shows a dialog.
    /// </summary>
    private static readonly ConcurrentBag<Dispatcher> IdleDialogDispatchers = [];

    /// <summary>
    /// The <see cref="Dispatcher"/> of the thread that shows the dialog, or <see langword="null"/> if the dialog is not
    /// requested to show.
    /// </summary>
    private volatile Dispatcher? _dialogDispatcher;

    /// <summary>
    /// The handle of the hidden owner of the dialog that is shown, or <see cref="IntPtr.Zero"/> if the dialog is not
    /// shown.
    /// </summary>
    private volatile IntPtr _hiddenOwnerHandle;

    /// <summary>
    /// A value indicating whether the dialog is requested to close.
    /// </summary>
    private volatile bool _closeRequested;

    /// <summary>
    /// Gets the dialog box title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the file name filter string, which determines the file types the user can choose.
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Gets the one-based index of the initially selected filter.
    /// </summary>
    public int FilterIndex { get; init; } = 1;

    /// <summary>
    /// Gets the initial file name.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets the initial directory.
    /// </summary>
    public string? InitialDirectory { get; init; }

    /// <summary>
    /// Shows the dialog over the <paramref name="owner"/> without blocking the UI thread or disabling the
    /// <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The <see cref="Window"/> to show the dialog over.</param>
    /// <returns>The selected file path, or <see langword="null"/> if the user canceled the dialog.</returns>
    public Task<string?> ShowAsync(Window owner)
    {
        // The owner is not used directly, as the dialog would disable it and a cross-thread owner would attach the thread
        // input. Instead, the dialog is owned by a hidden window with the owner client area.
        CreateParams hiddenOwnerParams = GetHiddenOwnerParams(owner);
        IntPtr ownerHandle = new WindowInteropHelper(owner).Handle;

        Dispatcher dialogDispatcher = IdleDialogDispatchers.TryTake(out Dispatcher? idleDispatcher)
            ? idleDispatcher
            : StartDialogThread();

        _dialogDispatcher = dialogDispatcher;

        return dialogDispatcher.InvokeAsync(() =>
        {
            try
            {
                return ShowDialog(hiddenOwnerParams, ownerHandle);
            }
            finally
            {
                IdleDialogDispatchers.Add(dialogDispatcher);
            }
        }).Task;
    }

    /// <summary>
    /// Brings the dialog to the foreground and activates it, if the dialog is shown.
    /// </summary>
    public void Activate()
    {
        IntPtr hiddenOwnerHandle = _hiddenOwnerHandle;

        if (hiddenOwnerHandle == IntPtr.Zero)
        {
            return;
        }

        // The enabled popup is the dialog, or the message the dialog shows, such as the overwrite prompt.
        IntPtr popup = GetWindow(hiddenOwnerHandle, 6 /* GW_ENABLEDPOPUP */);

        if (popup != IntPtr.Zero && popup != hiddenOwnerHandle)
        {
            NativeMethods.SetForegroundWindow(popup);
        }
    }

    /// <summary>
    /// Closes the dialog and the messages it shows, if the dialog is shown, or prevents the dialog from showing, if the
    /// dialog is about to show. The <see cref="ShowAsync"/> then returns <see langword="null"/>.
    /// </summary>
    public void Close()
    {
        _closeRequested = true;

        _dialogDispatcher?.InvokeAsync(CloseDialogAsync);
    }

    /// <summary>
    /// Creates the file dialog.
    /// </summary>
    /// <returns>The file dialog.</returns>
    protected abstract FileDialog CreateDialog();

    /// <summary>
    /// Closes the windows owned by the hidden owner on the dialog thread. The dialog thread processes the dispatcher
    /// operations in the dialog modal loop. The closing is repeated until the dialog is closed, as a disabled dialog
    /// under the message it shows does not close.
    /// </summary>
    private async Task CloseDialogAsync()
    {
        IntPtr hiddenOwnerHandle;

        while ((hiddenOwnerHandle = _hiddenOwnerHandle) != IntPtr.Zero)
        {
            EnumThreadWindows(GetCurrentThreadId(), (hWnd, _) =>
            {
                if (IsWindowVisible(hWnd) && IsWindowEnabled(hWnd) && IsOwnedBy(hWnd, hiddenOwnerHandle))
                {
                    PostMessage(hWnd, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }, IntPtr.Zero);

            await Task.Delay(50);
        }

        static bool IsOwnedBy(IntPtr hWnd, IntPtr owner)
        {
            for (IntPtr current = GetWindow(hWnd, 4 /* GW_OWNER */); current != IntPtr.Zero; current = GetWindow(current, 4 /* GW_OWNER */))
            {
                if (current == owner)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private string? ShowDialog(CreateParams hiddenOwnerParams, IntPtr ownerHandle)
    {
        if (_closeRequested)
        {
            return null;
        }

        using Icon appIcon = Resources.AppIcon;
        using Icon largeIcon = new(appIcon, SystemInformation.IconSize);
        using Icon smallIcon = new(appIcon, SystemInformation.SmallIconSize);

        NativeWindow hiddenOwner = new();

        try
        {
            hiddenOwner.CreateHandle(hiddenOwnerParams);

            // The dialog shows the icon of its owner.
            SetIcon(1 /* ICON_BIG */, largeIcon);
            SetIcon(0 /* ICON_SMALL */, smallIcon);

            _hiddenOwnerHandle = hiddenOwner.Handle;

            // The owner stays on top under the dialog, if the owner is a dialog itself.
            DialogZOrderManager.Instance.Register(hiddenOwner.Handle, ownerHandle);

            using FileDialog dialog = CreateDialog();

            dialog.Title = Title;
            dialog.Filter = Filter;
            dialog.FilterIndex = FilterIndex;
            dialog.FileName = FileName;
            dialog.InitialDirectory = InitialDirectory;

            return dialog.ShowDialog(hiddenOwner) == DialogResult.OK ? dialog.FileName : null;
        }
        finally
        {
            DialogZOrderManager.Instance.Unregister(hiddenOwner.Handle);

            _hiddenOwnerHandle = IntPtr.Zero;
            hiddenOwner.DestroyHandle();
        }

        void SetIcon(int type, Icon icon) =>
            NativeMethods.SendMessage(hiddenOwner.Handle, 0x0080 /* WM_SETICON */, (IntPtr)type, icon.Handle);
    }

    private static Dispatcher StartDialogThread()
    {
        TaskCompletionSource<Dispatcher> dispatcher = new();

        Thread thread = new(() =>
        {
            dispatcher.SetResult(Dispatcher.CurrentDispatcher);
            Dispatcher.Run();
        })
        {
            Name = nameof(NonBlockingFileDialog),
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return dispatcher.Task.Result;
    }

    private static CreateParams GetHiddenOwnerParams(Window owner)
    {
        IntPtr handle = new WindowInteropHelper(owner).Handle;

        NativeMethods.GetClientRect(handle, out RECT bounds);
        MapWindowPoints(handle, IntPtr.Zero /* HWND_DESKTOP */, ref bounds, 2);

        return new()
        {
            X = bounds.left,
            Y = bounds.top,
            Width = bounds.right - bounds.left,
            Height = bounds.bottom - bounds.top,
            Style = unchecked((int)0x80000000) /* WS_POPUP */,
            // The dialog is shown on top of all windows, including the owner that is always on top. The dialog is no
            // longer on top when it loses the foreground.
            ExStyle = 0x00000008 /* WS_EX_TOPMOST */
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT lpPoints, uint cPoints);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadWindowsProc lpfn, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint GetCurrentThreadId();

    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool EnumThreadWindowsProc(IntPtr hWnd, IntPtr lParam);
}
