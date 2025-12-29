using System;
using System.Runtime.InteropServices;
using LightGL.DynamicLibrary;

namespace LightGL
{
    class DynamicLibraryGl : IDynamicLibrary
    {
        public IntPtr GetMethod(string name)
        {
            IntPtr Return;
            switch (Platform.OS)
            {
                case OS.Windows:
                    Return = wglGetProcAddress(name);
                    break;
                default:
                    Return = glxGetProcAddress(name);
                    break;
            }
            if (Return == IntPtr.Zero)
            {
                Console.WriteLine("wglGetProcAddress Can't find '{0}' : {1:X8}", name, Marshal.GetLastWin32Error());
            }
            return Return;
        }

        public void Dispose()
        {
        }

        [DllImport(GL.DllWindows, EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
        private static extern IntPtr wglGetProcAddress(string lpszProc);

        [DllImport(GL.DllLinux, EntryPoint = "glXGetProcAddress")]
        private static extern IntPtr glxGetProcAddress([MarshalAs(UnmanagedType.LPTStr)] string procName);
    }
}
