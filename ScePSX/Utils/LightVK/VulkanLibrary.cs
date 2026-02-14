using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SysNativeLibrary = System.Runtime.InteropServices.NativeLibrary;

namespace LightVK
{
    public static unsafe partial class VulkanNative
    {
        private const CallingConvention CallConv = CallingConvention.StdCall;

        public static VulkanLibrary NativeLib;

        static VulkanNative()
        {
            NativeLib = LoadNativeLibrary();
            LoadFunctionPointers();
        }

        private static VulkanLibrary LoadNativeLibrary()
        {
            return VulkanLibrary.Load(GetVulkanName());
        }

        private static string GetVulkanName()
        {
            if (OperatingSystem.IsWindows())
            {
                return "vulkan-1.dll";
            } else if (OperatingSystem.IsAndroid())
            {
                return "libvulkan.so";
            } else if (OperatingSystem.IsLinux())
            {
                return "libvulkan.so.1";
            } else if (OperatingSystem.IsMacOS())
            {
                return "libvulkan.dylib";
            } else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }

    public class VulkanLibrary : IDisposable
    {
        private readonly string libraryName;
        private readonly IntPtr libraryHandle;
        internal VkInstance instance;

        public IntPtr NativeHandle => libraryHandle;

        protected VulkanLibrary(string libraryName)
        {
            this.libraryName = libraryName;
            libraryHandle = LoadLibrary(this.libraryName);
            if (libraryHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not load " + libraryName);
            }
        }

        protected IntPtr LoadLibrary(string libraryName)
        {
            if (SysNativeLibrary.TryLoad(libraryName, typeof(VulkanLibrary).Assembly, null, out var lib))
            {
                return lib;
            }

            Debug.WriteLine($" ===> Error loading native library {libraryName}");
            return IntPtr.Zero;
        }

        protected void FreeLibrary(IntPtr libraryHandle)
        {
            if (libraryHandle != IntPtr.Zero)
            {
                SysNativeLibrary.Free(libraryHandle);
            }
        }

        public unsafe void LoadFunction<T>(string name, out T field)
        {
            SysNativeLibrary.TryGetExport(libraryHandle, name, out IntPtr funcPtr);
            if (funcPtr == IntPtr.Zero)
            {
                funcPtr = VulkanNative.vkGetInstanceProcAddr(instance, (byte*)Marshal.StringToHGlobalAnsi(name));
            }

            if (funcPtr != IntPtr.Zero)
            {
                field = Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
            } else
            {
                field = default(T);
                Debug.WriteLine($" ===> Error loading function {name}");
            }
        }

        public void Dispose()
        {
            FreeLibrary(libraryHandle);
        }

        public static VulkanLibrary Load(string libraryName)
        {
            return new VulkanLibrary(libraryName);
        }
    }
}
