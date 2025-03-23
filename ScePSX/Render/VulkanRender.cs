using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Vulkan;
using Vulkan.Win32;
using static Khronos.Platform;
using static Vulkan.VulkanNative;

namespace ScePSX.Render
{
    public static class vkStrings
    {
        public static FixedUtf8String AppName { get; } = "ScePSX";
        public static FixedUtf8String EngineName { get; } = "VulkanRenderer";
        public static FixedUtf8String VK_KHR_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_surface";
        public static FixedUtf8String VK_KHR_WIN32_SURFACE_EXTENSION_NAME { get; } = "VK_KHR_win32_surface";
        public static FixedUtf8String VK_KHR_SWAPCHAIN_EXTENSION_NAME { get; } = "VK_KHR_swapchain";
        public static FixedUtf8String VK_EXT_DEBUG_REPORT_EXTENSION_NAME { get; } = "VK_EXT_debug_report";
        public static FixedUtf8String VK_LAYER_KHRONOS_validation { get; } = "VK_LAYER_KHRONOS_validation";
        public static FixedUtf8String VK_KHR_shader_non_semantic_info { get; } = "SPV_KHR_non_semantic_info";
        public static FixedUtf8String main { get; } = "main";
    }

    public class VulkanRenderer : UserControl, IRenderer, IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        private VkInstance instance;
        private VkPhysicalDevice physicalDevice;
        private VkDevice device;
        private VkQueue graphicsQueue;
        private VkQueue presentQueue;
        private VkSurfaceKHR surface;
        private VkSwapchainKHR swapChain;
        private vkRawList<VkImage> swapChainImages = new vkRawList<VkImage>();
        private vkRawList<VkImageView> swapChainImageViews = new vkRawList<VkImageView>();
        private VkFormat swapChainImageFormat;
        private VkExtent2D swapChainExtent;
        private VkRenderPass renderPass;
        private VkPipeline graphicsPipeline;
        private VkPipelineLayout pipelineLayout;
        private vkRawList<VkFramebuffer> framebuffers = new vkRawList<VkFramebuffer>();
        private vkRawList<VkCommandBuffer> commandBuffers = new vkRawList<VkCommandBuffer>();
        private VkDescriptorSetLayout descriptorSetLayout;
        private VkDescriptorPool descriptorPool;
        private VkDescriptorSet descriptorSet;
        private vkRawList<VkFence> inFlightFences = new vkRawList<VkFence>();
        private int currentFrame = 0;
        private VkRenderPassBeginInfo renderPassInfo;
        private VkCommandBufferBeginInfo drawbegininfo;

        private VkBuffer stagingBuffer;
        private VkDeviceMemory stagingBufferMemory;
        private VkImage image;
        private VkDeviceMemory imageMemory;

        private VkViewport viewport;
        private VkRect2D scissor;
        private VkCommandPool commandPool;

        private VkSubmitInfo submitInfo;
        private VkPresentInfoKHR presentInfo;

        private int graphicsQueueFamilyIndex = -1;
        private int presentQueueFamilyIndex = -1;

        private int currentWidth, currentHeight = 0;

        private bool _isDisposed = false;

        public RenderMode Mode => RenderMode.Vulkan;

