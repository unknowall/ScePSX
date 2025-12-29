using System;
using System.Runtime.InteropServices;

namespace ScePSX.DynamicLibrary
{
    public class DynamicLibraryWindows : IDynamicLibrary
    {
        IntPtr LibraryHandle;
        string LibraryName;

        public DynamicLibraryWindows(string LibraryName)
        {
            this.LibraryName = LibraryName;
            this.LibraryHandle = LoadLibrary(LibraryName);
            if (this.LibraryHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Can't find library '{LibraryName}'");
            }
        }

        public IntPtr GetMethod(string Name)
        {
            return GetProcAddress(this.LibraryHandle, Name);
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
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}