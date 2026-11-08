using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace

namespace WindowsVirtualDesktopHelper.VirtualDesktopAPI.Implementation;

internal sealed class VirtualDesktopServer2022(ImmersiveShellProvider immersiveShellProvider)
    : VirtualDesktop<VirtualDesktopServer2022.IVirtualDesktopManagerInternal>(immersiveShellProvider)
{
    protected override Guid GetCurrentDesktopId() =>
        VirtualDesktopManagerInternal!.GetCurrentDesktop(IntPtr.Zero).GetId();

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("62FDF88B-11CA-4AFB-8BD8-2296DFAE49E2")]
    internal interface IVirtualDesktop
    {
        bool IsViewVisible(IntPtr view);
        Guid GetId();
        IntPtr Unknown1();

        [return: MarshalAs(UnmanagedType.HString)]
        string GetName();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("094AFE11-44F2-4BA0-976F-29A97E263EE0")]
    internal interface IVirtualDesktopManagerInternal
    {
        int GetCount(IntPtr hWnd);
        void MoveViewToDesktop(IntPtr view, IVirtualDesktop desktop);
        bool CanViewMoveDesktops(IntPtr view);
        IVirtualDesktop GetCurrentDesktop(IntPtr hWnd);
        void GetDesktops(IntPtr hWnd, out IntPtr desktops);

        [PreserveSig]
        int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);

        void SwitchDesktop(IntPtr hWnd, IVirtualDesktop desktop);
        IVirtualDesktop CreateDesktop(IntPtr hWnd);
        void RemoveDesktop(IVirtualDesktop desktop, IVirtualDesktop fallback);
        IVirtualDesktop FindDesktop(ref Guid desktopId);
        void GetDesktopSwitchIncludeExcludeViews(IVirtualDesktop desktop, out IntPtr unknown1, out IntPtr unknown2);
        void SetDesktopName(IVirtualDesktop desktop, [MarshalAs(UnmanagedType.HString)] string name);
        void CopyDesktopState(IntPtr pView0, IntPtr pView1);
        int GetDesktopIsPerMonitor();
    }
}
