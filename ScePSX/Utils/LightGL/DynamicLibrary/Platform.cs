using System;
using System.Runtime.InteropServices;

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
        mips,
    }

    public static unsafe class Platform
    {
        public static bool IsWindows => OS == OS.Windows;

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
                    //Environment.OSVersion
                    if (OS == OS.Windows)
                    {
#pragma warning disable CS8601
                        _Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
#pragma warning restore CS8601
                        if (_Architecture == null)
                            _Architecture = "Can't get arch";
                    } else
                    {
                        try
                        {
                            var Result = ProcessUtils.ExecuteCommand("uname", "-m");
                            _Architecture = Result.OutputString;
                            //_Architecture = Environment.GetEnvironmentVariable("HOSTTYPE");
                            //_Architecture = Environment.GetEnvironmentVariable("MACHTYPE");
                        } catch
                        {
                            _Architecture = "Can't get arch";
                        }
                    }
                }
                switch (_Architecture)
                {
                    case "AMD64":
                        return Architecture.x64;
                }
                return (Architecture)Enum.Parse(typeof(Architecture), _Architecture);
            }
        }

        static Platform()
        {
            IsMono = Type.GetType("Mono.Runtime") != null;

            switch (Environment.OSVersion.Platform)
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
                    } else
                    {
                        OS = OS.Linux;
                    }
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }

            UnixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

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

#if false
			[DllImport("libc")]
			//[SuppressUnmanagedCodeSecurity]
			//[return: MarshalAs(UnmanagedType.BStr)]
			[return: MarshalAs(UnmanagedType.LPStr)]
			internal static extern string strerror(int errno);
#else
            [DllImport("libc", EntryPoint = "strerror")]
            private static extern nint _strerror(int errno);
            public static string strerror(int errno)
            {
                var ret = Marshal.PtrToStringAnsi(_strerror(errno));
                if (ret == null)
                    return string.Empty;
                return ret;
            }
#endif

            public static int errno()
            {
                return *__errno_location();
            }

            public static void reset_errno()
            {
                *__errno_location() = 0;
            }

            [DllImport("libc", EntryPoint = "mmap")]
            //internal static extern void* mmap(void* addr, uint len, uint prot, uint flags, uint off_t);
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

        /*
        const uint MAP_ANON = 1;
        const uint MAP_ANONYMOUS = 1;
        const uint MAP_FILE = 2;
        const uint MAP_PRIVATE = 4;
        const uint MAP_SHARED = 8;
        const uint MAP_FIXED = 0x10;
        */

        const uint PROT_NONE = 0;
        const uint PROT_READ = 1;
        const uint PROT_WRITE = 2;
        const uint PROT_EXEC = 4;

        const uint MAP_SHARED = 0x01; // Share changes
        const uint MAP_PRIVATE = 0x02; // Changes are private

        // #elif defined(__i386__) || defined(__s390__) || defined(__x86_64__)

        const uint MAP_FIXED = 0x10; // Interpret addr exactly
        const uint MAP_ANONYMOUS = 0x20; // don't use a file
        const uint MAP_GROWSDOWN = 0x0100; // stack-like segment
        const uint MAP_DENYWRITE = 0x0800; // ETXTBSY
        const uint MAP_EXECUTABLE = 0x1000; // mark it as an executable
        const uint MAP_LOCKED = 0x2000; // pages are locked
        const uint MAP_NORESERVE = 0x4000; //  don't check for reservations

        // const int MAP_FIXED	0x10		/* Interpret addr exactly */
        // const int MAP_ANONYMOUS	0x20		/* don't use a file */
        // const int MAP_GROWSDOWN	0x0100		/* stack-like segment */
        // const int MAP_DENYWRITE	0x0800		/* ETXTBSY */
        // const int MAP_EXECUTABLE	0x1000		/* mark it as an executable */
        // const int MAP_LOCKED	0x2000		/* pages are locked */
        // const int MAP_NORESERVE	0x4000		/* don't check for reservations */
        // const int MS_ASYNC	1		/* sync memory asynchronously */
        // const int MS_INVALIDATE	2		/* invalidate the caches */
        // const int MS_SYNC		4		/* synchronous memory sync */
        // const int MCL_CURRENT	1		/* lock all current mappings */
        // const int MCL_FUTURE	2		/* lock all future mappings */
        // const int MADV_NORMAL	0x0		/* default page-in behavior */
        // const int MADV_RANDOM	0x1		/* page-in minimum required */
        // const int MADV_SEQUENTIAL	0x2		/* read-ahead aggressively */
        // const int MADV_WILLNEED	0x3		/* pre-fault pages */
        // const int MADV_DONTNEED	0x4		/* discard these pages */

