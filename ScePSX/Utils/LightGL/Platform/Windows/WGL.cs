using System;
using System.Runtime.InteropServices;
using System.Security;

namespace LightGL.Windows
{
    public class Wgl
    {
        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr wglCreateContext(IntPtr hDc);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern bool wglDeleteContext(IntPtr oldContext);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr wglGetCurrentContext();

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern bool wglMakeCurrent(IntPtr hDc, IntPtr newContext);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern bool wglCopyContext(IntPtr hglrcSrc, IntPtr hglrcDst, uint mask);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern unsafe int wglChoosePixelFormat(IntPtr hDc, PixelFormatDescriptor* pPfd);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern unsafe int wglDescribePixelFormat(IntPtr hdc, int ipfd, uint cjpfd,
            PixelFormatDescriptor* ppfd);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr wglGetCurrentDC();

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr wglGetDefaultProcAddress(string lpszProc);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr wglGetProcAddress(string lpszProc);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern int wglGetPixelFormat(IntPtr hdc);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern unsafe bool wglSetPixelFormat(IntPtr hdc, int ipfd, PixelFormatDescriptor* ppfd);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern bool wglSwapBuffers(IntPtr hdc);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern bool wglShareLists(IntPtr hrcSrvShare, IntPtr hrcSrvSource);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr wglCreateLayerContext(IntPtr hDc, int level);

