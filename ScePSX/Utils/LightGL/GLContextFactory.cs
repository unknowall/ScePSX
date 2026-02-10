using LightGL.Android;
using LightGL.DynamicLibrary;
using LightGL.Linux;
using LightGL.Mac;
using LightGL.Windows;
using System;

namespace LightGL
{
    public class GlContextFactory
    {
        [ThreadStatic] public static IGlContext Current;

        public static bool IsWayLand = false;

        public enum GlProfile : int
        {
            Core = 1,
            Compatibility = 2,
        }

        public static IGlContext CreateWindowless() => CreateFromWindowHandle(IntPtr.Zero);

        public static IGlContext CreateFromWindowHandle(
            IntPtr windowHandle, int Major = 3, int Minor = 3,
            GlProfile arbProfile = GlProfile.Compatibility, int VSync = 0)
        {
            switch (Platform.OS)
            {
                case OS.Windows:
                    Current = WinGlContext.FromWindowHandle(windowHandle, Major, Minor, arbProfile, VSync);
                    break;
                case OS.Mac:
                    Current = MacGLContext.FromWindowHandle(windowHandle);
                    break;
                case OS.Linux:
                    Current = X11GLContext.FromWindowHandle(windowHandle, Major, Minor, arbProfile, VSync);
                    break;
                case OS.Android:
                    Current = AndroidGLContext.FromWindowHandle(windowHandle);
                    break;
                default:
                    throw new NotImplementedException($"Not implemented OS: {Platform.OS}");
            }

            return Current;
        }
    }

    public class CheckLinuxDisplay
    {
        public enum SessionType
        {
            Unknown,
            X11,
            Wayland
        }

        public static SessionType GetSessionType()
        {
            var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            var xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
            var display = Environment.GetEnvironmentVariable("DISPLAY");

            if (!string.IsNullOrEmpty(waylandDisplay))
            {
                return SessionType.Wayland;
            }

            if (!string.IsNullOrEmpty(xdgSessionType))
            {
                if (xdgSessionType.ToLowerInvariant() == "wayland")
                {
                    return SessionType.Wayland;
                } else if (xdgSessionType.ToLowerInvariant() == "x11")
                {
                    return SessionType.X11;
                }
            }

            if (!string.IsNullOrEmpty(display))
            {
                return SessionType.X11;
            }

            return SessionType.Unknown;
        }
    }

}
