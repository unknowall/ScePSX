using System;
using System.Runtime.InteropServices;

namespace LightGL.DynamicLibrary
{
    public class DynamicLibraryMac : IDynamicLibrary
    {
        string LibraryName;
        private nint LibraryHandle;

        public DynamicLibraryMac(string LibraryName)
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
            //  dlclose(this.dlHandle);
            //  this.dlHandle = IntPtr.Zero;
            //}
        }

        const int RTLD_NOW = 2;
        private const string LibDl = "libdl.dylib";

        [DllImport(LibDl)]
        private static extern nint dlopen(string fileName, int flags);

        [DllImport(LibDl)]
        private static extern nint dlsym(nint handle, string symbol);

        [DllImport(LibDl)]
        private static extern int dlclose(nint handle);

        [DllImport(LibDl)]
        private static extern string dlerror();
    }
}