        //[SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true)] public extern static unsafe Boolean wglDescribeLayerPlane(IntPtr hDc, int pixelFormat, int layerPlane, UInt32 nBytes, LayerPlaneDescriptor* plpd);
        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true)]
        public static extern unsafe int wglSetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries,
            int* pcr);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true)]
        public static extern unsafe int wglGetLayerPaletteEntries(IntPtr hdc, int iLayerPlane, int iStart, int cEntries,
            int* pcr);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true)]
        public static extern bool wglRealizeLayerPalette(IntPtr hdc, int iLayerPlane, bool bRealize);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true)]
        public static extern bool wglSwapLayerBuffers(IntPtr hdc, uint fuFlags);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, CharSet = CharSet.Auto)]
        public static extern bool wglUseFontBitmapsA(IntPtr hDc, int first, int count, int listBase);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, CharSet = CharSet.Auto)]
        public static extern bool wglUseFontBitmapsW(IntPtr hDc, int first, int count, int listBase);

        //[SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, CharSet = CharSet.Auto)] public extern static unsafe Boolean wglUseFontOutlinesA(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, GlyphMetricsFloat* glyphMetrics);
        //[SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, CharSet = CharSet.Auto)] public extern static unsafe Boolean wglUseFontOutlinesW(IntPtr hDC, Int32 first, Int32 count, Int32 listBase, float thickness, float deviation, Int32 fontMode, GlyphMetricsFloat* glyphMetrics);
        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern bool wglMakeContextCurrentEXT(IntPtr hDrawDc, IntPtr hReadDc, IntPtr hglrc);

        [SuppressUnmanagedCodeSecurity, DllImport(GL.DllWindows, ExactSpelling = true, SetLastError = true)]
        public static extern unsafe bool wglChoosePixelFormatEXT(IntPtr hdc, int* piAttribIList,
            float* pfAttribFList, uint nMaxFormats, [Out] int* piFormats, [Out] uint* nNumFormats);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormatDescriptor
    {
        public short Size;
        public short Version;
        public PixelFormatDescriptorFlags Flags;
        public PixelType PixelType;
        public byte ColorBits;
        public byte RedBits;
        public byte RedShift;
        public byte GreenBits;
        public byte GreenShift;
        public byte BlueBits;
        public byte BlueShift;
        public byte AlphaBits;
        public byte AlphaShift;
        public byte AccumBits;
        public byte AccumRedBits;
        public byte AccumGreenBits;
        public byte AccumBlueBits;
        public byte AccumAlphaBits;
        public byte DepthBits;
        public byte StencilBits;
        public byte AuxBuffers;
        public byte LayerType;
        private byte Reserved;
        public int LayerMask;
        public int VisibleMask;
        public int DamageMask;
    }

    public enum PixelType : byte
    {
        RGBA = 0,
        INDEXED = 1
    }

    [Flags]
    public enum PixelFormatDescriptorFlags
    {
        // PixelFormatDescriptor flags
        Doublebuffer = 0x01,
        Stereo = 0x02,
        DrawToWindow = 0x04,
        DrawToBitmap = 0x08,
        SupportGdi = 0x10,
        SupportOpengl = 0x20,
        GenericFormat = 0x40,
        NeedPalette = 0x80,
        NeedSystemPalette = 0x100,
        SwapExchange = 0x200,
        SwapCopy = 0x400,
        SwapLayerBuffers = 0x800,
        GenericAccelerated = 0x1000,
        SupportDirectdraw = 0x2000,
        SupportComposition = 0x8000,

        // PixelFormatDescriptor flags for use in ChoosePixelFormat only
        DepthDontcare = 0x20000000,
        DoublebufferDontcare = 0x40000000,
        StereoDontcare = unchecked((int)0x80000000)
    }

    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public unsafe struct Bitmap
    {
        public uint BmType;
        public uint BmWidth;
        public uint BmHeight;
        public uint BmWidthBytes;
        public uint BmPlanes;
        public uint BmBitsPixel;
        public void* BmBits;
    }

    [Flags]
    public enum WindowStyle : uint
    {
        Overlapped = 0x00000000,
        Popup = 0x80000000,
        Child = 0x40000000,
        Minimize = 0x20000000,
        Visible = 0x10000000,
        Disabled = 0x08000000,
        ClipSiblings = 0x04000000,
        ClipChildren = 0x02000000,
        Maximize = 0x01000000,
        Caption = 0x00C00000, // Border | DialogFrame
        Border = 0x00800000,
        DialogFrame = 0x00400000,
        VScroll = 0x00200000,
        HScreen = 0x00100000,
        SystemMenu = 0x00080000,
        ThickFrame = 0x00040000,
        Group = 0x00020000,
        TabStop = 0x00010000,

        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000,

        Tiled = Overlapped,
        Iconic = Minimize,
        SizeBox = ThickFrame,
        TiledWindow = OverlappedWindow,

        // Common window styles:
        OverlappedWindow = Overlapped | Caption | SystemMenu | ThickFrame | MinimizeBox | MaximizeBox,
        PopupWindow = Popup | Border | SystemMenu,
        ChildWindow = Child
    }

    [Flags]
    public enum ExtendedWindowStyle : uint
    {
        DialogModalFrame = 0x00000001,
        NoParentNotify = 0x00000004,
        Topmost = 0x00000008,
        AcceptFiles = 0x00000010,
        Transparent = 0x00000020,

        // #if(WINVER >= 0x0400)
        MdiChild = 0x00000040,
        ToolWindow = 0x00000080,
        WindowEdge = 0x00000100,
        ClientEdge = 0x00000200,
        ContextHelp = 0x00000400,
        // #endif

        // #if(WINVER >= 0x0400)
        Right = 0x00001000,
        Left = 0x00000000,
        RightToLeftReading = 0x00002000,
        LeftToRightReading = 0x00000000,
        LeftScrollbar = 0x00004000,
        RightScrollbar = 0x00000000,

        ControlParent = 0x00010000,
        StaticEdge = 0x00020000,
        ApplicationWindow = 0x00040000,

        OverlappedWindow = WindowEdge | ClientEdge,
        PaletteWindow = WindowEdge | ToolWindow | Topmost,
        // #endif

        // #if(_WIN32_WINNT >= 0x0500)
        Layered = 0x00080000,
        // #endif

        // #if(WINVER >= 0x0500)
        NoInheritLayout = 0x00100000, // Disable inheritence of mirroring by children
        RightToLeftLayout = 0x00400000, // Right to left mirroring
        // #endif /* WINVER >= 0x0500 */

        // #if(_WIN32_WINNT >= 0x0501)
        Composited = 0x02000000,
        // #endif /* _WIN32_WINNT >= 0x0501 */

        // #if(_WIN32_WINNT >= 0x0500)
        NoActivate = 0x08000000

        // #endif /* _WIN32_WINNT >= 0x0500 */
    }

    public enum WindowMessage : uint
    {
    }

    public enum CursorName
    {
        Arrow = 32512
    }

    [Flags]
    public enum ClassStyle
    {
        //None            = 0x0000,
        VRedraw = 0x0001,
        HRedraw = 0x0002,
        DoubleClicks = 0x0008,
        OwnDc = 0x0020,
        ClassDc = 0x0040,
        ParentDc = 0x0080,
        NoClose = 0x0200,
        SaveBits = 0x0800,
        ByteAlignClient = 0x1000,
        ByteAlignWindow = 0x2000,
        GlobalClass = 0x4000,

        Ime = 0x00010000,

        // #if(_WIN32_WINNT >= 0x0501)
        DropShadow = 0x00020000

        // #endif /* _WIN32_WINNT >= 0x0501 */
    }

    public delegate IntPtr WindowProcedure(IntPtr handle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct ExtendedWindowClass
    {
        public uint Size;

        public ClassStyle Style;

        //public WNDPROC WndProc;
        [MarshalAs(UnmanagedType.FunctionPtr)] public WindowProcedure WndProc;

        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public IntPtr MenuName;
        public IntPtr ClassName;
        public IntPtr IconSm;

        public static readonly uint SizeInBytes = (uint)Marshal.SizeOf(default(ExtendedWindowClass));
    }

    public enum WindowClass : uint
    {
        Alert = 1, /* "I need your attention now."*/

        MovableAlert =
            2, /* "I need your attention now, but I'm kind enough to let you switch out of this app to do other things."*/
        Modal = 3, /* system modal, not draggable*/
        MovableModal = 4, /* application modal, draggable*/
        Floating = 5, /* floats above all other application windows*/
        Document = 6, /* document windows*/
        Desktop = 7, /* desktop window (usually only one of these exists) - OS X only in CarbonLib 1.0*/
        Utility = 8, /* Available in CarbonLib 1.1 and later, and in Mac OS X*/
        Help = 10, /* Available in CarbonLib 1.1 and later, and in Mac OS X*/
        Sheet = 11, /* Available in CarbonLib 1.3 and later, and in Mac OS X*/
        Toolbar = 12, /* Available in CarbonLib 1.1 and later, and in Mac OS X*/
        Plain = 13, /* Available in CarbonLib 1.2.5 and later, and Mac OS X*/
        Overlay = 14, /* Available in Mac OS X*/
        SheetAlert = 15, /* Available in CarbonLib 1.3 and later, and in Mac OS X 10.1 and later*/
        AltPlain = 16, /* Available in CarbonLib 1.3 and later, and in Mac OS X 10.1 and later*/
        Drawer = 20, /* Available in Mac OS X 10.2 or later*/
        All = 0xFFFFFFFFu /* for use with GetFrontWindowOfClass, FindWindowOfClass, GetNextWindowOfClass*/
    }
}
