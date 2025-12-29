using System;
using System.Runtime.InteropServices;

namespace LightGL.DynamicLibrary
{
    public class DynamicLibraryWindows : IDynamicLibrary
    {
        nint LibraryHandle;
        string LibraryName;

        public DynamicLibraryWindows(string LibraryName)
        {
            this.LibraryName = LibraryName;
            LibraryHandle = LoadLibrary(LibraryName);
            if (LibraryHandle == nint.Zero)
            {
                throw new InvalidOperationException($"Can't find library '{LibraryName}'");
            }
        }

        public nint GetMethod(string Name)
        {
            return GetProcAddress(LibraryHandle, Name);
        }

        public void Dispose()
        {
            //if (this.hModule != IntPtr.Zero)
            //{
            //	FreeLibrary(this.hModule);
            //	this.hModule = IntPtr.Zero;
            //}
        }

        [DllImport("kernel32.dll")]
        private static extern nint LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern nint GetProcAddress(nint hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(nint hModule);
    }
}
