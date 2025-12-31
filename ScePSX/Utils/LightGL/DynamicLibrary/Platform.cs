using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#pragma warning disable CS8603

namespace LightGL.DynamicLibrary
{
    public enum OS
    {
        Windows,
        Linux,
        Mac,
        Android,
        IOS,
    }

    public enum Architecture
    {
        x64,
        x86,
        arm,
        arm64,
        mips,
    }

    public static unsafe class Platform
    {
        public static bool IsWindows => OS == OS.Windows;
        public static bool IsLinux => OS == OS.Linux;
        public static bool IsMac => OS == OS.Mac;
        public static bool IsAndroid => OS == OS.Android;
        public static bool IsIOS => OS == OS.IOS;

        public static bool IsPosix => !IsWindows;

        public static OS OS;

        public static bool Is64Bit => Environment.Is64BitProcess;
        public static bool Is32Bit => !Environment.Is64BitProcess;

        private static string _Architecture;

        public static Architecture Architecture
        {
            get
            {
                if (_Architecture == null)
                {
                    _Architecture = GetArchitectureString();
                }

                return _Architecture switch
                {
                    "AMD64" or "x86_64" => Architecture.x64,
                    "x86" or "i386" or "i686" => Architecture.x86,
                    "arm" or "armv7l" => Architecture.arm,
                    "aarch64" or "arm64" => Architecture.arm64,
                    "mips" => Architecture.mips,
                    _ => Architecture.arm, 
                };
            }
        }

        private static string GetArchitectureString()
        {
            try
            {
                if (OS == OS.Windows)
                {
                    var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                    return arch ?? "unknown";
                }
                else if (IsAndroid)
                {
                    return ReadAndroidProperty("ro.arch") ?? ReadAndroidProperty("ro.product.cpu.abi") ?? "unknown";
                }
                else if (IsIOS)
                {
                    return ExecuteUnixCommand("uname", "-m") ?? "unknown";
                }
                else
                {
                    return ExecuteUnixCommand("uname", "-m") ?? "unknown";
                }
            }
            catch
            {
                return "unknown";
            }
        }

