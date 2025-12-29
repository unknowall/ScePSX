using System;
using System.Runtime.InteropServices;
using ScePSX.GL.Android;
using ScePSX.GL.Linux;
using ScePSX.GL.Mac;
using ScePSX.GL.Windows;
using ScePSX.DynamicLibrary;

namespace ScePSX.GL
{
    public class GlContextFactory
    {
        [ThreadStatic] public static IGlContext Current;

        public static IGlContext CreateWindowless() => CreateFromWindowHandle(IntPtr.Zero);

        public static IGlContext CreateFromWindowHandle(IntPtr windowHandle) =>
            Platform.OS switch
            {
                OS.Windows => WinGlContext.FromWindowHandle(windowHandle),
                OS.Mac => MacGLContext.FromWindowHandle(windowHandle),
                OS.Linux => LinuxGlContext.FromWindowHandle(windowHandle),
                OS.Android => AndroidGLContext.FromWindowHandle(windowHandle),
                _ => throw new NotImplementedException($"Not implemented OS: {Platform.OS}")
            };
    }
}
