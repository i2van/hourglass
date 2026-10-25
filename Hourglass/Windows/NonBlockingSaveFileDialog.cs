using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

using Hourglass.Properties;

namespace Hourglass.Windows;

/// <summary>
/// A save file dialog that is shown on a dedicated thread, so the UI stays responsive while the dialog is open.
/// </summary>
public sealed class NonBlockingSaveFileDialog
{
    /// <summary>
    /// The <see cref="Dispatcher"/> of the thread that shows all dialogs. A single thread is used, as the shell does not
    /// release some resources it allocates for each thread that shows a dialog.
    /// </summary>
    private static readonly Lazy<Dispatcher> DialogDispatcher = new(StartDialogThread);

    /// <summary>
    /// The handle of the hidden owner of the dialog that is shown, or <see cref="IntPtr.Zero"/> if the dialog is not
    /// shown.
    /// </summary>
    private volatile IntPtr _hiddenOwnerHandle;

    /// <summary>
    /// Gets the dialog box title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the file name filter string, which determines the file types the user can choose.
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Gets the default file name extension, which is added if the user does not specify an extension.
    /// </summary>
    public string? DefaultExt { get; init; }

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
        // input. Instead, the dialog is owned by a hidden window with the owner client area and z-order.
        CreateParams hiddenOwnerParams = GetHiddenOwnerParams(owner);

        return DialogDispatcher.Value.InvokeAsync(() => ShowDialog(hiddenOwnerParams)).Task;
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
            SetForegroundWindow(popup);
        }
    }

    private string? ShowDialog(CreateParams hiddenOwnerParams)
    {
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

            using SaveFileDialog dialog = new()
            {
                Title = Title,
                Filter = Filter,
                DefaultExt = DefaultExt,
                AddExtension = true,
                OverwritePrompt = true,
                FileName = FileName,
                InitialDirectory = InitialDirectory
            };

            return dialog.ShowDialog(hiddenOwner) == DialogResult.OK ? dialog.FileName : null;
        }
        finally
        {
            _hiddenOwnerHandle = IntPtr.Zero;
            hiddenOwner.DestroyHandle();
        }

        void SetIcon(int type, Icon icon) =>
            SendMessage(hiddenOwner.Handle, 0x0080 /* WM_SETICON */, (IntPtr)type, icon.Handle);
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
            Name = nameof(NonBlockingSaveFileDialog),
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return dispatcher.Task.Result;
    }

    private static CreateParams GetHiddenOwnerParams(Window owner)
    {
        IntPtr handle = new WindowInteropHelper(owner).Handle;

        GetClientRect(handle, out RECT bounds);
        MapWindowPoints(handle, IntPtr.Zero /* HWND_DESKTOP */, ref bounds, 2);

        return new()
        {
            X = bounds.left,
            Y = bounds.top,
            Width = bounds.right - bounds.left,
            Height = bounds.bottom - bounds.top,
            Style = unchecked((int)0x80000000) /* WS_POPUP */,
            ExStyle = owner.Topmost ? 0x00000008 /* WS_EX_TOPMOST */ : 0
        };
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
    private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT lpPoints, uint cPoints);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
