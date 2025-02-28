using System;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScePSX
{
    public class D2DRenderer : Panel
    {
        private ID2D1Factory factoryobj;
        private ID2D1HwndRenderTarget renderTargetObj;
        private ID2D1Bitmap bitmapObj;

        private ColorF clearcolor = new ColorF { r = 0, g = 0, b = 0, a = 1 };
        private int[] pixels = new int[4096 * 2048];
        private int width;
        private int height;
        private int scale, oldscale;

        public D2DRenderer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            DoubleBuffered = false;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();

            this.ResizeRedraw = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitializeDirect2D();
        }

        private void InitializeDirect2D()
        {
            GCHandle handle = GCHandle.Alloc(factoryOptions, GCHandleType.Pinned);
            D2D1CreateFactory(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED, typeof(ID2D1Factory).GUID, handle.AddrOfPinnedObject(), out factoryobj);
            if (factoryobj == null)
            {
                Console.WriteLine("Failed to create D2D1Factory.");
                handle.Free();
                return;
            }
            handle.Free();

            float dpiX, dpiY;
            factoryobj.GetDesktopDpi(out dpiX, out dpiY);

            var renderTargetProperties = new RenderTargetProperties
            {
                type = D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
                pixelFormat = new PixelFormat
                {
                    format = 87, // DXGI_FORMAT_B8G8R8A8_UNORM
                    alphaMode = 1 // D2D1_ALPHA_MODE_PREMULTIPLIED
                },
                dpiX = dpiX,
                dpiY = dpiY,
                usage = 0, // D2D1_RENDER_TARGET_USAGE_NONE 0 D2D1_RENDER_TARGET_USAGE_GDI_COMPATIBLE 2
                minLevel = 0 // D2D1_FEATURE_LEVEL_DEFAULT
            };
            var hwndRenderTargetProperties = new HwndRenderTargetProperties
            {
                hwnd = this.Handle,
                pixelSize = new SizeU { width = (uint)this.ClientSize.Width, height = (uint)this.ClientSize.Height },
                presentOptions = 0 // DXGI_PRESENT_OPTIONS_NONE
            };

            int hr = factoryobj.CreateHwndRenderTarget(renderTargetProperties, hwndRenderTargetProperties, out renderTargetObj);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            if (renderTargetObj == null)
            {
                Console.WriteLine("CreateHwndRenderTarget returned success but renderTarget is null.");
                return;
            }

            var bitmapSize = new SizeU { width = (uint)width, height = (uint)height };
            renderTargetObj.CreateBitmap(ref bitmapSize, IntPtr.Zero, 0, IntPtr.Zero, out bitmapObj);
        }

        public void RenderBuffer(int[] pixels, int width, int height, int scale = 0)
        {
            this.pixels = pixels;
            this.width = width;
            this.height = height;
            this.scale = scale;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Render();
        }

        private unsafe void Render()
        {
            if (renderTargetObj == null || bitmapObj == null)
                return;

            if (scale > 0)
            {
                pixels = XbrScaler.ScaleXBR(pixels, width, height, scale);

                width = width * scale;
                height = height * scale;
            }

            if (oldscale != scale)
            {
                oldscale = scale;

                if (bitmapObj != null)
                {
                }

                renderTargetObj.CreateBitmap(new SizeU { width = (uint)width, height = (uint)height }, IntPtr.Zero, 0, IntPtr.Zero, out bitmapObj);
            }

            fixed (int* srcData = pixels)
            {

                bitmapObj.CopyFromMemory((IntPtr)srcData, (uint)(width * 4));
            }


            renderTargetObj.BeginDraw();

            renderTargetObj.Clear(ref clearcolor);

            var dstrect = new RectF { left = 0, top = 0, right = this.ClientSize.Width, bottom = this.ClientSize.Height };
            var srcrect = new RectF { left = 0, top = 0, right = width, bottom = height };
            renderTargetObj.DrawBitmap(bitmapObj, ref dstrect, 1.0f, 1, ref srcrect);

            renderTargetObj.EndDraw(out _, out _);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0 && renderTargetObj != null)
            {
                var dstrect = new SizeU { width = (uint)this.ClientSize.Width, height = (uint)this.ClientSize.Height };
                renderTargetObj.Resize(ref dstrect);
                this.Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        #region D2D1 API
        public enum D2D1_FACTORY_TYPE: uint
        {
            D2D1_FACTORY_TYPE_SINGLE_THREADED = 0,
            D2D1_FACTORY_TYPE_MULTI_THREADED = 1,
            D2D1_FACTORY_TYPE_FORCE_DWORD = 0xffffffff
        };

        [DllImport("D2d1.dll", EntryPoint = "D2D1CreateFactory", PreserveSig = false)]
        public static extern void D2D1CreateFactory(
            [In] D2D1_FACTORY_TYPE factoryType,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [In] IntPtr factoryOptions,
            [Out] out ID2D1Factory factory);

        [ComImport, Guid("06152247-6f50-465a-9245-118bfd3b6007"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID2D1Factory
        {
            int CreateHwndRenderTarget(
                [In] RenderTargetProperties renderTargetProperties,
                [In] HwndRenderTargetProperties hwndRenderTargetProperties,
                [Out] out ID2D1HwndRenderTarget hwndRenderTarget
            );

            void GetDesktopDpi(out float dpiX, out float dpiY);
        }

        [ComImport, Guid("2cd9069e-c5c5-4c0a-8d0e-fb0a3c7d440b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID2D1HwndRenderTarget
        {
            void BeginDraw();
            void EndDraw(out long tag1, out long tag2);
            void Clear(ref ColorF clearColor);
            void DrawBitmap(ID2D1Bitmap bitmap, ref RectF destRect, float opacity, int interpolationMode, ref RectF srcRect);
            void Resize(ref SizeU size);
            void CreateBitmap(ref SizeU size, IntPtr srcData, uint pitch, IntPtr bitmapProperties, out ID2D1Bitmap bitmap);
            void GetFactory(out ID2D1Factory factory);
        }

        [ComImport, Guid("a2296057-ea42-4099-983b-539fb6505426"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ID2D1Bitmap
        {
            void CopyFromMemory(IntPtr srcData, uint pitch);
        }

        public enum D2D1_RENDER_TARGET_TYPE : uint
        {
            D2D1_RENDER_TARGET_TYPE_DEFAULT = 0,
            D2D1_RENDER_TARGET_TYPE_SOFTWARE = 1,
            D2D1_RENDER_TARGET_TYPE_HARDWARE = 2,
            D2D1_RENDER_TARGET_TYPE_FORCE_DWORD = 0xffffffff
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1FactoryOptions
        {
            public uint debugLevel;
        }
        public D2D1FactoryOptions factoryOptions = new D2D1FactoryOptions { debugLevel = 0 };

        [StructLayout(LayoutKind.Sequential)]
        public struct PixelFormat
        {
            public int format;
            public int alphaMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RenderTargetProperties
        {
            public D2D1_RENDER_TARGET_TYPE type;
            public PixelFormat pixelFormat;
            public float dpiX;
            public float dpiY;
            public int usage;
            public int minLevel;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HwndRenderTargetProperties
        {
            public IntPtr hwnd;
            public SizeU pixelSize;
            public int presentOptions;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SizeU
        {
            public uint width;
            public uint height;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RectF
        {
            public float left;
            public float top;
            public float right;
            public float bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColorF
        {
            public float r;
            public float g;
            public float b;
            public float a;
        }
        #endregion
    }
}
