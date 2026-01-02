using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using LightVK;
using static LightVK.VulkanNative;

namespace ScePSX.Render
{
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

        public void Initialize(Control parent)
        {
            parent.SuspendLayout();
            Dock = DockStyle.Fill;
            Enabled = false;
            parent.Controls.Add(this);
            parent.ResumeLayout();
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

            fixed (VkCommandBufferBeginInfo* pdrawbegininfo = &drawbegininfo)
                vkBeginCommandBuffer(commandBuffer, pdrawbegininfo);

            fixed (VkViewport* pviewport = &viewport)
                vkCmdSetViewport(commandBuffer, 0, 1, pviewport);

            fixed (VkRect2D* pscissor = &scissor)
                vkCmdSetScissor(commandBuffer, 0, 1, pscissor);

            fixed (VkRenderPassBeginInfo* prendinfo = &renderPassInfo)
                vkCmdBeginRenderPass(commandBuffer, prendinfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);

            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, graphicsPipeline);

            fixed (VkDescriptorSet* pDescriptorSet = &descriptorSet)
                vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, pipelineLayout, 0, 1, pDescriptorSet, 0, null);

            if (currentWidth > 0 && currentHeight > 0)
                vkCmdDraw(commandBuffer, 4, 1, 0, 0);

            vkCmdEndRenderPass(commandBuffer);

            vkEndCommandBuffer(commandBuffer);
        }

        public unsafe void Present()
        {
            fixed (VkFence* pinFence = &inFlightFences[currentFrame])
            {
                vkWaitForFences(device, 1, pinFence, true, ulong.MaxValue);
                vkResetFences(device, 1, pinFence);
            }

            VkCommandBuffer cb = commandBuffers[currentFrame];
            submitInfo.pCommandBuffers = &cb;

            fixed (VkSubmitInfo* psubmitInfo = &submitInfo)
                vkQueueSubmit(graphicsQueue, 1, psubmitInfo, VkFence.Null);

            uint idx = (uint)currentFrame;
            VkSwapchainKHR _swapchain = swapChain;
            presentInfo.pSwapchains = &_swapchain;
            presentInfo.pImageIndices = &idx;

            fixed (VkPresentInfoKHR* ppresentInfo = &presentInfo)
                vkQueuePresentKHR(presentQueue, ppresentInfo);

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

            renderPassInfo = new VkRenderPassBeginInfo();
            renderPassInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
            renderPassInfo.clearValueCount = 0;
            renderPassInfo.renderPass = renderPass;

            drawbegininfo = new VkCommandBufferBeginInfo();
            drawbegininfo.sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
            drawbegininfo.flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;

            submitInfo = new VkSubmitInfo();
            submitInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO;
            submitInfo.commandBufferCount = 1;

            presentInfo = new VkPresentInfoKHR();
            presentInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR;
            presentInfo.swapchainCount = 1;

            Console.WriteLine($"[VULKAN] Initializationed...");

            Console.ResetColor();
        }

        public unsafe void VulkanDispose()
        {
            CleanupResources();

            vkDestroyPipeline(device, graphicsPipeline, null);
            vkDestroyPipelineLayout(device, pipelineLayout, null);
            vkDestroyRenderPass(device, renderPass, null);
            foreach (var framebuffer in framebuffers)
                vkDestroyFramebuffer(device, framebuffer, null);
            foreach (var imageView in swapChainImageViews)
                vkDestroyImageView(device, imageView, null);
            vkDestroySwapchainKHR(device, swapChain, null);
            vkDestroyDevice(device, null);
            vkDestroySurfaceKHR(instance, surface, null);
            vkDestroyInstance(instance, null);
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
            Extensions.Add(vkStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);

            //Extensions.Add(vkStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);

            fixed (IntPtr* extensionsBase = &Extensions.Items[0])
            {
                instanceCreateInfo.enabledExtensionCount = Extensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = (byte**)extensionsBase;

                VkResult result;
                fixed (VkInstance* pinstance = &instance)
                {
                    result = vkCreateInstance(&instanceCreateInfo, null, pinstance);
                }
                if (result != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan instance!");
                }
            }
        }

        private unsafe void CreateSurface(IntPtr hwnd)
        {
            IntPtr hinstance;

            if (IntPtr.Size == 8)
            {
                hinstance = GetWindowLongPtr(hwnd, -6);
            } else
            {
                hinstance = GetWindowLong32(hwnd, -6);
            }

            var surfaceCreateInfo = new VkWin32SurfaceCreateInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
                hinstance = hinstance,
                hwnd = hwnd
            };

            fixed(VkSurfaceKHR* psurface = &surface)
                if (vkCreateWin32SurfaceKHR(instance, &surfaceCreateInfo, null, psurface) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create Vulkan surface!");
                }
        }

        private unsafe void SelectPhysicalDevice()
        {
            uint deviceCount = 0;
            vkEnumeratePhysicalDevices(instance, &deviceCount, null);

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
                    Console.WriteLine($"[VULKAN] Selected physical device: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.deviceName)}");
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
            var deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
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

                fixed (VkDevice* pdevice = &device)
                    if (vkCreateDevice(physicalDevice, &deviceCreateInfo, null, pdevice) != VkResult.VK_SUCCESS)
                    {
                        throw new Exception("Failed to create logical device!");
                    }
            }

            fixed (VkQueue* graphicsQueue = &this.graphicsQueue)
                vkGetDeviceQueue(device, (uint)graphicsQueueFamilyIndex, 0, graphicsQueue);
            fixed (VkQueue* presentQueue = &this.presentQueue)
                vkGetDeviceQueue(device, (uint)presentQueueFamilyIndex, 0, presentQueue);
        }

        private unsafe void CreateRenderPass()
        {
            var colorAttachment = new VkAttachmentDescription
            {
                format = swapChainImageFormat,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
                storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
                stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
            };

            var colorAttachmentRef = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            };

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                colorAttachmentCount = 1,
                pColorAttachments = &colorAttachmentRef,
                pResolveAttachments = null
            };

            var dependency = new VkSubpassDependency
            {
                srcSubpass = unchecked((uint)(-1)),
                dstSubpass = 0,
                srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                srcAccessMask = VkAccessFlags.VK_ACCESS_NONE,
                dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT
            };

            var renderPassInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
                attachmentCount = 1,
                pAttachments = &colorAttachment,
                subpassCount = 1,
                pSubpasses = &subpass,
                dependencyCount = 1,
                pDependencies = &dependency

            };

            fixed (VkRenderPass* prenderPass = &renderPass)
                if (vkCreateRenderPass(device, &renderPassInfo, null, prenderPass) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create render pass!");
                }
        }

        private unsafe void CreateDescriptorSetLayout()
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

            fixed (VkDescriptorSetLayout* pdescriptorSetLayout = &descriptorSetLayout)
                if (vkCreateDescriptorSetLayout(device, &layoutInfo, null, pdescriptorSetLayout) != VkResult.VK_SUCCESS)
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

            // 定义顶点输入信息
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO,
                vertexBindingDescriptionCount = 0,
                pVertexBindingDescriptions = null,
                vertexAttributeDescriptionCount = 0,
                pVertexAttributeDescriptions = null
            };

            // 定义输入装配信息
            var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO,
                topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_STRIP,
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
                rasterizationSamples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT
            };

            // 定义颜色混合信息
            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT,
                blendEnable = false
            };

            var colorBlending = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
                logicOpEnable = false,
                logicOp = VkLogicOp.VK_LOGIC_OP_COPY,
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
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                setLayoutCount = 1,
                pSetLayouts = &dsl,
                pushConstantRangeCount = 0,
                pPushConstantRanges = null
            };

            fixed (VkPipelineLayout* ppipelineLayout = &pipelineLayout)
                if (vkCreatePipelineLayout(device, &pipelineLayoutInfo, null, ppipelineLayout) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create pipeline layout!");
                }

            // 创建图形管线
            vkFixedArray2<VkDynamicState> dynstate;
            dynstate.First = VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT;
            dynstate.Second = VkDynamicState.VK_DYNAMIC_STATE_SCISSOR;

            VkPipelineDynamicStateCreateInfo dyn = new VkPipelineDynamicStateCreateInfo();
            dyn.dynamicStateCount = dynstate.Count;
            dyn.pDynamicStates = &dynstate.First;

            vkFixedArray2<VkPipelineShaderStageCreateInfo> shaderStages;
            shaderStages.First = vertShaderStageInfo;
            shaderStages.Second = fragShaderStageInfo;

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
                layout = pipelineLayout,
                renderPass = renderPass,
                subpass = 0,
                basePipelineHandle = VkPipeline.Null,
                basePipelineIndex = -1,
                pDynamicState = &dyn
            };

            VkResult result;
            fixed (VkPipeline* pgraphicsPipeline = &graphicsPipeline)
                result = vkCreateGraphicsPipelines(device, VkPipelineCache.Null, 1, &pipelineInfo, null, pgraphicsPipeline);

            if (result != VkResult.VK_SUCCESS)
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

        private unsafe void CreateSwapChain(int width, int height)
        {
            var surfaceFormat = ChooseSurfaceFormat();
            var presentMode = ChoosePresentMode();
            var extent = new VkExtent2D((uint)width, (uint)height);

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
                imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
                preTransform = VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_INHERIT_BIT_KHR,
                compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
                presentMode = presentMode,
                clipped = true
            };

            fixed (VkSwapchainKHR* pswapChain = &swapChain)
                if (vkCreateSwapchainKHR(device, &swapChainCreateInfo, null, pswapChain) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to create swap chain!");
                }

            vkGetSwapchainImagesKHR(device, swapChain, &imageCount, null);

            swapChainImages.Resize(imageCount);

            fixed (VkImage* pswapChainImages = &swapChainImages[0])
                vkGetSwapchainImagesKHR(device, swapChain, &imageCount, pswapChainImages);

            swapChainImageFormat = surfaceFormat.format;
            swapChainExtent = extent;
        }

        private unsafe void CreateImageViews()
        {
            swapChainImageViews.Resize(swapChainImages.Count);
            for (int i = 0; i < swapChainImages.Count; i++)
            {
                VkImageViewCreateInfo imageViewCI = new VkImageViewCreateInfo();
                imageViewCI.image = swapChainImages[i];
                imageViewCI.viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D;
                imageViewCI.format = swapChainImageFormat;
                imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;
                imageViewCI.subresourceRange.baseMipLevel = 0;
                imageViewCI.subresourceRange.levelCount = 1;
                imageViewCI.subresourceRange.baseArrayLayer = 0;
                imageViewCI.subresourceRange.layerCount = 1;

                fixed (VkImageView* pimageView = &swapChainImageViews[i])
                    if (vkCreateImageView(device, &imageViewCI, null, pimageView) != VkResult.VK_SUCCESS)
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
                VkFramebufferCreateInfo framebufferCI = new VkFramebufferCreateInfo();
                framebufferCI.renderPass = renderPass;
                framebufferCI.attachmentCount = 1;
                framebufferCI.pAttachments = &attachment;
                framebufferCI.width = swapChainExtent.width;
                framebufferCI.height = swapChainExtent.height;
                framebufferCI.layers = 1;

                fixed (VkFramebuffer* pframebuffer = &framebuffers[i])
                    vkCreateFramebuffer(device, &framebufferCI, null, pframebuffer);
            }
        }

        private unsafe void CreateCommandBuffers()
        {
            commandBuffers.Resize(framebuffers.Count);

            var poolInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
                queueFamilyIndex = (uint)graphicsQueueFamilyIndex,
                flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT
            };

            fixed (VkCommandPool* commandPool = &this.commandPool)
                if (vkCreateCommandPool(device, &poolInfo, null, commandPool) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create command pool!");
            }

            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
                commandPool = commandPool,
                level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
                commandBufferCount = framebuffers.Count
            };

            var buffers = new VkCommandBuffer[framebuffers.Count];
            fixed (VkCommandBuffer* pcommandBuffers = &commandBuffers[0])
                if (vkAllocateCommandBuffers(device, &allocInfo, pcommandBuffers) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to allocate command buffers!");
            }

            VkFenceCreateInfo fenceInfo = new VkFenceCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
                flags = VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT
            };

            inFlightFences.Resize(framebuffers.Count);
            for (int i = 0; i < inFlightFences.Count; i++)
            {
                fixed (VkFence* pinFlightFence = &inFlightFences[i])
                    if (vkCreateFence(device, &fenceInfo, null, pinFlightFence) != VkResult.VK_SUCCESS)
                    throw new Exception("Failed to create fence!");
            }
        }

        private unsafe void CreateDescriptorPool()
        {
            vkFixedArray2<VkDescriptorPoolSize> poolSizes;
            poolSizes.First.type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
            poolSizes.First.descriptorCount = 1;

            VkDescriptorPoolCreateInfo poolInfo = new VkDescriptorPoolCreateInfo();
            poolInfo.poolSizeCount = 1;
            poolInfo.pPoolSizes = &poolSizes.First;
            poolInfo.maxSets = 1;

            fixed (VkDescriptorPool* pdescriptorPool = &descriptorPool)
                if (vkCreateDescriptorPool(device, &poolInfo, null, pdescriptorPool) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create descriptor pool!");
            }
        }

        private unsafe void CreateDescriptorSet()
        {
            VkDescriptorSetLayout dsl = descriptorSetLayout;
            VkDescriptorSetAllocateInfo allocInfo = new VkDescriptorSetAllocateInfo();
            allocInfo.descriptorPool = descriptorPool;
            allocInfo.pSetLayouts = &dsl;
            allocInfo.descriptorSetCount = 1;

            fixed (VkDescriptorSet* pdescriptorSet = &descriptorSet)
                if (vkAllocateDescriptorSets(device, &allocInfo, pdescriptorSet) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to allocate descriptor set!");
            }

            // 创建采样器
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

            // 创建图像视图
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

            // 更新描述符集
            VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                imageView = textureImageView,
                sampler = textureSampler
            };

            VkWriteDescriptorSet descriptorWrite = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                dstSet = descriptorSet,
                dstBinding = 0,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                descriptorCount = 1,
                pImageInfo = &imageInfo
            };

            vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
        }

        private unsafe VkSurfaceFormatKHR ChooseSurfaceFormat()
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

            if (formats.Count == 1 && formats[0].format == VkFormat.VK_FORMAT_UNDEFINED)
            {
                return new VkSurfaceFormatKHR { format = VkFormat.VK_FORMAT_B8G8R8A8_UNORM, colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR };
            }
            foreach (var format in formats)
            {
                if (format.format == VkFormat.VK_FORMAT_B8G8R8A8_UNORM && format.colorSpace == VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR)
                {
                    return format;
                }
            }
            return formats[0];
        }

        private unsafe VkPresentModeKHR ChoosePresentMode()
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

        private unsafe uint GetSwapChainImageCount()
        {
            vkRawList<VkSurfaceCapabilitiesKHR> capabilities = new vkRawList<VkSurfaceCapabilitiesKHR>(1);

            fixed(VkSurfaceCapabilitiesKHR* pcapabilities = &capabilities[0])
                vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, pcapabilities);
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

            TransitionImageLayout(image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
        }

        public unsafe void InitializeImage(int width, int height)
        {
            stagingBuffer = CreateStagingBuffer(width * height * 4);

            VkMemoryRequirements memRequirements;
            vkGetBufferMemoryRequirements(device, stagingBuffer, &memRequirements);
            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT)
            };
            fixed (VkDeviceMemory* pstagingBufferMemory = &stagingBufferMemory)
                vkAllocateMemory(device, &allocInfo, null, pstagingBufferMemory);
            vkBindBufferMemory(device, stagingBuffer, stagingBufferMemory, 0);

            image = CreateImage(width, height, VkFormat.VK_FORMAT_B8G8R8A8_UNORM);

            vkGetImageMemoryRequirements(device, image, &memRequirements);
            allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
            };
            fixed (VkDeviceMemory* pimageMemory = &imageMemory)
                vkAllocateMemory(device, &allocInfo, null, pimageMemory);
            vkBindImageMemory(device, image, imageMemory, 0);

            TransitionImageLayout(image, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
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
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
                size = (ulong)size,
                usage = VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE
            };

            VkBuffer stagingBuffer;
            if (vkCreateBuffer(device, &bufferInfo, null, &stagingBuffer) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create staging buffer!");
            }

            return stagingBuffer;
        }

        private unsafe VkImage CreateImage(int width, int height, VkFormat format)
        {
            var imageInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
                imageType = VkImageType.VK_IMAGE_TYPE_2D,
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                format = format,
                tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
                initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT
            };

            VkImage image;
            if (vkCreateImage(device, &imageInfo, null, &image) != VkResult.VK_SUCCESS)
            {
                throw new Exception("Failed to create image!");
            }

            VkMemoryRequirements memRequirements;
            vkGetImageMemoryRequirements(device, image, &memRequirements);

            var allocInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                allocationSize = memRequirements.size,
                memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
            };

            VkDeviceMemory imageMemory;
            if (vkAllocateMemory(device, &allocInfo, null, &imageMemory) != VkResult.VK_SUCCESS)
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
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
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

            vkCmdCopyBufferToImage(commandBuffer, buffer, image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &region);

            EndSingleTimeCommands(commandBuffer);
        }

        private unsafe uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
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

        private unsafe VkCommandBuffer BeginSingleTimeCommands()
        {
            VkCommandBufferAllocateInfo allocInfo = new VkCommandBufferAllocateInfo();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = commandPool;
            allocInfo.level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY;

            VkCommandBuffer cb;
            vkAllocateCommandBuffers(device, &allocInfo, &cb);

            VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo();
            beginInfo.flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;

            vkBeginCommandBuffer(cb, &beginInfo);

            return cb;
        }

        private unsafe void EndSingleTimeCommands(VkCommandBuffer commandBuffer)
        {
            vkEndCommandBuffer(commandBuffer);
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };
            vkQueueSubmit(graphicsQueue, 1, &submitInfo, VkFence.Null);
            vkQueueWaitIdle(graphicsQueue);
            vkFreeCommandBuffers(device, commandPool, 1, &commandBuffer);
        }

        private unsafe void TransitionImageLayout(VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            const uint VkQueueFamilyIgnored = ~0U;
            var commandBuffer = BeginSingleTimeCommands();
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
                }
            };
            VkPipelineStageFlags sourceStage;
            VkPipelineStageFlags destinationStage;
            if (oldLayout == VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL)
            {
                barrier.srcAccessMask = VkAccessFlags.VK_ACCESS_NONE;
                barrier.dstAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;
                sourceStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
                destinationStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;
            } else if (oldLayout == VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL && newLayout == VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
            {
                barrier.srcAccessMask = VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT;
                barrier.dstAccessMask = VkAccessFlags.VK_ACCESS_SHADER_READ_BIT;
                sourceStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_TRANSFER_BIT;
                destinationStage = VkPipelineStageFlags.VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
            } else
            {
                throw new Exception("Unsupported layout transition!");
            }
            vkCmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, &barrier);
            EndSingleTimeCommands(commandBuffer);
        }

        #endregion

    }

}