#if false
		const int EGENERIC      (_SIGN 99)  /* generic error */
		const int EPERM         (_SIGN  1)  /* operation not permitted */
		const int ENOENT        (_SIGN  2)  /* no such file or directory */
		const int ESRCH         (_SIGN  3)  /* no such process */
		const int EINTR         (_SIGN  4)  /* interrupted function call */
		const int EIO           (_SIGN  5)  /* input/output error */
		const int ENXIO         (_SIGN  6)  /* no such device or address */
		const int E2BIG         (_SIGN  7)  /* arg list too long */
		const int ENOEXEC       (_SIGN  8)  /* exec format error */
		const int EBADF         (_SIGN  9)  /* bad file descriptor */
		const int ECHILD        (_SIGN 10)  /* no child process */
		const int EAGAIN        (_SIGN 11)  /* resource temporarily unavailable */
		const int ENOMEM        (_SIGN 12)  /* not enough space */
		const int EACCES        (_SIGN 13)  /* permission denied */
		const int EFAULT        (_SIGN 14)  /* bad address */
		const int ENOTBLK       (_SIGN 15)  /* Extension: not a block special file */
		const int EBUSY         (_SIGN 16)  /* resource busy */
		const int EEXIST        (_SIGN 17)  /* file exists */
		const int EXDEV         (_SIGN 18)  /* improper link */
		const int ENODEV        (_SIGN 19)  /* no such device */
		const int ENOTDIR       (_SIGN 20)  /* not a directory */
		const int EISDIR        (_SIGN 21)  /* is a directory */
		const int EINVAL        (_SIGN 22)  /* invalid argument */
		const int ENFILE        (_SIGN 23)  /* too many open files in system */
		const int EMFILE        (_SIGN 24)  /* too many open files */
		const int ENOTTY        (_SIGN 25)  /* inappropriate I/O control operation */
		const int ETXTBSY       (_SIGN 26)  /* no longer used */
		const int EFBIG         (_SIGN 27)  /* file too large */
		const int ENOSPC        (_SIGN 28)  /* no space left on device */
		const int ESPIPE        (_SIGN 29)  /* invalid seek */
		const int EROFS         (_SIGN 30)  /* read-only file system */
		const int EMLINK        (_SIGN 31)  /* too many links */
		const int EPIPE         (_SIGN 32)  /* broken pipe */
		const int EDOM          (_SIGN 33)  /* domain error       (from ANSI C std) */
		const int ERANGE        (_SIGN 34)  /* result too large   (from ANSI C std) */
		const int EDEADLK       (_SIGN 35)  /* resource deadlock avoided */
		const int ENAMETOOLONG  (_SIGN 36)  /* file name too long */
		const int ENOLCK        (_SIGN 37)  /* no locks available */
		const int ENOSYS        (_SIGN 38)  /* function not implemented */
		const int ENOTEMPTY     (_SIGN 39)  /* directory not empty */
		const int ELOOP         (_SIGN 40)  /* too many levels of symlinks detected */
#endif

        public static DateTime UnixStart;

        public static long CurrentUnixMicroseconds => (DateTime.UtcNow - UnixStart).Ticks / (TimeSpan.TicksPerMillisecond / 1000);

        private const int SW_HIDE = 0;

        public static void HideConsole()
        {
            var hwnd = InternalWindows.GetConsoleWindow();
            InternalWindows.ShowWindow(hwnd, SW_HIDE);
        }

        public static bool IsMono
        {
            get; private set;
        }

        [DllImport("libc")]
        static extern int uname(nint buf);

        static bool IsRunningOnMac()
        {
            var buf = nint.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname(buf) == 0)
                {
                    var os = Marshal.PtrToStringAnsi(buf);
                    if (os == "Darwin")
                        return true;
                }
            } catch
            {
            } finally
            {
                if (buf != nint.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
            return false;
        }
    }
}
