using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static LightGL.GlContextFactory;

namespace LightGL.Linux
{
    public unsafe class WaylandGlContext : IGlContext
    {
        private static object Lock = new object();

        public static IntPtr DefaultEglDisplay;

        private static IntPtr wlDisplay;
        private static IntPtr Registry;
        private static IntPtr Compositor;
        private static IntPtr SharedContext;
        private static readonly object SharedLock = new object();
        private static int SharedRefCount = 0;
        private static bool DisplayCreated = false;

        private IntPtr Surface;
        private IntPtr EglWindow;
        private IntPtr Context;
        private IntPtr EglDisplay;
        private IntPtr EglConfig;
        private IntPtr EglSurface;

        private const uint WL_COMPOSITOR_INTERFACE_VERSION = 4;
        private const uint WL_COMPOSITOR_SINCE_VERSION = 1;
        private const int WL_REGISTRY_GLOBAL = 0;
        private const int WL_REGISTRY_GLOBAL_REMOVE = 1;

        [DllImport("libwayland-client.so.0")]
        public static extern IntPtr wl_display_connect(IntPtr name);

        [DllImport("libwayland-client.so.0")]
        public static extern int wl_display_roundtrip(IntPtr display);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_proxy_marshal_constructor", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr wl_proxy_marshal_constructor(IntPtr proxy, uint opcode, IntPtr interface_ptr, IntPtr version);

