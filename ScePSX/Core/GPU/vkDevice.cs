using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ScePSX.Render;

using Vulkan;
using Vulkan.Win32;
using static Vulkan.VulkanNative;

namespace ScePSX
{
    public class VulkanDevice : IDisposable
    {
        public VkInstance instance;
        public VkPhysicalDevice physicalDevice;
        public VkDevice device;

        public VkQueue graphicsQueue;
        public VkQueue presentQueue;
        public VkSurfaceKHR surface;

        private int graphicsQueueFamilyIndex = -1;
        private int presentQueueFamilyIndex = -1;

        public struct vkSwapchain
        {
            public VkSwapchainKHR Chain;
            public vkRawList<VkImage> Images;
            public vkRawList<VkImageView> ImageViews;
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
        }

        public struct vkGraphicsPipeline
        {
            public int width;
            public int height;
            public VkPipeline pipeline;
            public VkPipelineLayout layout;
        }

        public struct vkCMDS
        {
            public vkRawList<VkCommandBuffer> CMD;
            public VkCommandPool pool;
        }

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

            Console.WriteLine($"[Vulkan GPU] VulkanDevice Disposed");

            isDisposed = true;
        }

        public void VulkanInit(IntPtr hwnd, IntPtr hinst)
        {
            Console.ForegroundColor = ConsoleColor.Blue;

            Console.WriteLine($"[VULKAN GPU] VulkanDevice Initialization....");

            CreateInstance();
            CreateSurface(hinst, hwnd);
            SelectPhysicalDevice();
            CreateLogicalDevice();

            Console.WriteLine($"[VULKAN GPU] VulkanDevice Initializationed...");

            Console.ResetColor();

            isinit = true;
        }

        public unsafe void VulkanDispose()
        {
            vkDestroyDevice(device, IntPtr.Zero);
            vkDestroySurfaceKHR(instance, surface, IntPtr.Zero);
            vkDestroyInstance(instance, IntPtr.Zero);
        }

        public unsafe void CleanupSwapChain(vkSwapchain Chain)
        {
            foreach (var imageView in Chain.ImageViews)
            {
                if (imageView != VkImageView.Null)
                {
                    vkDestroyImageView(device, imageView, null);
                }
            }
            Chain.ImageViews.Clear();

            foreach (var image in Chain.Images)
            {
                if (image != VkImage.Null)
                {

                    vkDestroyImage(device, image, null);
                }
            }
            Chain.Images.Clear();

            if (Chain.Chain != VkSwapchainKHR.Null)
            {
                vkDestroySwapchainKHR(device, Chain.Chain, null);
                Chain.Chain = VkSwapchainKHR.Null;
            }
        }

        private static uint VK_MAKE_VERSION(uint major, uint minor, uint patch)
        {
            return (major << 22) | (minor << 12) | patch;
        }

        private unsafe void CreateInstance()
        {
            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                pApplicationName = vkStrings.AppName,
                applicationVersion = VK_MAKE_VERSION(1, 0, 0),
                pEngineName = vkStrings.EngineName,
                engineVersion = VK_MAKE_VERSION(1, 0, 0),
                apiVersion = VK_MAKE_VERSION(1, 0, 0)
            };
            var instanceCreateInfo = VkInstanceCreateInfo.New();
            instanceCreateInfo.pApplicationInfo = &appInfo;

            vkRawList<IntPtr> Extensions = new vkRawList<IntPtr>();
            Extensions.Add(vkStrings.VK_KHR_SURFACE_EXTENSION_NAME);
            Extensions.Add(vkStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);

            //Extensions.Add(vkStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);

