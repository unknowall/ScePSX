using System;

namespace LightGL.DynamicLibrary
{
    public interface IDynamicLibrary : IDisposable
    {
        nint GetMethod(string Name);
    }
}
