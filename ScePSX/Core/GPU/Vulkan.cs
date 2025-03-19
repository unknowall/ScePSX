using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

using Vulkan;
using Vulkan.Win32;
using static Vulkan.VulkanNative;

using ScePSX.Render;
using static ScePSX.VulkanDevice;
using System.Drawing;

namespace ScePSX
{
    public class VulkanGPU : IGPU
    {
        public GPUType type => GPUType.Vulkan;

        #region VARS

        const int VRAM_WIDTH = 1024;
        const int VRAM_HEIGHT = 512;

        const float VRamWidthF = VRAM_WIDTH;
        const float VRamHeightF = VRAM_HEIGHT;

        const int VRamWidthMask = VRAM_WIDTH - 1;
        const int VRamHeightMask = VRAM_HEIGHT - 1;

        const int TexturePageWidth = 256;
        const int TexturePageHeight = 256;

        const int TexturePageBaseXMult = 64;
        const int TexturePageBaseYMult = 256;

        const int ClutWidth = 256;
        const int ClutHeight = 1;

        const int ClutBaseXMult = 16;
        const int ClutBaseYMult = 1;

        static readonly int[] ColorModeClutWidths = { 16, 256, 0, 0 };
        static readonly int[] ColorModeTexturePageWidths =
        {
            TexturePageWidth / 4,
            TexturePageWidth / 2,
            TexturePageWidth,
            TexturePageWidth
        };

        private TDrawingArea DrawingAreaTopLeft, DrawingAreaBottomRight;
        private TDrawingOffset DrawingOffset;
        private VRAMTransfer _VRAMTransfer;

        private int TextureWindowXMask, TextureWindowYMask, TextureWindowXOffset, TextureWindowYOffset;

        private bool CheckMaskBit, ForceSetMaskBit;

        bool isDisposed = false;
        bool m_dither = false;
        bool m_realColor = false;

        bool m_semiTransparencyEnabled = false;
        byte m_semiTransparencyMode = 0;

        uint oldmaskbit, oldtexwin;
        short m_currentDepth = 1;

        int resolutionScale = 1;

        glTexPage m_TexPage;
        glClutAttribute m_clut;

        glRectangle<int> m_dirtyArea, m_clutArea, m_textureArea;

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public glPosition v_pos;
            public glColor v_color;
            public glTexCoord v_texCoord;
            public glClutAttribute v_clut;
            public glTexPage v_texPage;
            public Vector3 v_pos_high;
        }

        List<Vertex> Vertexs = new List<Vertex>();

        struct DisplayArea
        {
            public int x = 0;
            public int y = 0;
            public int width = 0;
            public int height = 0;

            public DisplayArea()
            {
            }
        };

        DisplayArea m_vramDisplayArea;
        DisplayArea m_targetDisplayArea;

        unsafe ushort* VRAM;

        #endregion

        VulkanDevice Device;

        VkRenderPass renderPass;

        vkSwapchain drawChain;
        vkBuffer stagingBuffer, vertexUBO, fragmentUBO, out24UBO , vaoUBO;
        vkTexture vramTexture, depthTexture, drawTexture, displayTexture;
        VkFramebuffer drawFramebuff, displayFrameBuff;
        VkImageView drawimageview2, displayimageview2;
        vkCMDS tempCmd;

        vkGraphicsPipeline currentPipeline;
        vkGraphicsPipeline ramViewPipeline, out24Pipeline, out16Pipeline, displayPipeline;
        vkGraphicsPipeline drawPipelineWithAddBlend, drawPipelineWithSubtractBlend, drawPipelineWithoutBlending;

        VkDescriptorPool descriptorPool, drawdescriptorPool, blankdescriptorPool;
        VkDescriptorSet displayDescriptorSet, drawDescriptorSet, blankDescriptorSet;
        VkDescriptorSetLayout out214DescriptorLayout, out16DescriptorLayout, drawDescriptorLayout, blankDescriptorLayout;

        VkSemaphore imageAvailableSemaphore, renderFinishedSemaphore;

        List<VkFramebuffer> framebuffers = new List<VkFramebuffer>();
        VkViewport viewport;
        VkRect2D scissor;

        Dictionary<VkFramebuffer, VkImageView> framebufferAttachments = new Dictionary<VkFramebuffer, VkImageView>();

