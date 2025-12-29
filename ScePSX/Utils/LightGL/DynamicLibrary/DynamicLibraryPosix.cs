using System;
using System.Runtime.InteropServices;

namespace LightGL.DynamicLibrary
{
    public class DynamicLibraryPosix : IDynamicLibrary
    {
        string LibraryName;
        private nint LibraryHandle;

        public DynamicLibraryPosix(string LibraryName)
        {
            this.LibraryName = LibraryName;
            LibraryHandle = dlopen(LibraryName, RTLD_NOW);
            if (LibraryHandle == nint.Zero)
            {
                throw new InvalidOperationException($"Can't find library '{LibraryName}' : {dlerror()}");
            }
            //Console.WriteLine(this.LibraryHandle);
        }

        public nint GetMethod(string Name)
        {
            return dlsym(LibraryHandle, Name);
        }

        public void Dispose()
        {
            //if (this.dlHandle != IntPtr.Zero)
            //{
            //	dlclose(this.dlHandle);
            //	this.dlHandle = IntPtr.Zero;
            //}
        }

        const int RTLD_NOW = 2;

        [DllImport("libdl.so")]
        private static extern nint dlopen(string fileName, int flags);

        [DllImport("libdl.so")]
        private static extern nint dlsym(nint handle, string symbol);

        [DllImport("libdl.so")]
        private static extern int dlclose(nint handle);

        [DllImport("libdl.so")]
        private static extern string dlerror();
    }
}
