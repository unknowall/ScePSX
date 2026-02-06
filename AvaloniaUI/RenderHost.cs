using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ScePSX.UI;

public class RenderHost : NativeControlHost
{
    public IntPtr NativeHandle;
    public IntPtr hInstance = Marshal.GetHINSTANCE(Assembly.GetEntryAssembly().GetModules()[0]);
    public bool ReSized;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        base.SizeChanged += RenderHost_SizeChanged;
        var Ret = base.CreateNativeControlCore(parent);
        NativeHandle = Ret.Handle;

        return Ret;
    }

    private void RenderHost_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var newSize = e.NewSize;
        if (newSize.Width > 10 && newSize.Height > 10)
        {
            GPUBackend.isResizeed = true;
            GPUBackend.ClientHeight = (int)newSize.Height;
            GPUBackend.ClientWidth = (int)newSize.Width;
        }
    }
}