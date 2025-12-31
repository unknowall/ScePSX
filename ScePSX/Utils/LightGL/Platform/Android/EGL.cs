using System;
using System.Runtime.InteropServices;
using System.Security;
using EGLClientBuffer = System.IntPtr;
using EGLConfig = System.IntPtr;
using EGLContext = System.IntPtr;
using EGLDisplay = System.IntPtr;
using EGLNativeDisplayType = System.IntPtr;
using EGLNativePixmapType = System.IntPtr;
using EGLNativeWindowType = System.IntPtr;
using EGLSurface = System.IntPtr;

namespace LightGL.Android
{
    public unsafe class EGL
    {
        public const int EGL_VERSION_1_0 = 1;
        public const int EGL_VERSION_1_1 = 1;
        public const int EGL_VERSION_1_2 = 1;
        public const int EGL_VERSION_1_3 = 1;
        public const int EGL_VERSION_1_4 = 1;
        public const bool EGL_FALSE = false;
        public const bool EGL_TRUE = true;
        public static EGLNativeDisplayType EGL_DEFAULT_DISPLAY = (EGLNativeDisplayType)0;
        public static EGLContext EGL_NO_CONTEXT = (EGLContext)0;
        public static EGLDisplay EGL_NO_DISPLAY = (EGLDisplay)0;
        public static EGLSurface EGL_NO_SURFACE = (EGLSurface)0;
        public const int EGL_DONT_CARE = -1;
        public const int EGL_SUCCESS = 0x3000;
        public const int EGL_NOT_INITIALIZED = 0x3001;
        public const int EGL_BAD_ACCESS = 0x3002;
        public const int EGL_BAD_ALLOC = 0x3003;
        public const int EGL_BAD_ATTRIBUTE = 0x3004;
        public const int EGL_BAD_CONFIG = 0x3005;
        public const int EGL_BAD_CONTEXT = 0x3006;
        public const int EGL_BAD_CURRENT_SURFACE = 0x3007;
        public const int EGL_BAD_DISPLAY = 0x3008;
        public const int EGL_BAD_MATCH = 0x3009;
        public const int EGL_BAD_NATIVE_PIXMAP = 0x300A;
        public const int EGL_BAD_NATIVE_WINDOW = 0x300B;
        public const int EGL_BAD_PARAMETER = 0x300C;
        public const int EGL_BAD_SURFACE = 0x300D;
        public const int EGL_CONTEXT_LOST = 0x300E;

        public static string eglGetErrorString(int error)
        {
            switch (error)
            {
                case EGL_SUCCESS:
                    return "EGL_SUCCESS";
                case EGL_NOT_INITIALIZED:
                    return "EGL_NOT_INITIALIZED";
                case EGL_BAD_ACCESS:
                    return "EGL_BAD_ACCESS";
                case EGL_BAD_ALLOC:
                    return "EGL_BAD_ALLOC";
                case EGL_BAD_ATTRIBUTE:
                    return "EGL_BAD_ATTRIBUTE";
                case EGL_BAD_CONFIG:
                    return "EGL_BAD_CONFIG";
                case EGL_BAD_CONTEXT:
                    return "EGL_BAD_CONTEXT";
                case EGL_BAD_CURRENT_SURFACE:
                    return "EGL_BAD_CURRENT_SURFACE";
                case EGL_BAD_DISPLAY:
                    return "EGL_BAD_DISPLAY";
                case EGL_BAD_MATCH:
                    return "EGL_BAD_MATCH";
                case EGL_BAD_NATIVE_PIXMAP:
                    return "EGL_BAD_NATIVE_PIXMAP";
                case EGL_BAD_NATIVE_WINDOW:
                    return "EGL_BAD_NATIVE_WINDOW";
                case EGL_BAD_PARAMETER:
                    return "EGL_BAD_PARAMETER";
                case EGL_BAD_SURFACE:
                    return "EGL_BAD_SURFACE";
                case EGL_CONTEXT_LOST:
                    return "EGL_CONTEXT_LOST";
                default:
                    return $"EGL_UNKNOWN_{error:X4}";
            }
        }

        public static string eglGetErrorString()
        {
            return eglGetErrorString(eglGetError());
        }

