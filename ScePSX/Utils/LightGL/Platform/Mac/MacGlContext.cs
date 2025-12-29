using System;
using System.Runtime.InteropServices;

namespace LightGL.Mac
{
    public class MacGLContext : IGlContext
    {
        private IntPtr _nsWindow;
        private IntPtr _glContext;
        private IntPtr _pixelFormat;
        private bool _releaseWindow;

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, int arg1, int arg2, int arg3, int arg4);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_size(IntPtr receiver, IntPtr selector, out int width, out int height);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr objc_msgSend_NSWindow_Init(
            IntPtr receiver,
            IntPtr selector,
            IntPtr contentRect,       // NSRect*
            nuint styleMask,          // NSUInteger
            nint backingType,         // NSBackingStoreType
            [MarshalAs(UnmanagedType.U1)] bool defer  // BOOL (macOS bool/char)
        );

        public MacGLContext(IntPtr window, bool releaseWindow)
        {
            _nsWindow = window;
            _releaseWindow = releaseWindow;

            _pixelFormat = CreatePixelFormat();

            if (_pixelFormat != IntPtr.Zero)
            {
                var selInitWithFormat = Selector.Get("initWithFormat:shareContext:");
                var clsOpenGLContext = Class.Get("NSOpenGLContext");
                var allocSel = Selector.Get("alloc");

                _glContext = objc_msgSend(clsOpenGLContext, allocSel);
                _glContext = objc_msgSend(_glContext, selInitWithFormat, _pixelFormat, IntPtr.Zero);

                if (_glContext != IntPtr.Zero && _nsWindow != IntPtr.Zero)
                {
                    var selSetView = Selector.Get("setView:");
                    var selContentView = Selector.Get("contentView");
                    IntPtr contentView = objc_msgSend(_nsWindow, selContentView);
                    objc_msgSend_void(_glContext, selSetView, contentView);
                }
            }
        }

        public static MacGLContext FromWindowHandle(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                windowHandle = CreateHiddenNSWindow(512, 512);
                return new MacGLContext(windowHandle, true);
            } else
            {
                return new MacGLContext(windowHandle, false);
            }
        }

        private IntPtr CreatePixelFormat()
        {
            int[] attrs = new int[]
            {
                5, 8,    // NSOpenGLPFARedSize = 5
                6, 8,    // NSOpenGLPFAGreenSize = 6
                7, 8,    // NSOpenGLPFABlueSize = 7
                16, 8,   // NSOpenGLPFAStencilSize = 16
                17, 16,  // NSOpenGLPFADepthSize = 17
                19, 1,   // NSOpenGLPFADoubleBuffer = 19
                0
            };

            GCHandle handle = GCHandle.Alloc(attrs, GCHandleType.Pinned);
            IntPtr attrPtr = handle.AddrOfPinnedObject();

            try
            {
                var clsPixelFormat = Class.Get("NSOpenGLPixelFormat");
                var allocSel = Selector.Get("alloc");
                var selInit = Selector.Get("initWithAttributes:");

                IntPtr pixelFormat = objc_msgSend(clsPixelFormat, allocSel);
                return objc_msgSend(pixelFormat, selInit, attrPtr);
            } finally
            {
                handle.Free();
            }
        }

        private static IntPtr CreateHiddenNSWindow(int width, int height)
        {
            var clsNSWindow = Class.Get("NSWindow");
            var clsNSRect = Class.Get("NSRect");
            var allocSel = Selector.Get("alloc");
            var selInit = Selector.Get("initWithContentRect:styleMask:backing:defer:");
            var selSetHidesOnDeactivate = Selector.Get("setHidesOnDeactivate:");
            var selSetIsVisible = Selector.Get("setIsVisible:");

            // macOS 原点在左下角
            var rect = new NSRect { x = 0, y = 0, width = width, height = height };
            IntPtr rectPtr = Marshal.AllocHGlobal(Marshal.SizeOf(rect));
            Marshal.StructureToPtr(rect, rectPtr, false);

            try
            {
                // styleMask = 0 (无样式), backing = 2 (NSBackingStoreBuffered), defer = false
                IntPtr window = objc_msgSend(clsNSWindow, allocSel);
                window = objc_msgSend_NSWindow_Init(
                    window,
                    selInit,
                    rectPtr,
                    0,       // NSWindowStyleMaskBorderless = 0
                    2,       // NSBackingStoreBuffered  = 2
                    false    // defer = NO
                );

                objc_msgSend_void(window, selSetIsVisible, (IntPtr)0);
                objc_msgSend_void(window, selSetHidesOnDeactivate, (IntPtr)1);

                return window;
            } finally
            {
                Marshal.FreeHGlobal(rectPtr);
            }
        }

        public GlContextSize Size
        {
            get
            {
                if (_nsWindow == IntPtr.Zero)
                    return new GlContextSize { Width = 0, Height = 0 };

                var selContentView = Selector.Get("contentView");
                var selFrame = Selector.Get("frame");
                var selGetWidthHeight = Selector.Get("size");

                IntPtr contentView = objc_msgSend(_nsWindow, selContentView);
                IntPtr frame = objc_msgSend(contentView, selFrame);

                int width = 0, height = 0;
                objc_msgSend_size(frame, selGetWidthHeight, out width, out height);

                return new GlContextSize { Width = width, Height = height };
            }
        }

        public void Dispose()
        {
            if (_glContext != IntPtr.Zero)
            {
                var selClearCurrent = Selector.Get("clearCurrentContext");
                objc_msgSend_void(_glContext, selClearCurrent);

                var selRelease = Selector.Get("release");
                objc_msgSend_void(_glContext, selRelease);
                _glContext = IntPtr.Zero;
            }

            if (_pixelFormat != IntPtr.Zero)
            {
                var selRelease = Selector.Get("release");
                objc_msgSend_void(_pixelFormat, selRelease);
                _pixelFormat = IntPtr.Zero;
            }

            if (_releaseWindow && _nsWindow != IntPtr.Zero)
            {
                var selClose = Selector.Get("close");
                var selRelease = Selector.Get("release");

                objc_msgSend_void(_nsWindow, selClose);
                objc_msgSend_void(_nsWindow, selRelease);
                _nsWindow = IntPtr.Zero;
            }
        }

        public IGlContext MakeCurrent()
        {
            if (_glContext != IntPtr.Zero)
            {
                var selMakeCurrentContext = Selector.Get("makeCurrentContext");
                objc_msgSend_void(_glContext, selMakeCurrentContext);
            }
            return this;
        }

        public IGlContext ReleaseCurrent()
        {
            if (_glContext != IntPtr.Zero)
            {
                var selClearCurrent = Selector.Get("clearCurrentContext");
                objc_msgSend_void(_glContext, selClearCurrent);
            }
            return this;
        }

        public IGlContext SwapBuffers()
        {
            if (_glContext != IntPtr.Zero)
            {
                var selFlushBuffer = Selector.Get("flushBuffer");
                objc_msgSend_void(_glContext, selFlushBuffer);
            }
            return this;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NSRect
        {
            public double x;
            public double y;
            public double width;
            public double height;
        }
    }

    internal static class Selector
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        public extern static IntPtr Get(string name);
    }

    internal static class Class
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        private extern static IntPtr objc_getClass(string name);

        public static IntPtr Get(string name)
        {
            var handle = objc_getClass(name);
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException($"Can't found Objective-C: {name}");
            }
            return handle;
        }
    }
}