        private static string ExecuteUnixCommand(string cmd, string args)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadAndroidProperty(string propName)
        {
            try
            {
                IntPtr buf = Marshal.AllocHGlobal(1024);
                if (__system_property_get(propName, buf) > 0)
                {
                    return Marshal.PtrToStringAnsi(buf)?.Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        [DllImport("libc", EntryPoint = "__system_property_get")]
        private static extern int __system_property_get(string name, IntPtr value);

        public static bool IsMono { get; private set; }

        public static DateTime UnixStart;

        public static long CurrentUnixMicroseconds => (DateTime.UtcNow - UnixStart).Ticks / (TimeSpan.TicksPerMillisecond / 1000);

        private const int SW_HIDE = 0;

        public static void HideConsole()
        {
            var hwnd = InternalWindows.GetConsoleWindow();
            InternalWindows.ShowWindow(hwnd, SW_HIDE);
        }

        static Platform()
        {
            IsMono = Type.GetType("Mono.Runtime") != null;

            UnixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var platform = Environment.OSVersion.Platform;

            switch (platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Win32S:
                    OS = OS.Windows;
                    break;

                case PlatformID.MacOSX:
                    OS = OS.Mac;
                    break;

                case PlatformID.Unix:
                    if (IsRunningOnMac())
                    {
                        OS = OS.Mac;
                    }
                    else if (IsRunningOnAndroid())
                    {
                        OS = OS.Android;
                    }
                    else if (IsRunningOnIOS())
                    {
                        OS = OS.IOS;
                    }
                    else
                    {
                        OS = OS.Linux;
                    }
                    break;

                default:
                    throw new PlatformNotSupportedException($"Unsupported platform: {platform}");
            }
        }

        private static bool IsRunningOnMac()
        {
            try
            {
                IntPtr buf = Marshal.AllocHGlobal(8192);
                if (uname(buf) == 0)
                {
#pragma warning disable CS8600
                    string osName = Marshal.PtrToStringAnsi(buf);
#pragma warning restore CS8600
                    return osName == "Darwin" && !IsRunningOnIOS(); // Darwin 但不是 iOS = Mac
                }
            }
            catch { }
            return false;
        }

        private static bool IsRunningOnAndroid()
        {
            try
            {
                if (System.IO.File.Exists("/system/bin/sh"))
                    return true;

                IntPtr buf = Marshal.AllocHGlobal(1024);
                if (__system_property_get("ro.build.version.release", buf) > 0)
                    return true;
            }
            catch { }
            return false;
        }

        private static bool IsRunningOnIOS()
        {
            try
            {
                if (System.IO.File.Exists("/System/Library/CoreServices/SystemVersion.plist") &&
                    !System.IO.File.Exists("/system/bin/sh")) // 排除 Mac
                {
                    return true;
                }

                IntPtr buf = Marshal.AllocHGlobal(8192);
                if (uname(buf) == 0)
                {
                    string machine = ExecuteUnixCommand("uname", "-m");
                    return machine == "arm64" || machine == "armv7" || machine == "i386" || machine == "x86_64"; // 模拟器
                }
            }
            catch { }
            return false;
        }

        [DllImport("libc")]
        private static extern int uname(IntPtr buf);

        public struct TimeSpec
        {
            public long sec;
            public long usec;
            public long total_usec => usec + sec * 1000 * 1000;
        }

        private class InternalUnix
        {
            [DllImport("libc")]
            internal static extern int* __errno_location();

            [DllImport("libc", EntryPoint = "strerror")]
            private static extern nint _strerror(int errno);
            public static string strerror(int errno)
            {
                var ret = Marshal.PtrToStringAnsi(_strerror(errno));
                return ret ?? string.Empty;
            }

            public static int errno() => *__errno_location();
            public static void reset_errno() => *__errno_location() = 0;

            [DllImport("libc", EntryPoint = "mmap")]
            internal static extern void* mmap(void* addr, uint len, uint prot, uint flags, int fildes, uint off);

            [DllImport("libc", EntryPoint = "munmap")]
            internal static extern int munmap(void* addr, uint len);

            [DllImport("libc", EntryPoint = "mprotect")]
            internal static extern int mprotect(void* start, ulong len, uint prot);
        }

        public class InternalWindows
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            internal static extern byte* VirtualAlloc(void* lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            internal static extern bool VirtualFree(void* lpAddress, uint dwSize, uint dwFreeType);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            internal static extern uint GetLastError();

            [DllImport("Kernel32.dll")]
            internal static extern nint GetConsoleWindow();

            [DllImport("user32.dll")]
            internal static extern bool ShowWindow(nint hWnd, int nCmdShow);
        }

        const uint MEM_RESERVE = 0x2000;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 0x04;
        const uint PAGE_GUARD = 0x100;
        const uint MEM_DECOMMIT = 0x4000;
        const uint MEM_RELEASE = 0x8000;
        const uint PROT_NONE = 0;
        const uint PROT_READ = 1;
        const uint PROT_WRITE = 2;
        const uint PROT_EXEC = 4;
        const uint MAP_SHARED = 0x01;
        const uint MAP_PRIVATE = 0x02;
        const uint MAP_FIXED = 0x10;
        const uint MAP_ANONYMOUS = 0x20;
        const uint MAP_GROWSDOWN = 0x0100;
        const uint MAP_DENYWRITE = 0x0800;
        const uint MAP_EXECUTABLE = 0x1000;
        const uint MAP_LOCKED = 0x2000;
        const uint MAP_NORESERVE = 0x4000;
    }

    public static class ProcessUtils
    {
        public static ProcessResult ExecuteCommand(string command, string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new ProcessResult
                {
                    OutputString = output.Trim(),
                    ErrorString = error.Trim(),
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                return new ProcessResult
                {
                    OutputString = string.Empty,
                    ErrorString = ex.Message,
                    ExitCode = -1
                };
            }
        }

        public struct ProcessResult
        {
            public string OutputString;
            public string ErrorString;
            public int ExitCode;
        }
    }
}

#pragma warning restore CS8603