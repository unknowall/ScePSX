using System;

namespace ScePSX.DynamicLibrary
{
    public interface IDynamicLibrary : IDisposable
    {
        IntPtr GetMethod(string Name);
    }
}