            fixed (IntPtr* extensionsBase = &Extensions.Items[0])
            {
                instanceCreateInfo.enabledExtensionCount = Extensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = (byte**)extensionsBase;

                VkResult result = vkCreateInstance(ref instanceCreateInfo, null, out instance);
                if (result != VkResult.Success)
                {
                    throw new Exception("Failed to create Vulkan instance!");
                }
            }
        }

        private unsafe void CreateSurface(HINSTANCE hinstance, IntPtr hwnd)
        {
            var surfaceCreateInfo = new VkWin32SurfaceCreateInfoKHR
            {
                sType = VkStructureType.Win32SurfaceCreateInfoKHR,
                hinstance = hinstance,
                hwnd = hwnd
            };

            if (vkCreateWin32SurfaceKHR(instance, &surfaceCreateInfo, null, out surface) != VkResult.Success)
            {
                throw new Exception("Failed to create Vulkan surface!");
            }
        }

        private unsafe void SelectPhysicalDevice()
        {
            uint deviceCount = 0;
            vkEnumeratePhysicalDevices(instance, ref deviceCount, null);

            if (deviceCount == 0)
            {
                throw new Exception("No Vulkan-capable physical devices found!");
            }

            var physicalDevices = new VkPhysicalDevice[deviceCount];
            vkEnumeratePhysicalDevices(instance, &deviceCount, (VkPhysicalDevice*)Marshal.UnsafeAddrOfPinnedArrayElement(physicalDevices, 0));

            foreach (var device in physicalDevices)
            {
                vkGetPhysicalDeviceProperties(device, out var deviceProperties);
                vkGetPhysicalDeviceFeatures(device, out var deviceFeatures);

                uint queueFamilyCount = 0;
                vkGetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

                var queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
                vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, (VkQueueFamilyProperties*)Marshal.UnsafeAddrOfPinnedArrayElement(queueFamilies, 0));

                bool hasGraphicsQueue = false;
                for (int i = 0; i < queueFamilies.Length; i++)
                {
                    if ((queueFamilies[i].queueFlags & VkQueueFlags.Graphics) != 0)
                    {
                        hasGraphicsQueue = true;
                        break;
                    }
                }
                if (hasGraphicsQueue)
                {
                    physicalDevice = device;
                    Console.WriteLine($"[VULKAN GPU] VulkanDevice: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.deviceName)}");
                    return;
                }
            }

            throw new Exception("No suitable physical device found!");
        }

        private unsafe void CreateLogicalDevice()
        {
            uint queueFamilyCount = 0;
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, null);

            var queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, (VkQueueFamilyProperties*)Marshal.UnsafeAddrOfPinnedArrayElement(queueFamilies, 0));

            for (int i = 0; i < queueFamilies.Length; i++)
            {
                if ((queueFamilies[i].queueFlags & VkQueueFlags.Graphics) != 0)
                {
                    graphicsQueueFamilyIndex = i;
                }

                vkGetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)i, surface, out var presentSupported);
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
                sType = VkStructureType.DeviceQueueCreateInfo,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                queueCount = 1,
                pQueuePriorities = &queuePriority
            };
            var deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.DeviceCreateInfo,
                queueCreateInfoCount = 1,
                pQueueCreateInfos = &queueCreateInfo,
                pEnabledFeatures = null
            };

            vkRawList<IntPtr> instanceExtensions = new vkRawList<IntPtr>();
            instanceExtensions.Add(vkStrings.VK_KHR_SWAPCHAIN_EXTENSION_NAME);

            fixed (IntPtr* ppEnabledExtensionNames = &instanceExtensions.Items[0])
            {
                deviceCreateInfo.enabledExtensionCount = instanceExtensions.Count;
                deviceCreateInfo.ppEnabledExtensionNames = (byte**)ppEnabledExtensionNames;

                if (vkCreateDevice(physicalDevice, &deviceCreateInfo, null, out device) != VkResult.Success)
                {
                    throw new Exception("Failed to create logical device!");
                }
            }

            vkGetDeviceQueue(device, (uint)graphicsQueueFamilyIndex, 0, out graphicsQueue);
            vkGetDeviceQueue(device, (uint)presentQueueFamilyIndex, 0, out presentQueue);
        }

        public unsafe vkMultisample CreateMultisample(int width, int height, VkSampleCountFlags sampleCount)
        {
            vkMultisample multisample = new vkMultisample();

            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                imageType = VkImageType.Image2D,
                format = VkFormat.R8g8b8a8Unorm,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                samples = sampleCount,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.TransientAttachment | VkImageUsageFlags.ColorAttachment,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined
            };

            vkCreateImage(device, &imageInfo, null, out multisample.ColorImage);

            vkGetImageMemoryRequirements(device, multisample.ColorImage, out var memReqs);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memReqs.size,
                memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
            };

            vkAllocateMemory(device, &allocInfo, null, out multisample.ColorImageMemory);

            vkBindImageMemory(device, multisample.ColorImage, multisample.ColorImageMemory, 0);

            var viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = multisample.ColorImage,
                viewType = VkImageViewType.Image2D,
                format = VkFormat.R8g8b8a8Unorm,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            vkCreateImageView(device, &viewInfo, null, out multisample.ColorImageView);

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

        public unsafe vkSwapchain CreateSwapChain(int width, int height)
        {
            vkSwapchain chain = new vkSwapchain();

            var surfaceFormat = ChooseSurfaceFormat();
            var presentMode = ChoosePresentMode();
            var extent = new VkExtent2D((uint)width, (uint)height);

            uint imageCount = GetSwapChainImageCount();
            var swapChainCreateInfo = new VkSwapchainCreateInfoKHR
            {
                sType = VkStructureType.SwapchainCreateInfoKHR,
                surface = surface,
                minImageCount = imageCount,
                imageFormat = surfaceFormat.format,
                imageColorSpace = surfaceFormat.colorSpace,
                imageExtent = extent,
                imageArrayLayers = 1,
                imageUsage = VkImageUsageFlags.ColorAttachment,
                preTransform = VkSurfaceTransformFlagsKHR.InheritKHR,
                compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR,
                presentMode = presentMode,
                clipped = true
            };

            if (vkCreateSwapchainKHR(device, ref swapChainCreateInfo, null, out chain.Chain) != VkResult.Success)
            {
                throw new Exception("Failed to create swap chain!");
            }

            vkGetSwapchainImagesKHR(device, chain.Chain, &imageCount, null);

            chain.Images = new vkRawList<VkImage>(imageCount);

            vkGetSwapchainImagesKHR(device, chain.Chain, &imageCount, out chain.Images[0]);

            chain.ImageViews = new vkRawList<VkImageView>(imageCount);

            for (int i = 0; i < chain.Images.Count; i++)
            {
                chain.ImageViews[i] = CreateImageView(chain.Images[i], chain.ImageFormat);
            }

            chain.ImageFormat = surfaceFormat.format;

            chain.Extent = extent;

            return chain;
        }

        public unsafe VkRenderPass CreateRenderPass(VkFormat format)
        {
            var colorAttachment = new VkAttachmentDescription
            {
                format = format,
                samples = VkSampleCountFlags.Count1,
                loadOp = VkAttachmentLoadOp.Clear,
                storeOp = VkAttachmentStoreOp.Store,
                stencilLoadOp = VkAttachmentLoadOp.DontCare,
                stencilStoreOp = VkAttachmentStoreOp.DontCare,
                initialLayout = VkImageLayout.Undefined,
                finalLayout = VkImageLayout.PresentSrcKHR
            };

            var colorAttachmentRef = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.ColorAttachmentOptimal
            };

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = 1,
                pColorAttachments = &colorAttachmentRef
            };

            var dependency = new VkSubpassDependency
            {
                srcSubpass = unchecked((uint)(-1)),
                dstSubpass = 0,
                srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                srcAccessMask = VkAccessFlags.None,
                dstAccessMask = VkAccessFlags.ColorAttachmentWrite
            };

            var renderPassInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.RenderPassCreateInfo,
                attachmentCount = 1,
                pAttachments = &colorAttachment,
                subpassCount = 1,
                pSubpasses = &subpass,
                dependencyCount = 1,
                pDependencies = &dependency
            };

            VkRenderPass pass;

            if (vkCreateRenderPass(device, &renderPassInfo, null, out pass) != VkResult.Success)
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
                loadOp = VkAttachmentLoadOp.Clear,
                storeOp = VkAttachmentStoreOp.Store,
                stencilLoadOp = VkAttachmentLoadOp.DontCare,
                stencilStoreOp = VkAttachmentStoreOp.DontCare,
                initialLayout = VkImageLayout.Undefined,
                finalLayout = VkImageLayout.ColorAttachmentOptimal
            };

            // 解析附件（用于将多采样结果解析到交换链图像）
            var resolveAttachment = new VkAttachmentDescription
            {
                format = colorFormat,
                samples = VkSampleCountFlags.Count1,
                loadOp = VkAttachmentLoadOp.DontCare,
                storeOp = VkAttachmentStoreOp.Store,
                stencilLoadOp = VkAttachmentLoadOp.DontCare,
                stencilStoreOp = VkAttachmentStoreOp.DontCare,
                initialLayout = VkImageLayout.Undefined,
                finalLayout = VkImageLayout.PresentSrcKHR
            };

            // 子通道
            var colorReference = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.ColorAttachmentOptimal
            };
            var resolveReference = new VkAttachmentReference
            {
                attachment = 1,
                layout = VkImageLayout.ColorAttachmentOptimal
            };
            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = 1,
                pColorAttachments = &colorReference,
                pResolveAttachments = &resolveReference
            };

            // 渲染通道依赖
            var dependency = new VkSubpassDependency
            {
                srcSubpass = SubpassExternal,
                dstSubpass = 0,
                srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                srcAccessMask = 0,
                dstAccessMask = VkAccessFlags.ColorAttachmentWrite
            };

            // 创建渲染通道
            var attachments = stackalloc VkAttachmentDescription[2] { colorAttachment, resolveAttachment };
            var renderPassInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.RenderPassCreateInfo,
                attachmentCount = 2,
                pAttachments = attachments,
                subpassCount = 1,
                pSubpasses = &subpass,
                dependencyCount = 1,
                pDependencies = &dependency
            };
            vkCreateRenderPass(device, &renderPassInfo, null, out var renderPass);
            return renderPass;
        }

        public unsafe VkDescriptorSetLayout CreateDescriptorSetLayout()
        {
            VkDescriptorSetLayoutBinding samplerLayoutBinding = new VkDescriptorSetLayoutBinding
            {
                binding = 0,
                descriptorType = VkDescriptorType.CombinedImageSampler,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.Fragment
            };

            VkDescriptorSetLayoutCreateInfo layoutInfo = new VkDescriptorSetLayoutCreateInfo
            {
                sType = VkStructureType.DescriptorSetLayoutCreateInfo,
                bindingCount = 1,
                pBindings = &samplerLayoutBinding
            };

            VkDescriptorSetLayout layout;

            if (vkCreateDescriptorSetLayout(device, ref layoutInfo, null, out layout) != VkResult.Success)
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
            VkSampleCountFlags count = VkSampleCountFlags.Count1,
            VkVertexInputBindingDescription bindingDescription = default,
            VkVertexInputAttributeDescription* VertexInput = null,
            uint VertexInputLength = 0,
            bool enableBlending  = false,
            VkBlendOp rgbEquation = VkBlendOp.Add
            )
        {
            vkGraphicsPipeline pipeline = new vkGraphicsPipeline();

            pipeline.width = width;
            pipeline.height = height;

            var vertShaderModule = CreateShaderModule(vert);
            var fragShaderModule = CreateShaderModule(frag);

            var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Vertex,
                module = vertShaderModule,
                pName = vkStrings.main
            };

            var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Fragment,
                module = fragShaderModule,
                pName = vkStrings.main
            };

            // 定义顶点输入信息
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                vertexBindingDescriptionCount = 0,
                pVertexBindingDescriptions = null,
                vertexAttributeDescriptionCount = 0,
                pVertexAttributeDescriptions = null
            };

            if (VertexInput != null)
            {
                vertexInputInfo.vertexBindingDescriptionCount = 1;
                vertexInputInfo.pVertexBindingDescriptions = &bindingDescription;

                vertexInputInfo.vertexAttributeDescriptionCount = VertexInputLength;
                vertexInputInfo.pVertexAttributeDescriptions = VertexInput;
            }

            // 定义输入装配信息
            var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                topology = VkPrimitiveTopology.TriangleStrip,
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
                sType = VkStructureType.PipelineViewportStateCreateInfo,
                viewportCount = 1,
                pViewports = &viewport,
                scissorCount = 1,
                pScissors = &scissor
            };

            // 定义光栅化信息
            var rasterizer = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                depthClampEnable = false,
                rasterizerDiscardEnable = false,
                polygonMode = VkPolygonMode.Fill,
                lineWidth = 1,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.Clockwise,
                depthBiasEnable = false
            };

            // 定义多重采样信息
            var multisampling = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                sampleShadingEnable = false,
                rasterizationSamples = count
            };

            // 定义颜色混合信息
            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A,
                blendEnable = enableBlending,
                srcColorBlendFactor = VkBlendFactor.Src1Alpha,
                dstColorBlendFactor = VkBlendFactor.Src1Color,
                colorBlendOp = rgbEquation,
                srcAlphaBlendFactor = VkBlendFactor.One,
                dstAlphaBlendFactor = VkBlendFactor.Zero,
                alphaBlendOp = VkBlendOp.Add,

            };

            var colorBlending = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                logicOpEnable = false,
                logicOp = VkLogicOp.Copy,
                attachmentCount = 1,
                pAttachments = &colorBlendAttachment,
                blendConstants_0 = 0,
                blendConstants_1 = 0,
                blendConstants_2 = 0,
                blendConstants_3 = 0
            };

            VkDescriptorSetLayout dsl = layout;
            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 1,
                pSetLayouts = &dsl,
                pushConstantRangeCount = 0,
                pPushConstantRanges = null
            };

            if (vkCreatePipelineLayout(device, &pipelineLayoutInfo, null, out pipeline.layout) != VkResult.Success)
            {
                throw new Exception("Failed to create pipeline layout!");
            }

            vkFixedArray2<VkDynamicState> dynstate;
            dynstate.First = VkDynamicState.Viewport;
            dynstate.Second = VkDynamicState.Scissor;

            VkPipelineDynamicStateCreateInfo dyn = VkPipelineDynamicStateCreateInfo.New();
            dyn.dynamicStateCount = dynstate.Count;
            dyn.pDynamicStates = &dynstate.First;

            vkFixedArray2<VkPipelineShaderStageCreateInfo> shaderStages;
            shaderStages.First = vertShaderStageInfo;
            shaderStages.Second = fragShaderStageInfo;

            var pipelineInfo = new VkGraphicsPipelineCreateInfo
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo,
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
                pDynamicState = &dyn
            };

            VkResult result = vkCreateGraphicsPipelines(device, VkPipelineCache.Null, 1, &pipelineInfo, null, out pipeline.pipeline);
            if (result != VkResult.Success)
            {
                throw new Exception("Failed to create graphics pipeline!");
            }

            vkDestroyShaderModule(device, vertShaderModule, null);
            vkDestroyShaderModule(device, fragShaderModule, null);

            return pipeline;
        }

        public unsafe void BindGraphicsPipeline(VkCommandBuffer commandBuffer, vkGraphicsPipeline pipeline)
        {
            if (commandBuffer == VkCommandBuffer.Null || pipeline.pipeline == VkPipeline.Null)
            {
                throw new Exception("Invalid command buffer or graphics pipeline!");
            }

            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, pipeline.pipeline);

            var viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = (float)pipeline.width,
                height = (float)pipeline.height,
                minDepth = 0,
                maxDepth = 1
            };

            var scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = new VkExtent2D { width = (uint)pipeline.width, height = (uint)pipeline.height }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);
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
                sType = VkStructureType.ShaderModuleCreateInfo,
                codeSize = (nuint)code.Length,
                pCode = (uint*)Marshal.UnsafeAddrOfPinnedArrayElement(code, 0)
            };

            if (vkCreateShaderModule(device, &createInfo, null, out var shaderModule) != VkResult.Success)
            {
                throw new Exception("Failed to create shader module!");
            }

            return shaderModule;
        }

        public unsafe VkImageView CreateImageView(VkImage Image, VkFormat format)
        {
            VkImageView imageview;

            VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();

            imageViewCI.image = Image;
            imageViewCI.viewType = VkImageViewType.Image2D;
            imageViewCI.format = format;
            imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            imageViewCI.subresourceRange.baseMipLevel = 0;
            imageViewCI.subresourceRange.levelCount = 1;
            imageViewCI.subresourceRange.baseArrayLayer = 0;
            imageViewCI.subresourceRange.layerCount = 1;

            if (vkCreateImageView(device, ref imageViewCI, null, out imageview) != VkResult.Success)
            {
                throw new Exception("Failed to create image view!");
            }

            return imageview;
        }

        public unsafe VkImage CreateImage(int width, int height, VkFormat format)
        {
            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                imageType = VkImageType.Image2D,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                format = format,
                samples = VkSampleCountFlags.Count1,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined
            };

            VkImage image;
            if (vkCreateImage(device, &imageInfo, null, out image) != VkResult.Success)
                throw new Exception("Failed to create VRAM image!");

            return image;
        }

        public unsafe VkDeviceMemory AllocateAndBindImageMemory(VkImage Image, VkMemoryPropertyFlags vpf)
        {
            VkMemoryRequirements memRequirements;

            vkGetImageMemoryRequirements(device, Image, out memRequirements);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
            };

            VkDeviceMemory imageMemory;

            vkAllocateMemory(device, ref allocInfo, null, out imageMemory);
            vkBindImageMemory(device, Image, imageMemory, 0);

            return imageMemory;
        }

        public unsafe void CmdClearColorImage(VkCommandBuffer commandBuffer, VkImage image, VkClearColorValue color, VkImageLayout layout = VkImageLayout.TransferDstOptimal)
        {
            VkImageSubresourceRange range = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                levelCount = 1,
                layerCount = 1
            };
            vkCmdClearColorImage(commandBuffer, image, layout, &color, 1, &range);
        }

        public unsafe void CmdSetViewportAndScissor(VkCommandBuffer commandBuffer, int x, int y, int width, int height)
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

            VkFramebufferCreateInfo framebufferCI = VkFramebufferCreateInfo.New();
            framebufferCI.renderPass = pass;
            framebufferCI.attachmentCount = 1;
            framebufferCI.pAttachments = &attachment;
            framebufferCI.width = width;
            framebufferCI.height = hegiht;
            framebufferCI.layers = 1;

            VkFramebuffer vfb;

            vkCreateFramebuffer(device, ref framebufferCI, null, out vfb);

            return vfb;
        }

        public unsafe VkFramebuffer CreateFramebuffer(VkRenderPass renderPass, VkImageView colorView, VkImageView resolveView, uint width, uint height)
        {
            var attachments = stackalloc VkImageView[2] { colorView, resolveView };
            var framebufferInfo = new VkFramebufferCreateInfo
            {
                sType = VkStructureType.FramebufferCreateInfo,
                renderPass = renderPass,
                attachmentCount = 2,
                pAttachments = attachments,
                width = width,
                height = height,
                layers = 1
            };
            vkCreateFramebuffer(device, &framebufferInfo, null, out var framebuffer);
            return framebuffer;
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
                sType = VkStructureType.CommandPoolCreateInfo,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                flags = VkCommandPoolCreateFlags.ResetCommandBuffer
            };

            if (vkCreateCommandPool(device, &poolInfo, null, out CMDS.pool) != VkResult.Success)
            {
                throw new Exception("Failed to create command pool!");
            }

            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                commandPool = CMDS.pool,
                level = VkCommandBufferLevel.Primary,
                commandBufferCount = count
            };

            var buffers = new VkCommandBuffer[count];
            if (vkAllocateCommandBuffers(device, &allocInfo, out CMDS.CMD[0]) != VkResult.Success)
            {
                throw new Exception("Failed to allocate command buffers!");
            }

            return CMDS;
        }

        public unsafe void DestoryCommandBuffers(vkCMDS CMDS)
        {
            vkFreeCommandBuffers(device, CMDS.pool, CMDS.CMD.Count, ref CMDS.CMD[0]);

            vkDestroyCommandPool(device, CMDS.pool, null);
        }

        public unsafe VkFence CreateSyncObject()
        {
            VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo
            {
                sType = VkStructureType.FenceCreateInfo,
                flags = VkFenceCreateFlags.Signaled // 初始化为已触发状态
            };

            VkFence fence;

            if (vkCreateFence(device, &fenceCreateInfo, null, out fence) != VkResult.Success)
                throw new Exception("Failed to create fence!");

            return fence;
        }

        public unsafe VkDescriptorPool CreateDescriptorPool(uint count = 1)
        {
            vkFixedArray2<VkDescriptorPoolSize> poolSizes;
            poolSizes.First.type = VkDescriptorType.CombinedImageSampler;
            poolSizes.First.descriptorCount = count;

            VkDescriptorPoolCreateInfo poolInfo = VkDescriptorPoolCreateInfo.New();
            poolInfo.poolSizeCount = count;
            poolInfo.pPoolSizes = &poolSizes.First;
            poolInfo.maxSets = count;

            VkDescriptorPool pool;

            if (vkCreateDescriptorPool(device, ref poolInfo, null, out pool) != VkResult.Success)
            {
                throw new Exception("Failed to create descriptor pool!");
            }

            return pool;
        }

        public unsafe VkDescriptorSet CreateDescriptorSet(VkDescriptorSetLayout layout, VkDescriptorPool pool)
        {
            VkDescriptorSetLayout dsl = layout;
            VkDescriptorSetAllocateInfo allocInfo = VkDescriptorSetAllocateInfo.New();
            allocInfo.descriptorPool = pool;
            allocInfo.pSetLayouts = &dsl;
            allocInfo.descriptorSetCount = 1;

            VkDescriptorSet set;

            if (vkAllocateDescriptorSets(device, ref allocInfo, out set) != VkResult.Success)
            {
                throw new Exception("Failed to allocate descriptor set!");
            }

            return set;
        }

        public unsafe void UpdateDescriptorSets(VkImage image, VkDescriptorSet set)
        {
            VkSamplerCreateInfo samplerInfo = new VkSamplerCreateInfo
            {
                sType = VkStructureType.SamplerCreateInfo,
                magFilter = VkFilter.Linear,
                minFilter = VkFilter.Linear,
                addressModeU = VkSamplerAddressMode.ClampToEdge,
                addressModeV = VkSamplerAddressMode.ClampToEdge,
                addressModeW = VkSamplerAddressMode.ClampToEdge,
                anisotropyEnable = false,
                borderColor = VkBorderColor.IntOpaqueBlack,
                unnormalizedCoordinates = false,
                compareEnable = false,
                compareOp = VkCompareOp.Always,
                mipLodBias = 0,
                minLod = 0,
                maxLod = 0
            };

            VkSampler textureSampler;
            if (vkCreateSampler(device, &samplerInfo, null, out textureSampler) != VkResult.Success)
            {
                throw new Exception("Failed to create texture sampler!");
            }

            VkImageViewCreateInfo viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = image,
                viewType = VkImageViewType.Image2D,
                format = VkFormat.B8g8r8a8Unorm,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            VkImageView textureImageView;
            if (vkCreateImageView(device, &viewInfo, null, out textureImageView) != VkResult.Success)
            {
                throw new Exception("Failed to create texture image view!");
            }

            VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                imageView = textureImageView,
                sampler = textureSampler
            };

            VkWriteDescriptorSet descriptorWrite = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                dstSet = set,
                dstBinding = 0,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.CombinedImageSampler,
                descriptorCount = 1,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
        }

        public unsafe void UpdateDescriptorSets(VkImage image, VkImageView imageView, VkSampler sampler, VkDescriptorSet set, uint bindingIndex)
        {
            var imageInfo = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                imageView = imageView,
                sampler = sampler
            };

            var descriptorWrite = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                dstSet = set,
                dstBinding = bindingIndex,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.CombinedImageSampler,
                descriptorCount = 1,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
        }

        public unsafe void UpdateDescriptorSets(vkRawList<VkDescriptorImageInfo> imageInfos, VkDescriptorSet set, uint bindingIndex = 0)
        {
            if (imageInfos == null || imageInfos.Count == 0 || set == VkDescriptorSet.Null)
            {
                throw new Exception("Invalid input parameters for UpdateDescriptorSets!");
            }

            fixed (VkDescriptorImageInfo* pinfo = &imageInfos[0])
            {
                var descriptorWrite = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.WriteDescriptorSet,
                    dstSet = set,
                    dstBinding = bindingIndex,
                    dstArrayElement = 0,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    descriptorCount = (uint)imageInfos.Count,
                    pImageInfo = pinfo
                };

                vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
            }
        }

        public unsafe vkTexture CreateTexture(int width, int height, VkFormat format)
        {
            vkTexture texture = new vkTexture();

            texture.width = width;
            texture.height = height;
            texture.format = format;

            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                imageType = VkImageType.Image2D,
                format = format,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.Count1,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined
            };

            if (vkCreateImage(device, &imageInfo, null, out texture.image) != VkResult.Success)
                throw new Exception("Failed to create image!");

            vkGetImageMemoryRequirements(device, texture.image, out var memReqs);
            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memReqs.size,
                memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
            };

            if (vkAllocateMemory(device, &allocInfo, null, out texture.imagememory) != VkResult.Success)
                throw new Exception("Failed to allocate image memory!");

            vkBindImageMemory(device, texture.image, texture.imagememory, 0);

            var viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = texture.image,
                viewType = VkImageViewType.Image2D,
                format = format,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            if (vkCreateImageView(device, &viewInfo, null, out texture.imageview) != VkResult.Success)
                throw new Exception("Failed to create image view!");

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
        }

        public unsafe vkTexture CreateVRAMTexture()
        {
            int vramWidth = 1024;
            int vramHeight = 512;
            VkFormat vramFormat = VkFormat.R5g5b5a1UnormPack16; // PS1 使用 R5G5B5A1 格式

            return CreateTexture(vramWidth, vramHeight, vramFormat);
        }

        public unsafe VkSurfaceFormatKHR ChooseSurfaceFormat()
        {
            uint formatCount = 0;
            vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, ref formatCount, null);
            if (formatCount == 0)
            {
                throw new Exception("No surface formats found!");
            }

            vkRawList<VkSurfaceFormatKHR> formats = new vkRawList<VkSurfaceFormatKHR>(formatCount);

            vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &formatCount, out formats[0]);

            if (formats.Count == 1 && formats[0].format == VkFormat.Undefined)
            {
                return new VkSurfaceFormatKHR { format = VkFormat.B8g8r8a8Unorm, colorSpace = VkColorSpaceKHR.SrgbNonlinearKHR };
            }
            foreach (var format in formats)
            {
                if (format.format == VkFormat.B8g8r8a8Unorm && format.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR)
                {
                    return format;
                }
            }
            return formats[0];
        }

        public unsafe VkPresentModeKHR ChoosePresentMode()
        {
            uint presentModeCount = 0;
            vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, ref presentModeCount, null);
            if (presentModeCount == 0)
            {
                throw new Exception("No present modes found!");
            }

            vkRawList<VkPresentModeKHR> presentModes = new vkRawList<VkPresentModeKHR>(presentModeCount);

            vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, out presentModes[0]);
            foreach (var presentMode in presentModes)
            {
                if (presentMode == VkPresentModeKHR.MailboxKHR)
                {
                    return presentMode;
                }
            }
            return VkPresentModeKHR.FifoKHR;
        }

        public uint GetSwapChainImageCount()
        {
            vkRawList<VkSurfaceCapabilitiesKHR> capabilities = new vkRawList<VkSurfaceCapabilitiesKHR>(1);

            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, out capabilities[0]);
            uint imageCount = capabilities[0].minImageCount + 1;
            if (capabilities[0].maxImageCount > 0 && imageCount > capabilities[0].maxImageCount)
            {
                imageCount = capabilities[0].maxImageCount;
            }
            return imageCount;
        }

        public uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
        {
            vkGetPhysicalDeviceMemoryProperties(physicalDevice, out var memProperties);

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

        public VkCommandBuffer BeginSingleCommands(VkCommandPool pool)
        {
            VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = pool;
            allocInfo.level = VkCommandBufferLevel.Primary;

            vkAllocateCommandBuffers(device, ref allocInfo, out VkCommandBuffer cb);

            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

            vkBeginCommandBuffer(cb, ref beginInfo);

            return cb;
        }

        public unsafe void EndSingleCommands(VkCommandBuffer commandBuffer, VkCommandPool pool)
        {
            vkEndCommandBuffer(commandBuffer);
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };
            vkQueueSubmit(graphicsQueue, 1, ref submitInfo, VkFence.Null);
            vkQueueWaitIdle(graphicsQueue);
            vkFreeCommandBuffers(device, pool, 1, ref commandBuffer);
        }

        public unsafe void BeginCommandBuffer(VkCommandBuffer commandBuffer)
        {
            var beginInfo = new VkCommandBufferBeginInfo
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.OneTimeSubmit // 一次性提交
            };

            if (vkBeginCommandBuffer(commandBuffer, &beginInfo) != VkResult.Success)
                throw new Exception("Failed to begin recording command buffer!");
        }

        public unsafe void EndCommandBuffer(VkCommandBuffer commandBuffer)
        {
            if (commandBuffer == VkCommandBuffer.Null)
            {
                throw new Exception("Invalid command buffer!");
            }

            if (vkEndCommandBuffer(commandBuffer) != VkResult.Success)
            {
                throw new Exception("Failed to end recording command buffer!");
            }
        }

        public unsafe void SubmitCommandBuffer(VkCommandBuffer commandBuffer, VkFence fence)
        {
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };

            if (vkQueueSubmit(graphicsQueue, 1, &submitInfo, fence) != VkResult.Success)
                throw new Exception("Failed to submit command buffer!");
        }

        public unsafe void WaitForFence(VkFence fence)
        {
            if (vkWaitForFences(device, 1, &fence, true, ulong.MaxValue) != VkResult.Success)
                throw new Exception("Failed to wait for fence!");

            // 重置围栏以供后续使用
            vkResetFences(device, 1, &fence);
        }

        public unsafe void TransitionImageLayout(VkCommandBuffer commandBuffer, VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            const uint VkQueueFamilyIgnored = ~0U;

            var barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                oldLayout = oldLayout,
                newLayout = newLayout,
                srcQueueFamilyIndex = VkQueueFamilyIgnored,
                dstQueueFamilyIndex = VkQueueFamilyIgnored,
                image = image,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    levelCount = 1,
                    layerCount = 1
                }
            };
            vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer, 0, 0, null, 0, null, 1, &barrier);
        }

        public unsafe vkBuffer CreateBuffer(int width, int height, int pixelsize = 2)
        {
            vkBuffer buffer = new vkBuffer();

            buffer.size = (ulong)(width * height * pixelsize);

            var bufferInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.BufferCreateInfo,
                size = buffer.size,
                usage = VkBufferUsageFlags.TransferSrc,
                sharingMode = VkSharingMode.Exclusive
            };
            vkCreateBuffer(device, &bufferInfo, null, out buffer.stagingBuffer);
            vkGetBufferMemoryRequirements(device, buffer.stagingBuffer, out var memReqs);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memReqs.size,
                memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent)
            };
            vkAllocateMemory(device, &allocInfo, null, out buffer.stagingMemory);
            vkBindBufferMemory(device, buffer.stagingBuffer, buffer.stagingMemory, 0);

            vkMapMemory(device, buffer.stagingMemory, 0, WholeSize, 0, &buffer.mappedData);

            return buffer;
        }

        public unsafe void DestoryBuffer(vkBuffer buff)
        {
            vkUnmapMemory(device, buff.stagingMemory);

            vkDestroyBuffer(device, buff.stagingBuffer, null);

            vkFreeMemory(device, buff.stagingMemory, null);
        }

        public unsafe VkSemaphore CreateSemaphore()
        {
            var semaphoreInfo = new VkSemaphoreCreateInfo
            {
                sType = VkStructureType.SemaphoreCreateInfo,
                pNext = null,
                flags = 0
            };

            if (vkCreateSemaphore(device, &semaphoreInfo, null, out var semaphore) != VkResult.Success)
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

            TransitionImageLayout(commandBuffer, texture.image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

            var copyRegion = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageOffset = new VkOffset3D { x = xOffset, y = yOffset, z = 0 },
                imageExtent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 }
            };

            vkCmdCopyBufferToImage(commandBuffer, buffer.stagingBuffer, texture.image, VkImageLayout.TransferDstOptimal, 1, &copyRegion);

            TransitionImageLayout(commandBuffer, texture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            EndSingleCommands(commandBuffer, pool);
        }

        public VkFormat FindDepthFormat()
        {
            VkFormat[] candidates = new VkFormat[]
            {
                VkFormat.D32Sfloat,         // 32 位浮点深度
                VkFormat.D32SfloatS8Uint,   // 32 位浮点深度 + 8 位模板
                VkFormat.D24UnormS8Uint     // 24 位归一化深度 + 8 位模板
            };

            foreach (var format in candidates)
            {
                if (IsFormatSupported(format, VkImageTiling.Optimal, VkFormatFeatureFlags.DepthStencilAttachment))
                {
                    return format;
                }
            }

            throw new Exception("Failed to find a supported depth format!");
        }

        public bool IsFormatSupported(VkFormat format, VkImageTiling tiling, VkFormatFeatureFlags features)
        {
            vkGetPhysicalDeviceFormatProperties(physicalDevice, format, out var formatProperties);

            if (tiling == VkImageTiling.Linear && (formatProperties.linearTilingFeatures & features) == features)
            {
                return true;
            } else if (tiling == VkImageTiling.Optimal && (formatProperties.optimalTilingFeatures & features) == features)
            {
                return true;
            }

            return false;
        }
    }
}
