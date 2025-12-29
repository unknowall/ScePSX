using System;
using System.Runtime.InteropServices;

using SDL2;

namespace ScePSX.GL.Mac
{
    public class MacGLContext : IGlContext
    {
        private IntPtr window;
        private IntPtr context;
        private bool releaseWindow;

        public MacGLContext(IntPtr window, bool releaseWindow)
        {
            this.window = window;
            this.context = SDL.SDL_GL_CreateContext(window);
            this.releaseWindow = releaseWindow;
        }

        public static MacGLContext FromWindowHandle(IntPtr windowHandle)
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            if (windowHandle == IntPtr.Zero)
            {
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 16);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
                SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY);

                windowHandle = SDL.SDL_CreateWindow("OpenGL", 0, 0, 512, 512, SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
                return new MacGLContext(windowHandle, true);
            }
            else
            {
                return new MacGLContext(windowHandle, false);
            }
        }

        public GlContextSize Size
        {
            get
            {
                int w, h;
                SDL.SDL_GetWindowSize(window, out w, out h);
                return new GlContextSize { Width = w, Height = h };
            }
        }

        public void Dispose()
        {
            SDL.SDL_GL_DeleteContext(context);
            if (releaseWindow)
            {
                SDL.SDL_DestroyWindow(window);
            }
        }

        public IGlContext MakeCurrent()
        {
            //Console.WriteLine($"Window window={window} context={context}");
            SDL.SDL_GL_MakeCurrent(window, context);
            return this;
        }

        public IGlContext ReleaseCurrent()
        {
            SDL.SDL_GL_MakeCurrent(window, IntPtr.Zero);
            return this;
        }

        public IGlContext SwapBuffers()
        {
            SDL.SDL_GL_SwapWindow(window);
            return this;
        }


        //private static readonly IntPtr NSOpenGLContext = Class.Get("NSOpenGLContext");
        //private static readonly IntPtr selCurrentContext = Selector.Get("currentContext");
        //private static readonly IntPtr selFlushBuffer = Selector.Get("flushBuffer");
        //private static readonly IntPtr selMakeCurrentContext = Selector.Get("makeCurrentContext");
        //private static readonly IntPtr selUpdate = Selector.Get("update");
    }

    internal static class Selector
    {
        // Frequently used selectors
        public static readonly IntPtr Init = Selector.Get("init");
        public static readonly IntPtr InitWithCoder = Selector.Get("initWithCoder:");
        public static readonly IntPtr Alloc = Selector.Get("alloc");
        public static readonly IntPtr Retain = Selector.Get("retain");
        public static readonly IntPtr Release = Selector.Get("release");
        public static readonly IntPtr Autorelease = Selector.Get("autorelease");

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        public extern static IntPtr Get(string name);
    }

    internal static class Class
    {
        internal const string LibObjC = "/usr/lib/libobjc.dylib";

        public static readonly IntPtr NSAutoreleasePool = Get("NSAutoreleasePool");
        public static readonly IntPtr NSDictionary = Get("NSDictionary");
        public static readonly IntPtr NSNumber = Get("NSNumber");
        public static readonly IntPtr NSUserDefaults = Get("NSUserDefaults");

        [DllImport(LibObjC)]
        private extern static IntPtr class_getName(IntPtr handle);

        [DllImport(LibObjC)]
        private extern static bool class_addMethod(IntPtr classHandle, IntPtr selector, IntPtr method, string types);

        [DllImport(LibObjC)]
        private extern static IntPtr objc_getClass(string name);

        [DllImport(LibObjC)]
        private extern static IntPtr objc_allocateClassPair(IntPtr parentClass, string name, int extraBytes);

        [DllImport(LibObjC)]
        private extern static void objc_registerClassPair(IntPtr classToRegister);

        [DllImport(LibObjC)]
        private extern static void objc_disposeClassPair(IntPtr cls);

        public static IntPtr Get(string name)
        {
            var id = objc_getClass(name);
            if (id == IntPtr.Zero)
            {
                throw new ArgumentException("Unknown class: " + name);
            }
            return id;
        }

        public static IntPtr AllocateClass(string className, string parentClass)
        {
            return objc_allocateClassPair(Get(parentClass), className, 0);
        }

        public static void RegisterClass(IntPtr handle)
        {
            objc_registerClassPair(handle);
        }

        public static void DisposeClass(IntPtr handle)
        {
            objc_disposeClassPair(handle);
        }

        public static void RegisterMethod(IntPtr handle, Delegate d, string selector, string typeString)
        {
            // TypeString info:
            // https://developer.apple.com/library/mac/documentation/Cocoa/Conceptual/ObjCRuntimeGuide/Articles/ocrtTypeEncodings.html

            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
            bool r = class_addMethod(handle, Selector.Get(selector), p, typeString);

            if (!r)
            {
                throw new ArgumentException("Could not register method " + d + " in class + " + class_getName(handle));
            }
        }
    }
}
