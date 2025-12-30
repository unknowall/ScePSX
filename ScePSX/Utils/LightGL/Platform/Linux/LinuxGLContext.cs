using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static LightGL.GlContextFactory;

namespace LightGL.Linux
{
    public unsafe class LinuxGlContext : IGlContext
    {
        private static object Lock = new object();

        public static IntPtr DefaultDisplay;

        public static int DefaultScreen;

        //static public IntPtr DefaultRootWindow;
        private static IntPtr _sharedContext;

        private IntPtr Display;
        private int _screen;
        private IntPtr WindowHandle;
        private IntPtr Context;

        [DllImport("libX11")]
        public static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11")]
        public static extern int XDefaultScreen(IntPtr display);

        [DllImport("libX11")]
        public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height,
            int borderWidth, int depth, int xclass, IntPtr visual, IntPtr valuemask,
            ref XSetWindowAttributes attributes);

        [DllImport("libX11")]
        public static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width,
            int height, int borderWidth, int border, int background);

        [DllImport("libX11")]
        public static extern IntPtr XRootWindow(IntPtr display, int screenNumber);

        [DllImport("libX11")]
        public static extern IntPtr XCreateColormap(IntPtr display, IntPtr window, IntPtr visual, int alloc);

        [DllImport("libX11")]
        public static extern void XMapWindow(IntPtr display, IntPtr window);

        [DllImport("libX11")]
        public static extern int XInitThreads();

        [DllImport("libX11")]
        public static extern IntPtr XGetVisualInfo(IntPtr display, IntPtr vinfoMask, ref XVisualInfo template, out int nitems);

        public static IGlContext FromWindowHandle(IntPtr windowHandle, int Major, int Minor, GlProfile arbProfile)
        {
            lock (Lock)
                return new LinuxGlContext(windowHandle, Major, Minor, arbProfile);
        }

        static LinuxGlContext() => XInitThreads();

        private LinuxGlContext(IntPtr windowHandle, int Major, int Minor, GlProfile arbProfile)
        {
            Console.WriteLine("InitialWindowHandle:{0:X8}", new UIntPtr(windowHandle.ToPointer()).ToUInt64());
            const int width = 128;
            const int height = 128;
            int fbcount;
            var visualAttributes = new List<int>();
#if false
			visualAttributes.AddRange(new[] { (int)GLXAttribute.RGBA });
			visualAttributes.AddRange(new[] { (int)GLXAttribute.RED_SIZE, 1 });
			visualAttributes.AddRange(new[] { (int)GLXAttribute.GREEN_SIZE, 1 });
			visualAttributes.AddRange(new[] { (int)GLXAttribute.BLUE_SIZE, 1 });
			visualAttributes.AddRange(new[] { (int)GLXAttribute.DOUBLEBUFFER, 1 });
			visualAttributes.AddRange(new[] { (int)0, 0 });
#else
            visualAttributes.AddRange(new[] { (int)GLXAttribute.DRAWABLE_TYPE, 1 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.RENDER_TYPE, 1 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.RED_SIZE, 8 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.GREEN_SIZE, 8 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.BLUE_SIZE, 8 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.ALPHA_SIZE, 8 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.DEPTH_SIZE, 24 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.STENCIL_SIZE, 8 });
            visualAttributes.AddRange(new[] { (int)GLXAttribute.DOUBLEBUFFER, 1 });
            visualAttributes.AddRange(new[] { 0, 0 });
#endif
            Console.WriteLine("++++++++++++++++++++++++");

            if (DefaultDisplay == IntPtr.Zero)
            {
                DefaultDisplay = XOpenDisplay(IntPtr.Zero);
                DefaultScreen = XDefaultScreen(DefaultDisplay);
                //DefaultRootWindow = XRootWindow(Display, DefaultScreen);
                //DefaultRootWindow = IntPtr.Zero;
            }

            Display = DefaultDisplay;
            _screen = DefaultScreen;

            //Console.WriteLine("{0}", GLX.ChooseVisual(Display, Screen, visualAttributes.ToArray()));

            Console.WriteLine("------------------------");
#if false
			var visinfo = (XVisualInfo*)GLX.ChooseVisual(Display, Screen, visualAttributes.ToArray()).ToPointer();
			//Console.WriteLine("------------------------");

			Console.WriteLine("++++++++++++++++++++++++");
#else
            var fbconfigs = GLX.glXChooseFBConfig(Display, _screen, visualAttributes.ToArray(), out fbcount);
            var visinfo = GLX.glXGetVisualFromFBConfig(Display, *fbconfigs);
#endif
            var root = XRootWindow(Display, visinfo->Screen);

            Console.WriteLine("++++++++++++++++++++++++");

            var info = *visinfo;
            //Console.WriteLine(info.VisualID);
            //info = default(XVisualInfo);
            //info.VisualID = new IntPtr(33); int nitems; XGetVisualInfo(Display, (IntPtr)XVisualInfoMask.ID, ref info, out nitems);

            if (windowHandle == IntPtr.Zero)
            {
                //var attr = default(XSetWindowAttributes);
                ///* window attributes */
                //attr.background_pixel = 0;
                //attr.border_pixel = 0;
                //attr.colormap = XCreateColormap(dpy, root, visinfo->visual, AllocNone);
                //attr.event_mask = StructureNotifyMask | ExposureMask | KeyPressMask;
                //mask = CWBackPixel | CWBorderPixel | CWColormap | CWEventMask;
                //
                //win = XCreateWindow(dpy, root, 0, 0, width, height,
                //			 0, visinfo->depth, InputOutput,
                //			 visinfo->visual, mask, &attr);

                var attr = default(XSetWindowAttributes);
                attr.background_pixel = IntPtr.Zero;
                attr.border_pixel = IntPtr.Zero;
                attr.colormap = XCreateColormap(Display, root, info.Visual, 0);
                attr.event_mask = (IntPtr)(EventMask.StructureNotifyMask | EventMask.ExposureMask | EventMask.KeyPressMask);
                //var mask = (IntPtr)(CreateWindowMask.CWBackPixel | CreateWindowMask.CWBorderPixel | CreateWindowMask.CWColormap | CreateWindowMask.CWEventMask);

                uint mask = (uint)SetWindowValuemask.ColorMap | (uint)SetWindowValuemask.EventMask |
                            (uint)SetWindowValuemask.BackPixel | (uint)SetWindowValuemask.BorderPixel;

                Console.WriteLine("{0}, {1}", info.Visual, info.VisualID);

                windowHandle = XCreateWindow(
                    Display,
                    root,
                    0, 0, width, height,
                    0,
                    info.Depth, (int)XWindowClass.InputOutput,
                    info.Visual, (IntPtr)mask, ref attr
                );
            }

            this.WindowHandle = windowHandle;

            //XMapWindow(Display, WindowHandle);


            //var Info = (XVisualInfo*)GLX.ChooseVisual(Display, Screen, visualAttributes.ToArray()).ToPointer();

            //GLX.glXCreateContextAttribsARB(
            Context = GLX.glXCreateContext(Display, &info, _sharedContext, false);
            GL.CheckError();


            var CreateContextAttribsARB = (CreateContextAttribsARB)Marshal.GetDelegateForFunctionPointer(GLX.glXGetProcAddress("glXCreateContextAttribsARB"), typeof(CreateContextAttribsARB));

            List<int> attributes = new List<int>();
            attributes.AddRange(new int[] { (int)ArbCreateContext.MajorVersion, Major });
            attributes.AddRange(new int[] { (int)ArbCreateContext.MinorVersion, Minor });
            attributes.AddRange(new int[] { (int)ArbCreateContext.ProfileMask,
                arbProfile == GlProfile.Compatibility ? (int)ArbCreateContext.CompatibilityProfileBit : (int)ArbCreateContext.CoreProfileBit
            });

            fixed (int* attributesPtr = attributes.ToArray())
            {
                var fbconfigs2 = GLX.glXChooseFBConfig(Display, _screen, new int[] {
                        (int)GLXAttribute.VISUAL_ID, (int)visinfo->VisualID,
                        0, 0,
                    }, out fbcount);

                Console.WriteLine("{0}", new IntPtr(fbconfigs2));
                Console.WriteLine("{0}", *fbconfigs2);

                GLX.glXDestroyContext(Display, this.Context);
                this.Context = CreateContextAttribsARB(Display, *fbconfigs, _sharedContext, false, attributesPtr);
            }

            if (_sharedContext == IntPtr.Zero)
            {
                _sharedContext = Context;
            }

            MakeCurrent();

            Console.WriteLine("VisualID:{0}", info.VisualID);
            Console.WriteLine("Display:{0:X8}", new UIntPtr(Display.ToPointer()).ToUInt64());
            Console.WriteLine("WindowHandle:{0:X8}", new UIntPtr(windowHandle.ToPointer()).ToUInt64());
            Console.WriteLine("Context:{0:X8}", new UIntPtr(Context.ToPointer()).ToUInt64());
            Console.WriteLine("Version:{0}", GL.GetString(GL.GL_VERSION));
            Console.WriteLine("Version:{0}.{1}", GL.GetInteger(GL.GL_MAJOR_VERSION), GL.GetInteger(GL.GL_MINOR_VERSION));
            Console.WriteLine("Vendor:{0}", GL.GetString(GL.GL_VENDOR));
            Console.WriteLine("Renderer:{0}", GL.GetString(GL.GL_RENDERER));
        }

        public delegate IntPtr CreateContextAttribsARB(IntPtr display, IntPtr fbconfig, IntPtr shareContext, bool direct, int* attribs);

        public GlContextSize Size => new GlContextSize { Width = 0, Height = 0 };

        public IGlContext MakeCurrent()
        {
            if (GLX.glXMakeCurrent(Display, WindowHandle, Context))
                return this;
            GL.CheckError();
            Console.WriteLine("glXMakeCurrent failed");
            return this;
        }

        public IGlContext ReleaseCurrent()
        {
            GLX.glXMakeCurrent(Display, IntPtr.Zero, IntPtr.Zero);
            GL.CheckError();
            return this;
        }

        public IGlContext SwapBuffers()
        {
            GLX.glXSwapBuffers(Display, WindowHandle);
            return this;
        }

        public void Dispose() => GLX.glXDestroyContext(Display, Context);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XSetWindowAttributes
    {
        public IntPtr BackgroundPixmap;
        public IntPtr background_pixel;
        public IntPtr border_pixmap;
        public IntPtr border_pixel;
        public Gravity bit_gravity;
        public Gravity win_gravity;
        public int backing_store;
        public IntPtr backing_planes;
        public IntPtr backing_pixel;
        public bool save_under;
        public IntPtr event_mask;
        public IntPtr do_not_propagate_mask;
        public bool override_redirect;
        public IntPtr colormap;
        public IntPtr cursor;
    }

    public enum Gravity
    {
        ForgetGravity = 0,
        NorthWestGravity = 1,
        NorthGravity = 2,
        NorthEastGravity = 3,
        WestGravity = 4,
        CenterGravity = 5,
        EastGravity = 6,
        SouthWestGravity = 7,
        SouthGravity = 8,
        SouthEastGravity = 9,
        StaticGravity = 10
    }

    internal enum XWindowClass
    {
        InputOutput = 1,
        InputOnly = 2
    }

    [Flags]
    public enum EventMask
    {
        NoEventMask = 0,
        KeyPressMask = 1 << 0,
        KeyReleaseMask = 1 << 1,
        ButtonPressMask = 1 << 2,
        ButtonReleaseMask = 1 << 3,
        EnterWindowMask = 1 << 4,
        LeaveWindowMask = 1 << 5,
        PointerMotionMask = 1 << 6,
        PointerMotionHintMask = 1 << 7,
        Button1MotionMask = 1 << 8,
        Button2MotionMask = 1 << 9,
        Button3MotionMask = 1 << 10,
        Button4MotionMask = 1 << 11,
        Button5MotionMask = 1 << 12,
        ButtonMotionMask = 1 << 13,
        KeymapStateMask = 1 << 14,
        ExposureMask = 1 << 15,
        VisibilityChangeMask = 1 << 16,
        StructureNotifyMask = 1 << 17,
        ResizeRedirectMask = 1 << 18,
        SubstructureNotifyMask = 1 << 19,
        SubstructureRedirectMask = 1 << 20,
        FocusChangeMask = 1 << 21,
        PropertyChangeMask = 1 << 22,
        ColormapChangeMask = 1 << 23,
        OwnerGrabButtonMask = 1 << 24
    }

    [Flags]
    internal enum CreateWindowMask : long //: ulong
    {
        CwBackPixmap = 1L << 0,
        CwBackPixel = 1L << 1,
        CwSaveUnder = 1L << 10,
        CwEventMask = 1L << 11,
        CwDontPropagate = 1L << 12,
        CwColormap = 1L << 13,
        CwCursor = 1L << 14,
        CwBorderPixmap = 1L << 2,
        CwBorderPixel = 1L << 3,
        CwBitGravity = 1L << 4,
        CwWinGravity = 1L << 5,
        CwBackingStore = 1L << 6,
        CwBackingPlanes = 1L << 7,
        CwBackingPixel = 1L << 8,
        CwOverrideRedirect = 1L << 9

        //CWY    = (1<<1),
        //CWWidth    = (1<<2),
        //CWHeight    = (1<<3),
        //CWBorderWidth    = (1<<4),
        //CWSibling    = (1<<5),
        //CWStackMode    = (1<<6),
    }

    [Flags]
    internal enum SetWindowValuemask
    {
        Nothing = 0,
        BackPixmap = 1,
        BackPixel = 2,
        BorderPixmap = 4,
        BorderPixel = 8,
        BitGravity = 16,
        WinGravity = 32,
        BackingStore = 64,
        BackingPlanes = 128,
        BackingPixel = 256,
        OverrideRedirect = 512,
        SaveUnder = 1024,
        EventMask = 2048,
        DontPropagate = 4096,
        ColorMap = 8192,
        Cursor = 16384
    }

    [Flags]
    internal enum XVisualInfoMask
    {
        No = 0x0,
        ID = 0x1,
        Screen = 0x2,
        Depth = 0x4,
        Class = 0x8,
        Red = 0x10,
        Green = 0x20,
        Blue = 0x40,
        ColormapSize = 0x80,
        BitsPerRGB = 0x100,
        All = 0x1FF
    }
}