        [StructLayout(LayoutKind.Sequential)]
        public struct drawvertUBO
        {
            public float u_resolutionScale;
            public int u_pgxp; // Vulkan 不支持 bool，用 int 代替
            public Matrix4x4 u_mvp; // 模型视图投影矩阵
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct drawfragUBO
        {
            public float u_srcBlend;
            public float u_destBlend;
            public int u_setMaskBit; // Vulkan 不支持 bool，用 int 代替
            public int u_drawOpaquePixels;
            public int u_drawTransparentPixels;
            public int u_dither;
            public int u_realColor;
            public Vector2 u_texWindowMask;
            public Vector2 u_texWindowOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct outUBO
        {
            public ivec4 u_srcRect; // 源矩形区域 (x, y, width, height)
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ivec4
        {
            public int x, y, z, w;
            public ivec4(int x, int y, int z, int w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }
        }

        public bool ViewVRam = false;
        public bool DisplayEnable = true;
        public int IRScale;
        public bool RealColor, PGXP, PGXPT, KEEPAR;
        const float AspectRatio = 4.0f / 3.0f;

        public VulkanGPU()
        {
            Device = new VulkanDevice();
        }

        public unsafe void Initialize()
        {
            VRAM = (ushort*)Marshal.AllocHGlobal((VRAM_WIDTH * VRAM_HEIGHT) * 2);

            Device.VulkanInit(NullRenderer.hwnd, NullRenderer.hinstance);

            drawChain = Device.CreateSwapChain(NullRenderer.ClientWidth, NullRenderer.ClientHeight);
            renderPass = Device.CreateRenderPass(drawChain.ImageFormat);

            stagingBuffer = Device.CreateBuffer(1024 * 512 * 2);

            vaoUBO = Device.CreateBuffer((ulong)(1024 * sizeof(drawvertUBO)), VkBufferUsageFlags.VertexBuffer);

            vramTexture = Device.CreateTexture(
                1024, 512, 
                VkFormat.R5g5b5a1UnormPack16,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled
                );
            drawTexture = Device.CreateTexture(
                1024, 512, 
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment
                );
            displayTexture = Device.CreateTexture(
                1024, 512, 
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment
                );

            CreateDepthResources(1024, 512);

            drawimageview2 = Device.CreateImageView(drawTexture.image, VkFormat.R8g8b8a8Unorm);

            drawFramebuff = Device.CreateFramebuffer(renderPass, drawTexture.imageview, drawimageview2, 1024, 512);

            displayimageview2 = Device.CreateImageView(displayTexture.image, VkFormat.R8g8b8a8Unorm);

            displayFrameBuff = Device.CreateFramebuffer(renderPass, displayTexture.imageview, displayimageview2, 1024, 512);

            var vertexType = typeof(Vertex);
            VkVertexInputAttributeDescription[] drawAttributes = new VkVertexInputAttributeDescription[6]
            {
                new() { // v_pos (location=0)
                    location = 0,
                    binding = 0,
                    format = VkFormat.R32g32b32Sfloat,
                    offset = (uint)Marshal.OffsetOf(vertexType, "v_pos")
                },
                new() { // v_pos_high (location=1)
                    location = 1,
                    binding = 0,
                    format = VkFormat.R32g32b32Sfloat,
                    offset = (uint)Marshal.OffsetOf(vertexType, "v_pos_high")
                },
                new() { // v_texCoord (location=2)
                    location = 2,
                    binding = 0,
                    format = VkFormat.R32g32b32Sfloat,
                    offset = (uint)Marshal.OffsetOf(vertexType, "v_texCoord")
                },
                new() { // v_color (location=3)
                    location = 3,
                    binding = 0,
                    format = VkFormat.R32g32b32Sfloat,
                    offset = (uint)Marshal.OffsetOf(vertexType, "v_color")
                },
                new() { // v_clut (location=4)
                    location = 4,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = (uint)Marshal.OffsetOf(vertexType, "v_clut")
                },
                new() { // v_texPage (location=5)
                    location = 5,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = (uint)Marshal.OffsetOf(vertexType, "v_texPage")
                }
            };

            VkVertexInputBindingDescription drawBinding = new()
            {
                binding = 0,
                stride = (uint)Marshal.SizeOf<Vertex>(),
                inputRate = VkVertexInputRate.Vertex
            };

            drawdescriptorPool = Device.CreateDescriptorPool(
                maxSets: 2,
                new[]
                {
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.CombinedImageSampler,
                        descriptorCount = 1
                    },
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.UniformBuffer,
                        descriptorCount = 2
                    }
                }
            );

            drawDescriptorLayout = Device.CreateDescriptorSetLayout(new[]
            {
                // 顶点着色器的 UniformBuffer (binding=0)
                new VkDescriptorSetLayoutBinding {
                    binding = 0,
                    descriptorType = VkDescriptorType.UniformBuffer,
                    descriptorCount = 1,
                    stageFlags = VkShaderStageFlags.Vertex
                },
                // 片段着色器的 UniformBuffer (binding=1)
                new VkDescriptorSetLayoutBinding {
                    binding = 1,
                    descriptorType = VkDescriptorType.UniformBuffer,
                    descriptorCount = 1,
                    stageFlags = VkShaderStageFlags.Fragment
                },
                // 片段着色器的 CombinedImageSampler (binding=2)
                new VkDescriptorSetLayoutBinding {
                    binding = 2,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    descriptorCount = 1,
                    stageFlags = VkShaderStageFlags.Fragment
                }
            });

            drawDescriptorSet = Device.CreateDescriptorSet(drawDescriptorLayout, drawdescriptorPool);

            vertexUBO = Device.CreateBuffer((ulong)sizeof(drawvertUBO),VkBufferUsageFlags.UniformBuffer);

            fragmentUBO = Device.CreateBuffer((ulong)sizeof(drawfragUBO), VkBufferUsageFlags.UniformBuffer);

            // 顶点着色器的 UniformBuffer (binding=0)
            var vertBufferInfo = new VkDescriptorBufferInfo
            {
                buffer = vertexUBO.stagingBuffer,
                offset = 0,
                range = (ulong)Marshal.SizeOf<drawvertUBO>()
            };

            // 片段着色器的 CombinedImageSampler (binding=1)
            var imageInfo = new VkDescriptorImageInfo
            {
                sampler = drawTexture.sampler,
                imageView = drawTexture.imageview,
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal
            };

            // 片段着色器的 UniformBuffer (binding=2)
            var fragBufferInfo = new VkDescriptorBufferInfo
            {
                buffer = fragmentUBO.stagingBuffer,
                offset = 0,
                range = (ulong)Marshal.SizeOf<drawfragUBO>()
            };

            Device.UpdateDescriptorSets(
                 new[]{
                    // 更新顶点着色器的 UniformBuffer (binding=0)
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = drawDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.UniformBuffer,
                        pBufferInfo = &vertBufferInfo
                    },
                    // 更新片段着色器的 UniformBuffer (binding=1)
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = drawDescriptorSet,
                        dstBinding = 1,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.UniformBuffer,
                        pBufferInfo =  &fragBufferInfo
                    },
                    // 更新片段着色器的 CombinedImageSampler (binding=2)
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = drawDescriptorSet,
                        dstBinding = 2,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&imageInfo
                    }
                 },
                 drawDescriptorSet
            );

            var drawVert = Device.LoadShaderFile("./Shaders/draw.vert.spv");
            var drawFrag = Device.LoadShaderFile("./Shaders/draw.frag.spv");

            // 创建主绘制管线（无混合）
            drawPipelineWithoutBlending = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVert,
                drawFrag,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6
            );

            // 创建加法混合管线
            drawPipelineWithAddBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVert,
                drawFrag,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6,
                true,
                VkBlendOp.Add
            );

            // 创建减法混合管线
            drawPipelineWithSubtractBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVert,
                drawFrag,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6,
                true,
                VkBlendOp.Subtract
            );

            VkVertexInputBindingDescription postBinding = new()
            {
                binding = 0,
                stride = (uint)Marshal.SizeOf<Vector2>(),
                inputRate = VkVertexInputRate.Vertex
            };

            VkVertexInputAttributeDescription[] postAttributes = new VkVertexInputAttributeDescription[1]
            {
                new() { location = 0, binding = 0, format = VkFormat.R32g32Sfloat, offset = 0 }
            };

            descriptorPool = Device.CreateDescriptorPool(
                maxSets: 2,
                new[]
                {
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.CombinedImageSampler,
                        descriptorCount = 1
                    },
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.UniformBuffer,
                        descriptorCount = 1
                    }
                }
            );

            out214DescriptorLayout = Device.CreateDescriptorSetLayout(new[]
            {
                new VkDescriptorSetLayoutBinding { // CombinedImageSampler (binding=0)
                    binding = 0,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    stageFlags = VkShaderStageFlags.Fragment
                },
                new VkDescriptorSetLayoutBinding { // UniformBuffer (binding=2)
                    binding = 2,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.UniformBuffer,
                    stageFlags = VkShaderStageFlags.Fragment
                }
            });

            displayDescriptorSet = Device.CreateDescriptorSet(out214DescriptorLayout, descriptorPool);


            out24UBO = Device.CreateBuffer((ulong)sizeof(outUBO), VkBufferUsageFlags.UniformBuffer);

            var outfragBufferInfo = new VkDescriptorBufferInfo
            {
                buffer = out24UBO.stagingBuffer,
                offset = 0,
                range = (ulong)Marshal.SizeOf<outUBO>()
            };

            Device.UpdateDescriptorSets(
                 new[]{
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = displayDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&imageInfo
                    },
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = displayDescriptorSet,
                        dstBinding = 2,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.UniformBuffer,
                        pBufferInfo =  &outfragBufferInfo
                    }
                 },
                 displayDescriptorSet
            );

            // 创建24位色输出管线
            var out24Vert = Device.LoadShaderFile("./Shaders/out24.vert.spv");
            var out24Frag = Device.LoadShaderFile("./Shaders/out24.frag.spv");
            out24Pipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                out214DescriptorLayout,
                out24Vert,
                out24Frag,
                1024, 512,
                VkSampleCountFlags.Count1,
                postBinding,
                postAttributes,
                1,
                false,
                VkBlendOp.Add
            );

            var out16Vert = Device.LoadShaderFile("./Shaders/out16.vert.spv");
            var out16Frag = Device.LoadShaderFile("./Shaders/out16.frag.spv");

            out16Pipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                out214DescriptorLayout,
                out16Vert,
                out16Frag,
                1024, 512,
                VkSampleCountFlags.Count1,
                postBinding,
                postAttributes,
                1,
                enableBlending: false,
                rgbEquation: VkBlendOp.Add
            );

            out16DescriptorLayout = Device.CreateDescriptorSetLayout(new[]
{
                new VkDescriptorSetLayoutBinding {
                    binding = 0,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    stageFlags = VkShaderStageFlags.Fragment
                }
            });

            blankdescriptorPool = Device.CreateDescriptorPool(
                maxSets: 1,
                new[]
                {
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.CombinedImageSampler,
                        descriptorCount = 1
                    },
                }
            );

            blankDescriptorLayout = Device.CreateDescriptorSetLayout(new[]
            {
                new VkDescriptorSetLayoutBinding { // CombinedImageSampler (binding=0)
                    binding = 0,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    stageFlags = VkShaderStageFlags.Fragment
                }
            });

            blankDescriptorSet = Device.CreateDescriptorSet(blankDescriptorLayout, blankdescriptorPool);

            var displayInfo = new VkDescriptorImageInfo
            {
                sampler = drawTexture.sampler,
                imageView = drawTexture.imageview,
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal
            };

            Device.UpdateDescriptorSets(
                 new[]{
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = blankDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&displayInfo
                    }
                 },
                 blankDescriptorSet
            );

            var ramViewVert = Device.LoadShaderFile("./Shaders/display.vert.spv");
            var ramViewFrag = Device.LoadShaderFile("./Shaders/display.frag.spv");

            ramViewPipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                blankDescriptorLayout,
                ramViewVert,
                ramViewFrag,
                1024, 512,
                VkSampleCountFlags.Count1
            );

            var displayVert = Device.LoadShaderFile("./Shaders/display.vert.spv");
            var displayFrag = Device.LoadShaderFile("./Shaders/display.frag.spv");

            displayPipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                blankDescriptorLayout,
                displayVert,
                displayFrag,
                1024, 512,
                VkSampleCountFlags.Count1
            );

            // 初始化同步对象
            imageAvailableSemaphore = Device.CreateSemaphore();
            renderFinishedSemaphore = Device.CreateSemaphore();
            tempCmd = Device.CreateCommandBuffers(3);

            // 配置视口和裁剪区域
            viewport = new VkViewport { width = 1024, height = 512, maxDepth = 1.0f };
            scissor = new VkRect2D { extent = new VkExtent2D(1024, 512) };

            RestoreRenderState();

            Console.WriteLine("[Vulkan GPU] Initialization Complete.");
        }

        public unsafe void Dispose()
        {

            if (isDisposed)
                return;

            Device.DestoryBuffer(stagingBuffer);

            Device.VulkanDispose();

            Marshal.FreeHGlobal((IntPtr)VRAM);

            Console.WriteLine($"[OpenGL GPU] Disposed");

            isDisposed = true;
        }

        public unsafe void CreateFramebuffers(vkSwapchain swapchain, VkRenderPass renderPass, vkTexture depthTexture)
        {
            foreach (var framebuffer in framebuffers)
            {
                Device.DestroyFramebuffer(framebuffer);
            }
            framebuffers.Clear();

            foreach (var imageView in swapchain.ImageViews)
            {
                var framebuffer = Device.CreateFramebuffer(
                    renderPass,
                    imageView,
                    depthTexture.imageview,
                    (uint)swapchain.Extent.width,
                    (uint)swapchain.Extent.height
                );
                framebuffers.Add(framebuffer);
            }
        }

        private unsafe void CreateDepthResources(int width, int height)
        {
            VkFormat depthFormat = Device.FindDepthFormat();

            depthTexture = Device.CreateTexture(width, height, depthFormat, VkImageAspectFlags.Depth);
        }

        private void UpdateViewportAndScissor()
        {
            viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = VRAM_WIDTH * resolutionScale,
                height = VRAM_HEIGHT * resolutionScale,
                minDepth = 0,
                maxDepth = 1
            };

            scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = new VkExtent2D { width = (uint)(VRAM_WIDTH * resolutionScale), height = (uint)(VRAM_HEIGHT * resolutionScale) }
            };
        }

        private unsafe void CleanupResources()
        {
            foreach (var framebuffer in framebuffers)
            {
                vkDestroyFramebuffer(Device.device, framebuffer, null);
            }
            framebuffers.Clear();

            if (depthTexture.image != VkImage.Null)
            {
                Device.DestroyTexture(depthTexture);
                depthTexture.image = VkImage.Null;
            }

            Device.CleanupSwapChain(drawChain);
        }

        public unsafe void SetResolutionScale(int scale)
        {
            if (scale < 1 || scale > 9)
            {
                return;
            }

            if (scale == resolutionScale)
            {
                return;
            }

            int newWidth = VRAM_WIDTH * scale;
            int newHeight = VRAM_HEIGHT * scale;

            VkPhysicalDeviceProperties deviceProperties = new VkPhysicalDeviceProperties();
            vkGetPhysicalDeviceProperties(Device.physicalDevice, out deviceProperties);
            uint maxTextureSize = deviceProperties.limits.maxImageDimension2D;
            if (newWidth > maxTextureSize || newHeight > maxTextureSize)
            {
                Console.WriteLine($"[VULKAN GPU] New resolution ({newWidth}x{newHeight}) exceeds maximum texture size ({maxTextureSize})");
                return;
            }

            int oldWidth = VRAM_WIDTH * resolutionScale;
            int oldHeight = VRAM_HEIGHT * resolutionScale;

            var newDrawTexture = Device.CreateTexture(newWidth, newHeight, VkFormat.R8g8b8a8Unorm);
            var newDisplayTexture = Device.CreateTexture(newWidth, newHeight, VkFormat.R8g8b8a8Unorm);

            if (drawTexture.image != VkImage.Null && displayTexture.image != VkImage.Null)
            {
                CopyTexture(drawTexture, newDrawTexture, oldWidth, oldHeight, newWidth, newHeight);
                CopyTexture(displayTexture, newDisplayTexture, oldWidth, oldHeight, newWidth, newHeight);
            }

            resolutionScale = scale;

            CleanupResources();

            drawTexture = newDrawTexture;
            displayTexture = newDisplayTexture;

            drawChain = Device.CreateSwapChain(newWidth, newHeight);
            CreateDepthResources(newWidth, newHeight);
            CreateFramebuffers(drawChain, renderPass, depthTexture);

            imageAvailableSemaphore = Device.CreateSemaphore();
            renderFinishedSemaphore = Device.CreateSemaphore();

            UpdateViewportAndScissor();

            Console.WriteLine($"[VULKAN GPU] Resolution scale updated to {scale}x ({newWidth}x{newHeight})");
        }

        public void SetParams(int[] Params)
        {
        }

        public void SetRam(byte[] Ram)
        {
        }

        public byte[] GetRam()
        {
            return null;
        }

        public void SetFrameBuff(byte[] FrameBuffer)
        {
        }

        public byte[] GetFrameBuff()
        {
            return null;
        }

        public VRAMTransfer GetVRAMTransfer()
        {
            return _VRAMTransfer;
        }

        public unsafe (int w, int h) GetPixels(bool is24bit, int DisplayVerticalStart, int DisplayVerticalEnd, int rx, int ry, int w, int h, int[] Pixels)
        {

            int offsetline = ((DisplayVerticalEnd - DisplayVerticalStart)) >> (h == 480 ? 0 : 1);
            if (offsetline < 0)
                return (0, -1);

            m_vramDisplayArea = new DisplayArea
            {
                x = rx,
                y = ry,
                width = w,
                height = offsetline * 2
            };

            m_targetDisplayArea = new DisplayArea
            {
                x = 0,
                y = 0,
                width = w,
                height = offsetline * 2
            };

            DrawBatch();

            RestoreRenderState();

            int targetWidth = ViewVRam ?
                VRAM_WIDTH * resolutionScale :
                m_targetDisplayArea.width * resolutionScale;

            int targetHeight = ViewVRam ?
                VRAM_HEIGHT * resolutionScale :
                m_targetDisplayArea.height * resolutionScale;

            if (targetWidth != displayTexture.width || targetHeight != displayTexture.height)
            {
                Device.DestroyTexture(displayTexture);

                vkDestroyImageView(Device.device, displayimageview2, null);

                vkDestroyFramebuffer(Device.device, displayFrameBuff, null);

                displayTexture = Device.CreateTexture(
                    targetWidth,
                    targetHeight,
                    VkFormat.R8g8b8a8Unorm
                );

                displayimageview2 = Device.CreateImageView(displayTexture.image, VkFormat.R8g8b8a8Unorm);

                displayFrameBuff = Device.CreateFramebuffer(renderPass, displayTexture.imageview, displayimageview2, (uint)targetWidth, (uint)targetHeight);
            }

            BeginRenderPass();

            currentPipeline = is24bit ? out24Pipeline : out16Pipeline;
            vkCmdBindPipeline(tempCmd.CMD[0], VkPipelineBindPoint.Graphics, currentPipeline.pipeline);

            UpdateViewportAndScissor(targetWidth, targetHeight);

            vkCmdDraw(tempCmd.CMD[0], 4, 1, 0, 0);

            EndRenderPass();

            PresentToSwapchain();

            if (RealColor != m_realColor)
            {
                m_realColor = RealColor;
            }
            if (IRScale != resolutionScale)
            {
                SetResolutionScale(IRScale);
                resolutionScale = IRScale;
            }

            return (targetWidth, targetHeight);
        }

        private unsafe void BeginRenderPass()
        {
            VkClearValue clearValue = new VkClearValue { color = new VkClearColorValue(0.0f, 0.0f, 0.0f, 1.0f) };

            vkFixedArray2<VkClearValue> clears;
            clears.First = clearValue;
            clears.Second = clearValue;

            var beginInfo = new VkRenderPassBeginInfo
            {
                sType = VkStructureType.RenderPassBeginInfo,
                renderPass = renderPass,
                framebuffer = displayFrameBuff,
                renderArea = new VkRect2D
                {
                    offset = new VkOffset2D(0, 0),
                    extent = new VkExtent2D((uint)displayTexture.width, (uint)displayTexture.height)
                },
                clearValueCount = 2,
                pClearValues = &clears.First
            };

            Device.BeginCommandBuffer(tempCmd.CMD[0]);

            vkCmdBeginRenderPass(tempCmd.CMD[0], &beginInfo, VkSubpassContents.Inline);
        }

        private unsafe void EndRenderPass()
        {
            vkCmdEndRenderPass(tempCmd.CMD[0]);

            Device.EndCommandBuffer(tempCmd.CMD[0]);

            VkCommandBuffer cmd = tempCmd.CMD[0];
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &cmd
            };

            VkResult result = vkQueueSubmit(Device.graphicsQueue, 1, &submitInfo, VkFence.Null);
            if (result != VkResult.Success)
            {
                throw new Exception($"Failed to submit command buffer: {result}");
            }
        }

        private unsafe void PresentToSwapchain()
        {
            uint imageIndex;
            VkResult result = vkAcquireNextImageKHR(
                Device.device,
                drawChain.Chain,
                ulong.MaxValue,
                imageAvailableSemaphore,
                VkFence.Null,
                &imageIndex
            );

            if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
            {
                RecreateSwapChain();
                return;
            } else if (result != VkResult.Success)
            {
                throw new Exception($"Failed to acquire swap chain image: {result}");
            }

            var rfs = renderFinishedSemaphore;
            VkSwapchainKHR chain = drawChain.Chain;
            var presentInfo = new VkPresentInfoKHR
            {
                sType = VkStructureType.PresentInfoKHR,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &rfs,
                swapchainCount = 1,
                pSwapchains = &chain,
                pImageIndices = &imageIndex
            };

            result = vkQueuePresentKHR(Device.presentQueue, &presentInfo);
            if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
            {
                RecreateSwapChain();
            } else if (result != VkResult.Success)
            {
                throw new Exception($"Failed to present swap chain image: {result}");
            }
        }

        private unsafe void RecreateSwapChain()
        {
            vkDeviceWaitIdle(Device.device);

            Device.CleanupSwapChain(drawChain);
            drawChain = Device.CreateSwapChain(NullRenderer.ClientWidth, NullRenderer.ClientHeight);
        }

        private unsafe void UpdateViewportAndScissor(int width, int height)
        {
            VkViewport viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = width,
                height = height,
                minDepth = 0.0f,
                maxDepth = 1.0f
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D(0, 0),
                extent = new VkExtent2D((uint)width, (uint)height)
            };

            vkCmdSetViewport(tempCmd.CMD[0], 0, 1, ref viewport);
            vkCmdSetScissor(tempCmd.CMD[0], 0, 1, ref scissor);
        }

        private unsafe void ClearTexture(vkTexture texture, float r, float g, float b, float a)
        {
            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            VkClearColorValue clearColor = new VkClearColorValue(r, g, b, a);

            Device.TransitionImageLayout(
                commandBuffer,
                texture.image,
                VkImageLayout.Undefined,
                VkImageLayout.TransferDstOptimal
            );

            Device.CmdClearColorImage(
                commandBuffer,
                texture.image,
                clearColor
            );

            Device.TransitionImageLayout(
                commandBuffer,
                texture.image,
                VkImageLayout.TransferDstOptimal,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;

            if (_VRAMTransfer.isRead)
            {
                CopyRectVRAMtoCPU(_VRAMTransfer.OriginX, _VRAMTransfer.OriginY, _VRAMTransfer.W, _VRAMTransfer.H);
            }
        }

        public void SetMaskBit(uint value)
        {
            if (oldmaskbit != value)
            {
                oldmaskbit = value;
                DrawBatch();

                ForceSetMaskBit = ((value & 1) != 0);
                CheckMaskBit = (((value >> 1) & 1) != 0);
            }
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            if (DrawingAreaTopLeft != value)
            {
                DrawBatch();

                DrawingAreaTopLeft = value;
            }
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            if (DrawingAreaBottomRight != value)
            {
                DrawBatch();

                DrawingAreaBottomRight = value;
            }
        }

        public void SetDrawingOffset(TDrawingOffset value)
        {
            DrawingOffset = value;
        }

        public void SetTextureWindow(uint value)
        {
            value &= 0xfffff;

            if (oldtexwin != value)
            {
                oldtexwin = value;

                DrawBatch();

                TextureWindowXMask = (int)(value & 0x1f);
                TextureWindowYMask = (int)((value >> 5) & 0x1f);

                TextureWindowXOffset = (int)((value >> 10) & 0x1f);
                TextureWindowYOffset = (int)((value >> 15) & 0x1f);
            }
        }

        public unsafe void FillRectVRAM(ushort left, ushort top, ushort width, ushort height, uint colorval)
        {
            var bounds = GetWrappedBounds(left, top, width, height);
            GrowDirtyArea(bounds);

            Vertex[] vertices = new Vertex[4];

            vertices[0].v_pos.x = (short)left;
            vertices[0].v_pos.y = (short)top;

            vertices[1].v_pos.x = (short)width;
            vertices[1].v_pos.y = (short)top;

            vertices[2].v_pos.x = (short)left;
            vertices[2].v_pos.y = (short)height;

            vertices[3].v_pos.x = (short)width;
            vertices[3].v_pos.y = (short)height;

            vertices[0].v_color.Value = colorval;
            vertices[1].v_color.Value = colorval;
            vertices[2].v_color.Value = colorval;
            vertices[3].v_color.Value = colorval;

            if (Vertexs.Count + 6 > 1024)
                DrawBatch();

            ushort texpage = (ushort)(0 | (1 << 11));

            SetDrawMode(texpage, 0, false);

            EnableSemiTransparency(false);

            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i].v_clut.Value = 0;
                vertices[i].v_texPage.Value = texpage;
                vertices[i].v_pos.z = m_currentDepth;
            }

            Vertexs.Add(vertices[0]);
            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);

            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);
            Vertexs.Add(vertices[3]);

            DrawBatch();
        }

        public unsafe void CopyRectVRAMtoVRAM(ushort srcX, ushort srcY, ushort destX, ushort destY, ushort width, ushort height)
        {
            var srcBounds = glRectangle<int>.FromExtents(srcX, srcY, width, height);
            var destBounds = glRectangle<int>.FromExtents(destX, destY, width, height);

            if (m_dirtyArea.Intersects(srcBounds))
            {
                UpdateReadTexture();
                m_dirtyArea.Grow(destBounds);
            } else
            {
                GrowDirtyArea(destBounds);
            }

            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            Device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

            var copyRegion = new VkImageBlit
            {
                srcSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                srcOffsets_0 = new VkOffset3D { x = srcX * resolutionScale, y = srcY * resolutionScale, z = 0 },
                srcOffsets_1 = new VkOffset3D { x = (srcX + width) * resolutionScale, y = (srcY + height) * resolutionScale, z = 1 },

                dstSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                dstOffsets_0 = new VkOffset3D { x = destX * resolutionScale, y = destY * resolutionScale, z = 0 },
                dstOffsets_1 = new VkOffset3D { x = (destX + width) * resolutionScale, y = (destY + height) * resolutionScale, z = 1 }
            };

            vkCmdBlitImage(
                commandBuffer,
                drawTexture.image,
                VkImageLayout.TransferSrcOptimal,
                drawTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &copyRegion,
                VkFilter.Nearest
            );

            Device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

        public unsafe void CopyRectVRAMtoCPU(int left, int top, int width, int height)
        {
            var readBounds = GetWrappedBounds(left, top, width, height);

            if (m_dirtyArea.Intersects(readBounds))
            {
                DrawBatch();
            }

            int readWidth = readBounds.GetWidth();
            int readHeight = readBounds.GetHeight();

            if (resolutionScale > 1)
            {
                WriteBackDrawTextureToVRAM();
            }

            VkImage sourceTexture = resolutionScale == 1 ? drawTexture.image : vramTexture.image;

            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            Device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferSrcOptimal);

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
                imageOffset = new VkOffset3D { x = readBounds.Left, y = readBounds.Top, z = 0 },
                imageExtent = new VkExtent3D { width = (uint)readWidth, height = (uint)readHeight, depth = 1 }
            };

            vkCmdCopyImageToBuffer(
                commandBuffer,
                sourceTexture,
                VkImageLayout.TransferSrcOptimal,
                stagingBuffer.stagingBuffer,
                1,
                &copyRegion
            );

            Device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.TransferSrcOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);

            Buffer.MemoryCopy(stagingBuffer.mappedData, VRAM + (readBounds.Left + readBounds.Top * VRAM_WIDTH) * 2, 0, readWidth * readHeight * 2);
        }

        public unsafe void CopyRectCPUtoVRAM(int left, int top, int width, int height)
        {
            var updateBounds = GetWrappedBounds(left, top, width, height);
            GrowDirtyArea(updateBounds);

            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            Device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferDstOptimal);

            int scaledLeft = left * resolutionScale;
            int scaledTop = top * resolutionScale;
            int scaledWidth = width * resolutionScale;
            int scaledHeight = height * resolutionScale;

            int bytesPerPixel = 2; // 每像素 2 字节（R5G5B5A1 格式）
            int bufferSize = width * height * bytesPerPixel;

            Buffer.MemoryCopy(
                VRAM + (updateBounds.Left + updateBounds.Top * VRAM_WIDTH) * bytesPerPixel,
                stagingBuffer.mappedData,
                0,
                bufferSize
            );

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
                imageOffset = new VkOffset3D { x = scaledLeft, y = scaledTop, z = 0 },
                imageExtent = new VkExtent3D { width = (uint)scaledWidth, height = (uint)scaledHeight, depth = 1 }
            };

            vkCmdCopyBufferToImage(
                commandBuffer,
                stagingBuffer.stagingBuffer,
                drawTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &copyRegion
            );

            Device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

        public unsafe uint ReadFromVRAM()
        {
            ushort Data0 = *(ushort*)(VRAM + _VRAMTransfer.X + _VRAMTransfer.Y * VRAM_WIDTH);
            _VRAMTransfer.X++;
            ushort Data1 = *(ushort*)(VRAM + _VRAMTransfer.X + _VRAMTransfer.Y * VRAM_WIDTH);
            _VRAMTransfer.X++;

            if (_VRAMTransfer.X == _VRAMTransfer.OriginX + _VRAMTransfer.W)
            {
                _VRAMTransfer.X -= _VRAMTransfer.W;
                _VRAMTransfer.Y++;
            }

            return (uint)((Data1 << 16) | Data0);
        }

        public unsafe void WriteToVRAM(ushort value)
        {
            *(ushort*)(VRAM + _VRAMTransfer.X + _VRAMTransfer.Y * _VRAMTransfer.W) = value;

            _VRAMTransfer.X++;

            if (_VRAMTransfer.X != _VRAMTransfer.OriginX + _VRAMTransfer.W)
                return;

            _VRAMTransfer.X -= _VRAMTransfer.W;
            _VRAMTransfer.Y++;
        }

        public void WriteDone()
        {
            CopyRectCPUtoVRAM(_VRAMTransfer.OriginX, _VRAMTransfer.OriginY, _VRAMTransfer.W, _VRAMTransfer.H);
        }

        private void SetDrawMode(ushort vtexPage, ushort vclut, bool dither)
        {
            if (m_realColor)
                dither = false;

            if (m_dither != dither)
            {
                DrawBatch();

                m_dither = dither;
            }

            if (m_TexPage.Value != vtexPage)
            {
                DrawBatch();

                m_TexPage.Value = vtexPage;

                SetSemiTransparencyMode(m_TexPage.SemiTransparencymode);

                if (!m_TexPage.TextureDisable)
                {
                    int texBaseX = m_TexPage.TexturePageBaseX * TexturePageBaseXMult;
                    int texBaseY = m_TexPage.TexturePageBaseY * TexturePageBaseYMult;
                    int texSize = ColorModeTexturePageWidths[m_TexPage.TexturePageColors];
                    m_textureArea = glRectangle<int>.FromExtents(texBaseX, texBaseY, texSize, texSize);

                    if (m_TexPage.TexturePageColors < 2)
                        UpdateClut(vclut);
                }
            } else if (m_clut.Value != vclut && !m_TexPage.TextureDisable && m_TexPage.TexturePageColors < 2)
            {
                DrawBatch();

                UpdateClut(vclut);
            }

            if (IntersectsTextureData(m_dirtyArea))
                UpdateReadTexture();
        }

        public unsafe void DrawBatch()
        {
            if (Vertexs.Count == 0)
                return;

            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            Device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferDstOptimal);

            int vertexBufferSize = Vertexs.Count * sizeof(Vertex);

            fixed (Vertex* vertexData = Vertexs.ToArray())
            {
                Buffer.MemoryCopy(
                    vertexData,
                    stagingBuffer.mappedData,
                    0,
                    vertexBufferSize
                );
            }

            Device.BindGraphicsPipeline(commandBuffer, currentPipeline);

            VkViewport viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = GetVRamTextureWidth(),
                height = GetVRamTextureHeight(),
                minDepth = 0,
                maxDepth = 1
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = new VkExtent2D { width = (uint)GetVRamTextureWidth(), height = (uint)GetVRamTextureHeight() }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

            VkBuffer vertexBuffer = stagingBuffer.stagingBuffer;
            ulong offset = 0;
            vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);

            if (m_semiTransparencyEnabled && (m_semiTransparencyMode == 2) && !m_TexPage.TextureDisable)
            {

                int* pushConstants = stackalloc int[2];

                pushConstants[0] = 1; // drawOpaquePixels = 1
                pushConstants[1] = 0; // drawTransparentPixels = 0

                vkCmdPushConstants(
                    commandBuffer,
                    currentPipeline.layout,
                    VkShaderStageFlags.Fragment,
                    0,
                    sizeof(int) * 2,
                    pushConstants
                );
                vkCmdDraw(commandBuffer, (uint)Vertexs.Count, 1, 0, 0);


                pushConstants[0] = 0; // drawOpaquePixels = 0
                pushConstants[1] = 1; // drawTransparentPixels = 1

                vkCmdPushConstants(
                    commandBuffer,
                    currentPipeline.layout,
                    VkShaderStageFlags.Fragment,
                    0,
                    sizeof(int) * 2,
                    pushConstants
                );
                vkCmdDraw(commandBuffer, (uint)Vertexs.Count, 1, 0, 0);
            } else
            {

                int* pushConstants = stackalloc int[2];

                pushConstants[0] = 1; // drawOpaquePixels = 1
                pushConstants[1] = 1; // drawTransparentPixels = 1

                vkCmdPushConstants(
                    commandBuffer,
                    currentPipeline.layout,
                    VkShaderStageFlags.Fragment,
                    0,
                    sizeof(int) * 2,
                    pushConstants
                );
                vkCmdDraw(commandBuffer, (uint)Vertexs.Count, 1, 0, 0);
            }

            Device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);

            Vertexs.Clear();

            UpdateCurrentDepth();
        }

        public void DrawLineBatch(bool isDithered, bool SemiTransparency)
        {
            glTexPage tp = new glTexPage();
            tp.TextureDisable = true;
            SetDrawMode(tp.Value, 0, isDithered);

            EnableSemiTransparency(SemiTransparency);

            UpdateCurrentDepth();
        }

        public void DrawLine(uint v1, uint v2, uint c1, uint c2, bool isTransparent, int SemiTransparency)
        {
            if (!IsDrawAreaValid())
                return;

            Vertex[] vertices = new Vertex[4];

            glPosition p1 = new glPosition();
            p1.x = (short)v1;
            p1.y = (short)(v1 >> 16);

            glPosition p2 = new glPosition();
            p2.x = (short)v2;
            p2.y = (short)(v2 >> 16);

            int dx = p2.x - p1.x;
            int dy = p2.y - p1.y;

            int absDx = Math.Abs(dx);
            int absDy = Math.Abs(dy);

            // 剔除过长的线段
            if (absDx > 1023 || absDy > 511)
                return;

            p1.x += DrawingOffset.X;
            p1.y += DrawingOffset.Y;
            p2.x += DrawingOffset.X;
            p2.y += DrawingOffset.Y;

            if (dx == 0 && dy == 0)
            {
                // 渲染一个点，使用第一个颜色
                vertices[0].v_pos = p1;
                vertices[1].v_pos = new glPosition((short)(p1.x + 1), p1.y);
                vertices[2].v_pos = new glPosition(p1.x, (short)(p1.y + 1));
                vertices[3].v_pos = new glPosition((short)(p1.x + 1), (short)(p1.y + 1));

                vertices[0].v_color.Value = c1;
                vertices[1].v_color.Value = c1;
                vertices[2].v_color.Value = c1;
                vertices[3].v_color.Value = c1;
            } else
            {
                short padX1 = 0;
                short padY1 = 0;
                short padX2 = 0;
                short padY2 = 0;

                short fillDx = 0;
                short fillDy = 0;

                // 根据线段的方向调整两端
                if (absDx > absDy)
                {
                    fillDx = 0;
                    fillDy = 1;

                    if (dx > 0)
                    {
                        // 从左到右
                        padX2 = 1;
                    } else
                    {
                        // 从右到左
                        padX1 = 1;
                    }
                } else
                {
                    fillDx = 1;
                    fillDy = 0;

                    if (dy > 0)
                    {
                        // 从上到下
                        padY2 = 1;
                    } else
                    {
                        // 从下到上
                        padY1 = 1;
                    }
                }

                short x1 = (short)(p1.x + padX1);
                short y1 = (short)(p1.y + padY1);
                short x2 = (short)(p2.x + padX2);
                short y2 = (short)(p2.y + padY2);

                vertices[0].v_pos = new glPosition(x1, y1);
                vertices[1].v_pos = new glPosition((short)(x1 + fillDx), (short)(y1 + fillDy));
                vertices[2].v_pos = new glPosition(x2, y2);
                vertices[3].v_pos = new glPosition((short)(x2 + fillDx), (short)(y2 + fillDy));

                vertices[0].v_color.Value = c1;
                vertices[1].v_color.Value = c1;
                vertices[2].v_color.Value = c2;
                vertices[3].v_color.Value = c2;
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                m_dirtyArea.Grow(vertices[i].v_pos.x, vertices[i].v_pos.y);

                vertices[i].v_clut.Value = 0;
                vertices[i].v_texPage.TextureDisable = true;
                vertices[i].v_pos.z = m_currentDepth;
            }

            if (Vertexs.Count + 6 > 1024)
                DrawBatch();

            Vertexs.Add(vertices[0]);
            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);

            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);
            Vertexs.Add(vertices[3]);
        }

        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
            if (primitive.IsTextured && primitive.IsRawTextured)
            {
                bgrColor = 0x808080;
            }
            if (!primitive.IsTextured)
            {
                primitive.texpage = (ushort)(primitive.texpage | (1 << 11));
            }

            SetDrawMode(primitive.texpage, primitive.clut, false);

            if (!IsDrawAreaValid())
                return;

            // 组装顶点数据（两个三角形：v0-v1-v2 和 v1-v2-v3）
            Vertex[] vertices = new Vertex[4];

            vertices[0].v_pos.x = origin.X;
            vertices[0].v_pos.y = origin.Y;

            vertices[1].v_pos.x = size.X;
            vertices[1].v_pos.y = origin.Y;

            vertices[2].v_pos.x = origin.X;
            vertices[2].v_pos.y = size.Y;

            vertices[3].v_pos.x = size.X;
            vertices[3].v_pos.y = size.Y;

            vertices[0].v_color.Value = bgrColor;
            vertices[1].v_color.Value = bgrColor;
            vertices[2].v_color.Value = bgrColor;
            vertices[3].v_color.Value = bgrColor;

            if (primitive.IsTextured)
            {
                short u1, u2, v1, v2;

                if (primitive.drawMode.TexturedRectangleXFlip)
                {
                    u1 = texture.X;
                    u2 = (short)(u1 - primitive.texwidth);
                } else
                {
                    u1 = texture.X;
                    u2 = (short)(u1 + primitive.texwidth);
                }

                if (primitive.drawMode.TexturedRectangleYFlip)
                {
                    v1 = texture.Y;
                    v2 = (short)(v1 - primitive.texheight);
                } else
                {
                    v1 = texture.Y;
                    v2 = (short)(v1 + primitive.texheight);
                }

                vertices[0].v_texCoord.u = u1;
                vertices[0].v_texCoord.v = v1;

                vertices[1].v_texCoord.u = u2;
                vertices[1].v_texCoord.v = v1;

                vertices[2].v_texCoord.u = u1;
                vertices[2].v_texCoord.v = v2;

                vertices[3].v_texCoord.u = u2;
                vertices[3].v_texCoord.v = v2;
            }

            if (Vertexs.Count + 6 > 1024)
                DrawBatch();

            EnableSemiTransparency(primitive.IsSemiTransparent);

            UpdateCurrentDepth();

            for (var i = 0; i < vertices.Length; i++)
            {
                m_dirtyArea.Grow(vertices[i].v_pos.x, vertices[i].v_pos.y);

                vertices[i].v_clut.Value = primitive.clut;
                vertices[i].v_texPage.Value = primitive.texpage;
                vertices[i].v_pos.z = m_currentDepth;
            }

            Vertexs.Add(vertices[0]);
            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);

            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);
            Vertexs.Add(vertices[3]);
        }

        public void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
        {

            int minX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
            int minY = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
            int maxX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
            int maxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

            if (maxX - minX > 1024 || maxY - minY > 512)
                return;

            if (!primitive.IsTextured)
            {
                primitive.texpage = (ushort)(primitive.texpage | (1 << 11));
            }

            SetDrawMode(primitive.texpage, primitive.clut, primitive.isDithered);

            if (!IsDrawAreaValid())
                return;

            if (primitive.IsTextured && primitive.IsRawTextured)
            {
                c0 = c1 = c2 = 0x808080;
            } else if (!primitive.IsShaded)
            {
                c1 = c2 = c0;
            }

            Vertex[] vertices = new Vertex[3];

            vertices[0].v_pos.x = v0.X;
            vertices[0].v_pos.y = v0.Y;

            vertices[1].v_pos.x = v1.X;
            vertices[1].v_pos.y = v1.Y;

            vertices[2].v_pos.x = v2.X;
            vertices[2].v_pos.y = v2.Y;

            vertices[0].v_texCoord.u = t0.X;
            vertices[0].v_texCoord.v = t0.Y;

            vertices[1].v_texCoord.u = t1.X;
            vertices[1].v_texCoord.v = t1.Y;

            vertices[2].v_texCoord.u = t2.X;
            vertices[2].v_texCoord.v = t2.Y;

            vertices[0].v_color.Value = c0;
            vertices[1].v_color.Value = c1;
            vertices[2].v_color.Value = c2;

            if (Vertexs.Count + 3 > 1024)
                DrawBatch();

            EnableSemiTransparency(primitive.IsSemiTransparent);

            UpdateCurrentDepth();

            for (var i = 0; i < vertices.Length; i++)
            {
                m_dirtyArea.Grow(vertices[i].v_pos.x, vertices[i].v_pos.y);

                vertices[i].v_clut.Value = primitive.clut;
                vertices[i].v_texPage.Value = primitive.texpage;
                vertices[i].v_pos.z = m_currentDepth;
            }

            Vertexs.AddRange(vertices);
        }

        private unsafe void RestoreRenderState()
        {
            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            if (drawPipelineWithoutBlending.pipeline != VkPipeline.Null)
            {
                vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, drawPipelineWithoutBlending.pipeline);
            }

            VkViewport viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = GetVRamTextureWidth(),
                height = GetVRamTextureHeight(),
                minDepth = 0,
                maxDepth = 1
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = new VkExtent2D { width = (uint)GetVRamTextureWidth(), height = (uint)GetVRamTextureHeight() }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

            VkBuffer vertexBuffer = vaoUBO.stagingBuffer;
            ulong offset = 0;
            vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);

            if (m_semiTransparencyEnabled)
            {
                vkCmdSetBlendConstants(commandBuffer, 1.0f);
            }

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

        private void ResetDepthBuffer()
        {
            DrawBatch();

            m_currentDepth = 1;

            RestoreRenderState();
        }

        private void UpdateCurrentDepth()
        {
            if (CheckMaskBit)
            {
                ++m_currentDepth;

                if (m_currentDepth == short.MaxValue)
                    ResetDepthBuffer();
            }
        }

        private int GetVRamTextureWidth()
        {
            return (VRAM_WIDTH * resolutionScale);
        }

        private int GetVRamTextureHeight()
        {
            return (VRAM_HEIGHT * resolutionScale);
        }

        private bool IsDrawAreaValid()
        {
            return DrawingAreaTopLeft.X <= DrawingAreaBottomRight.X && DrawingAreaTopLeft.Y <= DrawingAreaBottomRight.Y;
        }

        private float GetNormalizedDepth()
        {
            return (float)m_currentDepth / (float)short.MaxValue;
        }

        public void SetSemiTransparencyMode(byte semiTransparencyMode)
        {
            if (m_semiTransparencyMode == semiTransparencyMode)
                return;

            if (m_semiTransparencyEnabled || !m_TexPage.TextureDisable)
            {
                DrawBatch();
            }

            m_semiTransparencyMode = semiTransparencyMode;

            if (m_semiTransparencyEnabled)
            {
                UpdateBlendMode();
            }
        }

        private unsafe void UpdateBlendMode()
        {
            VkCommandBuffer commandBuffer;

            if (!m_semiTransparencyEnabled)
            {
                commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

                vkCmdSetBlendConstants(commandBuffer, 0.0f);

                Device.EndSingleCommands(commandBuffer, tempCmd.pool);

                currentPipeline = drawPipelineWithoutBlending;

                return;
            }

            float srcBlend = 1.0f;
            //float destBlend = 1.0f;

            switch (m_semiTransparencyMode)
            {
                case 0: // 半透明模式 0：平均混合
                    srcBlend = 0.5f;
                    //destBlend = 0.5f;
                    currentPipeline = drawPipelineWithAddBlend;
                    break;

                case 1: // 半透明模式 1：加法混合
                    srcBlend = 1.0f;
                    //destBlend = 1.0f;
                    currentPipeline = drawPipelineWithAddBlend;
                    break;

                case 2: // 半透明模式 2：减法混合
                    srcBlend = 1.0f;
                    //destBlend = 1.0f;
                    currentPipeline = drawPipelineWithSubtractBlend;
                    break;

                case 3: // 半透明模式 3：四分之一混合
                    srcBlend = 0.25f;
                    //destBlend = 1.0f;
                    currentPipeline = drawPipelineWithAddBlend;
                    break;

                default:
                    currentPipeline = drawPipelineWithoutBlending;
                    break;
            }

            commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            vkCmdSetBlendConstants(commandBuffer, srcBlend);

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

        private void EnableSemiTransparency(bool enabled)
        {
            if (m_semiTransparencyEnabled != enabled)
            {
                DrawBatch();

                m_semiTransparencyEnabled = enabled;

                UpdateBlendMode();
            }
        }

        private void UpdateReadTexture()
        {
            if (m_dirtyArea.Empty())
                return;

            DrawBatch();

            ResetDirtyArea();
        }

        private void UpdateClut(ushort vclut)
        {
            m_clut.Value = vclut;

            int clutBaseX = m_clut.X * ClutBaseXMult;
            int clutBaseY = m_clut.Y * ClutBaseYMult;
            int clutWidth = ColorModeClutWidths[m_TexPage.TexturePageColors];
            int clutHeight = 1;
            m_clutArea = glRectangle<int>.FromExtents(clutBaseX, clutBaseY, clutWidth, clutHeight);
        }

        private bool IntersectsTextureData(glRectangle<int> bounds)
        {
            return !m_TexPage.TextureDisable &&
                   (m_textureArea.Intersects(bounds) || (m_TexPage.TexturePageColors < 2 && m_clutArea.Intersects(bounds)));
        }

        private glRectangle<int> GetWrappedBounds(int left, int top, int width, int height)
        {
            if (left + width > VRAM_WIDTH)
            {
                left = 0;
                width = VRAM_WIDTH;
            }

            if (top + height > VRAM_HEIGHT)
            {
                top = 0;
                height = VRAM_HEIGHT;
            }

            return glRectangle<int>.FromExtents(left, top, width, height);
        }

        private void ResetDirtyArea()
        {
            m_dirtyArea.Left = VRAM_WIDTH;
            m_dirtyArea.Top = VRAM_HEIGHT;
            m_dirtyArea.Right = 0;
            m_dirtyArea.Bottom = 0;
        }

        private void GrowDirtyArea(glRectangle<int> bounds)
        {
            // 检查 bounds 是否需要覆盖待处理的批处理多边形
            if (m_dirtyArea.Intersects(bounds))
                DrawBatch();

            m_dirtyArea.Grow(bounds);

            // 检查 bounds 是否会覆盖当前的纹理数据
            if (IntersectsTextureData(bounds))
                DrawBatch();
        }

        private unsafe void WriteBackDrawTextureToVRAM()
        {
            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            Device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferSrcOptimal);

            Device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

            var blitRegion = new VkImageBlit
            {
                srcSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                srcOffsets_0 = new VkOffset3D { x = 0, y = 0, z = 0 },
                srcOffsets_1 = new VkOffset3D { x = VRAM_WIDTH * resolutionScale, y = VRAM_HEIGHT * resolutionScale, z = 1 },

                dstSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                dstOffsets_0 = new VkOffset3D { x = 0, y = 0, z = 0 },
                dstOffsets_1 = new VkOffset3D { x = VRAM_WIDTH, y = VRAM_HEIGHT, z = 1 }
            };

            vkCmdBlitImage(
                commandBuffer,
                drawTexture.image,
                VkImageLayout.TransferSrcOptimal,
                vramTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &blitRegion,
                VkFilter.Linear
            );

            Device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

        private unsafe void CopyTexture(vkTexture srcTexture, vkTexture dstTexture, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            VkCommandBuffer commandBuffer = Device.BeginSingleCommands(tempCmd.pool);

            var blitRegion = new VkImageBlit
            {
                srcSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                srcOffsets_0 = new VkOffset3D { x = 0, y = 0, z = 0 },
                srcOffsets_1 = new VkOffset3D { x = srcWidth, y = srcHeight, z = 1 },

                dstSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                dstOffsets_0 = new VkOffset3D { x = 0, y = 0, z = 0 },
                dstOffsets_1 = new VkOffset3D { x = dstWidth, y = dstHeight, z = 1 }
            };

            vkCmdBlitImage(
                commandBuffer,
                srcTexture.image,
                VkImageLayout.TransferSrcOptimal,
                dstTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &blitRegion,
                VkFilter.Linear
            );

            Device.EndSingleCommands(commandBuffer, tempCmd.pool);
        }

    }
}