        public VulkanRenderer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            DoubleBuffered = false;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.Size = new System.Drawing.Size(441, 246);
            this.Name = "VulkanRenderer";
            this.ResumeLayout(false);
        }

        public void Initialize(Control parentControl)
        {
            Parent = parentControl;
            Dock = DockStyle.Fill;
            Enabled = false;
            parentControl.Controls.Add(this);
        }

        public void SetParam(int Param)
        {
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            VulkanInit(this.Handle, this.ClientSize.Width, this.ClientSize.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (device == VkDevice.Null || _isDisposed)
                return;

            Draw();

            Present();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.ClientSize.Width == 0 || this.ClientSize.Height == 0 || device == VkDevice.Null)
            {
                return;
            }

            vkDeviceWaitIdle(device);

            CleanupSwapChain();

            CreateSwapChain(this.ClientSize.Width, this.ClientSize.Height);
            CreateImageViews();
            CreateFramebuffers();

            UpdateViewportAndScissor();

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;
            if (disposing)
            {
                vkDeviceWaitIdle(device);

                VulkanDispose();
            }
            _isDisposed = true;
            base.Dispose(disposing);
        }

        public unsafe void RenderBuffer(int[] pixels, int width, int height, ScaleParam scale)
        {
            if (_isDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RenderBuffer(pixels, width, height, scale)));
                return;
            }

            if (scale.scale > 0)
            {
                pixels = PixelsScaler.Scale(pixels, width, height, scale.scale, scale.mode);

                width = width * scale.scale;
                height = height * scale.scale;
            }

            if (currentWidth != width || currentHeight != height)
            {
                InitializeResources(width, height);
                currentWidth = width;
                currentHeight = height;
                UpdateViewportAndScissor();
            }

            if (device != VkDevice.Null && stagingBufferMemory != VkDeviceMemory.Null)
                UploadImage(pixels, width, height);

            Invalidate();
        }

        public unsafe void Draw()
        {
            VkCommandBuffer commandBuffer = commandBuffers[currentFrame];

            renderPassInfo.framebuffer = framebuffers[currentFrame];
            renderPassInfo.renderArea.extent = swapChainExtent;
            //VkClearValue clearValue = new VkClearValue() { color = new VkClearColorValue( ) };
            //renderPassInfo.clearValueCount = 1;
            //renderPassInfo.pClearValues = &clearValue;

            vkResetCommandBuffer(commandBuffer, 0);

            vkBeginCommandBuffer(commandBuffer, ref drawbegininfo);

            vkCmdSetViewport(commandBuffer, 0, 1, ref viewport);

            vkCmdSetScissor(commandBuffer, 0, 1, ref scissor);

            vkCmdBeginRenderPass(commandBuffer, ref renderPassInfo, VkSubpassContents.Inline);

            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, graphicsPipeline);

            vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, pipelineLayout, 0, 1, ref descriptorSet, 0, null);

            if (currentWidth > 0 && currentHeight > 0)
                vkCmdDraw(commandBuffer, 4, 1, 0, 0);

            vkCmdEndRenderPass(commandBuffer);

            vkEndCommandBuffer(commandBuffer);
        }

        public unsafe void Present()
        {
            vkWaitForFences(device, 1, ref inFlightFences[currentFrame], true, ulong.MaxValue);
            vkResetFences(device, 1, ref inFlightFences[currentFrame]);

            VkCommandBuffer cb = commandBuffers[currentFrame];
            submitInfo.pCommandBuffers = &cb;

            vkQueueSubmit(graphicsQueue, 1, ref submitInfo, VkFence.Null);

            uint idx = (uint)currentFrame;
            VkSwapchainKHR _swapchain = swapChain;
            presentInfo.pSwapchains = &_swapchain;
            presentInfo.pImageIndices = &idx;

            vkQueuePresentKHR(presentQueue, ref presentInfo);

            currentFrame = (currentFrame + 1) % (int)swapChainImages.Count;
        }

        public void VulkanInit(IntPtr hwnd, int width, int height)
        {
            Console.ForegroundColor = ConsoleColor.Blue;

            Console.WriteLine($"[VULKAN] Initialization....");

            CreateInstance();
            CreateSurface(hwnd);
            SelectPhysicalDevice();
            CreateLogicalDevice();
            CreateSwapChain(width, height);
            CreateImageViews();
            CreateRenderPass();
            CreateGraphicsPipeline();
            CreateFramebuffers();
            CreateCommandBuffers();

            renderPassInfo = VkRenderPassBeginInfo.New();
            renderPassInfo.clearValueCount = 0;
            renderPassInfo.renderPass = renderPass;

            drawbegininfo = VkCommandBufferBeginInfo.New();
            drawbegininfo.sType = VkStructureType.CommandBufferBeginInfo;
            drawbegininfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

            submitInfo = VkSubmitInfo.New();
            submitInfo.sType = VkStructureType.SubmitInfo;
            submitInfo.commandBufferCount = 1;

            presentInfo = VkPresentInfoKHR.New();
            presentInfo.sType = VkStructureType.PresentInfoKHR;
            presentInfo.swapchainCount = 1;

            Console.WriteLine($"[VULKAN] Initializationed...");

            Console.ResetColor();
        }

        public unsafe void VulkanDispose()
        {
            CleanupResources();

            vkDestroyPipeline(device, graphicsPipeline, IntPtr.Zero);
            vkDestroyPipelineLayout(device, pipelineLayout, IntPtr.Zero);
            vkDestroyRenderPass(device, renderPass, IntPtr.Zero);
            foreach (var framebuffer in framebuffers)
                vkDestroyFramebuffer(device, framebuffer, IntPtr.Zero);
            foreach (var imageView in swapChainImageViews)
                vkDestroyImageView(device, imageView, IntPtr.Zero);
            vkDestroySwapchainKHR(device, swapChain, IntPtr.Zero);
            vkDestroyDevice(device, IntPtr.Zero);
            vkDestroySurfaceKHR(instance, surface, IntPtr.Zero);
            vkDestroyInstance(instance, IntPtr.Zero);
        }

        private unsafe void CleanupSwapChain()
        {
            foreach (var framebuffer in framebuffers)
            {
                vkDestroyFramebuffer(device, framebuffer, null);
            }
            foreach (var imageView in swapChainImageViews)
            {
                vkDestroyImageView(device, imageView, null);
            }
            vkDestroySwapchainKHR(device, swapChain, null);

            framebuffers.Clear();
            swapChainImageViews.Clear();
        }

        private void UpdateViewportAndScissor()
        {
            viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = (float)swapChainExtent.width,
                height = (float)swapChainExtent.height,
                minDepth = 0,
                maxDepth = 1
            };

            scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = swapChainExtent
            };
        }

        #region VulkanInitialization

        public static uint VK_MAKE_VERSION(uint major, uint minor, uint patch)
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

        private unsafe void CreateSurface(IntPtr hwnd)
        {
            HINSTANCE hinstance;

            if (IntPtr.Size == 8)
            {
                hinstance = GetWindowLongPtr(hwnd, -6);
            } else
            {
                hinstance = GetWindowLong32(hwnd, -6);
            }

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
                    Console.WriteLine($"[VULKAN] Selected physical device: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.deviceName)}");
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

        private unsafe void CreateRenderPass()
        {
            var colorAttachment = new VkAttachmentDescription
            {
                format = swapChainImageFormat,
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
                pColorAttachments = &colorAttachmentRef,
                pResolveAttachments = null
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

            if (vkCreateRenderPass(device, &renderPassInfo, null, out renderPass) != VkResult.Success)
            {
                throw new Exception("Failed to create render pass!");
            }
        }

        private unsafe void CreateDescriptorSetLayout()
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

            if (vkCreateDescriptorSetLayout(device, ref layoutInfo, null, out descriptorSetLayout) != VkResult.Success)
            {
                throw new Exception("Failed to create descriptor set layout!");
            }
        }

        private byte[] LoadShader(string filename)
        {
            return System.IO.File.ReadAllBytes(filename);
        }

        private unsafe void CreateGraphicsPipeline()
        {
            var vertShaderCode = LoadShader("./Shaders/shader.vert.spv");
            var fragShaderCode = LoadShader("./Shaders/shader.frag.spv");

            var vertShaderModule = CreateShaderModule(vertShaderCode);
            var fragShaderModule = CreateShaderModule(fragShaderCode);

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
                width = (float)swapChainExtent.width,
                height = (float)swapChainExtent.height,
                minDepth = 0,
                maxDepth = 1
            };

            var scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = swapChainExtent
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
                rasterizationSamples = VkSampleCountFlags.Count1
            };

            // 定义颜色混合信息
            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A,
                blendEnable = false
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

            CreateDescriptorSetLayout();

            // 创建管线布局
            VkDescriptorSetLayout dsl = descriptorSetLayout;
            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 1,
                pSetLayouts = &dsl,
                pushConstantRangeCount = 0,
                pPushConstantRanges = null
            };

            if (vkCreatePipelineLayout(device, &pipelineLayoutInfo, null, out pipelineLayout) != VkResult.Success)
            {
                throw new Exception("Failed to create pipeline layout!");
            }

            // 创建图形管线
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
                layout = pipelineLayout,
                renderPass = renderPass,
                subpass = 0,
                basePipelineHandle = VkPipeline.Null,
                basePipelineIndex = -1,
                pDynamicState = &dyn
            };

            VkResult result = vkCreateGraphicsPipelines(device, VkPipelineCache.Null, 1, &pipelineInfo, null, out graphicsPipeline);
            if (result != VkResult.Success)
            {
                throw new Exception("Failed to create graphics pipeline!");
            }

            Console.WriteLine("[VULKAN] GraphicsPipelines Created.");

            // 清理着色器模块
            vkDestroyShaderModule(device, vertShaderModule, null);
            vkDestroyShaderModule(device, fragShaderModule, null);
        }

        private unsafe VkShaderModule CreateShaderModule(byte[] code)
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

        private unsafe void CreateSwapChain(int width, int height)
        {
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

            if (vkCreateSwapchainKHR(device, ref swapChainCreateInfo, null, out swapChain) != VkResult.Success)
            {
                throw new Exception("Failed to create swap chain!");
            }

            vkGetSwapchainImagesKHR(device, swapChain, &imageCount, null);

            swapChainImages.Resize(imageCount);

            vkGetSwapchainImagesKHR(device, swapChain, &imageCount, out swapChainImages[0]);

            swapChainImageFormat = surfaceFormat.format;
            swapChainExtent = extent;
        }

        private unsafe void CreateImageViews()
        {
            swapChainImageViews.Resize(swapChainImages.Count);
            for (int i = 0; i < swapChainImages.Count; i++)
            {
                VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
                imageViewCI.image = swapChainImages[i];
                imageViewCI.viewType = VkImageViewType.Image2D;
                imageViewCI.format = swapChainImageFormat;
                imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
                imageViewCI.subresourceRange.baseMipLevel = 0;
                imageViewCI.subresourceRange.levelCount = 1;
                imageViewCI.subresourceRange.baseArrayLayer = 0;
                imageViewCI.subresourceRange.layerCount = 1;

                if (vkCreateImageView(device, ref imageViewCI, null, out swapChainImageViews[i]) != VkResult.Success)
                {
                    throw new Exception("Failed to create image view!");
                }
            }
        }

        private unsafe void CreateFramebuffers()
        {
            framebuffers.Resize(swapChainImageViews.Count);
            for (uint i = 0; i < framebuffers.Count; i++)
            {
                VkImageView attachment = swapChainImageViews[i];
                VkFramebufferCreateInfo framebufferCI = VkFramebufferCreateInfo.New();
                framebufferCI.renderPass = renderPass;
                framebufferCI.attachmentCount = 1;
                framebufferCI.pAttachments = &attachment;
                framebufferCI.width = swapChainExtent.width;
                framebufferCI.height = swapChainExtent.height;
                framebufferCI.layers = 1;

                vkCreateFramebuffer(device, ref framebufferCI, null, out framebuffers[i]);
            }
        }

        private unsafe void CreateCommandBuffers()
        {
            commandBuffers.Resize(framebuffers.Count);

            var poolInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.CommandPoolCreateInfo,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                flags = VkCommandPoolCreateFlags.ResetCommandBuffer
            };

            if (vkCreateCommandPool(device, &poolInfo, null, out commandPool) != VkResult.Success)
            {
                throw new Exception("Failed to create command pool!");
            }

            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                commandPool = commandPool,
                level = VkCommandBufferLevel.Primary,
                commandBufferCount = framebuffers.Count
            };

            var buffers = new VkCommandBuffer[framebuffers.Count];
            if (vkAllocateCommandBuffers(device, &allocInfo, out commandBuffers[0]) != VkResult.Success)
            {
                throw new Exception("Failed to allocate command buffers!");
            }

            VkFenceCreateInfo fenceInfo = new VkFenceCreateInfo
            {
                sType = VkStructureType.FenceCreateInfo,
                flags = VkFenceCreateFlags.Signaled
            };

            inFlightFences.Resize(framebuffers.Count);
            for (int i = 0; i < inFlightFences.Count; i++)
            {
                if (vkCreateFence(device, ref fenceInfo, null, out inFlightFences[i]) != VkResult.Success)
                    throw new Exception("Failed to create fence!");
            }
        }

        private unsafe void CreateDescriptorPool()
        {
            vkFixedArray2<VkDescriptorPoolSize> poolSizes;
            poolSizes.First.type = VkDescriptorType.CombinedImageSampler;
            poolSizes.First.descriptorCount = 1;

            VkDescriptorPoolCreateInfo poolInfo = VkDescriptorPoolCreateInfo.New();
            poolInfo.poolSizeCount = 1;
            poolInfo.pPoolSizes = &poolSizes.First;
            poolInfo.maxSets = 1;

            if (vkCreateDescriptorPool(device, ref poolInfo, null, out descriptorPool) != VkResult.Success)
            {
                throw new Exception("Failed to create descriptor pool!");
            }
        }

        private unsafe void CreateDescriptorSet()
        {
            VkDescriptorSetLayout dsl = descriptorSetLayout;
            VkDescriptorSetAllocateInfo allocInfo = VkDescriptorSetAllocateInfo.New();
            allocInfo.descriptorPool = descriptorPool;
            allocInfo.pSetLayouts = &dsl;
            allocInfo.descriptorSetCount = 1;

            if (vkAllocateDescriptorSets(device, ref allocInfo, out descriptorSet) != VkResult.Success)
            {
                throw new Exception("Failed to allocate descriptor set!");
            }

            // 创建采样器
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

            // 创建图像视图
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

            // 更新描述符集
            VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                imageView = textureImageView,
                sampler = textureSampler
            };

            VkWriteDescriptorSet descriptorWrite = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                dstSet = descriptorSet,
                dstBinding = 0,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.CombinedImageSampler,
                descriptorCount = 1,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
        }

        private unsafe VkSurfaceFormatKHR ChooseSurfaceFormat()
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

        private unsafe VkPresentModeKHR ChoosePresentMode()
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

        private uint GetSwapChainImageCount()
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

        #endregion

        #region ImageResource

        private unsafe void UploadImage(int[] pixels, int width, int height)
        {
            if (device == VkDevice.Null)
                return;

            void* data;
            vkMapMemory(device, stagingBufferMemory, 0, (ulong)(width * height * 4), 0, &data);
            Marshal.Copy(pixels, 0, (IntPtr)data, width * height);
            vkUnmapMemory(device, stagingBufferMemory);

            CopyBufferToImage(stagingBuffer, image, width, height);

            TransitionImageLayout(image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);
        }

        public unsafe void InitializeImage(int width, int height)
        {
            stagingBuffer = CreateStagingBuffer(width * height * 4);
            vkGetBufferMemoryRequirements(device, stagingBuffer, out var memRequirements);
            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent)
            };
            vkAllocateMemory(device, ref allocInfo, null, out stagingBufferMemory);
            vkBindBufferMemory(device, stagingBuffer, stagingBufferMemory, 0);

            image = CreateImage(width, height, VkFormat.B8g8r8a8Unorm);
            vkGetImageMemoryRequirements(device, image, out memRequirements);
            allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
            };
            vkAllocateMemory(device, ref allocInfo, null, out imageMemory);
            vkBindImageMemory(device, image, imageMemory, 0);

            TransitionImageLayout(image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);
        }

        private void InitializeResources(int width, int height)
        {
            vkDeviceWaitIdle(device);

            CleanupResources();

            InitializeImage(width, height);

            CreateDescriptorPool();

            CreateDescriptorSet();
        }

        private unsafe void CleanupResources()
        {
            if (stagingBuffer != VkBuffer.Null)
            {
                vkDestroyBuffer(device, stagingBuffer, null);
                stagingBuffer = VkBuffer.Null;
            }
            if (stagingBufferMemory != VkDeviceMemory.Null)
            {
                vkFreeMemory(device, stagingBufferMemory, null);
                stagingBufferMemory = VkDeviceMemory.Null;
            }
            if (image != VkImage.Null)
            {
                vkDestroyImage(device, image, null);
                image = VkImage.Null;
            }
            if (imageMemory != VkDeviceMemory.Null)
            {
                vkFreeMemory(device, imageMemory, null);
                imageMemory = VkDeviceMemory.Null;
            }
            if (descriptorPool != VkDescriptorPool.Null)
            {
                vkDestroyDescriptorPool(device, descriptorPool, null);
                descriptorPool = VkDescriptorPool.Null;
            }
        }

        private unsafe VkBuffer CreateStagingBuffer(int size)
        {
            var bufferInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.BufferCreateInfo,
                size = (ulong)size,
                usage = VkBufferUsageFlags.TransferSrc,
                sharingMode = VkSharingMode.Exclusive
            };

            if (vkCreateBuffer(device, ref bufferInfo, null, out stagingBuffer) != VkResult.Success)
            {
                throw new Exception("Failed to create staging buffer!");
            }

            return stagingBuffer;
        }

        private unsafe VkImage CreateImage(int width, int height, VkFormat format)
        {
            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                imageType = VkImageType.Image2D,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                format = format,
                tiling = VkImageTiling.Optimal,
                initialLayout = VkImageLayout.Undefined,
                usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                samples = VkSampleCountFlags.Count1
            };

            if (vkCreateImage(device, ref imageInfo, null, out image) != VkResult.Success)
            {
                throw new Exception("Failed to create image!");
            }

            vkGetImageMemoryRequirements(device, image, out var memRequirements);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
            };

            if (vkAllocateMemory(device, ref allocInfo, null, out imageMemory) != VkResult.Success)
            {
                throw new Exception("Failed to allocate memory for image!");
            }

            vkBindImageMemory(device, image, imageMemory, 0);

            return image;
        }

        private unsafe void CopyBufferToImage(VkBuffer buffer, VkImage image, int width, int height)
        {
            VkCommandBuffer commandBuffer = BeginSingleTimeCommands();

            var subresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            };

            var region = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = subresource,
                imageOffset = new VkOffset3D { x = 0, y = 0, z = 0 },
                imageExtent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 }
            };

            vkCmdCopyBufferToImage(commandBuffer, buffer, image, VkImageLayout.TransferDstOptimal, 1, ref region);

            EndSingleTimeCommands(commandBuffer);
        }

        private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
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

        private VkCommandBuffer BeginSingleTimeCommands()
        {
            VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = commandPool;
            allocInfo.level = VkCommandBufferLevel.Primary;

            vkAllocateCommandBuffers(device, ref allocInfo, out VkCommandBuffer cb);

            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

            vkBeginCommandBuffer(cb, ref beginInfo);

            return cb;
        }

        private unsafe void EndSingleTimeCommands(VkCommandBuffer commandBuffer)
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
            vkFreeCommandBuffers(device, commandPool, 1, ref commandBuffer);
        }

        private unsafe void TransitionImageLayout(VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            const uint VkQueueFamilyIgnored = ~0U;
            var commandBuffer = BeginSingleTimeCommands();
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
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };
            VkPipelineStageFlags sourceStage;
            VkPipelineStageFlags destinationStage;
            if (oldLayout == VkImageLayout.Undefined && newLayout == VkImageLayout.TransferDstOptimal)
            {
                barrier.srcAccessMask = 0;
                barrier.dstAccessMask = VkAccessFlags.TransferWrite;
                sourceStage = VkPipelineStageFlags.TopOfPipe;
                destinationStage = VkPipelineStageFlags.Transfer;
            } else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.srcAccessMask = VkAccessFlags.TransferWrite;
                barrier.dstAccessMask = VkAccessFlags.ShaderRead;
                sourceStage = VkPipelineStageFlags.Transfer;
                destinationStage = VkPipelineStageFlags.FragmentShader;
            } else
            {
                throw new Exception("Unsupported layout transition!");
            }
            vkCmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, ref barrier);
            EndSingleTimeCommands(commandBuffer);
        }

        #endregion

    }

}
