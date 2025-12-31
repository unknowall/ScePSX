using System;
using System.Runtime.InteropServices;

namespace LightGL.Android
{
    public unsafe class AndroidGLContext : IGlContext
    {
        private static readonly object _sharedLock = new object();
        private static readonly object s_makeCurrentLock = new object();
        private static IntPtr _sharedContext = IntPtr.Zero;
        private static int _sharedRefCount = 0;

        private EGL.EGLDisplay Display;
        private EGL.EGLSurface Surface;
        private EGL.EGLContext Context;
        private EGL.EGLConfig Config;
        private IntPtr WindowHandle;
        private bool _createdSurface;

        public AndroidGLContext(IntPtr windowHandle, int EGLVersion = 2)
        {
            this.WindowHandle = windowHandle;

            Display = EGL.eglGetDisplay(EGL.EGL_DEFAULT_DISPLAY);
            int major = 0, minor = 0;

            if (!EGL.eglInitialize(Display, &major, &minor))
                throw new Exception("eglInitialize failed: " + EGL.eglGetErrorString());

            EGL.eglBindAPI(EGL.EGL_OPENGL_ES_API);

            int[] attribs = new int[]
            {
                EGL.EGL_RED_SIZE, 8,
                EGL.EGL_GREEN_SIZE, 8,
                EGL.EGL_BLUE_SIZE, 8,
                EGL.EGL_ALPHA_SIZE, 8,
                EGL.EGL_DEPTH_SIZE, 16,
                EGL.EGL_RENDERABLE_TYPE, EGL.EGL_OPENGL_ES2_BIT,
                EGL.EGL_NONE
            };

            EGL.EGLConfig cfg = IntPtr.Zero;
            int numConfigs = 0;
            fixed (int* attribPtr = attribs)
            {
                EGL.EGLConfig* cfgs = stackalloc EGL.EGLConfig[1];
                if (!EGL.eglChooseConfig(Display, attribPtr, cfgs, 1, &numConfigs) || numConfigs == 0)
                    throw new Exception("eglChooseConfig failed: " + EGL.eglGetErrorString());
                cfg = cfgs[0];
            }

            Config = cfg;

            Surface = EGL.eglCreateWindowSurface(Display, Config, (EGL.EGLNativeWindowType)WindowHandle, null);
            _createdSurface = Surface != EGL.EGL_NO_SURFACE;
            if (!_createdSurface)
                throw new Exception("eglCreateWindowSurface failed: " + EGL.eglGetErrorString());

            IntPtr sharePtr = IntPtr.Zero;
            bool becomeSharedRoot = false;
            lock (_sharedLock)
            {
                if (_sharedContext == IntPtr.Zero)
                {
                    sharePtr = IntPtr.Zero;
                    becomeSharedRoot = true;
                }
                else
                {
                    sharePtr = _sharedContext;
                    _sharedRefCount++;
                }
            }

            int[] ctxAttribs = new int[] { EGL.EGL_CONTEXT_CLIENT_VERSION, EGLVersion, EGL.EGL_NONE };
            fixed (int* ctxAttrPtr = ctxAttribs)
            {
                Context = EGL.eglCreateContext(Display, Config, (EGL.EGLContext)sharePtr, ctxAttrPtr);
            }

            lock (_sharedLock)
            {
                if (becomeSharedRoot)
                {
                    if (_sharedContext == IntPtr.Zero && Context != EGL.EGL_NO_CONTEXT)
                    {
                        _sharedContext = Context;
                        _sharedRefCount = 1;
                    }
                    else if (_sharedContext != IntPtr.Zero)
                    {
                        _sharedRefCount++;
                    }
                }
                else
                {
                    if (Context == EGL.EGL_NO_CONTEXT)
                    {
                        _sharedRefCount = Math.Max(0, _sharedRefCount - 1);
                    }
                }
            }

            if (Context == EGL.EGL_NO_CONTEXT)
                throw new Exception("eglCreateContext failed: " + EGL.eglGetErrorString());

            // make current
            if (!EGL.eglMakeCurrent(Display, Surface, Surface, Context))
                throw new Exception("eglMakeCurrent failed: " + EGL.eglGetErrorString());
        }

        public static AndroidGLContext FromWindowHandle(IntPtr WindowHandle) => new AndroidGLContext(WindowHandle);

        public GlContextSize Size
        {
            get
            {
                if (!_createdSurface) return new GlContextSize { Width = 0, Height = 0 };
                int w = 0, h = 0;
                EGL.eglQuerySurface(Display, Surface, EGL.EGL_WIDTH, &w);
                EGL.eglQuerySurface(Display, Surface, EGL.EGL_HEIGHT, &h);
                return new GlContextSize { Width = w, Height = h };
            }
        }

        public IGlContext MakeCurrent()
        {
            lock (s_makeCurrentLock)
            {
                EGL.eglMakeCurrent(Display, Surface, Surface, Context);
            }
            return this;
        }

        public IGlContext ReleaseCurrent()
        {
            lock (s_makeCurrentLock)
            {
                EGL.eglMakeCurrent(Display, EGL.EGL_NO_SURFACE, EGL.EGL_NO_SURFACE, EGL.EGL_NO_CONTEXT);
            }
            return this;
        }

        public IGlContext SwapBuffers()
        {
            EGL.eglSwapBuffers(Display, Surface);
            return this;
        }

        public IGlContext SetVSync(int vsync)
        {
            EGL.eglSwapInterval(Display, vsync);
            return this;
        }

        public void Dispose()
        {
            if (Context != EGL.EGL_NO_CONTEXT)
            {
                lock (_sharedLock)
                {
                    if (_sharedContext != IntPtr.Zero && Context == _sharedContext)
                    {
                        _sharedRefCount = Math.Max(0, _sharedRefCount - 1);
                        if (_sharedRefCount == 0)
                        {
                            EGL.eglDestroyContext(Display, Context);
                            _sharedContext = IntPtr.Zero;
                        }
                    }
                    else
                    {
                        EGL.eglDestroyContext(Display, Context);
                        if (_sharedContext != IntPtr.Zero && _sharedRefCount > 0)
                        {
                            _sharedRefCount = Math.Max(0, _sharedRefCount - 1);
                        }
                    }
                }

                Context = EGL.EGL_NO_CONTEXT;
            }

            if (_createdSurface && Surface != EGL.EGL_NO_SURFACE)
            {
                EGL.eglDestroySurface(Display, Surface);
                Surface = EGL.EGL_NO_SURFACE;
            }

            // 不在这里调用 eglTerminate(Display) — 由上层负责生命周期（或根据需要添加计数）
        }
    }
}