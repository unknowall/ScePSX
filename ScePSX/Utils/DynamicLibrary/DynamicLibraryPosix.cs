using System;
using System.Runtime.InteropServices;

namespace ScePSX.DynamicLibrary
{
    public class DynamicLibraryPosix : IDynamicLibrary
    {
        string LibraryName;
        private IntPtr LibraryHandle;

        public DynamicLibraryPosix(string LibraryName)
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
            //	dlclose(this.dlHandle);
            //	this.dlHandle = IntPtr.Zero;
            //}
        }

        const int RTLD_NOW = 2;

        [DllImport("libdl.so")]
        private static extern IntPtr dlopen(string fileName, int flags);

        [DllImport("libdl.so")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        private static extern int dlclose(IntPtr handle);

        [DllImport("libdl.so")]
        private static extern string dlerror();
    }
}