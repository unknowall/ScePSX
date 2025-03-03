using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DirectX.D2D1
{
    public struct D2D1ColorF
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public D2D1ColorF(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    public enum D2D1RenderTargetType
    {
        Default = 0,
        Software = 1,
        Hardware = 2,
    }

    public enum D2D1AlphaMode
    {
        Unknown = 0,
        Premultiplied = 1,
        Straight = 2,
        Ignore = 3,
    }

    public enum DxgiFormat
    {
        Unknown,
        B8G8R8A8UNorm = 87
    }

    [Flags]
    public enum D2D1RenderTargetUsages
    {
        None = 0x00000000,
        ForceBitmapRemoting = 0x00000001,
        GdiCompatible = 0x00000002,
    }

    public enum D2D1FeatureLevel
    {
        Default = 0,
        FeatureLevel91 = 0x9100,
        FeatureLevel100 = 0xa000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1RenderTargetProperties
    {
        public D2D1RenderTargetType renderTargetType;

        public D2D1PixelFormat pixelFormat;

        public float dpiX;

        public float dpiY;

        public D2D1RenderTargetUsages usage;

        public D2D1FeatureLevel minLevel;

        public D2D1RenderTargetProperties(D2D1RenderTargetType renderTargetType, D2D1PixelFormat pixelFormat, float dpiX, float dpiY, D2D1RenderTargetUsages usage, D2D1FeatureLevel minLevel)
        {
            this.renderTargetType = renderTargetType;
            this.pixelFormat = pixelFormat;
            this.dpiX = dpiX;
            this.dpiY = dpiY;
            this.usage = usage;
            this.minLevel = minLevel;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1SizeU
    {
        public uint width;
        public uint height;

        public D2D1SizeU(uint width, uint height)
        {
            this.width = width;
            this.height = height;
        }
    }

    public enum D2D1AntialiasMode
    {
        PerPrimitive = 0,
        Aliased = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1PixelFormat
    {
        private DxgiFormat format;
        private D2D1AlphaMode alphaMode;

        public D2D1PixelFormat(DxgiFormat format, D2D1AlphaMode alphaMode)
        {
            this.format = format;
            this.alphaMode = alphaMode;
        }

        public static D2D1PixelFormat Default
        {
            get
            {
                return new D2D1PixelFormat(DxgiFormat.Unknown, D2D1AlphaMode.Unknown);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1BitmapProperties
    {
        public D2D1PixelFormat pixelFormat;
        public float dpiX;
        public float dpiY;

        public D2D1BitmapProperties(D2D1PixelFormat pixelFormat, float dpiX, float dpiY)
        {
            this.pixelFormat = pixelFormat;
            this.dpiX = dpiX;
            this.dpiY = dpiY;
        }

        public static D2D1BitmapProperties Default
        {
            get
            {
                return new D2D1BitmapProperties(D2D1PixelFormat.Default, 96.0f, 96.0f);
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1RectF
    {
        public float left;
        public float top;
        public float right;
        public float bottom;

        public D2D1RectF(float left, float top, float right, float bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public static D2D1RectF Infinite
        {
            get
            {
                return new D2D1RectF(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            }
        }
    }

    public enum D2D1BitmapInterpolationMode
    {
        NearestNeighbor = 0,
        Linear = 1,
        Cubic = 2,
        MultiSampleLinear = 3,
        Anisotropic = 4,
        HighQualityCubic = 5,
        Fant = 6,
        MipmapLinear = 7
    }

    public interface ID2D1Releasable
    {
        void Dispose();
        void Release();
    }

    public enum D2D1FactoryType
    {
        SingleThreaded = 0,
        MultiThreaded = 1,
    }

    public enum D2D1DebugLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Information = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1FactoryOptions
    {
        public D2D1DebugLevel debugLevel;
        public D2D1FactoryOptions(D2D1DebugLevel debugLevel)
        {
            this.debugLevel = debugLevel;
        }
    }

    [Flags]
    public enum D2D1PresentOptions
    {
        None = 0x00000000,
        RetainContents = 0x00000001,
        Immediately = 0x00000002,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1HwndRenderTargetProperties
    {

        private IntPtr hwnd;
        private D2D1SizeU pixelSize;
        private D2D1PresentOptions presentOptions;

        public D2D1HwndRenderTargetProperties(IntPtr hwnd)
        {
            this.hwnd = hwnd;
            this.pixelSize = new D2D1SizeU(0U, 0U);
            this.presentOptions = D2D1PresentOptions.None;
        }

        public D2D1HwndRenderTargetProperties(IntPtr hwnd, D2D1SizeU pixelSize, D2D1PresentOptions presentOptions)
        {
            this.hwnd = hwnd;
            this.pixelSize = pixelSize;
            this.presentOptions = presentOptions;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1SizeF
    {
        private float width;
        private float height;

        public D2D1SizeF(float width, float height)
        {
            this.width = width;
            this.height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1RectU
    {
        private uint left;
        private uint top;
        private uint right;
        private uint bottom;

        public D2D1RectU(uint left, uint top, uint right, uint bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D2D1Point2U
    {
        private uint x;
        private uint y;

        public D2D1Point2U(uint x, uint y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Flags]
    public enum D2D1WindowStates
    {
        None = 0x0000000,
        Occluded = 0x0000001,
    }

    public enum D2D1TextAntialiasMode
    {
        Default = 0,
        ClearType = 1,
        Grayscale = 2,
        Aliased = 3,
    }

    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [DllImport("D2d1.dll", EntryPoint = "D2D1CreateFactory", PreserveSig = false)]
        public static extern void D2D1CreateFactory(
                [In] D2D1FactoryType factoryType,
                [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [In] IntPtr factoryOptions,
                [Out] out ID2D1Factory factory);
    }

    public sealed class D2D1Factory : IDisposable, ID2D1Releasable
    {
        private readonly ID2D1Factory factory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal D2D1Factory(ID2D1Factory factory)
        {
            this.factory = factory;
        }

        public object Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.factory;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(D2D1Factory value)
        {
            return value != null && value.Handle != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static D2D1Factory Create(D2D1FactoryType factoryType)
        {
            NativeMethods.D2D1CreateFactory(factoryType, typeof(ID2D1Factory).GUID, IntPtr.Zero, out ID2D1Factory factory);

            if (factory == null)
            {
                return null;
            }

            return new D2D1Factory(factory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static D2D1Factory Create(D2D1FactoryType factoryType, D2D1FactoryOptions factoryOptions)
        {
            ID2D1Factory factory;

            GCHandle handle = GCHandle.Alloc(factoryOptions, GCHandleType.Pinned);

            try
            {
                NativeMethods.D2D1CreateFactory(factoryType, typeof(ID2D1Factory).GUID, handle.AddrOfPinnedObject(), out factory);
            } finally
            {
                handle.Free();
            }

            if (factory == null)
            {
                return null;
            }

            return new D2D1Factory(factory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static D2D1Factory Create(D2D1FactoryType factoryType, D2D1DebugLevel debugLevel)
        {
            var factoryOptions = new D2D1FactoryOptions(debugLevel);
            return D2D1Factory.Create(factoryType, factoryOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ToBoolean()
        {
            return this.Handle != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Marshal.ReleaseComObject(this.Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Marshal.FinalReleaseComObject(this.Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReloadSystemMetrics()
        {
            this.factory.ReloadSystemMetrics();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetDesktopDpi(out float dpiX, out float dpiY)
        {
            this.factory.GetDesktopDpi(out dpiX, out dpiY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D2D1HwndRenderTarget CreateHwndRenderTarget(D2D1RenderTargetProperties renderTargetProperties, D2D1HwndRenderTargetProperties hwndRenderTargetProperties)
        {
            this.factory.CreateHwndRenderTarget(ref renderTargetProperties, ref hwndRenderTargetProperties, out ID2D1HwndRenderTarget hwndRenderTarget);

            if (hwndRenderTarget == null)
            {
                return null;
            }

            return new D2D1HwndRenderTarget(hwndRenderTarget);
        }

    }

    public abstract class D2D1Resource : IDisposable, ID2D1Releasable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal D2D1Resource()
        {
        }

        public abstract object Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(D2D1Resource value)
        {
            return value != null && value.Handle != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ToBoolean()
        {
            return this.Handle != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Marshal.ReleaseComObject(this.Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Marshal.FinalReleaseComObject(this.Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D2D1Factory GetFactory()
        {
            this.GetHandle<ID2D1Resource>().GetFactory(out ID2D1Factory factory);

            if (factory == null)
            {
                return null;
            }

            return new D2D1Factory(factory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetHandle<T>()
        {
            return (T)this.Handle;
        }
    }

    public sealed class D2D1Bitmap : D2D1Resource
    {
        private readonly ID2D1Bitmap bitmap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal D2D1Bitmap(ID2D1Bitmap bitmap)
        {
            this.bitmap = bitmap;
        }

        public override object Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.bitmap;
            }
        }

        public D2D1SizeF Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.bitmap.GetSize(out D2D1SizeF size);
                return size;
            }
        }

        public D2D1SizeU PixelSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.bitmap.GetPixelSize(out D2D1SizeU size);
                return size;
            }
        }

        public D2D1PixelFormat PixelFormat
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.bitmap.GetPixelFormat(out D2D1PixelFormat pixelFormat);
                return pixelFormat;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetDpi(out float dpiX, out float dpiY)
        {
            this.bitmap.GetDpi(out dpiX, out dpiY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromBitmap(D2D1Bitmap srcBitmap)
        {
            if (srcBitmap == null)
            {
                throw new ArgumentNullException(nameof(srcBitmap));
            }

            this.bitmap.CopyFromBitmap(IntPtr.Zero, srcBitmap.bitmap, IntPtr.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromBitmap(D2D1Point2U destPoint, D2D1Bitmap srcBitmap, D2D1RectU srcRect)
        {
            if (srcBitmap == null)
            {
                throw new ArgumentNullException(nameof(srcBitmap));
            }

            GCHandle destPointHandle = GCHandle.Alloc(destPoint, GCHandleType.Pinned);
            GCHandle srcRectHandle = GCHandle.Alloc(srcRect, GCHandleType.Pinned);

            try
            {
                this.bitmap.CopyFromBitmap(destPointHandle.AddrOfPinnedObject(), srcBitmap.bitmap, srcRectHandle.AddrOfPinnedObject());
            } finally
            {
                destPointHandle.Free();
                srcRectHandle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromMemory(IntPtr srcData, uint pitch)
        {
            this.bitmap.CopyFromMemory(IntPtr.Zero, srcData, pitch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromMemory(byte[] srcData, uint pitch)
        {
            if (srcData == null)
            {
                throw new ArgumentNullException(nameof(srcData));
            }

            this.bitmap.CopyFromMemory(IntPtr.Zero, Marshal.UnsafeAddrOfPinnedArrayElement(srcData, 0), pitch);
        }
    }

    public sealed class D2D1HwndRenderTarget : D2D1Resource
    {
        private readonly ID2D1HwndRenderTarget renderTarget;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal D2D1HwndRenderTarget(ID2D1HwndRenderTarget renderTarget)
        {
            this.renderTarget = renderTarget;
        }

        public override object Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.renderTarget;
            }
        }

        public ID2D1HwndRenderTarget RenderTarget
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.renderTarget;
            }
        }

        public IntPtr Hwnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.renderTarget.GetHwnd();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D2D1WindowStates CheckWindowState()
        {
            return this.renderTarget.CheckWindowState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(D2D1SizeU pixelSize)
        {
            this.renderTarget.Resize(ref pixelSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D2D1Bitmap CreateBitmap(D2D1SizeU size, IntPtr srcData, uint pitch, D2D1BitmapProperties bitmapProperties)
        {
            this.renderTarget.CreateBitmap(size, srcData, pitch, ref bitmapProperties, out ID2D1Bitmap bitmap);

            if (bitmap == null)
            {
                return null;
            }

            return new D2D1Bitmap(bitmap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D2D1Bitmap CreateBitmap(D2D1SizeU size, byte[] srcData, uint pitch, D2D1BitmapProperties bitmapProperties)
        {
            this.renderTarget.CreateBitmap(size, srcData == null ? IntPtr.Zero : Marshal.UnsafeAddrOfPinnedArrayElement(srcData, 0), pitch, ref bitmapProperties, out ID2D1Bitmap bitmap);

            if (bitmap == null)
            {
                return null;
            }

            return new D2D1Bitmap(bitmap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D2D1Bitmap CreateBitmap(D2D1SizeU size, D2D1BitmapProperties bitmapProperties)
        {
            this.renderTarget.CreateBitmap(size, IntPtr.Zero, 0U, ref bitmapProperties, out ID2D1Bitmap bitmap);

            if (bitmap == null)
            {
                return null;
            }

            return new D2D1Bitmap(bitmap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawBitmap(D2D1Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            this.renderTarget.DrawBitmap(bitmap.GetHandle<ID2D1Bitmap>(), IntPtr.Zero, 1.0f, D2D1BitmapInterpolationMode.Linear, IntPtr.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawBitmap(D2D1Bitmap bitmap, D2D1RectF destinationRectangle, float opacity, D2D1BitmapInterpolationMode interpolationMode)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            GCHandle destinationRectangleHandle = GCHandle.Alloc(destinationRectangle, GCHandleType.Pinned);

            try
            {
                this.renderTarget.DrawBitmap(bitmap.GetHandle<ID2D1Bitmap>(), destinationRectangleHandle.AddrOfPinnedObject(), opacity, interpolationMode, IntPtr.Zero);
            } finally
            {
                destinationRectangleHandle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawBitmap(D2D1Bitmap bitmap, D2D1RectF destinationRectangle, float opacity, D2D1BitmapInterpolationMode interpolationMode, D2D1RectF sourceRectangle)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            GCHandle destinationRectangleHandle = GCHandle.Alloc(destinationRectangle, GCHandleType.Pinned);
            GCHandle sourceRectangleHandle = GCHandle.Alloc(sourceRectangle, GCHandleType.Pinned);

            try
            {
                this.renderTarget.DrawBitmap(bitmap.GetHandle<ID2D1Bitmap>(), destinationRectangleHandle.AddrOfPinnedObject(), opacity, interpolationMode, sourceRectangleHandle.AddrOfPinnedObject());
            } finally
            {
                destinationRectangleHandle.Free();
                sourceRectangleHandle.Free();
            }
        }

        public D2D1AntialiasMode AntialiasMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.renderTarget.GetAntialiasMode();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this.renderTarget.SetAntialiasMode(value);
            }
        }

        public D2D1TextAntialiasMode TextAntialiasMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.renderTarget.GetTextAntialiasMode();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this.renderTarget.SetTextAntialiasMode(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginDraw()
        {
            this.renderTarget.BeginDraw();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndDraw()
        {
            this.renderTarget.EndDraw(out _, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            this.renderTarget.Clear(IntPtr.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(D2D1ColorF clearColor)
        {
            GCHandle clearColorHandle = GCHandle.Alloc(clearColor, GCHandleType.Pinned);

            try
            {
                this.renderTarget.Clear(clearColorHandle.AddrOfPinnedObject());
            } finally
            {
                clearColorHandle.Free();
            }
        }
    }

    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    [ComImport, Guid("2cd90691-12e2-11dc-9fed-001143a055f9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ID2D1Resource
    {
        [PreserveSig]
        void GetFactory(
            [Out] out ID2D1Factory factory);
    }

    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    [ComImport, Guid("06152247-6f50-465a-9245-118bfd3b6007")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID2D1Factory
    {
        void ReloadSystemMetrics();

        [PreserveSig]
        void GetDesktopDpi(
            [Out] out float dpiX,
            [Out] out float dpiY);

        void CreateRectangleGeometry(
            [In] ref D2D1RectF rectangle,
            [Out] out IntPtr rectangleGeometry);

        void CreateRoundedRectangleGeometry(
            [In] ref IntPtr roundedRectangle,
            [Out] out IntPtr roundedRectangleGeometry);

        void CreateEllipseGeometry(
            [In] ref IntPtr ellipse,
            [Out] out IntPtr ellipseGeometry);

        void CreateGeometryGroup(
            [In] IntPtr fillMode,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] geometries,
            [In] uint geometriesCount,
            [Out] out IntPtr geometryGroup);

        void CreateTransformedGeometry(
            [In] IntPtr sourceGeometry,
            [In] ref IntPtr transform,
            [Out] out IntPtr transformedGeometry);

        void CreatePathGeometry(
            [Out] out IntPtr pathGeometry);

        void CreateStrokeStyle(
            [In] ref IntPtr strokeStyleProperties,
            [In, MarshalAs(UnmanagedType.LPArray)] float[] dashes,
            [In] uint dashesCount,
            [Out] out IntPtr strokeStyle);

        void CreateDrawingStateBlock(
            [In] IntPtr drawingStateDescription,
            [In] IntPtr textRenderingParams,
            [Out] out IntPtr drawingStateBlock);

        void CreateWicBitmapRenderTarget(
            [In] IntPtr target,
            [In] ref D2D1RenderTargetProperties renderTargetProperties,
            [Out] out IntPtr renderTarget);

        void CreateHwndRenderTarget(
            [In] ref D2D1RenderTargetProperties renderTargetProperties,
            [In] ref D2D1HwndRenderTargetProperties hwndRenderTargetProperties,
            [Out] out ID2D1HwndRenderTarget hwndRenderTarget);

        void CreateDxgiSurfaceRenderTarget(
            [In] IntPtr dxgiSurface,
            [In] ref D2D1RenderTargetProperties renderTargetProperties,
            [Out] out IntPtr renderTarget);

        void CreateDCRenderTarget(
            [In] ref D2D1RenderTargetProperties renderTargetProperties,
            [Out] out IntPtr renderTarget);
    }

    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    [ComImport, Guid("a2296057-ea42-4099-983b-539fb6505426")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID2D1Bitmap
    {
        [PreserveSig]
        void GetFactory(
            [Out] out ID2D1Factory factory);

        [PreserveSig]
        void GetSize(
            [Out] out D2D1SizeF size);

        [PreserveSig]
        void GetPixelSize(
            [Out] out D2D1SizeU size);

        [PreserveSig]
        void GetPixelFormat(
            [Out] out D2D1PixelFormat pixelFormat);

        [PreserveSig]
        void GetDpi(
            [Out] out float dpiX,
            [Out] out float dpiY);

        void CopyFromBitmap(
            [In] IntPtr destPoint,
            [In] ID2D1Bitmap bitmap,
            [In] IntPtr srcRect);

        void CopyFromRenderTarget(
            [In] IntPtr destPoint,
            [In] IntPtr renderTarget,
            [In] IntPtr srcRect);

        void CopyFromMemory(
            [In] IntPtr destRect,
            [In] IntPtr srcData,
            [In] uint pitch);
    }

    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    [ComImport, Guid("2cd90698-12e2-11dc-9fed-001143a055f9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID2D1HwndRenderTarget
    {
        [PreserveSig]
        void GetFactory(
            [Out] out ID2D1Factory factory);

        void CreateBitmap(
            [In] D2D1SizeU size,
            [In] IntPtr srcData,
            [In] uint pitch,
            [In] ref D2D1BitmapProperties bitmapProperties,
            [Out] out ID2D1Bitmap bitmap);

        void CreateBitmapFromWicBitmap(
            [In] IntPtr wicBitmapSource,
            [In] IntPtr bitmapProperties,
            [Out] out ID2D1Bitmap bitmap);

        void CreateSharedBitmap(
            [In] ref Guid riid,
            [In, Out] IntPtr data,
            [In] IntPtr bitmapProperties,
            [Out] out ID2D1Bitmap bitmap);

        void CreateBitmapBrush(
            [In] ID2D1Bitmap bitmap,
            [In] IntPtr bitmapBrushProperties,
            [In] IntPtr brushProperties,
            [Out] out IntPtr bitmapBrush);

        void CreateSolidColorBrush(
            [In] ref D2D1ColorF color,
            [In] IntPtr brushProperties,
            [Out] out IntPtr solidColorBrush);

        void CreateGradientStopCollection(
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] gradientStops,
            [In] uint gradientStopsCount,
            [In] IntPtr colorInterpolationGamma,
            [In] IntPtr extendMode,
            [Out] out IntPtr gradientStopCollection);

        void CreateLinearGradientBrush(
            [In] ref IntPtr linearGradientBrushProperties,
            [In] IntPtr brushProperties,
            [In] IntPtr gradientStopCollection,
            [Out] out IntPtr linearGradientBrush);

        void CreateRadialGradientBrush(
            [In] ref IntPtr radialGradientBrushProperties,
            [In] IntPtr brushProperties,
            [In] IntPtr gradientStopCollection,
            [Out] out IntPtr radialGradientBrush);

        void CreateCompatibleRenderTarget(
            [In] IntPtr desiredSize,
            [In] IntPtr desiredPixelSize,
            [In] IntPtr desiredFormat,
            [In] IntPtr options,
            [Out] out IntPtr bitmapRenderTarget);

        void CreateLayer(
            [In] IntPtr size,
            [Out] out IntPtr layer);

        void CreateMesh(
            [Out] out IntPtr mesh);

        [PreserveSig]
        void DrawLine(
            [In] IntPtr point0,
            [In] IntPtr point1,
            [In] IntPtr brush,
            [In] float strokeWidth,
            [In] IntPtr strokeStyle);

        [PreserveSig]
        void DrawRectangle(
            [In] ref D2D1RectF rect,
            [In] IntPtr brush,
            [In] float strokeWidth,
            [In] IntPtr strokeStyle);

        [PreserveSig]
        void FillRectangle(
            [In] ref D2D1RectF rect,
            [In] IntPtr brush);

        [PreserveSig]
        void DrawRoundedRectangle(
            [In] ref IntPtr roundedRect,
            [In] IntPtr brush,
            [In] float strokeWidth,
            [In] IntPtr strokeStyle);

        [PreserveSig]
        void FillRoundedRectangle(
            [In] ref IntPtr roundedRect,
            [In] IntPtr brush);

        [PreserveSig]
        void DrawEllipse(
            [In] ref IntPtr ellipse,
            [In] IntPtr brush,
            [In] float strokeWidth,
            [In] IntPtr strokeStyle);

        [PreserveSig]
        void FillEllipse(
            [In] ref IntPtr ellipse,
            [In] IntPtr brush);

        [PreserveSig]
        void DrawGeometry(
            [In] IntPtr geometry,
            [In] IntPtr brush,
            [In] float strokeWidth,
            [In] IntPtr strokeStyle);

        [PreserveSig]
        void FillGeometry(
            [In] IntPtr geometry,
            [In] IntPtr brush,
            [In] IntPtr opacityBrush);

        [PreserveSig]
        void FillMesh(
            [In] IntPtr mesh,
            [In] IntPtr brush);

        [PreserveSig]
        void FillOpacityMask(
            [In] ID2D1Bitmap opacityMask,
            [In] IntPtr brush,
            [In] IntPtr content,
            [In] IntPtr destinationRectangle,
            [In] IntPtr sourceRectangle);

        [PreserveSig]
        void DrawBitmap(
            [In] ID2D1Bitmap bitmap,
            [In] IntPtr destinationRectangle,
            [In] float opacity,
            [In] D2D1BitmapInterpolationMode interpolationMode,
            [In] IntPtr sourceRectangle);

        [PreserveSig]
        void DrawText(
            [In, MarshalAs(UnmanagedType.LPWStr)] string text,
            [In] uint textLength,
            [In] IntPtr textFormat,
            [In] ref D2D1RectF layoutRect,
            [In] IntPtr defaultForegroundBrush,
            [In] IntPtr options,
            [In] IntPtr measuringMode);

        [PreserveSig]
        void DrawTextLayout(
            [In] IntPtr origin,
            [In] IntPtr textLayout,
            [In] IntPtr defaultForegroundBrush,
            [In] IntPtr options);

        [PreserveSig]
        void DrawGlyphRun(
            [In] IntPtr baselineOrigin,
            [In] IntPtr glyphRun,
            [In] IntPtr foregroundBrush,
            [In] IntPtr measuringMode);

        [PreserveSig]
        void SetTransform(
            [In] ref IntPtr transform);

        [PreserveSig]
        void GetTransform(
            [Out] out IntPtr transform);

        [PreserveSig]
        void SetAntialiasMode(
            [In] D2D1AntialiasMode antialiasMode);

        [PreserveSig]
        D2D1AntialiasMode GetAntialiasMode();

        [PreserveSig]
        void SetTextAntialiasMode(
            [In] D2D1TextAntialiasMode textAntialiasMode);

        [PreserveSig]
        D2D1TextAntialiasMode GetTextAntialiasMode();

        [PreserveSig]
        void SetTextRenderingParams(
            [In] IntPtr textRenderingParams);

        [PreserveSig]
        void GetTextRenderingParams(
            [Out] out IntPtr textRenderingParams);

        [PreserveSig]
        void SetTags(
            [In] ulong tag1,
            [In] ulong tag2);

        [PreserveSig]
        void GetTags(
            [Out] out ulong tag1,
            [Out] out ulong tag2);

        [PreserveSig]
        void PushLayer(
            [In] ref IntPtr layerParameters,
            [In] IntPtr layer);

        [PreserveSig]
        void PopLayer();

        void Flush(
            [Out] out ulong tag1,
            [Out] out ulong tag2);

        [PreserveSig]
        void SaveDrawingState(
            [In, Out] IntPtr drawingStateBlock);

        [PreserveSig]
        void RestoreDrawingState(
            [In] IntPtr drawingStateBlock);

        [PreserveSig]
        void PushAxisAlignedClip(
            [In] ref D2D1RectF clipRect,
            [In] D2D1AntialiasMode antialiasMode);

        [PreserveSig]
        void PopAxisAlignedClip();

        [PreserveSig]
        void Clear(
            [In] IntPtr clearColor);

        [PreserveSig]
        void BeginDraw();

        void EndDraw(
            [Out] out ulong tag1,
            [Out] out ulong tag2);

        [PreserveSig]
        void GetPixelFormat(
            [Out] out D2D1PixelFormat pixelFormat);

        [PreserveSig]
        void SetDpi(
            [In] float dpiX,
            [In] float dpiY);

        [PreserveSig]
        void GetDpi(
            [Out] out float dpiX,
            [Out] out float dpiY);

        [PreserveSig]
        void GetSize(
            [Out] out D2D1SizeF size);

        [PreserveSig]
        void GetPixelSize(
            [Out] out D2D1SizeU size);

        [PreserveSig]
        uint GetMaximumBitmapSize();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsSupported(
            [In] ref D2D1RenderTargetProperties renderTargetProperties);

        [PreserveSig]
        D2D1WindowStates CheckWindowState();

        void Resize(
            [In] ref D2D1SizeU pixelSize);

        [PreserveSig]
        IntPtr GetHwnd();
    }

}
