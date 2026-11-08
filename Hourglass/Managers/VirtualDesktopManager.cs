using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;

using WindowsVirtualDesktopHelper.VirtualDesktopAPI.Implementation;

namespace Hourglass.Managers;

// ReSharper disable ExceptionNotDocumented

public sealed class VirtualDesktopManager : Manager
{
    public static readonly VirtualDesktopManager Instance = new();

    private readonly Lazy<ICurrentVirtualDesktop?> _currentVirtualDesktop = new(GetVirtualDesktop);

    private VirtualDesktopManager()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing && _currentVirtualDesktop.IsValueCreated)
        {
            _currentVirtualDesktop.Value?.Dispose();
        }

        base.Dispose(disposing);
    }

    public void MoveToCurrentVirtualDesktop(Window window) =>
        _currentVirtualDesktop.Value?.MoveTo(new WindowInteropHelper(window).Handle);

    private static ICurrentVirtualDesktop? GetVirtualDesktop()
    {
        foreach (var virtualDesktop in EnumerateVirtualDesktops())
        {
            if (virtualDesktop.IsValid)
            {
                return virtualDesktop;
            }

            virtualDesktop.Dispose();
        }

        return null;

        static IEnumerable<ICurrentVirtualDesktop> EnumerateVirtualDesktops()
        {
            using var immersiveShellProvider = new ImmersiveShellProvider();

            yield return new VirtualDesktopWin11_Insider25314(immersiveShellProvider);
            yield return new VirtualDesktopWin11_Insider22631(immersiveShellProvider);
            yield return new VirtualDesktopWin11_Insider(immersiveShellProvider);
            yield return new VirtualDesktopWin11_23H2_2921(immersiveShellProvider);
            yield return new VirtualDesktopWin11_23H2(immersiveShellProvider);
            yield return new VirtualDesktopWin11_22H2(immersiveShellProvider);
            yield return new VirtualDesktopWin11_21H2(immersiveShellProvider);
            yield return new VirtualDesktopServer2022(immersiveShellProvider);
            yield return new VirtualDesktopWin10(immersiveShellProvider);
        }
    }
}
