using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LightGL.Linux;
using LightVK;

namespace ScePSX.UI;

public class RenderHost : NativeControlHost
{
    public IntPtr NativeHandle;
    public IntPtr hInstance;
    public bool ReSized;

    public void CancelSizeChanged()
    {
        base.SizeChanged -= RenderHost_SizeChanged;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        base.SizeChanged += RenderHost_SizeChanged;
        var Ret = base.CreateNativeControlCore(parent);
        NativeHandle = Ret.Handle;
        if (parent.HandleDescriptor == "XID")
        {
            VulkanDevice.OsEnv = VulkanDevice.vkOsEnv.LINUX_XLIB;

            GetDisplayFromCurrentWindow(this);
        } else
        {
            VulkanDevice.OsEnv = VulkanDevice.vkOsEnv.WIN;

            hInstance = Marshal.GetHINSTANCE(Assembly.GetEntryAssembly().GetModules()[0]);
        }
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

    //Need reflection for X11 display acces
    //https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.X11/X11Window.cs
    //https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.X11/X11Info.cs
#pragma warning disable CS8605
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", Justification = "Need reflection for X11 display acces")]
    public void GetDisplayFromCurrentWindow(Control control)
    {
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel?.PlatformImpl != null)
        {
            //Console.WriteLine($"PlatformImpl type: {topLevel.PlatformImpl.GetType().FullName}");
            var implType = topLevel.PlatformImpl.GetType();
            var _x11Field = implType.GetField("_x11", BindingFlags.Instance | BindingFlags.NonPublic);

            if (_x11Field != null)
            {
                var x11 = _x11Field.GetValue(topLevel.PlatformImpl);
                if (x11 != null)
                {
                    //Console.WriteLine($"✓ Got x11 from PlatformImpl._x11: {x11}");
                    var displayProp = x11.GetType().GetProperty("Display", BindingFlags.Instance | BindingFlags.Public);
                    var screenProp = x11.GetType().GetProperty("DefaultScreen", BindingFlags.Instance | BindingFlags.Public);

                    if (screenProp != null)
                    {
                        var screen = (int)screenProp.GetValue(x11);
                        X11GLContext.DefaultScreen = screen;
                        //Console.WriteLine($"✓ Got DefaultScreen from X11Info: {screen}");
                    }

                    if (displayProp != null)
                    {
                        var display = (IntPtr)displayProp.GetValue(x11);
                        X11GLContext.DefaultDisplay = display;
                        hInstance = display;
                        //Console.WriteLine($"✓ Got Display from X11Info: 0x{display:X}");
                    }
                }
            }
        }
    }
#pragma warning restore CS8605
}

public class SoftDrawHost : Control
{
    private WriteableBitmap? _bitmap;
    private int _currentWidth;
    private int _currentHeight;
    private object _renderLock = new object();
    public bool KeepAR = true;

    public void RenderPixels(int[] pixels, int width, int height, ScaleParam scale)
    {
        if (width <= 0 || height <= 0 || pixels == null || pixels.Length < width * height)
            return;

        lock (_renderLock)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (scale.scale > 0)
                    {
                        pixels = PixelsScaler.Scale(pixels, width, height, scale.scale, scale.mode);

                        width = width * scale.scale;
                        height = height * scale.scale;
                    }

                    if (_bitmap == null || _currentWidth != width || _currentHeight != height)
                    {
                        _bitmap?.Dispose();
                        _bitmap = new WriteableBitmap(
                            new PixelSize(width, height),
                            new Vector(96, 96),
                            PixelFormat.Bgra8888,
                            AlphaFormat.Premul); //AlphaFormat.Premul
                        _currentWidth = width;
                        _currentHeight = height;
                    }

                    using (var lockedBitmap = _bitmap.Lock())
                    {
                        Marshal.Copy(pixels, 0, lockedBitmap.Address, Math.Min(pixels.Length, width * height));
                    }

                    InvalidateVisual();
                } catch (Exception ex)
                {
                    Console.WriteLine($"Render error: {ex}");
                }
            }, DispatcherPriority.Render);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect destRect;

        if (_bitmap != null)
        {
            if (KeepAR)
            {
                destRect = Calculate4_3Rect(Bounds);
            } else
                destRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
            var sourceRect = new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height);

            context.DrawImage(_bitmap, sourceRect, destRect);
        }
    }

    private Rect Calculate4_3Rect(Rect availableRect)
    {
        double availableWidth = availableRect.Width;
        double availableHeight = availableRect.Height;

        double targetRatio = 4.0 / 3.0;
        double availableRatio = availableWidth / availableHeight;

        double width, height;

        if (availableRatio > targetRatio)
        {
            height = availableHeight;
            width = height * targetRatio;
        } else
        {
            width = availableWidth;
            height = width / targetRatio;
        }

        double x = (availableWidth - width) / 2;
        double y = (availableHeight - height) / 2;

        return new Rect(x, y, width, height);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (KeepAR)
        {
            if (double.IsInfinity(availableSize.Width))
            {
                return new Size(400, 300);
            }

            double width = availableSize.Width;
            double height = width * 3.0 / 4.0;

            return new Size(width, height);
        } else
            return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return base.ArrangeOverride(finalSize);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _bitmap?.Dispose();
        _bitmap = null;
        base.OnDetachedFromVisualTree(e);
    }
}
