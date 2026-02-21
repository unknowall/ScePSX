/*
 * LightVK
 * 
 * github: http://github.com/unknowall/LightVK
 * 
 * for projects:
 *
 * github: http://github.com/unknowall/ScePSX
 * 
 * github: http://github.com/unknowall/ScePSP
 * 
 * unknowall - sgfree@hotmail.com
 * 
 */

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static LightVK.VulkanNative;

namespace LightVK
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate VkBool32 vkDebugReportCallback(
        VkDebugReportFlagsEXT flags,
        VkDebugReportObjectTypeEXT objectType,
        ulong objectHandle,
        IntPtr location,
        int messageCode,
        IntPtr pLayerPrefix,
        IntPtr pMessage,
        IntPtr pUserData
        );

    public class VulkanDevice : IDisposable
    {
        public static vkOsEnv OsEnv = vkOsEnv.WIN;
        public VkInstance instance;
        public VkPhysicalDevice physicalDevice;
        public VkDevice device;
        public VkPhysicalDeviceProperties deviceProperties;

        public VkQueue graphicsQueue;
        public VkQueue presentQueue;
        public VkSurfaceKHR surface;

        private int graphicsQueueFamilyIndex = -1;
        private int presentQueueFamilyIndex = -1;

        private static VkDebugReportCallbackEXT _debugCallback;
        private static vkDebugReportCallback _callbackDelegate;

        public enum vkOsEnv
        {
            WIN,
            LINUX_XLIB,
            LINUX_WAYLAND,
            ANDROID,
            MACOS
        }

        public struct vkSwapchain
        {
            public VkSwapchainKHR Chain;
            public vkRawList<VkImage> Images;
            public vkRawList<VkImageView> ImageViews;
            public vkRawList<VkFramebuffer> framebuffes;
            public VkFormat ImageFormat;
            public VkExtent2D Extent;
        }

        public struct vkMultisample
        {
            public VkImage ColorImage;
            public VkImageView ColorImageView;
            public VkDeviceMemory ColorImageMemory;
        }

        public struct vkBuffer
        {
            public ulong size;
            public VkBuffer stagingBuffer;
            public VkDeviceMemory stagingMemory;
            public unsafe void* mappedData;
        }

        public struct vkTexture
        {
            public int width;
            public int height;
            public VkFormat format;
            public VkImage image;
            public VkImageView imageview;
            public VkDeviceMemory imagememory;
            public VkSampler sampler;
            public VkImageLayout layout;
        }

        public struct vkGraphicsPipeline
        {
            public int width;
            public int height;
            public VkPipeline pipeline;
            public VkPipelineLayout layout;
            public VkDescriptorSetLayout descriptorSetLayout;
            public VkDescriptorSet descriptorSet;
        }

        public struct vkCMDS
        {
            public vkRawList<VkCommandBuffer> CMD;
            public VkCommandPool pool;
        }

        bool isDebug = false;
        bool isDisposed = false;
        bool isinit = false;

        public VulkanDevice()
        {

        }

        public unsafe void Dispose()
        {
            if (isDisposed || !isinit)
                return;

            VulkanDispose();
        }

        public unsafe void VulkanInit(IntPtr hwnd, IntPtr hinst, bool validation = false)
        {
            Console.WriteLine($"[Vulkan Device] VulkanDevice Initialization....");

            if (!validation)
            {
                CreateInstance();
            } else
            {
                CreateDebugInstance();
            }
            if (OperatingSystem.IsWindows() || OsEnv == vkOsEnv.WIN)
            {
                CreateSurfaceWin(hinst, hwnd);
            }
            if (OperatingSystem.IsLinux() || OsEnv == vkOsEnv.LINUX_XLIB)
            {
                CreateSurfaceLinuxXLib(hinst, hwnd);
            }
            if (OperatingSystem.IsLinux() && OsEnv == vkOsEnv.LINUX_WAYLAND)
            {
                CreateSurfaceLinuxWayLand(hinst, hwnd);
            }
            if (OperatingSystem.IsAndroid() || OsEnv == vkOsEnv.ANDROID)
            {
                CreateSurfaceAndroid(hwnd);
            }
            if (OperatingSystem.IsMacOS() || OsEnv == vkOsEnv.MACOS)
            {
                CreateSurfaceMacOS(hwnd);
            }
            SelectPhysicalDevice();
            fixed (VkPhysicalDeviceProperties* ptr = &deviceProperties)
                vkGetPhysicalDeviceProperties(physicalDevice, ptr);
            CreateLogicalDevice();

            Console.WriteLine($"[Vulkan Device] VulkanDevice Initializationed...");

            isinit = true;
        }

        public unsafe void VulkanDispose()
        {
            vkDestroySurfaceKHR(instance, surface, null);

            vkDestroyDevice(device, null);
            if (isDebug)
            {
                IntPtr proc;
                byte[] bytes = Encoding.UTF8.GetBytes("vkDestroyDebugReportCallbackEXT" + '\0');
                fixed (byte* ptr = bytes)
                {
                    proc = vkGetInstanceProcAddr(instance, ptr);
                    if (proc == IntPtr.Zero)
                    {
                        Console.WriteLine("[Vulkan Device] vkDestroyDebugReportCallbackEXT not available.");
                        GC.KeepAlive(_callbackDelegate);
                        isDebug = true;
                        return;
                    }
                }

                var createCallback = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugReportCallbackEXTDelegate>(proc);

                createCallback(instance, _debugCallback, null);
            }
            vkDestroyInstance(instance, null);

            Console.WriteLine($"[Vulkan Device] Disposed");

            isDisposed = true;
        }

        private static uint VK_MAKE_VERSION(uint major, uint minor, uint patch)
        {
            return (major << 22) | (minor << 12) | patch;
        }

        private unsafe void CreateInstance()
        {
            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
                pApplicationName = vkStrings.AppName,
                applicationVersion = VK_MAKE_VERSION(1, 0, 0),
                pEngineName = vkStrings.EngineName,
                engineVersion = VK_MAKE_VERSION(1, 0, 0),
                apiVersion = VK_MAKE_VERSION(1, 0, 0)
            };
            var instanceCreateInfo = new VkInstanceCreateInfo();
            instanceCreateInfo.pApplicationInfo = &appInfo;

            vkRawList<IntPtr> Extensions = new vkRawList<IntPtr>();
            Extensions.Add(vkStrings.VK_KHR_SURFACE_EXTENSION_NAME);
            if (OperatingSystem.IsWindows() || OsEnv == vkOsEnv.WIN)
            {
                Extensions.Add(vkStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsLinux() || OsEnv == vkOsEnv.LINUX_XLIB)
            {
                Extensions.Add(vkStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsLinux() && OsEnv == vkOsEnv.LINUX_WAYLAND)
            {
                Extensions.Add(vkStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsAndroid() || OsEnv == vkOsEnv.ANDROID)
            {
                Extensions.Add(vkStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsMacOS() || OsEnv == vkOsEnv.MACOS)
            {
                Extensions.Add(vkStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME);
            }

            fixed (IntPtr* extensionsBase = &Extensions.Items[0])
            {
                instanceCreateInfo.enabledExtensionCount = Extensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = (byte**)extensionsBase;

                VkResult result;
                fixed (VkInstance* Ptr2 = &instance)
                {
                    result = vkCreateInstance(&instanceCreateInfo, null, Ptr2);
                }
                if (result != VkResult.VK_SUCCESS)
                {
                    Console.WriteLine($"vkCreateInstance failed with: {result}");

                    if (result == VkResult.VK_ERROR_EXTENSION_NOT_PRESENT)
                        Console.WriteLine("fail: VK_ERROR_EXTENSION_NOT_PRESENT");
                    else if (result == VkResult.VK_ERROR_LAYER_NOT_PRESENT)
                        Console.WriteLine("fail：VK_ERROR_LAYER_NOT_PRESENT");
                    else if (result == VkResult.VK_ERROR_INCOMPATIBLE_DRIVER)
                        Console.WriteLine("fail：VK_ERROR_INCOMPATIBLE_DRIVER");

                    throw new Exception("Failed to create Vulkan instance!");
                }
            }
        }

        private unsafe void CreateDebugInstance()
        {
            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
                pApplicationName = vkStrings.AppName,
                applicationVersion = VK_MAKE_VERSION(1, 0, 0),
                pEngineName = vkStrings.EngineName,
                engineVersion = VK_MAKE_VERSION(1, 0, 0),
                apiVersion = VK_MAKE_VERSION(1, 0, 0)
            };
            var instanceCreateInfo = new VkInstanceCreateInfo();
            instanceCreateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
            instanceCreateInfo.pApplicationInfo = &appInfo;

            vkRawList<IntPtr> Extensions = new vkRawList<IntPtr>();

            Extensions.Add(vkStrings.VK_KHR_SURFACE_EXTENSION_NAME);
            if (OperatingSystem.IsWindows() || OsEnv == vkOsEnv.WIN)
            {
                Extensions.Add(vkStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsLinux() || OsEnv == vkOsEnv.LINUX_XLIB)
            {
                Extensions.Add(vkStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsLinux() && OsEnv == vkOsEnv.LINUX_WAYLAND)
            {
                Extensions.Add(vkStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsAndroid() || OsEnv == vkOsEnv.ANDROID)
            {
                Extensions.Add(vkStrings.VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
            }
            if (OperatingSystem.IsMacOS() || OsEnv == vkOsEnv.MACOS)
            {
                Extensions.Add(vkStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME);
            }
            Extensions.Add(vkStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);

            vkRawList<IntPtr> Extensions1 = new vkRawList<IntPtr>();

            Extensions1.Add(vkStrings.VK_LAYER_KHRONOS_validation);

            fixed (IntPtr* extensionsBase = &Extensions.Items[0])
            fixed (IntPtr* ppEnabledLayerNames = &Extensions1.Items[0])
            {
                instanceCreateInfo.enabledExtensionCount = Extensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = (byte**)extensionsBase;

                instanceCreateInfo.enabledLayerCount = 1;
                instanceCreateInfo.ppEnabledLayerNames = (byte**)ppEnabledLayerNames;

                VkResult result;
                fixed (VkInstance* instance = &this.instance)
                {
                    result = vkCreateInstance(&instanceCreateInfo, null, instance);
                }
                if (result != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan instance!");
                }
            }

            _callbackDelegate = DebugCallbackHandler;
            IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackDelegate);

            var debugInfo = new VkDebugReportCallbackCreateInfoEXT
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEBUG_REPORT_CALLBACK_CREATE_INFO_EXT,
                flags = VkDebugReportFlagsEXT.VK_DEBUG_REPORT_ERROR_BIT_EXT | VkDebugReportFlagsEXT.VK_DEBUG_REPORT_WARNING_BIT_EXT,
                pfnCallback = callbackPtr,
                pUserData = null,
                pNext = null
            };

            _debugCallback = new VkDebugReportCallbackEXT(0);

            IntPtr proc;
            byte[] bytes = Encoding.UTF8.GetBytes("vkCreateDebugReportCallbackEXT" + '\0');
            fixed (byte* ptr = bytes)
            {
                proc = vkGetInstanceProcAddr(instance, ptr);
                if (proc == IntPtr.Zero)
                {
                    Console.WriteLine("[Vulkan Device] vkCreateDebugReportCallbackEXT not available.");
                    GC.KeepAlive(_callbackDelegate);
                    isDebug = true;
                    return;
                }
            }

            var createCallback = Marshal.GetDelegateForFunctionPointer<vkCreateDebugReportCallbackEXTDelegate>(proc);

            fixed (VkDebugReportCallbackEXT* pCallback = &_debugCallback)
            {
                VkResult dbgRes = createCallback(instance, &debugInfo, null, pCallback);
                if (dbgRes != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan debug report callback!");
                }
            }

            GC.KeepAlive(_callbackDelegate);

            isDebug = true;
        }

        private static VkBool32 DebugCallbackHandler(
            VkDebugReportFlagsEXT flags,
            VkDebugReportObjectTypeEXT objectType,
            ulong objectHandle,
            IntPtr location,
            int messageCode,
            IntPtr pLayerPrefix,
            IntPtr pMessage,
            IntPtr pUserData)
        {
            string message = Marshal.PtrToStringAnsi(pMessage);
            Console.WriteLine($"\r\n[VULKAN DBEUG] {flags}:\r\n\r\n{message}\r\n");
            return VkBool32.False;
        }

        private unsafe void CreateSurfaceWin(IntPtr hinstance, IntPtr hwnd)
        {
            var surfaceCreateInfo = new VkWin32SurfaceCreateInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
                hinstance = hinstance,
                hwnd = hwnd
            };

            fixed (VkSurfaceKHR* surface = &this.surface)
                if (vkCreateWin32SurfaceKHR(instance, &surfaceCreateInfo, null, surface) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan surface!");
                }
        }

        private unsafe void CreateSurfaceLinuxXLib(IntPtr display, IntPtr window)
        {
            var surfaceCreateInfo = new VkXlibSurfaceCreateInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR,
                dpy = display,
                window = (IntPtr)(uint)window
            };

            fixed (VkSurfaceKHR* surface = &this.surface)
            {
                if (vkCreateXlibSurfaceKHR(instance, &surfaceCreateInfo, null, surface) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan X11 Surface!");
                }
            }
        }

        private unsafe void CreateSurfaceLinuxWayLand(IntPtr display, IntPtr window)
        {
            var surfaceCreateInfo = new VkWaylandSurfaceCreateInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR,
                display = display,
                surface = (IntPtr)(uint)window
            };

            fixed (VkSurfaceKHR* surface = &this.surface)
            {
                if (vkCreateWaylandSurfaceKHR(instance, &surfaceCreateInfo, null, surface) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan WayLand Surface!");
                }
            }
        }

        private unsafe void CreateSurfaceAndroid(IntPtr window)
        {
            var surfaceCreateInfo = new VkAndroidSurfaceCreateInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR,
                window = window,
                pNext = null,
                flags = 0,
            };

            fixed (VkSurfaceKHR* surface = &this.surface)
            {
                if (vkCreateAndroidSurfaceKHR(instance, &surfaceCreateInfo, null, surface) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan Android Surface!");
                }
            }
        }

        private unsafe void CreateSurfaceMacOS(IntPtr window)
        {
            var surfaceCreateInfo = new VkMacOSSurfaceCreateInfoMVK
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MACOS_SURFACE_CREATE_INFO_MVK,
                pView = (void*)window,
                pNext = null,
                flags = 0,
            };

            fixed (VkSurfaceKHR* surface = &this.surface)
            {
                if (vkCreateMacOSSurfaceMVK(instance, &surfaceCreateInfo, null, surface) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan MacOS Surface!");
                }
            }
        }

        private unsafe void SelectPhysicalDevice()
        {
            uint deviceCount = 0;
            var result = vkEnumeratePhysicalDevices(instance, &deviceCount, null);
            Console.WriteLine($"vkEnumeratePhysicalDevices result: {result}, deviceCount: {deviceCount}");
            if (deviceCount == 0)
            {
                throw new Exception("No Vulkan-capable physical devices found!");
            }

            var physicalDevices = new VkPhysicalDevice[deviceCount];
            vkEnumeratePhysicalDevices(instance, &deviceCount, (VkPhysicalDevice*)Marshal.UnsafeAddrOfPinnedArrayElement(physicalDevices, 0));

            foreach (var device in physicalDevices)
            {
                VkPhysicalDeviceProperties deviceProperties;
                VkPhysicalDeviceFeatures deviceFeatures;
                vkGetPhysicalDeviceProperties(device, &deviceProperties);
                vkGetPhysicalDeviceFeatures(device, &deviceFeatures);

                uint queueFamilyCount = 0;
                vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

                var queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
                vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, (VkQueueFamilyProperties*)Marshal.UnsafeAddrOfPinnedArrayElement(queueFamilies, 0));

                bool hasGraphicsQueue = false;
                for (int i = 0; i < queueFamilies.Length; i++)
                {
                    if ((queueFamilies[i].queueFlags & VkQueueFlags.VK_QUEUE_GRAPHICS_BIT) != 0)
                    {
                        hasGraphicsQueue = true;
                        break;
                    }
                }
                if (hasGraphicsQueue)
                {
                    physicalDevice = device;
                    Console.WriteLine($"[Vulkan Device] VulkanDevice: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.deviceName)}");
                    return;
                }
            }

            throw new Exception("No suitable physical device found!");
        }

        private unsafe void CreateLogicalDevice()
        {
            uint queueFamilyCount = 0;
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);

            var queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, (VkQueueFamilyProperties*)Marshal.UnsafeAddrOfPinnedArrayElement(queueFamilies, 0));

            for (int i = 0; i < queueFamilies.Length; i++)
            {
                if ((queueFamilies[i].queueFlags & VkQueueFlags.VK_QUEUE_GRAPHICS_BIT) != 0)
                {
                    graphicsQueueFamilyIndex = i;
                }

                VkBool32 presentSupported;
                vkGetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)i, surface, &presentSupported);
                if (presentSupported)
                {
                    presentQueueFamilyIndex = i;
                }

                if (graphicsQueueFamilyIndex != -1 && presentQueueFamilyIndex != -1)
                {
                    break;
                }
            }

            if (graphicsQueueFamilyIndex == -1 || presentQueueFamilyIndex == -1)
            {
                throw new Exception("Failed to find required queue families!");
            }

            float queuePriority = 1.0f;
            var queueCreateInfo = new VkDeviceQueueCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                queueCount = 1,
                pQueuePriorities = &queuePriority
            };

            VkPhysicalDeviceFeatures enabledFeatures = new VkPhysicalDeviceFeatures();
            enabledFeatures.samplerAnisotropy = VkBool32.True;
            enabledFeatures.dualSrcBlend = VkBool32.True;

            var timelineFeaturesEnable = new VkPhysicalDeviceTimelineSemaphoreFeatures
            {
                //VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_TIMELINE_SEMAPHORE_FEATURES
                sType = (VkStructureType)1000207000,
                timelineSemaphore = VkBool32.True
            };

            var deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
                queueCreateInfoCount = 1,
                pQueueCreateInfos = &queueCreateInfo,
                pEnabledFeatures = &enabledFeatures,
                pNext = &timelineFeaturesEnable
            };

            vkRawList<IntPtr> instanceExtensions = new vkRawList<IntPtr>();
            instanceExtensions.Add(vkStrings.VK_KHR_SWAPCHAIN_EXTENSION_NAME);

            fixed (IntPtr* ppEnabledExtensionNames = &instanceExtensions.Items[0])
            {
                deviceCreateInfo.enabledExtensionCount = instanceExtensions.Count;
                deviceCreateInfo.ppEnabledExtensionNames = (byte**)ppEnabledExtensionNames;

                fixed (VkDevice* device = &this.device)
                    if (vkCreateDevice(physicalDevice, &deviceCreateInfo, null, device) != VkResult.VK_SUCCESS)
                    {
                        throw new Exception("Failed to create logical device!");
                    }
            }

            fixed (VkQueue* graphicsQueue = &this.graphicsQueue)
                vkGetDeviceQueue(device, (uint)graphicsQueueFamilyIndex, 0, graphicsQueue);
            fixed (VkQueue* presentQueue = &this.presentQueue)
                vkGetDeviceQueue(device, (uint)presentQueueFamilyIndex, 0, presentQueue);
        }

        public unsafe vkMultisample CreateMultisample(int width, int height, VkSampleCountFlags sampleCount)
        {
            vkMultisample multisample = new vkMultisample();

            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
                imageType = VkImageType.VK_IMAGE_TYPE_2D,
                format = VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                samples = sampleCount,
                tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
                usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSIENT_ATTACHMENT_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED
            };

            VkImage ColorImage;
            vkCreateImage(device, &imageInfo, null, &ColorImage);

            multisample.ColorImage = ColorImage;

            VkMemoryRequirements memReqs;
            vkGetImageMemoryRequirements(device, multisample.ColorImage, &memReqs);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memReqs.size,
                memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
            };

            VkDeviceMemory ColorImageMemory;
            vkAllocateMemory(device, &allocInfo, null, &ColorImageMemory);

            multisample.ColorImageMemory = ColorImageMemory;

            vkBindImageMemory(device, multisample.ColorImage, multisample.ColorImageMemory, 0);

            var viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                image = multisample.ColorImage,
                viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
                format = VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            VkImageView ColorImageView;
            vkCreateImageView(device, &viewInfo, null, &ColorImageView);

            multisample.ColorImageView = ColorImageView;

            return multisample;
        }

        public unsafe void DestroyMuiltsample(vkMultisample sample)
        {
            if (sample.ColorImage != VkImage.Null)
                vkDestroyImage(device, sample.ColorImage, null);

            if (sample.ColorImageView != VkImageView.Null)
                vkDestroyImageView(device, sample.ColorImageView, null);

            if (sample.ColorImageMemory != VkDeviceMemory.Null)
                vkFreeMemory(device, sample.ColorImageMemory, null);
        }

        public unsafe vkSwapchain CreateSwapChain(VkRenderPass pass, int width, int height)
        {
            vkSwapchain chain = new vkSwapchain();

            // 查询表面能力
            VkSurfaceCapabilitiesKHR surfaceCapabilities;
            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, &surfaceCapabilities);

            // 钳位尺寸到有效范围
            var extent = new VkExtent2D((uint)width, (uint)height);
            extent.width = Math.Clamp(extent.width, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width);
            extent.height = Math.Clamp(extent.height, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height);

            var surfaceFormat = ChooseSurfaceFormat();
            var presentMode = ChoosePresentMode();
            //var extent = new VkExtent2D((uint)width, (uint)height);

            uint imageCount = GetSwapChainImageCount();
            var swapChainCreateInfo = new VkSwapchainCreateInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
                surface = surface,
                minImageCount = imageCount,
                imageFormat = surfaceFormat.format,
                imageColorSpace = surfaceFormat.colorSpace,
                imageExtent = extent,
                imageArrayLayers = 1,
                imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
                preTransform = surfaceCapabilities.currentTransform,
                //preTransform = VkSurfaceTransformFlagsKHR.InheritKHR,
                compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
                presentMode = presentMode,
                clipped = true
            };

            VkSwapchainKHR Chain;

            if (vkCreateSwapchainKHR(device, &swapChainCreateInfo, null, &Chain) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create swap chain!");
            }

            chain.Chain = Chain;

            vkGetSwapchainImagesKHR(device, chain.Chain, &imageCount, null);

            chain.Images = new vkRawList<VkImage>(imageCount);

            fixed (VkImage* outptr = &chain.Images.Items[0])
                vkGetSwapchainImagesKHR(device, chain.Chain, &imageCount, outptr);

            chain.ImageViews = new vkRawList<VkImageView>(imageCount);
            //chain.ImageViews2 = new vkRawList<VkImageView>(imageCount);

            chain.ImageFormat = surfaceFormat.format;

            chain.Extent = extent;

            for (int i = 0; i < chain.Images.Count; i++)
            {
                Console.WriteLine($"[Vulkan Device] SwapChain Image {i} 0x{chain.Images[i].Handle:X}");
                chain.ImageViews[i] = CreateImageView(chain.Images[i], chain.ImageFormat);
                //chain.ImageViews2[i] = CreateImageView(chain.Images[i], chain.ImageFormat);
            }

            chain.framebuffes = new vkRawList<VkFramebuffer>(imageCount);

            for (uint i = 0; i < chain.framebuffes.Count; i++)
            {
                chain.framebuffes[i] = CreateFramebuffer(pass, chain.ImageViews[i], extent.width, extent.height);
            }

            return chain;
        }

        public unsafe void CleanupSwapChain(vkSwapchain Chain)
        {
            foreach (var fb in Chain.framebuffes)
            {
                if (fb != VkFramebuffer.Null)
                {
                    vkDestroyFramebuffer(device, fb, null);
                }
            }
            Chain.framebuffes.Resize(0);
            Chain.framebuffes = null;
            foreach (var imageView in Chain.ImageViews)
            {
                if (imageView != VkImageView.Null)
                {
                    vkDestroyImageView(device, imageView, null);
                }
            }
            Chain.ImageViews.Resize(0);
            Chain.ImageViews = null;

            //foreach (var image in Chain.Images)
            //{ 
            //    if (image != VkImage.Null)
            //    {
            //
            //        vkDestroyImage(device, image, null);
            //    }
            //}
            Chain.Images.Resize(0);
            Chain.Images = null;


            if (Chain.Chain != VkSwapchainKHR.Null)
            {
                vkDestroySwapchainKHR(device, Chain.Chain, null);
                Chain.Chain = VkSwapchainKHR.Null;
            }
        }

        public unsafe VkRenderPass CreateRenderPass(VkFormat format,
            VkAttachmentLoadOp loadop = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            VkImageLayout initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            bool Depth = false,
            bool HasSubpass = false
            )
        {
            int attachcount = Depth ? 2 : 1;

            var colorAttachment = new VkAttachmentDescription
            {
                format = format,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                loadOp = loadop,
                storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
                stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
                initialLayout = initialLayout,
                finalLayout = finalLayout
            };

            var depthAttachment = new VkAttachmentDescription
            {
                format = VkFormat.VK_FORMAT_D16_UNORM,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
                storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
                stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL,
                finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
            };

            var colorAttachmentRef = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            };

            var inputRef = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
            };

            var depthAttachmentRef = new VkAttachmentReference
            {
                attachment = 1,
                layout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
            };

            var subpass1 = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                inputAttachmentCount = 1,
                pInputAttachments = &inputRef
            };

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                colorAttachmentCount = 1,
                pColorAttachments = &colorAttachmentRef,
                pResolveAttachments = null,
                pDepthStencilAttachment = Depth ? &depthAttachmentRef : null,
            };

            var dependency = new VkSubpassDependency
            {
                srcSubpass = (uint)(HasSubpass ? 0 : unchecked((uint)-1)),
                dstSubpass = (uint)(HasSubpass ? 1 : 0),

                srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                srcAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
                dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT,
                dependencyFlags = VkDependencyFlags.VK_DEPENDENCY_BY_REGION_BIT
            };

            if (Depth)
            {
                dependency.srcStageMask |= VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
                dependency.dstStageMask |= VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
                dependency.srcAccessMask |= VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
                dependency.dstAccessMask |= VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT;
            }

            var attachments = stackalloc VkAttachmentDescription[2] { colorAttachment, depthAttachment };

            var mulitsubpas = stackalloc VkSubpassDescription[2] { subpass, subpass1 };

            var renderPassInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
                attachmentCount = (uint)attachcount,
                pAttachments = attachments,
                subpassCount = (uint)(HasSubpass ? 2 : 1),
                pSubpasses = HasSubpass ? mulitsubpas : &subpass,
                dependencyCount = 1,
                pDependencies = &dependency
            };

            VkRenderPass pass;

            if (vkCreateRenderPass(device, &renderPassInfo, null, &pass) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create render pass!");
            }

            return pass;
        }

        public unsafe VkRenderPass CreateRenderPass(VkFormat colorFormat, VkSampleCountFlags sampleCount)
        {
            // 多采样颜色附件
            var colorAttachment = new VkAttachmentDescription
            {
                format = colorFormat,
                samples = sampleCount,
                loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
                storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
                stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            };

            // 解析附件（用于将多采样结果解析到交换链图像）
            var resolveAttachment = new VkAttachmentDescription
            {
                format = colorFormat,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
                stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
            };

            var colorReference = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            };

            var resolveReference = new VkAttachmentReference
            {
                attachment = 1,
                layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            };

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                colorAttachmentCount = 1,
                pColorAttachments = &colorReference,
                pResolveAttachments = &resolveReference
            };

            var dependency = new VkSubpassDependency
            {
                srcSubpass = VK_SUBPASS_EXTERNAL,
                dstSubpass = 0,
                srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                srcAccessMask = 0,
                dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT
            };

            var attachments = stackalloc VkAttachmentDescription[2] { colorAttachment, resolveAttachment };
            var renderPassInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
                attachmentCount = 2,
                pAttachments = attachments,
                subpassCount = 1,
                pSubpasses = &subpass,
                dependencyCount = 1,
                pDependencies = &dependency
            };

            VkRenderPass pass;

            if (vkCreateRenderPass(device, &renderPassInfo, null, &pass) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create render pass!");
            }

            return pass;
        }

        public unsafe VkDescriptorSetLayout CreateDescriptorSetLayout()
        {
            VkDescriptorSetLayoutBinding samplerLayoutBinding = new VkDescriptorSetLayoutBinding
            {
                binding = 0,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
            };

            VkDescriptorSetLayoutCreateInfo layoutInfo = new VkDescriptorSetLayoutCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
                bindingCount = 1,
                pBindings = &samplerLayoutBinding
            };

            VkDescriptorSetLayout layout;

            if (vkCreateDescriptorSetLayout(device, &layoutInfo, null, &layout) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create descriptor set layout!");
            }

            return layout;
        }

        public byte[] LoadShaderFile(string filename)
        {
            return System.IO.File.ReadAllBytes(filename);
        }

        public unsafe vkGraphicsPipeline CreateGraphicsPipeline(
            VkRenderPass pass, VkExtent2D ext, VkDescriptorSetLayout layout,
            byte[] vert, byte[] frag,
            int width, int height,
            bool HasDepth,
            VkSampleCountFlags count = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            VkVertexInputBindingDescription bindingDescription = default,
            VkVertexInputAttributeDescription[] VertexInput = default,
            VkPipelineColorBlendAttachmentState blendstate = default,
            VkPushConstantRange[] PushConstant = default,
            VkPrimitiveTopology topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST,
            float[] blendConstants = default
            )
        {
            vkGraphicsPipeline pipeline = new vkGraphicsPipeline();

            pipeline.descriptorSetLayout = layout;
            pipeline.width = width;
            pipeline.height = height;

            var vertShaderModule = CreateShaderModule(vert);
            var fragShaderModule = CreateShaderModule(frag);

            var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
                stage = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
                module = vertShaderModule,
                pName = vkStrings.main
            };

            var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
                stage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
                module = fragShaderModule,
                pName = vkStrings.main
            };

            // 定义输入装配信息
            var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO,
                topology = topology,
                primitiveRestartEnable = false
            };

            // 定义视口和裁剪区域
            var viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = (float)width,
                height = (float)height,
                minDepth = 0,
                maxDepth = 1
            };

            var scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = ext
            };

            var viewportState = new VkPipelineViewportStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO,
                viewportCount = 1,
                pViewports = &viewport,
                scissorCount = 1,
                pScissors = &scissor
            };

            // 定义光栅化信息
            var rasterizer = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO,
                depthClampEnable = false,
                rasterizerDiscardEnable = false,
                polygonMode = VkPolygonMode.VK_POLYGON_MODE_FILL,
                lineWidth = 1,
                cullMode = VkCullModeFlags.VK_CULL_MODE_NONE,
                frontFace = VkFrontFace.VK_FRONT_FACE_CLOCKWISE,
                depthBiasEnable = false
            };

            // 定义多重采样信息
            var multisampling = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO,
                sampleShadingEnable = false,
                rasterizationSamples = count
            };

            // 定义颜色混合信息
            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT,
                blendEnable = false,
                srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA,
                dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,
                colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
                srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
                alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD,

            };

            var depthAttachment = new VkPipelineDepthStencilStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO,
                depthTestEnable = VkBool32.True,
                depthWriteEnable = VkBool32.True,
                depthCompareOp = VkCompareOp.VK_COMPARE_OP_ALWAYS,
                depthBoundsTestEnable = VkBool32.False,
                stencilTestEnable = VkBool32.False,
            };

            var colorBlending = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
                logicOpEnable = false,
                logicOp = VkLogicOp.VK_LOGIC_OP_COPY,
                attachmentCount = 1,
                pAttachments = blendstate.blendEnable ? &blendstate : &colorBlendAttachment,
                blendConstants_0 = 0,
                blendConstants_1 = 0,
                blendConstants_2 = 0,
                blendConstants_3 = 0
            };

            if (blendConstants != null && blendConstants.Length == 4)
            {
                colorBlending.blendConstants_0 = blendConstants[0];
                colorBlending.blendConstants_1 = blendConstants[1];
                colorBlending.blendConstants_2 = blendConstants[2];
                colorBlending.blendConstants_3 = blendConstants[3];
            }

            vkFixedArray2<VkPushConstantRange> push;

            uint pushcount = 0;

            if (PushConstant != null)
            {

                if (PushConstant[0].size > 0)
                {
                    push.First = PushConstant[0];
                    pushcount++;
                }

                if (PushConstant.Length > 1 && PushConstant[1].size > 0)
                {
                    push.Second = PushConstant[1];
                    pushcount++;
                }
            }

            VkDescriptorSetLayout dsl = layout;
            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                setLayoutCount = 1,
                pSetLayouts = &dsl,
                pushConstantRangeCount = pushcount,
                pPushConstantRanges = pushcount > 0 ? &push.First : null
            };

            VkPipelineLayout layout1;
            if (vkCreatePipelineLayout(device, &pipelineLayoutInfo, null, &layout1) != VkResult.VK_SUCCESS)
            {
                throw new Exception("vkCreatePipelineLayout Failed!");
            }
            pipeline.layout = layout1;

            vkFixedArray2<VkDynamicState> dynstate;
            dynstate.First = VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT;
            dynstate.Second = VkDynamicState.VK_DYNAMIC_STATE_SCISSOR;
            //dynstate.Third = VkDynamicState.VK_DYNAMIC_STATE_BLEND_CONSTANTS;
            //dynstate.Fourth = VkDynamicState.VK_DYNAMIC_STATE_DEPTH_BOUNDS;

            VkPipelineDynamicStateCreateInfo dyn = new VkPipelineDynamicStateCreateInfo();
            dyn.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO;
            dyn.dynamicStateCount = dynstate.Count;
            dyn.pDynamicStates = &dynstate.First;

            vkFixedArray2<VkPipelineShaderStageCreateInfo> shaderStages;
            shaderStages.First = vertShaderStageInfo;
            shaderStages.Second = fragShaderStageInfo;

            // 定义顶点输入信息
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO,
                vertexBindingDescriptionCount = 0,
                pVertexBindingDescriptions = null,
                vertexAttributeDescriptionCount = 0,
                pVertexAttributeDescriptions = null
            };
            if (VertexInput != null)
            {
                vertexInputInfo.vertexBindingDescriptionCount = 1;
                vertexInputInfo.pVertexBindingDescriptions = &bindingDescription;

                vertexInputInfo.vertexAttributeDescriptionCount = (uint)VertexInput.Length;
                vertexInputInfo.pVertexAttributeDescriptions =
                    (VkVertexInputAttributeDescription*)Marshal.UnsafeAddrOfPinnedArrayElement<VkVertexInputAttributeDescription>(VertexInput, 0);
            }
            var pipelineInfo = new VkGraphicsPipelineCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO,
                stageCount = shaderStages.Count,
                pStages = &shaderStages.First,
                pVertexInputState = &vertexInputInfo,
                pInputAssemblyState = &inputAssembly,
                pViewportState = &viewportState,
                pRasterizationState = &rasterizer,
                pMultisampleState = &multisampling,
                pColorBlendState = &colorBlending,
                layout = pipeline.layout,
                renderPass = pass,
                subpass = 0,
                basePipelineHandle = VkPipeline.Null,
                basePipelineIndex = -1,
                pDynamicState = &dyn,
                pDepthStencilState = HasDepth ? &depthAttachment : null
            };

            VkPipeline pipeline1;
            VkResult result = vkCreateGraphicsPipelines(device, VkPipelineCache.Null, 1, &pipelineInfo, null, &pipeline1);
            if (result != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create pipeline!");
            }
            pipeline.pipeline = pipeline1;

            Console.WriteLine($"[Vulkan Device] Create pipeline 0x{pipeline.pipeline.Handle:X} Success");

            vkDestroyShaderModule(device, vertShaderModule, null);
            vkDestroyShaderModule(device, fragShaderModule, null);

            return pipeline;
        }

        public unsafe void BindGraphicsPipeline(VkCommandBuffer commandBuffer, vkGraphicsPipeline pipeline, bool SetScissor = true, int x = 0, int y = 0)
        {
            if (commandBuffer == VkCommandBuffer.Null || pipeline.pipeline == VkPipeline.Null)
            {
                throw new Exception("Invalid command buffer or pipeline!");
            }

            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, pipeline.pipeline);

            if (SetScissor)
            {
                var viewport = new VkViewport
                {
                    x = (float)x,
                    y = (float)y,
                    width = (float)pipeline.width,
                    height = (float)pipeline.height,
                    minDepth = 0,
                    maxDepth = 1
                };

                var scissor = new VkRect2D
                {
                    offset = new VkOffset2D { x = x, y = y },
                    extent = new VkExtent2D { width = (uint)pipeline.width, height = (uint)pipeline.height }
                };

                vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
                vkCmdSetScissor(commandBuffer, 0, 1, &scissor);
            }
        }

        public unsafe void BeginRenderPass(VkCommandBuffer cmd,
            VkRenderPass renderPass,
            VkFramebuffer framebuffer,
            int width, int height,
            bool clear = true,
            float r = 0.0f, float g = 0.0f, float b = 0.0f, float a = 1.0f
            )
        {
            VkClearValue clr1 = new VkClearValue { color = new VkClearColorValue(r, g, b, a) };
            VkClearValue clr2 = new VkClearValue { depthStencil = new VkClearDepthStencilValue { depth = 1.0f, stencil = 0 } };

            var clearValue = stackalloc VkClearValue[2] { clr1, clr2 };

            VkRenderPassBeginInfo renderPassInfo = new VkRenderPassBeginInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,
                renderPass = renderPass,
                framebuffer = framebuffer,
                renderArea = new VkRect2D
                {
                    offset = new VkOffset2D(0, 0),
                    extent = new VkExtent2D(width, height)
                },
                clearValueCount = clear ? (uint)2 : 0,
                pClearValues = clear ? clearValue : null,

            };
            vkCmdBeginRenderPass(cmd, &renderPassInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);
        }

        public unsafe void BindDescriptorSet(VkCommandBuffer cmd, vkGraphicsPipeline pipeline, VkDescriptorSet descriptorSet)
        {
            vkCmdBindDescriptorSets(cmd, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, pipeline.layout, 0, 1, &descriptorSet, 0, null);
        }

        public unsafe void BindDescriptorSet(VkCommandBuffer cmd, vkGraphicsPipeline pipeline, VkDescriptorSet descriptorSet, uint[] Offsets)
        {
            fixed (uint* poffset = &Offsets[0])
            {
                vkCmdBindDescriptorSets(cmd, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, pipeline.layout, 0, 1, &descriptorSet, (uint)Offsets.Length, poffset);
            }
        }

        public unsafe void DestroyGraphicsPipeline(vkGraphicsPipeline pipeline)
        {
            if (pipeline.pipeline != VkPipeline.Null)
            {
                vkDestroyPipeline(device, pipeline.pipeline, null);
                pipeline.pipeline = VkPipeline.Null;
            }

            if (pipeline.layout != VkPipelineLayout.Null)
            {
                vkDestroyPipelineLayout(device, pipeline.layout, null);
                pipeline.layout = VkPipelineLayout.Null;
            }
        }

        public unsafe VkShaderModule CreateShaderModule(byte[] code)
        {
            var createInfo = new VkShaderModuleCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
                codeSize = (nuint)code.Length,
                pCode = (uint*)Marshal.UnsafeAddrOfPinnedArrayElement(code, 0)
            };

            VkShaderModule shaderModule;
            if (vkCreateShaderModule(device, &createInfo, null, &shaderModule) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create shader module!");
            }

            return shaderModule;
        }

        public unsafe VkImageView CreateImageView(VkImage Image, VkFormat format, VkImageAspectFlags aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT)
        {
            VkImageView imageview;

            VkImageViewCreateInfo imageViewCI = new VkImageViewCreateInfo();

            imageViewCI.sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
            imageViewCI.image = Image;
            imageViewCI.viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D;
            imageViewCI.format = format;
            imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;
            imageViewCI.subresourceRange.baseMipLevel = 0;
            imageViewCI.subresourceRange.levelCount = 1;
            imageViewCI.subresourceRange.baseArrayLayer = 0;
            imageViewCI.subresourceRange.layerCount = 1;

            if (vkCreateImageView(device, &imageViewCI, null, &imageview) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create image view!");
            }

            return imageview;
        }

        public unsafe VkImage CreateImage(int width, int height, VkFormat format)
        {
            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
                imageType = VkImageType.VK_IMAGE_TYPE_2D,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                format = format,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
                usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED
            };

            VkImage image;
            if (vkCreateImage(device, &imageInfo, null, &image) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to create VRAM image!");

            return image;
        }

        public unsafe VkDeviceMemory AllocateAndBindImageMemory(VkImage Image, VkMemoryPropertyFlags vpf)
        {
            VkMemoryRequirements memRequirements;

            vkGetImageMemoryRequirements(device, Image, &memRequirements);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
            };

            VkDeviceMemory imageMemory;

            vkAllocateMemory(device, &allocInfo, null, &imageMemory);
            vkBindImageMemory(device, Image, imageMemory, 0);

            return imageMemory;
        }

        public unsafe void CmdClearColorImage(VkCommandBuffer commandBuffer, VkImage image, VkClearColorValue color, VkImageLayout layout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL)
        {
            VkImageSubresourceRange range = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                levelCount = 1,
                layerCount = 1
            };

            vkFixedArray2<VkClearColorValue> clears;
            clears.First = color;
            clears.Second = color;

            vkCmdClearColorImage(commandBuffer, image, layout, &clears.First, 1, &range);
        }

        public unsafe void SetViewportAndScissor(VkCommandBuffer commandBuffer, int x, int y, int width, int height)
        {
            VkViewport viewport = new VkViewport
            {
                x = x,
                y = y,
                width = width,
                height = height,
                minDepth = 0,
                maxDepth = 1
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = x, y = y },
                extent = new VkExtent2D { width = (uint)width, height = (uint)height }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);
        }

        public unsafe VkFramebuffer CreateFramebuffer(VkRenderPass pass, VkImageView attach, uint width, uint hegiht)
        {
            VkImageView attachment = attach;

            VkFramebufferCreateInfo framebufferCI = new VkFramebufferCreateInfo();
            framebufferCI.sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO;
            framebufferCI.renderPass = pass;
            framebufferCI.attachmentCount = 1;
            framebufferCI.pAttachments = &attachment;
            framebufferCI.width = width;
            framebufferCI.height = hegiht;
            framebufferCI.layers = 1;

            VkFramebuffer vfb;

            vkCreateFramebuffer(device, &framebufferCI, null, &vfb);

            return vfb;
        }

        public unsafe VkFramebuffer CreateFramebuffer(VkRenderPass pass, VkImageView attach, VkImageView attach1, uint width, uint hegiht)
        {
            VkImageView attachment = attach;

            vkFixedArray2<VkImageView> attachs;
            attachs.First = attach;
            attachs.Second = attach1;

            VkFramebufferCreateInfo framebufferCI = new VkFramebufferCreateInfo();
            framebufferCI.sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO;
            framebufferCI.renderPass = pass;
            framebufferCI.attachmentCount = 2;
            framebufferCI.pAttachments = &attachs.First;
            framebufferCI.width = width;
            framebufferCI.height = hegiht;
            framebufferCI.layers = 1;

            VkFramebuffer vfb;

            vkCreateFramebuffer(device, &framebufferCI, null, &vfb);

            return vfb;
        }

        public unsafe void DestroyFramebuffer(VkFramebuffer framebuffer)
        {
            if (framebuffer != VkFramebuffer.Null)
            {
                vkDestroyFramebuffer(device, framebuffer, null);
            }
        }

        public unsafe vkCMDS CreateCommandBuffers(uint count)
        {
            vkCMDS CMDS = new vkCMDS();

            CMDS.CMD = new vkRawList<VkCommandBuffer>(count);

            var poolInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT
            };

            if (vkCreateCommandPool(device, &poolInfo, null, &CMDS.pool) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create command pool!");
            }

            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
                commandPool = CMDS.pool,
                level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
                commandBufferCount = count
            };

            var buffers = new VkCommandBuffer[count];
            fixed (VkCommandBuffer* pcmd = &CMDS.CMD[0])
            {
                if (vkAllocateCommandBuffers(device, &allocInfo, pcmd) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to allocate command buffers!");
                }
            }

            return CMDS;
        }

        public unsafe VkCommandPool CreateCommandPool()
        {
            var poolInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT
            };

            VkCommandPool pool;

            if (vkCreateCommandPool(device, &poolInfo, null, &pool) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create command pool!");
            }

            return pool;
        }

        public unsafe void DestoryCommandBuffers(vkCMDS CMDS)
        {
            fixed (VkCommandBuffer* pcmd = &CMDS.CMD[0])
            {
                vkFreeCommandBuffers(device, CMDS.pool, CMDS.CMD.Count, pcmd);
            }

            vkDestroyCommandPool(device, CMDS.pool, null);
        }

        public unsafe VkDescriptorPool CreateDescriptorPool(uint count = 1)
        {
            vkFixedArray2<VkDescriptorPoolSize> poolSizes;
            poolSizes.First.type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
            poolSizes.First.descriptorCount = count;

            VkDescriptorPoolCreateInfo poolInfo = new VkDescriptorPoolCreateInfo();
            poolInfo.poolSizeCount = count;
            poolInfo.pPoolSizes = &poolSizes.First;
            poolInfo.maxSets = count;
            poolInfo.flags = VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT;

            VkDescriptorPool pool;

            if (vkCreateDescriptorPool(device, &poolInfo, null, &pool) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create descriptor pool!");
            }

            return pool;
        }

        public unsafe VkDescriptorPool CreateDescriptorPool(uint maxSets, VkDescriptorPoolSize[] poolSizes)
        {
            fixed (VkDescriptorPoolSize* pPoolSizes = &poolSizes[0])
            {
                VkDescriptorPoolCreateInfo poolInfo = new VkDescriptorPoolCreateInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
                    poolSizeCount = (uint)poolSizes.Length,
                    pPoolSizes = pPoolSizes,
                    maxSets = maxSets,
                    flags = VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT // 允许单独释放描述符集
                };

                VkDescriptorPool pool;
                if (vkCreateDescriptorPool(device, &poolInfo, null, &pool) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create descriptor pool!");
                }
                return pool;
            }
        }

        public unsafe VkDescriptorSet AllocateDescriptorSet(VkDescriptorSetLayout layout, VkDescriptorPool pool)
        {
            VkDescriptorSetLayout dsl = layout;
            VkDescriptorSetAllocateInfo allocInfo = new VkDescriptorSetAllocateInfo();
            allocInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO;
            allocInfo.descriptorPool = pool;
            allocInfo.pSetLayouts = &dsl;
            allocInfo.descriptorSetCount = 1;

            VkDescriptorSet set;

            if (vkAllocateDescriptorSets(device, &allocInfo, &set) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to allocate descriptor set!");
            }

            return set;
        }

        public unsafe VkDescriptorSetLayout CreateDescriptorSetLayout(VkDescriptorSetLayoutBinding[] bindings)
        {

            fixed (VkDescriptorSetLayoutBinding* pBindings = &bindings[0])
            {
                VkDescriptorSetLayoutCreateInfo layoutInfo = new VkDescriptorSetLayoutCreateInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
                    bindingCount = (uint)bindings.Length,
                    pBindings = pBindings
                };

                VkDescriptorSetLayout layout;
                if (vkCreateDescriptorSetLayout(device, &layoutInfo, null, &layout) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create descriptor set layout!");
                }
                return layout;
            }
        }

        public unsafe void UpdateDescriptorSets(VkImage image, VkDescriptorSet set, uint binding)
        {
            VkSamplerCreateInfo samplerInfo = new VkSamplerCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
                magFilter = VkFilter.VK_FILTER_LINEAR,
                minFilter = VkFilter.VK_FILTER_LINEAR,
                addressModeU = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
                addressModeV = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
                addressModeW = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
                anisotropyEnable = false,
                borderColor = VkBorderColor.VK_BORDER_COLOR_INT_OPAQUE_BLACK,
                unnormalizedCoordinates = false,
                compareEnable = false,
                compareOp = VkCompareOp.VK_COMPARE_OP_ALWAYS,
                mipLodBias = 0,
                minLod = 0,
                maxLod = 0
            };

            VkSampler textureSampler;
            if (vkCreateSampler(device, &samplerInfo, null, &textureSampler) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create texture sampler!");
            }

            VkImageViewCreateInfo viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                image = image,
                viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
                format = VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            VkImageView textureImageView;
            if (vkCreateImageView(device, &viewInfo, null, &textureImageView) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create texture image view!");
            }

            VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                imageView = textureImageView,
                sampler = textureSampler
            };

            VkWriteDescriptorSet descriptorWrite = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = set,
                dstBinding = binding,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                descriptorCount = 1,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
        }

        public unsafe void UpdateDescriptorSets(VkImageView imageView, VkSampler sampler, VkDescriptorSet set, uint bindingIndex = 0)
        {
            var imageInfo = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                imageView = imageView,
                sampler = sampler
            };

            var descriptorWrite = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = set,
                dstBinding = bindingIndex,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                descriptorCount = 1,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
        }

        public unsafe void UpdateDescriptorSets(VkDescriptorImageInfo[] imageInfos, VkDescriptorSet set, uint bindingIndex = 0)
        {
            if (imageInfos == null || imageInfos.Length == 0 || set == VkDescriptorSet.Null)
            {
                throw new Exception("Invalid input parameters for UpdateDescriptorSets!");
            }

            fixed (VkDescriptorImageInfo* pinfo = &imageInfos[0])
            {
                var descriptorWrite = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    dstSet = set,
                    dstBinding = bindingIndex,
                    dstArrayElement = 0,
                    descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                    descriptorCount = (uint)imageInfos.Length,
                    pImageInfo = pinfo
                };

                vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
            }
        }

        public unsafe void UpdateDescriptorSets(VkWriteDescriptorSet[] writes, VkDescriptorSet set)
        {
            fixed (VkWriteDescriptorSet* pwrites = &writes[0])
            {
                vkUpdateDescriptorSets(device, (uint)writes.Length, pwrites, 0, null);
            }
        }

        public unsafe vkTexture CreateTexture(
            int width, int height, VkFormat format,
            VkImageAspectFlags aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
            VkImageUsageFlags usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            VkFilter Filter = VkFilter.VK_FILTER_LINEAR,
            VkSamplerAddressMode addressMode = VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            VkSamplerMipmapMode MinmapFilter = VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_LINEAR,
            VkMemoryPropertyFlags memoryPropertyFlags = VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
            VkImageTiling tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL
            )
        {
            vkTexture texture = new vkTexture();

            texture.width = width;
            texture.height = height;
            texture.format = format;

            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
                imageType = VkImageType.VK_IMAGE_TYPE_2D,
                format = format,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                tiling = tiling,
                usage = usage,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED
            };

            if (vkCreateImage(device, &imageInfo, null, &texture.image) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to create image!");

            // 分配图像内存
            VkMemoryRequirements memReqs;
            vkGetImageMemoryRequirements(device, texture.image, &memReqs);
            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memReqs.size,
                memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, memoryPropertyFlags)
            };

            if (vkAllocateMemory(device, &allocInfo, null, &texture.imagememory) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to allocate image memory!");

            vkBindImageMemory(device, texture.image, texture.imagememory, 0);

            // 创建图像视图
            var viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                image = texture.image,
                viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
                format = format,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = aspectMask,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            if (vkCreateImageView(device, &viewInfo, null, &texture.imageview) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to create image view!");

            // 创建采样器
            var samplerInfo = new VkSamplerCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
                magFilter = Filter, // 放大时的过滤方式
                minFilter = Filter, // 缩小时的过滤方式
                mipmapMode = MinmapFilter, // Mipmap 过滤方式
                addressModeU = addressMode, // U 方向的寻址模式
                addressModeV = addressMode, // V 方向的寻址模式
                addressModeW = addressMode, // W 方向的寻址模式
                mipLodBias = 0.0f, // Mipmap LOD 偏移
                anisotropyEnable = VkBool32.False, // 各向异性过滤
                maxAnisotropy = 16, // 最大各向异性值
                compareEnable = VkBool32.False, // 禁用深度比较
                compareOp = VkCompareOp.VK_COMPARE_OP_ALWAYS, // 深度比较操作
                minLod = 0.0f, // 最小 LOD 值
                maxLod = 1.0f, // 最大 LOD 值
                borderColor = VkBorderColor.VK_BORDER_COLOR_FLOAT_OPAQUE_BLACK, // 边界颜色
                unnormalizedCoordinates = VkBool32.False // 使用归一化纹理坐标
            };

            if (vkCreateSampler(device, &samplerInfo, null, &texture.sampler) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to create sampler!");

            return texture;
        }

        public unsafe void DestroyTexture(vkTexture texture)
        {
            if (texture.image == VkImage.Null)
                return;

            if (texture.imageview != VkImageView.Null)
            {
                vkDestroyImageView(device, texture.imageview, null);
                texture.imageview = VkImageView.Null;
            }

            if (texture.image != VkImage.Null)
            {
                vkDestroyImage(device, texture.image, null);
                texture.image = VkImage.Null;
            }

            if (texture.imagememory != VkDeviceMemory.Null)
            {
                vkFreeMemory(device, texture.imagememory, null);
                texture.imagememory = VkDeviceMemory.Null;
            }

            if (texture.sampler != VkSampler.Null)
            {
                vkDestroySampler(device, texture.sampler, null);
                texture.sampler = VkSampler.Null;
            }
        }

        public unsafe VkSurfaceFormatKHR ChooseSurfaceFormat()
        {
            uint formatCount = 0;
            vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &formatCount, null);
            if (formatCount == 0)
            {
                throw new Exception("No surface formats found!");
            }

            vkRawList<VkSurfaceFormatKHR> formats = new vkRawList<VkSurfaceFormatKHR>(formatCount);

            fixed (VkSurfaceFormatKHR* pformats = &formats[0])
                vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &formatCount, pformats);
            ;
            if (formats.Items.Count() == 1 && formats[0].format == VkFormat.VK_FORMAT_UNDEFINED)
            {
                return new VkSurfaceFormatKHR { format = VkFormat.VK_FORMAT_B8G8R8A8_UNORM, colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR };
            }
            foreach (var format in formats.Items)
            {
                if (format.format == VkFormat.VK_FORMAT_R8G8B8A8_UNORM && format.colorSpace == VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR)
                {
                    return format;
                }
            }
            return formats[0];
        }

        public unsafe VkPresentModeKHR ChoosePresentMode()
        {
            uint presentModeCount = 0;
            vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, null);
            if (presentModeCount == 0)
            {
                throw new Exception("No present modes found!");
            }

            vkRawList<VkPresentModeKHR> presentModes = new vkRawList<VkPresentModeKHR>(presentModeCount);

            fixed (VkPresentModeKHR* ppresentModes = &presentModes[0])
                vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, ppresentModes);

            foreach (var presentMode in presentModes)
            {
                if (presentMode == VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR)
                {
                    return presentMode;
                }
            }
            return VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
        }

        public unsafe uint GetSwapChainImageCount()
        {
            vkRawList<VkSurfaceCapabilitiesKHR> capabilities = new vkRawList<VkSurfaceCapabilitiesKHR>(1);

            fixed (VkSurfaceCapabilitiesKHR* pcapabilities = &capabilities[0])
                vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, pcapabilities);

            uint imageCount = capabilities[0].minImageCount + 1;
            if (capabilities[0].maxImageCount > 0 && imageCount > capabilities[0].maxImageCount)
            {
                imageCount = capabilities[0].maxImageCount;
            }
            return imageCount;
        }

        public unsafe uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
        {
            VkPhysicalDeviceMemoryProperties memProperties;

            vkGetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);

            for (uint i = 0; i < memProperties.memoryTypeCount; i++)
            {
                VkMemoryType memoryType = i switch
                {
                    0 => memProperties.memoryTypes_0,
                    1 => memProperties.memoryTypes_1,
                    2 => memProperties.memoryTypes_2,
                    3 => memProperties.memoryTypes_3,
                    4 => memProperties.memoryTypes_4,
                    5 => memProperties.memoryTypes_5,
                    6 => memProperties.memoryTypes_6,
                    7 => memProperties.memoryTypes_7,
                    8 => memProperties.memoryTypes_8,
                    9 => memProperties.memoryTypes_9,
                    10 => memProperties.memoryTypes_10,
                    11 => memProperties.memoryTypes_11,
                    12 => memProperties.memoryTypes_12,
                    13 => memProperties.memoryTypes_13,
                    14 => memProperties.memoryTypes_14,
                    15 => memProperties.memoryTypes_15,
                    16 => memProperties.memoryTypes_16,
                    17 => memProperties.memoryTypes_17,
                    18 => memProperties.memoryTypes_18,
                    19 => memProperties.memoryTypes_19,
                    20 => memProperties.memoryTypes_20,
                    21 => memProperties.memoryTypes_21,
                    22 => memProperties.memoryTypes_22,
                    23 => memProperties.memoryTypes_23,
                    24 => memProperties.memoryTypes_24,
                    25 => memProperties.memoryTypes_25,
                    26 => memProperties.memoryTypes_26,
                    27 => memProperties.memoryTypes_27,
                    28 => memProperties.memoryTypes_28,
                    29 => memProperties.memoryTypes_29,
                    30 => memProperties.memoryTypes_30,
                    31 => memProperties.memoryTypes_31,
                    _ => throw new Exception("Unsupported memory type index!")
                };

                if ((typeFilter & (1 << (int)i)) != 0 &&
                    (memoryType.propertyFlags & properties) == properties)
                {
                    return i;
                }
            }

            throw new Exception("Failed to find suitable memory type!");
        }

        public unsafe VkCommandBuffer BeginSingleCommands(VkCommandPool pool)
        {
            VkCommandBufferAllocateInfo allocInfo = new VkCommandBufferAllocateInfo();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = pool;
            allocInfo.level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY;

            VkCommandBuffer cb;
            vkAllocateCommandBuffers(device, &allocInfo, &cb);

            VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo();
            beginInfo.flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;

            vkBeginCommandBuffer(cb, &beginInfo);

            return cb;
        }

        public unsafe void EndSingleCommands(VkCommandBuffer commandBuffer, VkCommandPool pool)
        {
            vkEndCommandBuffer(commandBuffer);

            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };

            vkFreeCommandBuffers(device, pool, 1, &commandBuffer);
        }

        public unsafe void BeginCommandBuffer(VkCommandBuffer commandBuffer)
        {
            var beginInfo = new VkCommandBufferBeginInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
                flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
            };

            if (vkBeginCommandBuffer(commandBuffer, &beginInfo) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to begin recording command buffer!");

            //Console.WriteLine($"[vkBeginCommandBuffer] 0x{commandBuffer.Handle:X}");
        }

        public unsafe void EndCommandBuffer(VkCommandBuffer commandBuffer)
        {
            if (commandBuffer == VkCommandBuffer.Null)
            {
                throw new Exception("Invalid command buffer!");
            }

            if (vkEndCommandBuffer(commandBuffer) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to end recording command buffer!");
            }

            //Console.WriteLine($"[vkEndCommandBuffer] 0x{commandBuffer.Handle:X}");
        }

        public unsafe void EndAndWaitCommandBuffer(VkCommandBuffer commandBuffer)
        {

            if (vkEndCommandBuffer(commandBuffer) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to end command buffer!");

            VkSubmitInfo submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };

            VkFence fence = CreateFence(false);

            vkQueueSubmit(graphicsQueue, 1, &submitInfo, fence);

            vkWaitForFences(device, 1, &fence, VkBool32.True, ulong.MaxValue);
            vkDestroyFence(device, fence, null);

            vkResetCommandBuffer(commandBuffer, VkCommandBufferResetFlags.None);

            //Console.WriteLine($"[EndAndWaitCommandBuffer] 0x{commandBuffer.Handle:X}");
        }

        public unsafe void SubmitAndWaitCommandBuffer(VkCommandBuffer commandBuffer)
        {
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };

            VkFence fence = CreateFence(false);

            if (vkQueueSubmit(graphicsQueue, 1, &submitInfo, fence) != VkResult.VK_SUCCESS)
                Console.WriteLine("Failed to submit command buffer!");

            vkWaitForFences(device, 1, &fence, VkBool32.True, ulong.MaxValue);
            vkDestroyFence(device, fence, null);

            vkResetCommandBuffer(commandBuffer, VkCommandBufferResetFlags.None);
        }

        public unsafe void SubmitCommandBuffer(VkCommandBuffer commandBuffer)
        {
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };

            if (vkQueueSubmit(graphicsQueue, 1, &submitInfo, VkFence.Null) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to submit command buffer!");
        }

        public unsafe VkFence CreateFence(bool Signaled)
        {
            VkFence fence;

            VkFenceCreateInfo fenceInfo = new VkFenceCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
                flags = Signaled ? VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT : VkFenceCreateFlags.None
            };

            if (vkCreateFence(device, &fenceInfo, null, &fence) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to create fence!");

            return fence;
        }

        public unsafe void WaitForFence(VkFence fence)
        {
            if (vkWaitForFences(device, 1, &fence, true, ulong.MaxValue) != VkResult.VK_SUCCESS)
                throw new Exception("Failed to wait for fence!");

            // 重置围栏以供后续使用
            vkResetFences(device, 1, &fence);
        }

        public unsafe VkImageLayout TransitionImageLayout(
            VkCommandBuffer commandBuffer, vkTexture texture, VkImageLayout oldLayout, VkImageLayout newLayout,
            VkPipelineStageFlags stage1 = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,
            VkPipelineStageFlags stage2 = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
            VkImageAspectFlags aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT
            )
        {
            //Console.WriteLine($"[TransitionImageLayout] vkTexture Image 0x{texture.image.Handle:X}: {oldLayout} → {newLayout} ");

            if (oldLayout == newLayout)
                return oldLayout;

            const uint VkQueueFamilyIgnored = ~0U;

            var (srcStage, dstStage) = GetPipelineStages(oldLayout, newLayout);
            var (srcAccess, dstAccess) = GetAccessMasks(oldLayout, newLayout);

            var barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
                oldLayout = oldLayout,
                newLayout = newLayout,
                srcQueueFamilyIndex = VkQueueFamilyIgnored,
                dstQueueFamilyIndex = VkQueueFamilyIgnored,
                image = texture.image,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = aspectMask,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                srcAccessMask = srcAccess,
                dstAccessMask = dstAccess
            };
            vkCmdPipelineBarrier(commandBuffer, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);

            return newLayout;
        }

        private (VkAccessFlags src, VkAccessFlags dst) GetAccessMasks(VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            VkAccessFlags srcAccess = oldLayout switch
            {
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL => VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL => VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL => VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL => VkAccessFlags.VK_ACCESS_SHADER_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR => VkAccessFlags.VK_ACCESS_MEMORY_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PREINITIALIZED => VkAccessFlags.VK_ACCESS_HOST_WRITE_BIT,
                _ => VkAccessFlags.VK_ACCESS_NONE
            };

            VkAccessFlags dstAccess = newLayout switch
            {
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL => VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL => VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL => VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL => VkAccessFlags.VK_ACCESS_SHADER_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR => VkAccessFlags.VK_ACCESS_MEMORY_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_GENERAL => VkAccessFlags.VK_ACCESS_SHADER_READ_BIT | VkAccessFlags.VK_ACCESS_SHADER_WRITE_BIT,
                _ => VkAccessFlags.VK_ACCESS_NONE
            };

            return (srcAccess, dstAccess);
        }

        private (VkPipelineStageFlags src, VkPipelineStageFlags dst) GetPipelineStages(
            VkImageLayout oldLayout,
            VkImageLayout newLayout)
        {
            VkPipelineStageFlags srcStage = oldLayout switch
            {
                VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED => VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PREINITIALIZED => VkPipelineStageFlags.VK_PIPELINE_STAGE_HOST_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR => VkPipelineStageFlags.VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,
                _ => VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT
            };

            VkPipelineStageFlags dstStage = newLayout switch
            {
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR => VkPipelineStageFlags.VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_GENERAL => VkPipelineStageFlags.VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                _ => VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT
            };

            return (srcStage, dstStage);
        }

        public unsafe VkImageLayout TransitionImageLayout(
            VkCommandBuffer commandBuffer, vkTexture texture, VkImageLayout oldLayout, VkImageLayout newLayout,
            VkPipelineStageFlags stage1,
            VkPipelineStageFlags stage2,
            VkAccessFlags srcmask,
            VkAccessFlags dstmask
        )
        {
            //Console.WriteLine($"[TransitionImageLayout] Image 0x{texture.image.Handle:X}: {oldLayout} → {newLayout} ");

            const uint VkQueueFamilyIgnored = ~0U;

            var barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
                oldLayout = oldLayout,
                newLayout = newLayout,
                srcQueueFamilyIndex = VkQueueFamilyIgnored,
                dstQueueFamilyIndex = VkQueueFamilyIgnored,
                image = texture.image,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    levelCount = 1,
                    layerCount = 1
                },
                srcAccessMask = srcmask,
                dstAccessMask = dstmask
            };

            vkCmdPipelineBarrier(commandBuffer, stage1, stage2, 0, 0, null, 0, null, 1, &barrier);

            return newLayout;
        }

        public unsafe VkImageLayout TransitionImageLayout(
            VkCommandBuffer commandBuffer, VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout,
            VkPipelineStageFlags stage1 = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,
            VkPipelineStageFlags stage2 = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT
        )
        {
            //Console.WriteLine($"[TransitionImageLayout] Image 0x{image.Handle:X}: {oldLayout} → {newLayout} ");

            const uint VkQueueFamilyIgnored = ~0U;

            var (srcStage, dstStage) = GetPipelineStages(oldLayout, newLayout);
            var (srcAccess, dstAccess) = GetAccessMasks(oldLayout, newLayout);

            var barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
                oldLayout = oldLayout,
                newLayout = newLayout,
                srcQueueFamilyIndex = VkQueueFamilyIgnored,
                dstQueueFamilyIndex = VkQueueFamilyIgnored,
                image = image,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                srcAccessMask = srcAccess,
                dstAccessMask = dstAccess
            };
            vkCmdPipelineBarrier(commandBuffer, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);

            return newLayout;
        }

        public unsafe vkBuffer CreateBuffer(ulong size, VkBufferUsageFlags usage = VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT)
        {
            vkBuffer buffer = new vkBuffer();

            buffer.size = size;

            var bufferInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
                size = buffer.size,
                usage = usage,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
            };
            vkCreateBuffer(device, &bufferInfo, null, &buffer.stagingBuffer);

            VkMemoryRequirements memReqs;
            vkGetBufferMemoryRequirements(device, buffer.stagingBuffer, &memReqs);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memReqs.size,
                memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
            };
            vkAllocateMemory(device, &allocInfo, null, &buffer.stagingMemory);
            vkBindBufferMemory(device, buffer.stagingBuffer, buffer.stagingMemory, 0);

            //vkMapMemory(device, buffer.stagingMemory, 0, WholeSize, 0, &buffer.mappedData);

            return buffer;
        }

        public unsafe void UpdateBuffWaitAndBind(VkCommandBuffer cmd, vkBuffer buffer, IntPtr data, long size)
        {
            void* mappedData;

            vkMapMemory(device, buffer.stagingMemory, 0, VK_WHOLE_SIZE, 0, &mappedData);

            Buffer.MemoryCopy((void*)data, mappedData, size, size);

            vkUnmapMemory(device, buffer.stagingMemory);

            VkBufferMemoryBarrier barrier = new()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER,
                srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
                dstAccessMask = VkAccessFlags.VK_ACCESS_VERTEX_ATTRIBUTE_READ_BIT,
                buffer = buffer.stagingBuffer,
                size = VK_WHOLE_SIZE
            };
            vkCmdPipelineBarrier(cmd,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_VERTEX_INPUT_BIT,
                0, 0, null, 1, &barrier, 0, null);

            ulong vertexBufferOffset = 0;
            vkCmdBindVertexBuffers(cmd, 0, 1, &buffer.stagingBuffer, &vertexBufferOffset);
        }

        public unsafe void DrawVertexAndEndRenderPass(VkCommandBuffer cmd, vkBuffer buff, uint count)
        {
            ulong vertexBufferOffset = 0;
            vkCmdBindVertexBuffers(cmd, 0, 1, &buff.stagingBuffer, &vertexBufferOffset);

            vkCmdDraw(cmd, (uint)count, 1, 0, 0);

            vkCmdEndRenderPass(cmd);
        }

        public unsafe void DestoryBuffer(vkBuffer buff)
        {
            //vkUnmapMemory(device, buff.stagingMemory);

            vkDestroyBuffer(device, buff.stagingBuffer, null);

            vkFreeMemory(device, buff.stagingMemory, null);
        }

        public unsafe VkSemaphore CreateSemaphore()
        {
            var semaphoreInfo = new VkSemaphoreCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
                pNext = null,
                flags = 0
            };

            VkSemaphore semaphore;
            if (vkCreateSemaphore(device, &semaphoreInfo, null, &semaphore) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create semaphore!");
            }

            return semaphore;
        }

        public unsafe void DestorySemaphore(VkSemaphore semaphore)
        {
            vkDestroySemaphore(device, semaphore, null);
        }

        public unsafe void UpdateTexture(VkCommandPool pool, vkBuffer buffer, vkTexture texture, IntPtr data, int xOffset, int yOffset, int width, int height, int pixelsize = 2)
        {
            var bufferSize = (ulong)(width * height * pixelsize);

            Buffer.MemoryCopy((void*)data, buffer.mappedData, bufferSize, bufferSize);

            var commandBuffer = BeginSingleCommands(pool);

            TransitionImageLayout(commandBuffer, texture.image, texture.layout, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);

            var copyRegion = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageOffset = new VkOffset3D { x = xOffset, y = yOffset, z = 0 },
                imageExtent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 }
            };

            vkCmdCopyBufferToImage(commandBuffer, buffer.stagingBuffer, texture.image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &copyRegion);

            TransitionImageLayout(commandBuffer, texture.image, texture.layout, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);

            EndSingleCommands(commandBuffer, pool);
        }

        public VkFormat FindDepthFormat()
        {
            VkFormat[] candidates = new VkFormat[]
            {
                VkFormat.VK_FORMAT_D32_SFLOAT,         // 32 位浮点深度
                VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT,   // 32 位浮点深度 + 8 位模板
                VkFormat.VK_FORMAT_D24_UNORM_S8_UINT     // 24 位归一化深度 + 8 位模板
            };

            foreach (var format in candidates)
            {
                if (IsFormatSupported(format, VkImageTiling.VK_IMAGE_TILING_OPTIMAL, VkFormatFeatureFlags.VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT))
                {
                    return format;
                }
            }

            throw new Exception("Failed to find a supported depth format!");
        }

        public unsafe bool IsFormatSupported(VkFormat format, VkImageTiling tiling, VkFormatFeatureFlags features)
        {

            VkFormatProperties formatProperties;
            vkGetPhysicalDeviceFormatProperties(physicalDevice, format, &formatProperties);

            if (tiling == VkImageTiling.VK_IMAGE_TILING_LINEAR && (formatProperties.linearTilingFeatures & features) == features)
            {
                return true;
            } else if (tiling == VkImageTiling.VK_IMAGE_TILING_OPTIMAL && (formatProperties.optimalTilingFeatures & features) == features)
            {
                return true;
            }

            return false;
        }

        public unsafe void UpdateDescriptorImage(VkDescriptorSet set, vkTexture texture, uint binding)
        {

            var imageInfo = new VkDescriptorImageInfo
            {
                imageView = texture.imageview,
                sampler = texture.sampler,
                imageLayout = texture.layout
            };

            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = set,
                dstBinding = binding,
                descriptorCount = 1,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &write, 0, null);
        }

        public VkPipelineStageFlags GetPipelineStageForLayout(VkImageLayout layout)
        {
            switch (layout)
            {
                case VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED:
                    return VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
                case VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL:
                    return VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;
                case VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL:
                    return VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
                case VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:
                    return VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
                default:
                    throw new ArgumentException("Unsupported layout for pipeline stage");
            }
        }

        public uint GetMinUniformBufferAlignment()
        {
            return (uint)deviceProperties.limits.minUniformBufferOffsetAlignment;
        }

    }
}