        [DllImport("libwayland-client.so.0", EntryPoint = "wl_registry_interface", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetWlRegistryInterface();

        [DllImport("libwayland-client.so.0")]
        public static extern void wl_registry_add_listener(IntPtr registry, IntPtr listener, IntPtr data);

        [DllImport("libwayland-client.so.0")]
        public static extern void wl_proxy_add_listener(IntPtr proxy, IntPtr listener, IntPtr data);

        [DllImport("libwayland-client.so.0")]
        public static extern IntPtr wl_registry_bind(IntPtr registry, uint name, IntPtr Interface, uint version);

        [DllImport("libwayland-client.so.0")]
        public static extern IntPtr wl_compositor_create_surface(IntPtr compositor);

        [DllImport("libwayland-egl.so.1")]
        public static extern IntPtr wl_egl_window_create(IntPtr surface, int width, int height);

        [DllImport("libwayland-egl.so.1")]
        public static extern void wl_egl_window_destroy(IntPtr eglWindow);

        [DllImport("libwayland-egl.so.1")]
        public static extern void wl_egl_window_resize(IntPtr eglWindow, int width, int height, int dx, int dy);

        [DllImport("libEGL.so.1")]
        public static extern IntPtr eglGetDisplay(IntPtr nativeDisplay);

        [DllImport("libEGL.so.1")]
        public static extern bool eglInitialize(IntPtr display, int* major, int* minor);

        [DllImport("libEGL.so.1")]
        public static extern bool eglChooseConfig(IntPtr display, int* attribList, IntPtr* configs, int configSize, int* numConfig);

        [DllImport("libEGL.so.1")]
        public static extern IntPtr eglCreateContext(IntPtr display, IntPtr config, IntPtr shareContext, int* attribList);

        [DllImport("libEGL.so.1")]
        public static extern IntPtr eglCreateWindowSurface(IntPtr display, IntPtr config, IntPtr nativeWindow, int* attribList);

        [DllImport("libEGL.so.1")]
        public static extern bool eglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);

        [DllImport("libEGL.so.1")]
        public static extern bool eglSwapBuffers(IntPtr display, IntPtr surface);

        [DllImport("libEGL.so.1")]
        public static extern IntPtr eglGetProcAddress(IntPtr procname);

        [DllImport("libEGL.so.1")]
        public static extern bool eglGetConfigAttrib(IntPtr display, IntPtr config, int attribute, int* value);

        [DllImport("libEGL.so.1")]
        public static extern bool eglDestroyContext(IntPtr display, IntPtr context);

        [DllImport("libEGL.so.1")]
        public static extern bool eglDestroySurface(IntPtr display, IntPtr surface);

        [DllImport("libEGL.so.1")]
        public static extern bool eglTerminate(IntPtr display);

        [DllImport("libEGL.so.1")]
        public static extern bool eglQuerySurface(IntPtr display, IntPtr surface, int attribute, int* value);

        [DllImport("libEGL.so.1")]
        public static extern bool eglSwapInterval(IntPtr display, int interval);

        [DllImport("libEGL.so.1")]
        public static extern int eglGetError();

        [DllImport("libdl")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl")]
        private static extern int dlclose(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct WlRegistryListener
        {
            public IntPtr global;
            public IntPtr global_remove;
        }

        private static WlRegistryGlobalDelegate _globalDelegate;
        private static WlRegistryGlobalRemoveDelegate _globalRemoveDelegate;
        private static WlRegistryListener _registryListener;
        private static GCHandle _listenerHandle;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WlRegistryGlobalDelegate(IntPtr data, IntPtr registry, uint name, IntPtr Interface, uint version);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WlRegistryGlobalRemoveDelegate(IntPtr data, IntPtr registry, uint name);

        static WaylandGlContext()
        {
            _globalDelegate = OnRegistryGlobal;
            _globalRemoveDelegate = OnRegistryGlobalRemove;

            _registryListener = new WlRegistryListener
            {
                global = Marshal.GetFunctionPointerForDelegate(_globalDelegate),
                global_remove = Marshal.GetFunctionPointerForDelegate(_globalRemoveDelegate)
            };
        }

        private static void OnRegistryGlobal(IntPtr data, IntPtr registry, uint name, IntPtr Interface, uint version)
        {
            string ifaceName = Marshal.PtrToStringAnsi(Interface);
            if (ifaceName == "wl_compositor" && Compositor == IntPtr.Zero)
            {
                IntPtr compositorIface = GetWlCompositorInterface();
                Compositor = wl_registry_bind(registry, name, compositorIface, WL_COMPOSITOR_INTERFACE_VERSION);
            }
        }

        private static void OnRegistryGlobalRemove(IntPtr data, IntPtr registry, uint name)
        {
        }

        private static IntPtr GetWlCompositorInterface()
        {
            const int RTLD_NOW = 2;
            IntPtr libHandle = dlopen("libwayland-client.so.0", RTLD_NOW);
            IntPtr ifacePtr = dlsym(libHandle, "wl_compositor_interface");
            dlclose(libHandle);
            return ifacePtr;
        }

        public static IntPtr WlDisplayGetRegistry(IntPtr display)
        {
            IntPtr registryInterface = GetWlRegistryInterface();

            const uint WL_DISPLAY_GET_REGISTRY = 1;

            // display (proxy), opcode(1), interface, version(NULL)
            return wl_proxy_marshal_constructor(display, WL_DISPLAY_GET_REGISTRY, registryInterface, IntPtr.Zero);
        }

        public static IGlContext FromWindowHandle(IntPtr windowHandle, int Major, int Minor, GlProfile arbProfile, int VSync = 0)
        {
            lock (Lock)
                return new WaylandGlContext(windowHandle, Major, Minor, arbProfile, VSync);
        }

        private WaylandGlContext(IntPtr windowHandle, int Major, int Minor, GlProfile arbProfile, int VSync = 0)
        {
            const int width = 128;
            const int height = 128;
            wlDisplay = DefaultEglDisplay;

            if (wlDisplay == IntPtr.Zero || windowHandle == IntPtr.Zero)
            {
                wlDisplay = wl_display_connect(IntPtr.Zero);
                if (wlDisplay == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to connect to Wayland display");
                }

                if (windowHandle == IntPtr.Zero)
                {
                    Registry = WlDisplayGetRegistry(wlDisplay);
                    if (Registry == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to get Wayland registry");
                    }

                    Console.WriteLine($"Registry wlDisplay 0x{wlDisplay:X} Registry 0x{Registry:X}");

                    _listenerHandle = GCHandle.Alloc(_registryListener, GCHandleType.Pinned);

                    wl_registry_add_listener(Registry, _listenerHandle.AddrOfPinnedObject(), IntPtr.Zero);

                    wl_display_roundtrip(wlDisplay);

                    if (Compositor == IntPtr.Zero)
                    {
                        throw new Exception("Compositor not found");
                    }

                    Surface = wl_compositor_create_surface(Compositor);

                    EglWindow = wl_egl_window_create(Surface, width, height);

                    EglDisplay = eglGetDisplay(wlDisplay);
                }

                DisplayCreated = true;
            } else
            {
                EglWindow = windowHandle;
                EglDisplay = DefaultEglDisplay;
            }

            int major, minor;
            if (!eglInitialize(EglDisplay, &major, &minor))
                Console.WriteLine($"EGL initialized Error: {eglGetError()}");

            var attributes = new List<int>();
            attributes.AddRange(new[] { (int)EglAttribute.SurfaceType, (int)EglSurfaceType.Window });
            attributes.AddRange(new[] { (int)EglAttribute.RenderableType, (int)EglRenderableType.OpenGLBit });
            attributes.AddRange(new[] { (int)EglAttribute.RedSize, 8 });
            attributes.AddRange(new[] { (int)EglAttribute.GreenSize, 8 });
            attributes.AddRange(new[] { (int)EglAttribute.BlueSize, 8 });
            attributes.AddRange(new[] { (int)EglAttribute.AlphaSize, 8 });
            attributes.AddRange(new[] { (int)EglAttribute.DepthSize, 24 });
            attributes.AddRange(new[] { (int)EglAttribute.StencilSize, 8 });
            attributes.AddRange(new[] { (int)EglAttribute.DoubleBuffer, 1 });
            attributes.Add((int)EglAttribute.None);

            int configCount;
            fixed (int* attributesPtr = attributes.ToArray())
            {
                fixed (nint* cfgPtr = &EglConfig)
                    if (!eglChooseConfig(EglDisplay, attributesPtr, cfgPtr, 1, &configCount))
                        Console.WriteLine($"eglChooseConfig Error {eglGetError()}");
            }

            IntPtr sharePtr;
            bool setAsSharedRoot = false;
            lock (SharedLock)
            {
                if (SharedContext == IntPtr.Zero)
                {
                    sharePtr = IntPtr.Zero;
                    setAsSharedRoot = true;
                } else
                {
                    sharePtr = SharedContext;
                    SharedRefCount++;
                }
            }

            var contextAttributes = new List<int>();
            contextAttributes.AddRange(new int[] { (int)EglContextAttribute.ContextMajorVersion, Major });
            contextAttributes.AddRange(new int[] { (int)EglContextAttribute.ContextMinorVersion, Minor });
            contextAttributes.AddRange(new int[] { (int)EglContextAttribute.ContextProfileMask,
                arbProfile == GlProfile.Compatibility ? (int)EglContextProfile.Compatibility : (int)EglContextProfile.Core });
            contextAttributes.Add((int)EglContextAttribute.None);

            fixed (int* contextAttributesPtr = contextAttributes.ToArray())
            {
                Context = eglCreateContext(EglDisplay, EglConfig, sharePtr, contextAttributesPtr);
            }

            if (Context == 0)
                Console.WriteLine($"eglCreateContext Error {eglGetError()}");

            lock (SharedLock)
            {
                if (setAsSharedRoot)
                {
                    if (SharedContext == IntPtr.Zero && Context != IntPtr.Zero)
                    {
                        SharedContext = Context;
                        SharedRefCount = 1;
                    } else
                    {
                        if (SharedContext != IntPtr.Zero)
                        {
                            SharedRefCount++;
                        }
                    }
                } else
                {
                    if (Context == IntPtr.Zero)
                    {
                        SharedRefCount = Math.Max(0, SharedRefCount - 1);
                    }
                }
            }

            EglSurface = eglCreateWindowSurface(EglDisplay, EglConfig, EglWindow, null);

            if (EglSurface == 0)
                Console.WriteLine($"eglCreateWindowSurface Error {eglGetError()}");

            MakeCurrent();

            SetVSync(VSync);
        }

        public GlContextSize Size => new GlContextSize { Width = 0, Height = 0 };

        public IGlContext MakeCurrent()
        {
            if (eglMakeCurrent(EglDisplay, EglSurface, EglSurface, Context))
                return this;
            GL.CheckError();
            Console.WriteLine("eglMakeCurrent failed");
            return this;
        }

        public IGlContext ReleaseCurrent()
        {
            eglMakeCurrent(EglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            GL.CheckError();
            return this;
        }

        public IGlContext SwapBuffers()
        {
            eglSwapBuffers(EglDisplay, EglSurface);
            return this;
        }

        public IGlContext SetVSync(int vsync)
        {
            eglSwapInterval(EglDisplay, vsync);
            return this;
        }

        public void Dispose()
        {
            ReleaseCurrent();

            if (Context != IntPtr.Zero)
            {
                eglDestroyContext(EglDisplay, Context);
                Context = IntPtr.Zero;
            }

            lock (SharedLock)
            {
                if (SharedContext != IntPtr.Zero)
                {
                    SharedRefCount = Math.Max(0, SharedRefCount - 1);
                    if (SharedRefCount == 0)
                    {
                        eglDestroyContext(EglDisplay, SharedContext);
                        SharedContext = IntPtr.Zero;
                        if (_listenerHandle.IsAllocated)
                        {
                            _listenerHandle.Free();
                        }
                    }
                }
            }

            if (EglSurface != IntPtr.Zero && DisplayCreated)
            {
                eglDestroySurface(EglDisplay, EglSurface);
                EglSurface = IntPtr.Zero;
            }

            if (EglWindow != IntPtr.Zero && DisplayCreated)
            {
                wl_egl_window_destroy(EglWindow);
                EglWindow = IntPtr.Zero;
            }

            if (EglDisplay != IntPtr.Zero && DisplayCreated)
            {
                eglTerminate(EglDisplay);
                EglDisplay = IntPtr.Zero;
            }
        }
    }

    public enum EglAttribute
    {
        None = 0x3038,
        SurfaceType = 0x3033,        // EGL_SURFACE_TYPE
        RenderableType = 0x3040,     // EGL_RENDERABLE_TYPE
        RedSize = 0x3024,            // EGL_RED_SIZE
        GreenSize = 0x3023,          // EGL_GREEN_SIZE
        BlueSize = 0x3022,           // EGL_BLUE_SIZE
        AlphaSize = 0x3021,          // EGL_ALPHA_SIZE
        DepthSize = 0x3025,          // EGL_DEPTH_SIZE
        StencilSize = 0x3026,        // EGL_STENCIL_SIZE
        DoubleBuffer = 0x3032,       // EGL_DOUBLE_BUFFER
        Samples = 0x3031,            // EGL_SAMPLES
        SampleBuffers = 0x3032       // EGL_SAMPLE_BUFFERS
    }

    public enum EglSurfaceType
    {
        Window = 0x0004,
        Pbuffer = 0x0002,
        Pixmap = 0x0001
    }

    public enum EglRenderableType
    {
        OpenGLBit = 0x0008,
        OpenGLESBit = 0x0040,
        OpenVGBit = 0x0020
    }

    public enum EglContextAttribute
    {
        None = 0x3038,
        ContextMajorVersion = 0x3098,
        ContextMinorVersion = 0x30FB,
        ContextProfileMask = 0x30FD
    }

    public enum EglContextProfile
    {
        Core = 0x0001,
        Compatibility = 0x0002
    }
}