        public readonly struct EGLDisplay
        {
            public readonly IntPtr Handle;
            public EGLDisplay(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLDisplay d) => d.Handle;
            public static implicit operator EGLDisplay(IntPtr p) => new EGLDisplay(p);
            public static implicit operator EGLDisplay(int v) => new EGLDisplay((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLContext
        {
            public readonly IntPtr Handle;
            public EGLContext(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLContext c) => c.Handle;
            public static implicit operator EGLContext(IntPtr p) => new EGLContext(p);
            public static implicit operator EGLContext(int v) => new EGLContext((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLConfig
        {
            public readonly IntPtr Handle;
            public EGLConfig(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLConfig c) => c.Handle;
            public static implicit operator EGLConfig(IntPtr p) => new EGLConfig(p);
            public static implicit operator EGLConfig(int v) => new EGLConfig((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLSurface
        {
            public readonly IntPtr Handle;
            public EGLSurface(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLSurface s) => s.Handle;
            public static implicit operator EGLSurface(IntPtr p) => new EGLSurface(p);
            public static implicit operator EGLSurface(int v) => new EGLSurface((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLNativeDisplayType
        {
            public readonly IntPtr Handle;
            public EGLNativeDisplayType(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLNativeDisplayType d) => d.Handle;
            public static implicit operator EGLNativeDisplayType(IntPtr p) => new EGLNativeDisplayType(p);
            public static implicit operator EGLNativeDisplayType(int v) => new EGLNativeDisplayType((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLNativeWindowType
        {
            public readonly IntPtr Handle;
            public EGLNativeWindowType(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLNativeWindowType w) => w.Handle;
            public static implicit operator EGLNativeWindowType(IntPtr p) => new EGLNativeWindowType(p);
            public static implicit operator EGLNativeWindowType(int v) => new EGLNativeWindowType((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLNativePixmapType
        {
            public readonly IntPtr Handle;
            public EGLNativePixmapType(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLNativePixmapType p) => p.Handle;
            public static implicit operator EGLNativePixmapType(IntPtr p) => new EGLNativePixmapType(p);
            public static implicit operator EGLNativePixmapType(int v) => new EGLNativePixmapType((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public readonly struct EGLClientBuffer
        {
            public readonly IntPtr Handle;
            public EGLClientBuffer(IntPtr handle) => Handle = handle;
            public static implicit operator IntPtr(EGLClientBuffer b) => b.Handle;
            public static implicit operator EGLClientBuffer(IntPtr p) => new EGLClientBuffer(p);
            public static implicit operator EGLClientBuffer(int v) => new EGLClientBuffer((IntPtr)v);
            public override string ToString() => Handle.ToString();
        }

        public const int EGL_BUFFER_SIZE = 0x3020;
        public const int EGL_ALPHA_SIZE = 0x3021;
        public const int EGL_BLUE_SIZE = 0x3022;
        public const int EGL_GREEN_SIZE = 0x3023;
        public const int EGL_RED_SIZE = 0x3024;
        public const int EGL_DEPTH_SIZE = 0x3025;
        public const int EGL_STENCIL_SIZE = 0x3026;
        public const int EGL_CONFIG_CAVEAT = 0x3027;
        public const int EGL_CONFIG_ID = 0x3028;
        public const int EGL_LEVEL = 0x3029;
        public const int EGL_MAX_PBUFFER_HEIGHT = 0x302A;
        public const int EGL_MAX_PBUFFER_PIXELS = 0x302B;
        public const int EGL_MAX_PBUFFER_WIDTH = 0x302C;
        public const int EGL_NATIVE_RENDERABLE = 0x302D;
        public const int EGL_NATIVE_VISUAL_ID = 0x302E;
        public const int EGL_NATIVE_VISUAL_TYPE = 0x302F;
        public const int EGL_SAMPLES = 0x3031;
        public const int EGL_SAMPLE_BUFFERS = 0x3032;
        public const int EGL_SURFACE_TYPE = 0x3033;
        public const int EGL_TRANSPARENT_TYPE = 0x3034;
        public const int EGL_TRANSPARENT_BLUE_VALUE = 0x3035;
        public const int EGL_TRANSPARENT_GREEN_VALUE = 0x3036;
        public const int EGL_TRANSPARENT_RED_VALUE = 0x3037;
        public const int EGL_NONE = 0x3038; /* Attrib list terminator */
        public const int EGL_BIND_TO_TEXTURE_RGB = 0x3039;
        public const int EGL_BIND_TO_TEXTURE_RGBA = 0x303A;
        public const int EGL_MIN_SWAP_INTERVAL = 0x303B;
        public const int EGL_MAX_SWAP_INTERVAL = 0x303C;
        public const int EGL_LUMINANCE_SIZE = 0x303D;
        public const int EGL_ALPHA_MASK_SIZE = 0x303E;
        public const int EGL_COLOR_BUFFER_TYPE = 0x303F;
        public const int EGL_RENDERABLE_TYPE = 0x3040;
        public const int EGL_MATCH_NATIVE_PIXMAP = 0x3041;
        public const int EGL_CONFORMANT = 0x3042;
        public const int EGL_SLOW_CONFIG = 0x3050; /* EGL_CONFIG_CAVEAT value */
        public const int EGL_NON_CONFORMANT_CONFIG = 0x3051; /* EGL_CONFIG_CAVEAT value */
        public const int EGL_TRANSPARENT_RGB = 0x3052; /* EGL_TRANSPARENT_TYPE value */
        public const int EGL_RGB_BUFFER = 0x308E; /* EGL_COLOR_BUFFER_TYPE value */
        public const int EGL_LUMINANCE_BUFFER = 0x308F; /* EGL_COLOR_BUFFER_TYPE value */
        public const int EGL_NO_TEXTURE = 0x305C;
        public const int EGL_TEXTURE_RGB = 0x305D;
        public const int EGL_TEXTURE_RGBA = 0x305E;
        public const int EGL_TEXTURE_2D = 0x305F;
        public const int EGL_PBUFFER_BIT = 0x0001; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_PIXMAP_BIT = 0x0002; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_WINDOW_BIT = 0x0004; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_VG_COLORSPACE_LINEAR_BIT = 0x0020; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_VG_ALPHA_FORMAT_PRE_BIT = 0x0040; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_MULTISAMPLE_RESOLVE_BOX_BIT = 0x0200; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_SWAP_BEHAVIOR_PRESERVED_BIT = 0x0400; /* EGL_SURFACE_TYPE mask bits */
        public const int EGL_OPENGL_ES_BIT = 0x0001; /* EGL_RENDERABLE_TYPE mask bits */
        public const int EGL_OPENVG_BIT = 0x0002; /* EGL_RENDERABLE_TYPE mask bits */
        public const int EGL_OPENGL_ES2_BIT = 0x0004; /* EGL_RENDERABLE_TYPE mask bits */
        public const int EGL_OPENGL_BIT = 0x0008; /* EGL_RENDERABLE_TYPE mask bits */
        public const int EGL_VENDOR = 0x3053;
        public const int EGL_VERSION = 0x3054;
        public const int EGL_EXTENSIONS = 0x3055;
        public const int EGL_CLIENT_APIS = 0x308D;
        public const int EGL_HEIGHT = 0x3056;
        public const int EGL_WIDTH = 0x3057;
        public const int EGL_LARGEST_PBUFFER = 0x3058;
        public const int EGL_TEXTURE_FORMAT = 0x3080;
        public const int EGL_TEXTURE_TARGET = 0x3081;
        public const int EGL_MIPMAP_TEXTURE = 0x3082;
        public const int EGL_MIPMAP_LEVEL = 0x3083;
        public const int EGL_RENDER_BUFFER = 0x3086;
        public const int EGL_VG_COLORSPACE = 0x3087;
        public const int EGL_VG_ALPHA_FORMAT = 0x3088;
        public const int EGL_HORIZONTAL_RESOLUTION = 0x3090;
        public const int EGL_VERTICAL_RESOLUTION = 0x3091;
        public const int EGL_PIXEL_ASPECT_RATIO = 0x3092;
        public const int EGL_SWAP_BEHAVIOR = 0x3093;
        public const int EGL_MULTISAMPLE_RESOLVE = 0x3099;
        public const int EGL_BACK_BUFFER = 0x3084;
        public const int EGL_SINGLE_BUFFER = 0x3085;
        public const int EGL_VG_COLORSPACE_sRGB = 0x3089; /* EGL_VG_COLORSPACE value */
        public const int EGL_VG_COLORSPACE_LINEAR = 0x308A; /* EGL_VG_COLORSPACE value */
        public const int EGL_VG_ALPHA_FORMAT_NONPRE = 0x308B; /* EGL_ALPHA_FORMAT value */
        public const int EGL_VG_ALPHA_FORMAT_PRE = 0x308C; /* EGL_ALPHA_FORMAT value */
        public const int EGL_DISPLAY_SCALING = 10000;
        public const int EGL_UNKNOWN = -1;
        public const int EGL_BUFFER_PRESERVED = 0x3094; /* EGL_SWAP_BEHAVIOR value */
        public const int EGL_BUFFER_DESTROYED = 0x3095; /* EGL_SWAP_BEHAVIOR value */
        public const int EGL_OPENVG_IMAGE = 0x3096;
        public const int EGL_CONTEXT_CLIENT_TYPE = 0x3097;
        public const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
        public const int EGL_MULTISAMPLE_RESOLVE_DEFAULT = 0x309A; /* EGL_MULTISAMPLE_RESOLVE value */
        public const int EGL_MULTISAMPLE_RESOLVE_BOX = 0x309B; /* EGL_MULTISAMPLE_RESOLVE value */
        public const int EGL_OPENGL_ES_API = 0x30A0;
        public const int EGL_OPENVG_API = 0x30A1;
        public const int EGL_OPENGL_API = 0x30A2;
        public const int EGL_DRAW = 0x3059;
        public const int EGL_READ = 0x305A;
        public const int EGL_CORE_NATIVE_ENGINE = 0x305B;
        public const int EGL_COLORSPACE = EGL_VG_COLORSPACE;
        public const int EGL_ALPHA_FORMAT = EGL_VG_ALPHA_FORMAT;
        public const int EGL_COLORSPACE_sRGB = EGL_VG_COLORSPACE_sRGB;
        public const int EGL_COLORSPACE_LINEAR = EGL_VG_COLORSPACE_LINEAR;
        public const int EGL_ALPHA_FORMAT_NONPRE = EGL_VG_ALPHA_FORMAT_NONPRE;
        public const int EGL_ALPHA_FORMAT_PRE = EGL_VG_ALPHA_FORMAT_PRE;

        const string DLL_EGL = "libEGL";

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern int eglGetError();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLDisplay eglGetDisplay(EGLNativeDisplayType display_id);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglInitialize(EGLDisplay dpy, int* major, int* minor);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglTerminate(EGLDisplay dpy);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern string eglQueryString(EGLDisplay dpy, int name);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglGetConfigs(EGLDisplay dpy, EGLConfig* configs, int config_size, int* num_config);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglChooseConfig(EGLDisplay dpy, int* attrib_list, EGLConfig* configs, int config_size, int* num_config);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglGetConfigAttrib(EGLDisplay dpy, EGLConfig config, int attribute, int* value);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLSurface eglCreateWindowSurface(EGLDisplay dpy, EGLConfig config, EGLNativeWindowType win, int* attrib_list);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLSurface eglCreatePbufferSurface(EGLDisplay dpy, EGLConfig config, int* attrib_list);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLSurface eglCreatePixmapSurface(EGLDisplay dpy, EGLConfig config, EGLNativePixmapType pixmap, int* attrib_list);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglDestroySurface(EGLDisplay dpy, EGLSurface surface);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglQuerySurface(EGLDisplay dpy, EGLSurface surface, int attribute, int* value);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglBindAPI(int api);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern int eglQueryAPI();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglWaitClient();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglReleaseThread();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLSurface eglCreatePbufferFromClientBuffer(EGLDisplay dpy, int buftype,
            EGLClientBuffer buffer, EGLConfig config, int* attrib_list);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglSurfaceAttrib(EGLDisplay dpy, EGLSurface surface, int attribute, int value);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglBindTexImage(EGLDisplay dpy, EGLSurface surface, int buffer);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglReleaseTexImage(EGLDisplay dpy, EGLSurface surface, int buffer);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglSwapInterval(EGLDisplay dpy, int interval);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLContext eglCreateContext(EGLDisplay dpy, EGLConfig config, EGLContext share_context, int* attrib_list);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglDestroyContext(EGLDisplay dpy, EGLContext ctx);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglMakeCurrent(EGLDisplay dpy, EGLSurface draw, EGLSurface read, EGLContext ctx);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLContext eglGetCurrentContext();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLSurface eglGetCurrentSurface(int readdraw);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern EGLDisplay eglGetCurrentDisplay();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglQueryContext(EGLDisplay dpy, EGLContext ctx, int attribute, int* value);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglWaitGL();

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglWaitNative(int engine);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglSwapBuffers(EGLDisplay dpy, EGLSurface surface);

        [DllImport(DLL_EGL), SuppressUnmanagedCodeSecurity]
        public static extern bool eglCopyBuffers(EGLDisplay dpy, EGLSurface surface, EGLNativePixmapType target);

        public static int eglGetConfigAttrib(EGLDisplay dpy, EGLConfig config, int attribute)
        {
            int @out = 0;
            if (!eglGetConfigAttrib(dpy, config, attribute, &@out))
                throw new Exception("Can't get value");
            return @out;
        }
    }
}
