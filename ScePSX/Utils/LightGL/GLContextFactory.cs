using System;
using LightGL.Android;
using LightGL.DynamicLibrary;
using LightGL.Linux;
using LightGL.Mac;
using LightGL.Windows;

namespace LightGL
{
    public class GlContextFactory
    {
        [ThreadStatic] public static IGlContext Current;

        public enum GlProfile : int
        {
            Core = 1,
            Compatibility = 2,
        }

        public static IGlContext CreateWindowless() => CreateFromWindowHandle(IntPtr.Zero);

        public static IGlContext CreateFromWindowHandle(
            IntPtr windowHandle, int Major = 3, int Minor = 3,
            GlProfile arbProfile = GlProfile.Compatibility) =>
            Platform.OS switch
            {
                OS.Windows => WinGlContext.FromWindowHandle(windowHandle, Major, Minor, arbProfile),
                OS.Mac => MacGLContext.FromWindowHandle(windowHandle),
                OS.Linux => LinuxGlContext.FromWindowHandle(windowHandle, Major, Minor, arbProfile),
                OS.Android => AndroidGLContext.FromWindowHandle(windowHandle),
                _ => throw new NotImplementedException($"Not implemented OS: {Platform.OS}")
            };
    }
}
