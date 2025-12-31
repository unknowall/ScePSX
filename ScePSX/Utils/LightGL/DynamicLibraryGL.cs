using System;
using System.Runtime.InteropServices;
using LightGL.DynamicLibrary;

namespace LightGL
{
    class DynamicLibraryGl : IDynamicLibrary
    {
        // Cache for mac/linux dlopen handles (avoid repeated opens)
        private static IntPtr s_macHandle = IntPtr.Zero;
        private static IntPtr s_posixHandle = IntPtr.Zero;
        private const int RTLD_NOW = 2;

        public IntPtr GetMethod(string name)
        {
            IntPtr result = IntPtr.Zero;

            switch (Platform.OS)
            {
                case OS.Windows:
                    result = wglGetProcAddress(name);
                    if (result == IntPtr.Zero)
                    {
                        var module = GetModuleHandle(GL.DllWindows);
                        if (module != IntPtr.Zero)
                        {
                            result = GetProcAddress(module, name);
                        }
                    }
                    break;

                case OS.Mac:
                    if (s_macHandle == IntPtr.Zero)
                    {
                        s_macHandle = dlopen_mac(GL.DllMac, RTLD_NOW);
                    }
                    if (s_macHandle != IntPtr.Zero)
                    {
                        result = dlsym_mac(s_macHandle, name);
                    }
                    break;

                case OS.Android:
                    result = eglGetProcAddress_lib(name);
                    if (result == IntPtr.Zero)
                    {
                        if (s_posixHandle == IntPtr.Zero)
                        {
                            s_posixHandle = dlopen_posix(GL.DllAndroid, RTLD_NOW);
                        }
                        if (s_posixHandle != IntPtr.Zero)
                        {
                            result = dlsym_posix(s_posixHandle, name);
                        }
                    }
                    break;

                case OS.Linux:
                case OS.IOS:
                default:
                    result = glxGetProcAddressARB(name);
                    if (result == IntPtr.Zero)
                        result = glxGetProcAddress(name);
                    if (result == IntPtr.Zero)
                    {
                        if (s_posixHandle == IntPtr.Zero)
                        {
                            s_posixHandle = dlopen_posix(GL.DllLinux, RTLD_NOW);
                        }
                        if (s_posixHandle != IntPtr.Zero)
                        {
                            result = dlsym_posix(s_posixHandle, name);
                        }
                    }
                    break;
            }

            if (result == IntPtr.Zero)
            {
                if (Platform.IsWindows)
                {
                    Console.WriteLine("GetProcAddress Can't find '{0}' : {1:X8}", name, Marshal.GetLastWin32Error());
                }
                else
                {
                    Console.WriteLine("GetProcAddress Can't find '{0}' on {1}", name, Platform.OS);
                }
            }

            return result;
        }

        public void Dispose()
        {
            // keep handles for process lifetime; explicit cleanup omitted as existing code does
        }

        // --- Windows helpers ---
        [DllImport(GL.DllWindows, EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
        private static extern IntPtr wglGetProcAddress(string lpszProc);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        // --- Linux (GLX) ---
        [DllImport(GL.DllLinux, EntryPoint = "glXGetProcAddressARB")]
        private static extern IntPtr glxGetProcAddressARB([MarshalAs(UnmanagedType.LPStr)] string procName);

        [DllImport(GL.DllLinux, EntryPoint = "glXGetProcAddress")]
        private static extern IntPtr glxGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string procName);

        // --- Android / EGL ---
        [DllImport("libEGL.so", EntryPoint = "eglGetProcAddress")]
        private static extern IntPtr eglGetProcAddress_lib([MarshalAs(UnmanagedType.LPStr)] string procName);

        // Also try eglGetProcAddress exported from the GL library (if present)
        [DllImport(GL.DllAndroid, EntryPoint = "eglGetProcAddress", ExactSpelling = true)]
        private static extern IntPtr eglGetProcAddress_from_gl(string procName);

        // --- POSIX dlopen/dlsym (Linux) ---
        [DllImport("libdl.so")]
        private static extern IntPtr dlopen_posix(string fileName, int flags);

        [DllImport("libdl.so")]
        private static extern IntPtr dlsym_posix(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        private static extern IntPtr dlerror_posix();

        // --- macOS dlopen/dlsym (libdl.dylib) ---
        [DllImport("libdl.dylib")]
        private static extern IntPtr dlopen_mac(string fileName, int flags);

        [DllImport("libdl.dylib")]
        private static extern IntPtr dlsym_mac(IntPtr handle, string symbol);

        [DllImport("libdl.dylib")]
        private static extern IntPtr dlerror_mac();
    }
}