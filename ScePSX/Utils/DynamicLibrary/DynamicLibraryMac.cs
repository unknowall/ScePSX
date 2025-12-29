using System;
using System.Runtime.InteropServices;

namespace ScePSX.DynamicLibrary
{
    public class DynamicLibraryMac : IDynamicLibrary
    {
        string LibraryName;
        private IntPtr LibraryHandle;

        public DynamicLibraryMac(string LibraryName)
        {
            this.LibraryName = LibraryName;
            this.LibraryHandle = dlopen(LibraryName, RTLD_NOW);
            if (this.LibraryHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Can't find library '{LibraryName}' : {dlerror()}");
            }
            //Console.WriteLine(this.LibraryHandle);
        }

        public IntPtr GetMethod(string Name)
        {
            return dlsym(this.LibraryHandle, Name);
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
        private static extern IntPtr dlopen(string fileName, int flags);

        [DllImport(LibDl)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport(LibDl)]
        private static extern int dlclose(IntPtr handle);

        [DllImport(LibDl)]
        private static extern string dlerror();
    }
}
