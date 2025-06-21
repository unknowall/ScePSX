/*
 * ScePSX Vulkan Backend
 * 
 * github: http://github.com/unknowall/ScePSX
 * 
 * unknowall - sgfree@hotmail.com
 * 
 */

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using ScePSX.Core.GPU;
using ScePSX.Render;

using Vulkan;
using static ScePSX.VulkanDevice;
using static Vulkan.VulkanNative;

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

        uint oldtexwin;
        int setMaskBit;
        short m_currentDepth = 1;

        int resolutionScale = 1;

        vkTexPage m_TexPage;
        vkClutAttribute m_clut;

        vkRectangle<int> m_dirtyArea, m_clutArea, m_textureArea;

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector2Int
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Explicit, Size = 84)]
        public struct Vertex
        {
            [FieldOffset(0)] public vkPosition v_pos;
            [FieldOffset(8)] public Vector3 v_pos_high;
            [FieldOffset(20)] public vkTexCoord v_texCoord;
            [FieldOffset(24)] public vkColor v_color;
            [FieldOffset(36)] public vkClutAttribute v_clut;
            [FieldOffset(40)] public vkTexPage v_texPage;

            [FieldOffset(44)] public float u_srcBlend;
            [FieldOffset(48)] public float u_destBlend;
            [FieldOffset(52)] public int u_setMaskBit;
            [FieldOffset(56)] public int u_drawOpaquePixels;
            [FieldOffset(60)] public int u_drawTransparentPixels;

            [FieldOffset(64)] public Vector2Int u_texWindowMask;
            [FieldOffset(72)] public Vector2Int u_texWindowOffset;

            [FieldOffset(80)] public int BlendMode;
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

        unsafe ushort* VRAM;
        unsafe int* convertedData;
        unsafe byte* drawFragData;
        unsafe void* samplerData, readTextureData;

        VulkanDevice Device;

        VkRenderPass drawPass, renderPass;

        vkSwapchain renderChain;

        vkBuffer VaoBuffer, vertexUBO, fragmentUBO;
        vkTexture samplerTexture, readTexture, drawTexture, depthTexture;
        VkFramebuffer drawFramebuff;
        vkCMDS renderCmd, DrawCMD;

        vkGraphicsPipeline drawMain, drawSubtract, SwapChainPipeline, CurrentPipeline;

        VkDescriptorPool SwapChaindescriptorPool, drawdescriptorPool;
        VkDescriptorSet SwapChainDescriptorSet, drawDescriptorSet;
        VkDescriptorSetLayout SwapChainDescriptorLayout, drawDescriptorLayout;

        class FrameFence
        {
            public VkSemaphore ImageAvailable;
            public VkSemaphore RenderFinished;
            public VkFence InFlightFence;
        }
        FrameFence[] frameFences;

        int frameIndex;

        VkViewport viewport;
        VkRect2D scissor;

        [StructLayout(LayoutKind.Sequential)]
        struct drawvertUBO
        {
            public float u_resolutionScale;
            public int u_pgxp;
            public Matrix4x4 u_mvp;
        }
        drawvertUBO drawVert;

        [StructLayout(LayoutKind.Sequential)]
        struct drawfragUBO
        {
            public int u_dither;
            public int u_realColor;
        }
        drawfragUBO drawFrag;

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        struct SrcRectUBO
        {
            [FieldOffset(0)] public int x;
            [FieldOffset(4)] public int y;
            [FieldOffset(8)] public int w;
            [FieldOffset(12)] public int h;
            [FieldOffset(16)] public int is24bit;

            public SrcRectUBO(int x, int y, int w, int h, int is24bit)
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
                this.is24bit = is24bit;
            }
        }

        private static readonly byte[] LookupTable1555to8888 = new byte[32];

        ulong vaoOffset = 0;
        uint currentBlockFirstVertex;
        VkCommandBuffer CurrentDrawCMD;
        float SrcBlend = 1.0f;
        float DstBlend = 1.0f;
        bool HasTexture;
        bool TransparencyEnabled;
        int BlendMode = 0;

        public struct TextureBlock
        {
            public int x;
            public int y;
            public int width;
            public int height;
        }
        private List<TextureBlock> textureBlocks = new List<TextureBlock>();

        uint minAlignment;
        int alignedRowPitch;
        uint minUboAlignment;

        #endregion

        public bool ViewVRam = false;
        public int IRScale;
        public bool RealColor, PGXP, PGXPT, KEEPAR;
        const float AspectRatio = 4.0f / 3.0f;

        public VulkanGPU()
        {
            Device = new VulkanDevice();

            for (int i = 0; i < 32; i++)
            {
                LookupTable1555to8888[i] = (byte)((i * 255 + 15) / 31);
            }
        }

        public unsafe void Initialize()
        {

            VRAM = (ushort*)Marshal.AllocHGlobal((VRAM_WIDTH * VRAM_HEIGHT) * 2);

            convertedData = (int*)Marshal.AllocHGlobal((VRAM_WIDTH * VRAM_HEIGHT) * 4);

            Device.VulkanInit(NullRenderer.hwnd, NullRenderer.hinstance);

            renderPass = Device.CreateRenderPass
                (Device.ChooseSurfaceFormat().format,
                VkAttachmentLoadOp.Clear,
                VkImageLayout.Undefined,
                VkImageLayout.PresentSrcKHR
                );

            drawPass = Device.CreateRenderPass(
                Device.ChooseSurfaceFormat().format,
                VkAttachmentLoadOp.Load,
                VkImageLayout.ColorAttachmentOptimal,
                VkImageLayout.ColorAttachmentOptimal,
                true,
                true
                );

            renderChain = Device.CreateSwapChain(renderPass, NullRenderer.ClientWidth, NullRenderer.ClientHeight);

            CreateSyncObjects();

            renderCmd = Device.CreateCommandBuffers(renderChain.Images.Count);

            DrawCMD = Device.CreateCommandBuffers(2);

            CreateSampleTexture();

            (drawTexture, depthTexture, drawFramebuff) = CreateDrawTexture();

            /////////////////////////////////////////////////////////////////////////////////////////

            ConfigDrawDescriptorSets();

            VkPipelineColorBlendAttachmentState blendAttachment = new()
            {
                blendEnable = VkBool32.True,
                srcColorBlendFactor = VkBlendFactor.Src1Alpha,
                dstColorBlendFactor = VkBlendFactor.Src1Color,
                colorBlendOp = VkBlendOp.Add,
                srcAlphaBlendFactor = VkBlendFactor.One,
                dstAlphaBlendFactor = VkBlendFactor.Zero,
                alphaBlendOp = VkBlendOp.Add,
                colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
            };

            VkVertexInputAttributeDescription[] drawAttributes = ConfigVertex();

            VkVertexInputBindingDescription drawBinding = new()
            {
                binding = 0,
                stride = (uint)Marshal.SizeOf<Vertex>(),
                inputRate = VkVertexInputRate.Vertex
            };

            var drawVertbytes = Device.LoadShaderFile("./Shaders/draw.vert.spv");
            var drawFragbytes = Device.LoadShaderFile("./Shaders/draw.frag.spv");

            drawMain = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                true,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                blendAttachment,
                default,
                VkPrimitiveTopology.TriangleList,
                new float[] { 0, 0, 0, 0 }
            );

            //blendAttachment.srcColorBlendFactor = VkBlendFactor.One;
            //blendAttachment.dstColorBlendFactor = VkBlendFactor.One;
            blendAttachment.colorBlendOp = VkBlendOp.ReverseSubtract;

            // 减法混合（Blend Mode 2）
            drawSubtract = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                true,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                blendAttachment,
                default,
                VkPrimitiveTopology.TriangleList,
                new float[] { 1.0f, 1.0f, 1.0f, 1.0f }
            );

            /////////////////////////////////////////////////////////////////////////////////////////

            ConfigSwapChainDescriptorSet();

            var pushConstantRanges = new VkPushConstantRange[1];
            pushConstantRanges[0] = new VkPushConstantRange
            {
                stageFlags = VkShaderStageFlags.Fragment,
                offset = 0,
                size = (uint)sizeof(SrcRectUBO)
            };

            var outVert = Device.LoadShaderFile("./Shaders/out.vert.spv");
            var outFrag = Device.LoadShaderFile("./Shaders/out.frag.spv");
            SwapChainPipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                SwapChainDescriptorLayout,
                outVert,
                outFrag,
                VRAM_WIDTH, VRAM_HEIGHT,
                false,
                VkSampleCountFlags.Count1,
                default,
                default,
                default,
                pushConstantRanges,
                VkPrimitiveTopology.TriangleStrip
            );

            /////////////////////////////////////////////////////////////////////////////////////////

            InitLayoutAndSet();

            //Calc Vram write alignment
            VkImageSubresource subresource = new VkImageSubresource();
            subresource.aspectMask = VkImageAspectFlags.Color;
            subresource.mipLevel = 0;
            subresource.arrayLayer = 0;

            VkSubresourceLayout layout;
            vkGetImageSubresourceLayout(Device.device, samplerTexture.image, &subresource, &layout);
            alignedRowPitch = (int)layout.rowPitch;

            minAlignment = (uint)Device.deviceProperties.limits.minMemoryMapAlignment;
            //alignedRowPitch = (VRAM_WIDTH * 4 + (int)minAlignment - 1) & ~((int)minAlignment - 1);

            viewport = new VkViewport { width = VRAM_WIDTH, height = VRAM_HEIGHT, maxDepth = 1.0f };
            scissor = new VkRect2D { extent = new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT) };

            m_realColor = true;

            drawVert.u_resolutionScale = 1.0f;

            UpdateVert();

            drawFrag.u_realColor = 1;
            drawFrag.u_dither = 0;

            UpdateFrag();

            StartRenderPass();

            Console.WriteLine("[Vulkan GPU] Initialization Complete.");
        }

        public unsafe VkVertexInputAttributeDescription[] ConfigVertex()
        {
            VaoBuffer = Device.CreateBuffer((ulong)(8192 * 10 * sizeof(Vertex)), VkBufferUsageFlags.VertexBuffer | VkBufferUsageFlags.TransferDst);

            void* mappedData;
            vkMapMemory(Device.device, VaoBuffer.stagingMemory, 0, WholeSize, 0, &mappedData);
            VaoBuffer.mappedData = mappedData;

            var vertexType = typeof(Vertex);
            return new VkVertexInputAttributeDescription[]
            {
                // v_pos (location=0)
                new()
                {
                    location = 0,
                    binding = 0,
                    format = VkFormat.R16g16b16a16Sscaled,
                    offset = 0
                },

                // v_pos_high (location=1)
                new()
                {
                    location = 1,
                    binding = 0,
                    format = VkFormat.R32g32b32Sfloat,
                    offset = 8
                },

                // v_texCoord (location=2)
                new()
                {
                    location = 2,
                    binding = 0,
                    format = VkFormat.R16g16Sscaled,
                    offset = 20
                },

                // v_color (location=3)
                new()
                {
                    location = 3,
                    binding = 0,
                    format = VkFormat.R8g8b8Unorm,
                    offset = 24
                },

                // v_clut (location=4)
                new()
                {
                    location = 4,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 36
                },

                // v_texPage (location=5)
                new()
                {
                    location = 5,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 40
                },

                // u_srcBlend (location=6) - float
                new()
                {
                    location = 6,
                    binding = 0,
                    format = VkFormat.R32Sfloat,
                    offset = 44
                },

                // u_destBlend (location=7) - float
                new()
                {
                    location = 7,
                    binding = 0,
                    format = VkFormat.R32Sfloat,
                    offset = 48
                },

                // u_setMaskBit (location=8) - int
                new()
                {
                    location = 8,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 52
                },

                // u_drawOpaquePixels (location=9) - int
                new()
                {
                    location = 9,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 56
                },

                // u_drawTransparentPixels (location=10) - int
                new()
                {
                    location = 10,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 60
                },

                // u_texWindowMask (location=11) - Vector2Int (ivec2)
                new()
                {
                    location = 11,
                    binding = 0,
                    format = VkFormat.R32g32Sint,
                    offset = 64
                },

                // u_texWindowOffset (location=12) - Vector2Int (ivec2)
                new()
                {
                    location = 12,
                    binding = 0,
                    format = VkFormat.R32g32Sint,
                    offset = 72
                },

                //BlendMode
                new()
                {
                    location = 13,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 80
                }
            };
        }

        public unsafe void ConfigDrawDescriptorSets()
        {
            minUboAlignment = (uint)Device.GetMinUniformBufferAlignment();

            uint uboSize = (uint)Marshal.SizeOf<drawvertUBO>();
            vertexUBO = Device.CreateBuffer(uboSize * 2, VkBufferUsageFlags.UniformBuffer | VkBufferUsageFlags.TransferDst);

            uboSize = (uint)Marshal.SizeOf<drawfragUBO>();
            fragmentUBO = Device.CreateBuffer(uboSize * 2, VkBufferUsageFlags.UniformBuffer | VkBufferUsageFlags.TransferDst);

            void* mappedData;
            vkMapMemory(Device.device, fragmentUBO.stagingMemory, 0, WholeSize, 0, &mappedData);
            drawFragData = (byte*)mappedData;

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
                        type = VkDescriptorType.UniformBuffer,//UniformBufferDynamic,
                        descriptorCount = 2
                    }
                }
            );

            drawDescriptorLayout = Device.CreateDescriptorSetLayout(new[]
            {
                // 顶点着色器的 UniformBuffer (binding=0)
                new VkDescriptorSetLayoutBinding {
                    binding = 0,
                    descriptorType = VkDescriptorType.UniformBuffer, //UniformBufferDynamic,
                    descriptorCount = 1,
                    stageFlags = VkShaderStageFlags.Vertex
                },
                // 片段着色器的 UniformBuffer (binding=1)
                new VkDescriptorSetLayoutBinding {
                    binding = 1,
                    descriptorType = VkDescriptorType.UniformBuffer, //UniformBufferDynamic,
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

            drawDescriptorSet = Device.AllocateDescriptorSet(drawDescriptorLayout, drawdescriptorPool);

            // 顶点着色器的 UniformBuffer (binding=0)
            var vertBufferInfo = new VkDescriptorBufferInfo
            {
                buffer = vertexUBO.stagingBuffer,
                offset = 0,
                range = 80//(ulong)Marshal.SizeOf<drawvertUBO>()
            };

            // 片段着色器的 CombinedImageSampler (binding=1)
            var imageInfo = new VkDescriptorImageInfo
            {
                sampler = samplerTexture.sampler,
                imageView = samplerTexture.imageview,
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
                        descriptorType = VkDescriptorType.UniformBuffer, //UniformBufferDynamic
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
                        descriptorType = VkDescriptorType.UniformBuffer, //UniformBufferDynamic
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
        }

        public unsafe void ConfigSwapChainDescriptorSet()
        {
            SwapChaindescriptorPool = Device.CreateDescriptorPool(
            maxSets: 1,
            new[]
            {
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.CombinedImageSampler,
                        descriptorCount = 1
                    }
                }
            );

            SwapChainDescriptorLayout = Device.CreateDescriptorSetLayout(new[]
            {
                new VkDescriptorSetLayoutBinding { // CombinedImageSampler (binding=0)
                    binding = 0,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    stageFlags = VkShaderStageFlags.Fragment
                }
            });

            SwapChainDescriptorSet = Device.AllocateDescriptorSet(SwapChainDescriptorLayout, SwapChaindescriptorPool);

            VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
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
                        dstSet = SwapChainDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&imageInfo
                    }
                 },
                 SwapChainDescriptorSet
            );
        }

        public unsafe void InitLayoutAndSet()
        {
            Device.BeginCommandBuffer(DrawCMD.CMD[0]);

            VkBufferMemoryBarrier barrier = new()
            {
                sType = VkStructureType.BufferMemoryBarrier,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.VertexAttributeRead,
                buffer = VaoBuffer.stagingBuffer,
                size = WholeSize
            };

            vkCmdPipelineBarrier(DrawCMD.CMD[0],
                VkPipelineStageFlags.Transfer,
                VkPipelineStageFlags.VertexInput,
                0, 0, null, 1, ref barrier, 0, null);

            samplerTexture.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                samplerTexture,
                samplerTexture.layout,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            readTexture.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                readTexture,
                readTexture.layout,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            Device.EndAndWaitCommandBuffer(DrawCMD.CMD[0]);
        }

        public unsafe void CreateSampleTexture()
        {
            samplerTexture = Device.CreateTexture(
                VRAM_WIDTH, VRAM_HEIGHT,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc,
                VkFilter.Nearest,
                VkSamplerAddressMode.ClampToEdge,
                VkSamplerMipmapMode.Linear,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                VkImageTiling.Linear
                );

            void* dataptr;
            vkMapMemory(Device.device, samplerTexture.imagememory, 0, WholeSize, 0, &dataptr);
            samplerData = dataptr;

            readTexture = Device.CreateTexture(
                VRAM_WIDTH, VRAM_HEIGHT,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc,
                VkFilter.Nearest,
                VkSamplerAddressMode.ClampToEdge,
                VkSamplerMipmapMode.Linear,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                VkImageTiling.Linear
                );

            vkMapMemory(Device.device, readTexture.imagememory, 0, WholeSize, 0, &dataptr);
            readTextureData = dataptr;

            Console.WriteLine($"[Vulkan GPU] SamplerTexture 0x{samplerTexture.image.Handle:X}");
        }

        private (vkTexture tex, vkTexture dep, VkFramebuffer fb) CreateDrawTexture()
        {
            var tex = Device.CreateTexture(
                VRAM_WIDTH * resolutionScale, VRAM_HEIGHT * resolutionScale,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc | VkImageUsageFlags.InputAttachment,
                VkFilter.Nearest,
                VkSamplerAddressMode.ClampToEdge,
                VkSamplerMipmapMode.Linear,
                VkMemoryPropertyFlags.DeviceLocal,
                VkImageTiling.Optimal
                );

            Console.WriteLine($"[Vulkan GPU] DrawTexture 0x{tex.image.Handle:X}");

            var dep = Device.CreateTexture(
                VRAM_WIDTH * resolutionScale, VRAM_HEIGHT * resolutionScale,
                VkFormat.D16Unorm,
                VkImageAspectFlags.Depth,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.TransferSrc,
                VkFilter.Nearest,
                VkSamplerAddressMode.ClampToBorder,
                VkSamplerMipmapMode.Nearest,
                VkMemoryPropertyFlags.DeviceLocal,
                VkImageTiling.Optimal
                );

            Console.WriteLine($"[Vulkan GPU] DepthTexture 0x{dep.image.Handle:X}");

            Device.BeginCommandBuffer(DrawCMD.CMD[0]);

            tex.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                tex,
                tex.layout,
                VkImageLayout.ColorAttachmentOptimal
            );

            dep.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                dep,
                dep.layout,
                VkImageLayout.DepthStencilAttachmentOptimal,
                VkPipelineStageFlags.TopOfPipe,
                VkPipelineStageFlags.Transfer,
                VkImageAspectFlags.Depth
            );

            Device.EndAndWaitCommandBuffer(DrawCMD.CMD[0]);

            var fb = Device.CreateFramebuffer(drawPass, tex.imageview, dep.imageview, (uint)(VRAM_WIDTH * resolutionScale), (uint)(VRAM_HEIGHT * resolutionScale));

            return (tex, dep, fb);
        }

        private void CreateSyncObjects()
        {
            frameFences = new FrameFence[renderChain.Images.Count];

            for (int i = 0; i < renderChain.Images.Count; i++)
            {
                frameFences[i] = new FrameFence
                {
                    ImageAvailable = Device.CreateSemaphore(),
                    RenderFinished = Device.CreateSemaphore(),
                    InFlightFence = Device.CreateFence(true)
                };
            }
        }

        public unsafe void Dispose()
        {
            if (isDisposed)
                return;

            vkQueueWaitIdle(Device.presentQueue);
            vkQueueWaitIdle(Device.graphicsQueue);

            vkUnmapMemory(Device.device, fragmentUBO.stagingMemory);
            vkUnmapMemory(Device.device, samplerTexture.imagememory);
            vkUnmapMemory(Device.device, readTexture.imagememory);
            vkUnmapMemory(Device.device, VaoBuffer.stagingMemory);

            foreach (var frame in frameFences)
            {
                vkDestroySemaphore(Device.device, frame.ImageAvailable, null);
                vkDestroySemaphore(Device.device, frame.RenderFinished, null);
                vkDestroyFence(Device.device, frame.InFlightFence, null);
            }

            Device.DestroyFramebuffer(drawFramebuff);

            Device.DestoryCommandBuffers(renderCmd);
            Device.DestoryCommandBuffers(DrawCMD);

            vkDestroyDescriptorSetLayout(Device.device, SwapChainDescriptorLayout, null);
            vkDestroyDescriptorSetLayout(Device.device, drawDescriptorLayout, null);

            vkDestroyDescriptorPool(Device.device, SwapChaindescriptorPool, 0);
            vkDestroyDescriptorPool(Device.device, drawdescriptorPool, 0);

            Device.DestroyGraphicsPipeline(SwapChainPipeline);
            Device.DestroyGraphicsPipeline(drawMain);
            Device.DestroyGraphicsPipeline(drawSubtract);

            Device.DestroyTexture(drawTexture);
            Device.DestroyTexture(depthTexture);
            Device.DestroyTexture(samplerTexture);
            Device.DestroyTexture(readTexture);

            Device.DestoryBuffer(VaoBuffer);
            Device.DestoryBuffer(vertexUBO);
            Device.DestoryBuffer(fragmentUBO);

            Device.CleanupSwapChain(renderChain);

            vkDestroyRenderPass(Device.device, renderPass, null);
            vkDestroyRenderPass(Device.device, drawPass, null);

            Device.VulkanDispose();

            Marshal.FreeHGlobal((IntPtr)VRAM);
            Marshal.FreeHGlobal((IntPtr)convertedData);

            Console.WriteLine($"[Vulkan GPU] Disposed");

            isDisposed = true;
        }

        private void SetRealColor(bool realColor)
        {
            if (m_realColor != realColor)
            {
                m_realColor = realColor;
                drawFrag.u_realColor = realColor ? 1 : 0;
                UpdateFrag();
            }
        }

        public unsafe void SetResolutionScale(int scale)
        {
            if (scale < 1 || scale > 12)
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

            resolutionScale = scale;

            vkTexture newDrawTexture, newDepthTexture;
            VkFramebuffer newDrawFramebuffer;
            (newDrawTexture, newDepthTexture, newDrawFramebuffer) = CreateDrawTexture();

            CopyTexture(drawTexture, newDrawTexture, oldWidth, oldHeight, newWidth, newHeight);

            Device.DestroyFramebuffer(drawFramebuff);

            Device.DestroyTexture(depthTexture);

            Device.DestroyTexture(drawTexture);

            drawTexture = newDrawTexture;

            depthTexture = newDepthTexture;

            drawFramebuff = newDrawFramebuffer;

            VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
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
                        dstSet = SwapChainDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&imageInfo
                    }
                 },
                 SwapChainDescriptorSet
            );

            SetViewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            Console.WriteLine($"[VULKAN GPU] Resolution scale updated to {scale}x ({newWidth}x{newHeight})");
        }

        public void SetParams(int[] Params)
        {
        }

        public unsafe void SetRam(byte[] Ram)
        {
            Marshal.Copy(Ram, 0, (IntPtr)VRAM, Ram.Length);

            WriteTexture(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            CopyTexture(samplerTexture, drawTexture, VRAM_WIDTH, VRAM_HEIGHT, VRAM_WIDTH, VRAM_HEIGHT);
        }

        public unsafe byte[] GetRam()
        {
            byte[] data = new byte[(VRAM_WIDTH * VRAM_HEIGHT) * 2];

            //CopyRectVRAMtoCPU(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            Marshal.Copy((IntPtr)VRAM, data, 0, data.Length);

            return data;
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
                x = rx + 3,
                y = ry + 3,
                width = w - 6,
                height = offsetline * 2 - 6
            };

            RecordVertexs();

            EndRenderPass();

            if (NullRenderer.isResizeed)
            {
                NullRenderer.isResizeed = false;

                vkQueueWaitIdle(Device.presentQueue);

                RecreateSwapChain();
            }

            RenderToWindow(is24bit);

            PGXPVector.Clear();

            if (IRScale != resolutionScale)
                SetResolutionScale(IRScale);

            if (RealColor != m_realColor)
            {
                SetRealColor(RealColor);
            }

            PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;

            drawVert.u_pgxp = PGXP ? 1 : 0;
            UpdateVert();

            SetViewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            StartRenderPass();

            return (m_vramDisplayArea.width, m_vramDisplayArea.height);
        }

        private unsafe void RenderToSwapchain(VkCommandBuffer cmd, uint chainidx, bool is24bit)
        {
            Device.BeginCommandBuffer(cmd);

            drawTexture.layout = Device.TransitionImageLayout(
                cmd,
                drawTexture,
                drawTexture.layout,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            m_vramDisplayArea.x = m_vramDisplayArea.x * resolutionScale;

            m_vramDisplayArea.y = m_vramDisplayArea.y * resolutionScale;

            m_vramDisplayArea.width = ViewVRam ?
                VRAM_WIDTH * resolutionScale :
                m_vramDisplayArea.width * resolutionScale;

            m_vramDisplayArea.height = ViewVRam ?
                VRAM_HEIGHT * resolutionScale :
                m_vramDisplayArea.height * resolutionScale;

            Device.BeginRenderPass(cmd, renderPass,
                        renderChain.framebuffes[(int)chainidx],
                        (int)renderChain.Extent.width,
                        (int)renderChain.Extent.height,
                        true, 0, 0, 0, 1);

            Device.BindGraphicsPipeline(cmd, SwapChainPipeline, false);

            SrcRectUBO u_srcRect = new SrcRectUBO(
                m_vramDisplayArea.x,
                m_vramDisplayArea.y,
                m_vramDisplayArea.width,
                m_vramDisplayArea.height,
                is24bit ? 1 : 0
            );

            vkCmdPushConstants(
                cmd,
                SwapChainPipeline.layout,
                VkShaderStageFlags.Fragment,
                0,
                (uint)sizeof(SrcRectUBO),
                &u_srcRect
            );

            Device.BindDescriptorSet(cmd, SwapChainPipeline, SwapChainDescriptorSet);

            int renderX = 0;
            int renderY = 0;
            int width = (int)renderChain.Extent.width;
            int hgight = (int)renderChain.Extent.height;

            if (KEEPAR)
            {
                float displayWidth = m_vramDisplayArea.width;
                float displayHeight = ViewVRam ? m_vramDisplayArea.height : (displayWidth / AspectRatio);

                float renderScale = Math.Min(width / displayWidth, hgight / displayHeight);

                //if (!StretchToFit)
                //    renderScale = Math.Max(1.0f, (float)Math.Floor(renderScale));

                int renderWidth = (int)(displayWidth * renderScale);
                int renderHeight = (int)(displayHeight * renderScale);
                renderX = (width - renderWidth) / 2;
                renderY = (hgight - renderHeight) / 2;

                Device.SetViewportAndScissor(cmd, renderX, renderY, renderWidth, renderHeight);
            } else
                Device.SetViewportAndScissor(cmd, renderX, renderY, width, hgight);

            vkCmdDraw(cmd, 4, 1, 0, 0);

            vkCmdEndRenderPass(cmd);

            drawTexture.layout = Device.TransitionImageLayout(
                cmd,
                drawTexture,
                VkImageLayout.ShaderReadOnlyOptimal,
                VkImageLayout.ColorAttachmentOptimal
            );

            Device.EndCommandBuffer(cmd);

        }

        private unsafe void RenderToWindow(bool is24bit)
        {
            FrameFence currentFrame = frameFences[frameIndex];

            vkWaitForFences(Device.device, 1, ref currentFrame.InFlightFence, true, ulong.MaxValue);
            vkResetFences(Device.device, 1, ref currentFrame.InFlightFence);

            uint imageIndex;
            VkResult result = vkAcquireNextImageKHR(
                Device.device,
                renderChain.Chain,
                ulong.MaxValue,
                currentFrame.ImageAvailable,
                VkFence.Null,
                &imageIndex
            );

            VkCommandBuffer cmd = renderCmd.CMD[imageIndex];

            //Console.WriteLine($"[Vulkan GPU] Present Start frameIndex {frameIndex} CMD 0x{cmd.Handle:X}!");

            RenderToSwapchain(cmd, imageIndex, is24bit);

            // 提交呈现命令
            VkCommandBuffer presentCmd = renderCmd.CMD[imageIndex];

            VkPipelineStageFlags waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
            VkSemaphore ws = currentFrame.ImageAvailable;
            VkSemaphore ss = currentFrame.RenderFinished;
            VkSwapchainKHR chain = renderChain.Chain;

            VkSubmitInfo submitInfo = VkSubmitInfo.New();
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &ws;
            submitInfo.pWaitDstStageMask = &waitStages;
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &presentCmd;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &ss;

            vkQueueSubmit(Device.graphicsQueue, 1, &submitInfo, currentFrame.InFlightFence);

            VkPresentInfoKHR presentInfo = new VkPresentInfoKHR
            {
                sType = VkStructureType.PresentInfoKHR,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &ss,
                swapchainCount = 1,
                pSwapchains = &chain,
                pImageIndices = &imageIndex
            };

            vkQueuePresentKHR(Device.presentQueue, &presentInfo);

            //Console.WriteLine($"[Vulkan GPU] Present vkQueuePresentKHR done!");

            frameIndex = (frameIndex + 1) % (int)renderChain.Images.Count;
        }

        private unsafe void RecreateSwapChain()
        {
            vkDeviceWaitIdle(Device.device);

            Device.CleanupSwapChain(renderChain);
            renderChain = Device.CreateSwapChain(renderPass, NullRenderer.ClientWidth, NullRenderer.ClientHeight);
        }

        private void UpdateScissor()
        {
            int x = DrawingAreaTopLeft.X;
            int y = DrawingAreaTopLeft.Y;
            int width = Math.Max(DrawingAreaBottomRight.X - DrawingAreaTopLeft.X + 0, 0);
            int height = Math.Max(DrawingAreaBottomRight.Y - DrawingAreaTopLeft.Y + 0, 0);

            if (width == 0 || height == 0)
                return;

            RecordVertexs();

            SetScissor(x, y, width, height);

            //Console.WriteLine($"UpdateScissor mask {x},{y} - {width},{height}");
        }

        private unsafe void SetScissor(int x, int y, int width, int height)
        {
            scissor = new VkRect2D
            {
                offset = new VkOffset2D(x * resolutionScale, y * resolutionScale),
                extent = new VkExtent2D((uint)(width * resolutionScale), (uint)(height * resolutionScale))
            };
        }

        private void SetViewport(int left, int top, int width, int height)
        {
            viewport = new VkViewport
            {
                x = (float)left * resolutionScale,
                y = (float)top * resolutionScale,
                width = (float)width * resolutionScale,
                height = (float)height * resolutionScale,
                minDepth = 0,
                maxDepth = 1
            };
        }

        public unsafe void SetMaskBit(uint value)
        {
            ForceSetMaskBit = ((value & 1) != 0);
            CheckMaskBit = (((value >> 1) & 1) != 0);

            setMaskBit = ForceSetMaskBit ? 1 : 0;
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            DrawingAreaTopLeft = value;

            UpdateScissor();
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            DrawingAreaBottomRight = value;

            UpdateScissor();
        }

        public void SetDrawingOffset(TDrawingOffset value)
        {
            DrawingOffset = value;
        }

        public unsafe void SetTextureWindow(uint value)
        {
            value &= 0xfffff;

            if (oldtexwin != value)
            {
                oldtexwin = value;

                TextureWindowXMask = (int)(value & 0x1f);
                TextureWindowYMask = (int)((value >> 5) & 0x1f);

                TextureWindowXOffset = (int)((value >> 10) & 0x1f);
                TextureWindowYOffset = (int)((value >> 15) & 0x1f);

                //Console.WriteLine($"SetTextureWindow mask {TextureWindowXMask},{TextureWindowYMask} offset {TextureWindowXOffset},{TextureWindowYOffset}");
            }
        }

        public unsafe void FillRectVRAM(ushort left, ushort top, ushort width, ushort height, uint colorval)
        {
            //var color = vkColor.FromUInt32(colorval);
            byte r = (byte)(colorval);
            byte g = (byte)(colorval >> 8);
            byte b = (byte)(colorval >> 16);

            VkClearAttachment colorAttachment = new VkClearAttachment
            {
                aspectMask = VkImageAspectFlags.Color,
                colorAttachment = 0,
                clearValue = new VkClearValue
                {
                    color = new VkClearColorValue
                    {
                        float32_0 = r / 255.0f,
                        float32_1 = g / 255.0f,
                        float32_2 = b / 255.0f,
                        float32_3 = 0//color.a / 255.0f
                    }
                }
            };

            VkClearRect clearRect = new VkClearRect
            {
                rect = new VkRect2D
                {
                    offset = new VkOffset2D { x = left * resolutionScale, y = top * resolutionScale },
                    extent = new VkExtent2D { width = (uint)(width * resolutionScale), height = (uint)(height * resolutionScale) }
                },
                baseArrayLayer = 0,
                layerCount = 1
            };

            RecordVertexs();

            vkCmdClearAttachments(CurrentDrawCMD, 1, &colorAttachment, 1, &clearRect);

            ushort* dst = VRAM + left + top * VRAM_WIDTH;
            for (int row = 0; row < height; row++)
            {
                ushort* srcPtr = dst + row * VRAM_WIDTH;
                for (int i = 0; i < width; i++)
                {
                    srcPtr[i] = (ushort)colorval;
                }
            }

            WriteTexture(left, top, width, height);
        }

        public unsafe void CopyRectVRAMtoVRAM(ushort srcX, ushort srcY, ushort destX, ushort destY, ushort width, ushort height)
        {
            if (srcX == destX && srcY == destY)
                return;

            var srcBounds = vkRectangle<int>.FromExtents(srcX, srcY, width, height);
            var destBounds = vkRectangle<int>.FromExtents(destX, destY, width, height);

            if (m_dirtyArea.Intersects(srcBounds))
            {
                UpdateReadTexture();
                m_dirtyArea.Grow(destBounds);
            } else
            {
                GrowDirtyArea(destBounds);
            }

            //Console.WriteLine($"[Vulkan GPU] CopyRectVRAMtoVRAM {srcX},{srcY} -> {destX},{destY} [{width},{height}]");

            RecordVertexs();
            EndRenderPass();

            CopyTextureRect(drawTexture,
                srcX * resolutionScale, srcY * resolutionScale,
                destX * resolutionScale, destY * resolutionScale,
                width * resolutionScale, height * resolutionScale);

            ushort* src = VRAM + srcX + srcY * VRAM_WIDTH;
            ushort* dst = VRAM + destX + destY * VRAM_WIDTH;
            int RowPitch = width * 2;
            for (int row = 0; row < height; row++)
            {
                ushort* srcPtr = src + row * VRAM_WIDTH;
                ushort* dstPtr = dst + row * VRAM_WIDTH;

                Buffer.MemoryCopy(
                    srcPtr,
                    dstPtr,
                    RowPitch,
                    RowPitch
                );
            }

            StartRenderPass();
        }

        public unsafe void CopyRectVRAMtoCPU(int left, int top, int width, int height)
        {
            if (m_dirtyArea.Intersects(GetWrappedBounds(left, top, width, height)))
            {
                //Console.WriteLine($"[Vulkan GPU] CopyRectVRAMtoCPU {left},{top} [{width},{height}]");

                RecordVertexs();
                EndRenderPass();

                CopyTexture(drawTexture, readTexture, drawTexture.width, drawTexture.height, VRAM_WIDTH, VRAM_HEIGHT);

                byte* srcBase = (byte*)readTextureData;
                int srcRowBytes = width * 4;
                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcBase + ((top + y) * alignedRowPitch) + (left * 4);
                    byte* dstRow = (byte*)convertedData + y * srcRowBytes;
                    Buffer.MemoryCopy(srcRow, dstRow, srcRowBytes, srcRowBytes);
                }

                ushort rgb8888to1555(int color)
                {
                    byte m = (byte)((color & 0xFF000000) >> 24);
                    byte r = (byte)((color & 0x00FF0000) >> 16 + 3);
                    byte g = (byte)((color & 0x0000FF00) >> 8 + 3);
                    byte b = (byte)((color & 0x000000FF) >> 3);

                    return (ushort)(m << 15 | r << 10 | g << 5 | b);
                }

                ushort* dst = VRAM + left + top * VRAM_WIDTH;
                int srcidx = 0;
                for (int row = 0; row < height; row++)
                {
                    ushort* dstPtr = dst + row * VRAM_WIDTH;
                    for (int i = 0; i < width; i++)
                    {
                        int color = convertedData[srcidx++];
                        dstPtr[i] = rgb8888to1555(color);
                    }
                }

                StartRenderPass();
            }
        }

        //这里是在裸写显存，不要随便改
        // Direct VRAM memory write for performance. Handle with care.
        // Yes, I'm writing mapped VRAM directly. It's faster. Don't "fix" it.
        public unsafe void WriteTexture(int x, int y, int width, int height)
        {
            ushort* src = VRAM + x + y * VRAM_WIDTH;
            int dstidx = 0;
            for (int row = 0; row < height; row++)
            {
                ushort* srcPtr = src + row * VRAM_WIDTH;

                for (int i = 0; i < width; i++)
                {
                    ushort color = srcPtr[i];
                    byte m = (byte)(color >> 15);
                    byte r = LookupTable1555to8888[color & 0x1F];
                    byte g = LookupTable1555to8888[(color >> 5) & 0x1F];
                    byte b = LookupTable1555to8888[(color >> 10) & 0x1F];

                    convertedData[dstidx++] = (m << 24) | (b << 16) | (g << 8) | r;
                }
            }

            byte* dstBase = (byte*)samplerData;
            int srcRowBytes = width * 4;
            for (int row = 0; row < height; row++)
            {
                byte* srcRow = (byte*)convertedData + row * srcRowBytes;
                byte* dstRow = dstBase + (y + row) * alignedRowPitch + x * 4;

                Buffer.MemoryCopy(srcRow, dstRow, srcRowBytes, srcRowBytes);
            }

            //ulong startOffset = (ulong)(y * VRAM_WIDTH + x) * 4;
            //ulong endOffset = startOffset + (ulong)(height * alignedRowPitch);

            //ulong flushStart = (startOffset / minAlignment) * minAlignment;
            //ulong flushEnd = ((endOffset + minAlignment - 1) / minAlignment) * minAlignment;
            //ulong flushSize = flushEnd - flushStart;

            //VkMappedMemoryRange range = new VkMappedMemoryRange();
            //range.sType = VkStructureType.MappedMemoryRange;
            //range.memory = samplerTexture.imagememory;
            //range.offset = flushStart;
            //range.size = flushSize;

            //vkFlushMappedMemoryRanges(Device.device, 1, &range);
        }

        public unsafe void CopyRectCPUtoVRAM(int originX, int originY, int width, int height)
        {
            if (width <= 0 || height <= 0 || VRAM == null)
                return;

            GrowDirtyArea(GetWrappedBounds(originX, originY, width, height));

            bool wrapX = (originX + width) > VRAM_WIDTH;
            bool wrapY = (originY + height) > VRAM_HEIGHT;

            if (wrapX || wrapY)
            {
                // 计算宽度和高度的分段
                int width2 = wrapX ? (originX + width) % VRAM_WIDTH : 0;
                int height2 = wrapY ? (originY + height) % VRAM_HEIGHT : 0;
                int width1 = width - width2;
                int height1 = height - height2;

                //Console.WriteLine($"[Vulkan GPU] {originX + width},{originY + height} wrapX {wrapX} & wrapY {wrapY} | {width2},{height2} [{width1},{height1}]");

                if (wrapX && !wrapY)
                {
                    // 只有宽度回绕
                    WriteTexture(originX, originY, width1, height);
                    WriteTexture(0, originY, width2, height);
                } else if (!wrapX && wrapY)
                {
                    // 只有高度回绕
                    WriteTexture(originX, originY, width, height1);
                    WriteTexture(originX, 0, width, height2);
                } else if (wrapX && wrapY)
                {
                    // 宽度和高度都回绕
                    WriteTexture(originX, originY, width1, height1);
                    WriteTexture(0, originY, width2, height1);
                    WriteTexture(originX, 0, width1, height2);
                    WriteTexture(0, 0, width2, height2);
                }
            } else
            {
                // 没有回绕
                WriteTexture(originX, originY, width, height);
            }

            textureBlocks.Add(new TextureBlock { x = originX, y = originY, width = width, height = height });
        }

        public unsafe void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;

            if (_VRAMTransfer.isRead)
            {
                CopyRectVRAMtoCPU(_VRAMTransfer.OriginX, _VRAMTransfer.OriginY, _VRAMTransfer.W, _VRAMTransfer.H);
            }
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
            if (_VRAMTransfer.Y > VRAM_HEIGHT)
                _VRAMTransfer.Y -= VRAM_HEIGHT;

            *(ushort*)(VRAM + _VRAMTransfer.X + _VRAMTransfer.Y * VRAM_WIDTH) = value;

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

        public void SetDrawMode(ushort vtexPage, ushort vclut, bool dither)
        {
            if (m_realColor)
                dither = false;

            if (m_dither != dither)
            {
                m_dither = dither;
                drawFrag.u_dither = m_dither ? 1 : 0;
                UpdateFrag();
            }

            m_currentDepth++;

            if (m_TexPage.Value != vtexPage)
            {
                m_TexPage.Value = vtexPage;

                SetSemiTransparencyMode(m_TexPage.SemiTransparencymode);

                if (!m_TexPage.TextureDisable)
                {
                    int texBaseX = m_TexPage.TexturePageBaseX * TexturePageBaseXMult;
                    int texBaseY = m_TexPage.TexturePageBaseY * TexturePageBaseYMult;
                    int texSize = ColorModeTexturePageWidths[m_TexPage.TexturePageColors];
                    m_textureArea = vkRectangle<int>.FromExtents(texBaseX, texBaseY, texSize, texSize);

                    if (m_TexPage.TexturePageColors < 2)
                        UpdateClut(vclut);
                }
            } else if (m_clut.Value != vclut && !m_TexPage.TextureDisable && m_TexPage.TexturePageColors < 2)
            {
                UpdateClut(vclut);
            }

            if (IntersectsTextureData(m_dirtyArea))
                UpdateReadTexture();
        }

        public unsafe void StartRenderPass()
        {
            //Console.WriteLine($"[Vulkan GPU] FRAME ----------------------------------------------- ");

            vaoOffset = 0;

            m_currentDepth = 1;

            CurrentDrawCMD = DrawCMD.CMD[1];

            if (CurrentPipeline.pipeline.Handle == 0)
            {
                SrcBlend = 1.0f;
                DstBlend = 0.0f;
                CurrentPipeline = drawMain;
            }

            SetViewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            SetScissor(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            Device.BeginCommandBuffer(CurrentDrawCMD);

            Device.BeginRenderPass(CurrentDrawCMD, drawPass, drawFramebuff, drawTexture.width, drawTexture.height, true);

            vkCmdBindVertexBuffers(CurrentDrawCMD, 0, 1, ref VaoBuffer.stagingBuffer, ref vaoOffset);

            vkCmdSetViewport(CurrentDrawCMD, 0, 1, ref viewport);

            Device.BindDescriptorSet(CurrentDrawCMD, drawMain, drawDescriptorSet);

            Device.BindDescriptorSet(CurrentDrawCMD, drawSubtract, drawDescriptorSet);

        }

        public void EndRenderPass()
        {
            vkCmdNextSubpass(CurrentDrawCMD, VkSubpassContents.Inline);

            vkCmdEndRenderPass(CurrentDrawCMD);

            vramToDrawTexture();

            Device.EndAndWaitCommandBuffer(CurrentDrawCMD);
        }

        public unsafe void UploadVertexs(List<Vertex> vertexs)
        {
            ulong size = 0;

            var sourceSpan = CollectionsMarshal.AsSpan(vertexs);
            fixed (Vertex* pSrc = sourceSpan)
            {
                byte* pDst = (byte*)VaoBuffer.mappedData + vaoOffset;
                size = (ulong)(sourceSpan.Length * sizeof(Vertex));
                Buffer.MemoryCopy(pSrc, pDst, size, size);
            }

            currentBlockFirstVertex = (uint)(vaoOffset / (ulong)sizeof(Vertex));

            vaoOffset += size;
        }

        public unsafe void RecordVertexs()
        {
            if (Vertexs.Count == 0)
                return;

            if (Vertexs.Count >= 8192)
            {
                Console.WriteLine($"[Vulkan GPU] RecordVertexs Overflow {Vertexs.Count}");
                Vertexs.Clear();
                return;
            }

            //Console.WriteLine($"[Vulkan GPU] RecordVertexs: mode {m_semiTransparencyMode} , {Vertexs.Count} vertices，Offset {vaoOffset}");

            UploadVertexs(Vertexs);

            vkCmdSetScissor(CurrentDrawCMD, 0, 1, ref scissor);

            // 模式2需先绘制不透明部分
            if (TransparencyEnabled && BlendMode == 3 && HasTexture) //semiTransparencyMode 2
            {
                foreach (ref Vertex vertex in CollectionsMarshal.AsSpan(Vertexs))
                {
                    vertex.u_drawOpaquePixels = 1;
                    vertex.u_drawTransparentPixels = 0;
                }

                Device.BindGraphicsPipeline(CurrentDrawCMD, drawMain, false);

                vkCmdDraw(CurrentDrawCMD, (uint)Vertexs.Count, 1, currentBlockFirstVertex, 0);

                foreach (ref Vertex vertex in CollectionsMarshal.AsSpan(Vertexs))
                {
                    vertex.u_drawOpaquePixels = 0;
                    vertex.u_drawTransparentPixels = 1;
                }

                //Console.WriteLine($"[Vulkan GPU] RecordVertexs MODE 2, {Vertexs.Count} vertices，Offset {vaoOffset}");

                UploadVertexs(Vertexs);
            }

            Device.BindGraphicsPipeline(CurrentDrawCMD, CurrentPipeline, false);

            vkCmdDraw(CurrentDrawCMD, (uint)Vertexs.Count, 1, currentBlockFirstVertex, 0);

            Vertexs.Clear();
        }

        public void DrawLineBatch(bool isDithered, bool SemiTransparency)
        {
            glTexPage tp = new glTexPage();

            tp.TextureDisable = true;

            SetDrawMode(tp.Value, 0, isDithered);

            EnableSemiTransparency(SemiTransparency);
        }

        public void DrawLine(uint v1, uint v2, uint c1, uint c2, bool isTransparent, int SemiTransparency)
        {
            if (!IsDrawAreaValid())
                return;

            Vertex[] vertices = new Vertex[4];

            vkPosition p1 = new vkPosition();
            p1.x = (short)v1;
            p1.y = (short)(v1 >> 16);

            vkPosition p2 = new vkPosition();
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
                var color1 = vkColor.FromUInt32(c1);
                // 渲染一个点，使用第一个颜色
                vertices[0].v_pos = p1;
                vertices[1].v_pos = new vkPosition((short)(p1.x + 1), p1.y);
                vertices[2].v_pos = new vkPosition(p1.x, (short)(p1.y + 1));
                vertices[3].v_pos = new vkPosition((short)(p1.x + 1), (short)(p1.y + 1));

                vertices[0].v_color = color1;
                vertices[1].v_color = color1;
                vertices[2].v_color = color1;
                vertices[3].v_color = color1;
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

                vertices[0].v_pos = new vkPosition(x1, y1);
                vertices[1].v_pos = new vkPosition((short)(x1 + fillDx), (short)(y1 + fillDy));
                vertices[2].v_pos = new vkPosition(x2, y2);
                vertices[3].v_pos = new vkPosition((short)(x2 + fillDx), (short)(y2 + fillDy));

                var color1 = vkColor.FromUInt32(c1);
                var color2 = vkColor.FromUInt32(c2);

                vertices[0].v_color = color1;
                vertices[1].v_color = color1;
                vertices[2].v_color = color2;
                vertices[3].v_color = color2;
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                m_dirtyArea.Grow(vertices[i].v_pos.x, vertices[i].v_pos.y);

                vertices[i].v_clut.Value = 0;
                vertices[i].v_texPage.TextureDisable = true;
                vertices[i].v_pos.z = m_currentDepth;

                if (PGXP)
                {
                    vertices[i].v_pos_high = new Vector3((float)vertices[i].v_pos.x, (float)vertices[i].v_pos.y, (float)vertices[i].v_pos.z);
                    //PGXPVector.HighPos HighPos;
                    //if (PGXPVector.Find(vertices[i].v_pos.x, vertices[i].v_pos.y, out HighPos))
                    //{
                    //    vertices[i].v_pos_high = new Vector3((float)HighPos.x, (float)HighPos.y, (float)HighPos.z);
                    //}
                }

                vertices[i].u_srcBlend = SrcBlend;
                vertices[i].u_destBlend = DstBlend;
                vertices[i].u_setMaskBit = setMaskBit;
                vertices[i].u_drawOpaquePixels = 1;
                vertices[i].u_drawTransparentPixels = 1;
                vertices[i].u_texWindowMask.X = 0;
                vertices[i].u_texWindowMask.Y = 0;
                vertices[i].u_texWindowOffset.X = 0;
                vertices[i].u_texWindowOffset.Y = 0;
                vertices[i].BlendMode = BlendMode;
            }

            Vertexs.Add(vertices[0]);
            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);

            Vertexs.Add(vertices[1]);
            Vertexs.Add(vertices[2]);
            Vertexs.Add(vertices[3]);
        }

        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
            if (!IsDrawAreaValid())
                return;

            if (primitive.IsTextured && primitive.IsRawTextured)
            {
                bgrColor = 0x808080;
            }
            if (!primitive.IsTextured)
            {
                primitive.texpage = (ushort)(primitive.texpage | (1 << 11));
            }

            var color = vkColor.FromUInt32(bgrColor);

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

            vertices[0].v_color = color;
            vertices[1].v_color = color;
            vertices[2].v_color = color;
            vertices[3].v_color = color;

            SetDrawMode(primitive.texpage, primitive.clut, false);

            EnableSemiTransparency(primitive.IsSemiTransparent);

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

            for (var i = 0; i < vertices.Length; i++)
            {
                m_dirtyArea.Grow(vertices[i].v_pos.x, vertices[i].v_pos.y);

                vertices[i].v_clut.Value = primitive.clut;
                vertices[i].v_texPage.Value = primitive.texpage;
                vertices[i].v_pos.z = m_currentDepth;

                if (PGXP)
                {
                    PGXPVector.HighPos HighPos;
                    if (PGXPVector.Find(vertices[i].v_pos.x, vertices[i].v_pos.y, out HighPos))
                    {
                        vertices[i].v_pos_high = new Vector3((float)HighPos.x, (float)HighPos.y, (float)HighPos.z);
                        //Console.WriteLine($"[PGXP] PGXPVector Find x {HighPos.x}, y {HighPos.y}, invZ {HighPos.z}");
                    } else
                    {
                        //Console.WriteLine($"[PGXP] DrawRect PGXPVector Miss x {vertices[i].v_pos.x}, y {vertices[i].v_pos.y}");
                        vertices[i].v_pos_high = new Vector3((float)vertices[i].v_pos.x, (float)vertices[i].v_pos.y, (float)vertices[i].v_pos.z);
                    }
                }

                vertices[i].u_srcBlend = SrcBlend;
                vertices[i].u_destBlend = DstBlend;
                vertices[i].u_setMaskBit = setMaskBit;
                vertices[i].u_drawOpaquePixels = 1;
                vertices[i].u_drawTransparentPixels = 1;
                vertices[i].u_texWindowMask.X = TextureWindowXMask;
                vertices[i].u_texWindowMask.Y = TextureWindowYMask;
                vertices[i].u_texWindowOffset.X = TextureWindowXOffset;
                vertices[i].u_texWindowOffset.Y = TextureWindowYOffset;
                vertices[i].BlendMode = BlendMode;
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
            if (!IsDrawAreaValid())
                return;

            if (PGXPT)
            {
                int minX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
                int minY = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
                int maxX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
                int maxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

                if (maxX - minX > VRAM_WIDTH || maxY - minY > VRAM_HEIGHT)
                    return;
            }

            if (!primitive.IsTextured)
            {
                primitive.texpage = (ushort)(primitive.texpage | (1 << 11));
            }

            if (primitive.IsTextured && primitive.IsRawTextured)
            {
                c0 = c1 = c2 = 0x808080;
            } else if (!primitive.IsShaded)
            {
                c1 = c2 = c0;
            }

            SetDrawMode(primitive.texpage, primitive.clut, primitive.isDithered);

            EnableSemiTransparency(primitive.IsSemiTransparent);

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

            vertices[0].v_color = vkColor.FromUInt32(c0);
            vertices[1].v_color = vkColor.FromUInt32(c1);
            vertices[2].v_color = vkColor.FromUInt32(c2);

            for (var i = 0; i < vertices.Length; i++)
            {
                m_dirtyArea.Grow(vertices[i].v_pos.x, vertices[i].v_pos.y);

                vertices[i].v_clut.Value = primitive.clut;
                vertices[i].v_texPage.Value = primitive.texpage;
                vertices[i].v_pos.z = m_currentDepth;

                if (PGXP)
                {
                    PGXPVector.HighPos HighPos;
                    if (PGXPVector.Find(vertices[i].v_pos.x, vertices[i].v_pos.y, out HighPos))
                    {
                        vertices[i].v_pos_high = new Vector3((float)HighPos.x, (float)HighPos.y, (float)HighPos.z);
                        //Console.WriteLine($"[PGXP] PGXPVector Find x {HighPos.x}, y {HighPos.y}, invZ {HighPos.z}");
                    } else
                    {
                        //Console.WriteLine($"[PGXP] DrawTriangle PGXPVector Miss x {vertices[i].v_pos.x}, y {vertices[i].v_pos.y}");
                        vertices[i].v_pos_high = new Vector3((float)vertices[i].v_pos.x, (float)vertices[i].v_pos.y, (float)vertices[i].v_pos.z);
                    }
                }

                vertices[i].u_srcBlend = SrcBlend;
                vertices[i].u_destBlend = DstBlend;
                vertices[i].u_setMaskBit = setMaskBit;
                vertices[i].u_drawOpaquePixels = 1;
                vertices[i].u_drawTransparentPixels = 1;
                vertices[i].u_texWindowMask.X = TextureWindowXMask;
                vertices[i].u_texWindowMask.Y = TextureWindowYMask;
                vertices[i].u_texWindowOffset.X = TextureWindowXOffset;
                vertices[i].u_texWindowOffset.Y = TextureWindowYOffset;
                vertices[i].BlendMode = BlendMode;
            }

            Vertexs.AddRange(vertices);
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

        private bool InDrawArea(int x, int y)
        {
            return x >= DrawingAreaTopLeft.X &&
                   x <= DrawingAreaBottomRight.X &&
                   y >= DrawingAreaTopLeft.Y &&
                   y <= DrawingAreaBottomRight.Y;
        }

        private float GetNormalizedDepth()
        {
            return (float)m_currentDepth / (float)short.MaxValue;
        }

        public void SetSemiTransparencyMode(byte semiTransparencyMode)
        {
            if (m_semiTransparencyMode == semiTransparencyMode)
                return;

            m_semiTransparencyMode = semiTransparencyMode;

            if (m_semiTransparencyEnabled)
            {
                UpdateBlendMode();
            }
        }

        private void EnableSemiTransparency(bool enabled)
        {
            if (m_semiTransparencyEnabled != enabled)
            {
                m_semiTransparencyEnabled = enabled;

                UpdateBlendMode();
            }
        }

        public unsafe void UpdateBlendMode()
        {
            SrcBlend = 1.0f;
            DstBlend = 0.0f;
            vkGraphicsPipeline NewPipeline = drawMain;
            int NewBlendMode = 0;

            if (m_semiTransparencyEnabled)
            {
                switch (m_semiTransparencyMode)
                {
                    case 0:
                        SrcBlend = 0.5f;
                        DstBlend = 0.5f;
                        break;
                    case 1:
                        SrcBlend = 1.0f;
                        DstBlend = 1.0f;
                        break;
                    case 2:
                        SrcBlend = 1.0f;
                        DstBlend = 1.0f;
                        NewPipeline = drawSubtract;
                        break;
                    case 3:
                        SrcBlend = 0.25f;
                        DstBlend = 1.0f;
                        break;
                }
                NewBlendMode = m_semiTransparencyMode + 1;
            }

            if (BlendMode != NewBlendMode && (BlendMode == 3 || NewBlendMode == 3))
            {
                RecordVertexs();
            }

            CurrentPipeline = NewPipeline;
            HasTexture = !m_TexPage.TextureDisable;
            TransparencyEnabled = m_semiTransparencyEnabled;
            BlendMode = NewBlendMode;
        }

        private void UpdateReadTexture()
        {
            if (m_dirtyArea.Empty())
                return;

            ResetDirtyArea();
        }

        private void UpdateClut(ushort vclut)
        {
            m_clut.Value = vclut;

            int clutBaseX = m_clut.X * ClutBaseXMult;
            int clutBaseY = m_clut.Y * ClutBaseYMult;
            int clutWidth = ColorModeClutWidths[m_TexPage.TexturePageColors];
            int clutHeight = 1;

            m_clutArea = vkRectangle<int>.FromExtents(clutBaseX, clutBaseY, clutWidth, clutHeight);
        }

        private bool IntersectsTextureData(vkRectangle<int> bounds)
        {
            return !m_TexPage.TextureDisable &&
                   (m_textureArea.Intersects(bounds) || (m_TexPage.TexturePageColors < 2 && m_clutArea.Intersects(bounds)));
        }

        private vkRectangle<int> GetWrappedBounds(int left, int top, int width, int height)
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

            return vkRectangle<int>.FromExtents(left, top, width, height);
        }

        private void ResetDirtyArea()
        {
            m_dirtyArea.Left = VRAM_WIDTH;
            m_dirtyArea.Top = VRAM_HEIGHT;
            m_dirtyArea.Right = 0;
            m_dirtyArea.Bottom = 0;
        }

        private void GrowDirtyArea(vkRectangle<int> bounds)
        {
            // 需覆盖待处理的批处理多边形
            //if (m_dirtyArea.Intersects(bounds))
            //{
            //    RecordVertexs();
            //    Console.WriteLine($"[Vulkan GPU] GrowDirtyArea Intersects {bounds} - Write DrawAera.");
            //}

            m_dirtyArea.Grow(bounds);

            // 需覆盖当前的纹理数据
            if (IntersectsTextureData(bounds))
            {
                if (Vertexs.Count > 0)
                {
                    //10fps损失
                    RecordVertexs();
                    EndRenderPass();
                    StartRenderPass();
                    //Console.WriteLine($"[Vulkan GPU] GrowDirtyArea Intersects {bounds} - Update Texture.");
                }
            }
        }

        private unsafe void vramToDrawTexture()
        {
            if (textureBlocks.Count == 0)
                return;

            List<VkImageBlit> regions = new List<VkImageBlit>();

            foreach (var block in textureBlocks)
            {
                int sx = block.x;
                int sy = block.y;
                int sw = block.width;
                int sh = block.height;

                int dx = sx * resolutionScale;
                int dy = sy * resolutionScale;
                int dw = sw * resolutionScale;
                int dh = sh * resolutionScale;

                VkImageBlit region = new VkImageBlit
                {
                    srcSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.Color,
                        mipLevel = 0,
                        baseArrayLayer = 0,
                        layerCount = 1
                    },
                    srcOffsets_0 = new VkOffset3D { x = sx, y = sy, z = 0 },
                    srcOffsets_1 = new VkOffset3D { x = sx + sw, y = sy + sh, z = 1 },

                    dstSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.Color,
                        mipLevel = 0,
                        baseArrayLayer = 0,
                        layerCount = 1
                    },
                    dstOffsets_0 = new VkOffset3D { x = dx, y = dy, z = 0 },
                    dstOffsets_1 = new VkOffset3D { x = dx + dw, y = dy + dh, z = 1 }
                };

                regions.Add(region);
            }

            textureBlocks.Clear();

            VkCommandBuffer cmd = DrawCMD.CMD[0];

            Device.BeginCommandBuffer(cmd);

            samplerTexture.layout = Device.TransitionImageLayout(
                cmd,
                samplerTexture,
                samplerTexture.layout,
                VkImageLayout.TransferSrcOptimal
            );

            drawTexture.layout = Device.TransitionImageLayout(
                cmd,
                drawTexture,
                drawTexture.layout,
                VkImageLayout.TransferDstOptimal
            );

            fixed (VkImageBlit* pRegions = regions.ToArray())
            {
                vkCmdBlitImage(
                    cmd,
                    samplerTexture.image, VkImageLayout.TransferSrcOptimal,
                    drawTexture.image, VkImageLayout.TransferDstOptimal,
                    (uint)regions.Count,
                    pRegions,
                    VkFilter.Nearest
                );
            }

            samplerTexture.layout = Device.TransitionImageLayout(
                cmd,
                samplerTexture,
                samplerTexture.layout,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            drawTexture.layout = Device.TransitionImageLayout(
                cmd,
                drawTexture,
                drawTexture.layout,
                VkImageLayout.ColorAttachmentOptimal
            );

            Device.EndAndWaitCommandBuffer(cmd);

            //Console.WriteLine($"[Vulkan GPU] Texture Sample TO Draw {regions.Count}");
        }

        private unsafe void CopyTextureRect(vkTexture srcTexture, int srcx, int srcy, int dstx, int dsty, int width, int height)
        {
            VkCommandBuffer cmd = DrawCMD.CMD[0];

            Device.BeginCommandBuffer(cmd);

            VkImageCopy copyRegion = new VkImageCopy
            {
                srcSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                dstSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                srcOffset = new VkOffset3D { x = srcx, y = srcy, z = 0 },
                dstOffset = new VkOffset3D { x = dstx, y = dsty, z = 0 },
                extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 }
            };

            VkImageLayout oldsrclayout = srcTexture.layout;

            srcTexture.layout = Device.TransitionImageLayout(
                cmd,
                srcTexture,
                srcTexture.layout,
                VkImageLayout.TransferDstOptimal
            );

            vkCmdCopyImage(
                cmd,
                srcTexture.image,
                VkImageLayout.TransferDstOptimal,
                srcTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &copyRegion);

            srcTexture.layout = Device.TransitionImageLayout(
                cmd,
                srcTexture,
                srcTexture.layout,
                oldsrclayout
            );

            Device.EndAndWaitCommandBuffer(cmd);
        }

        private unsafe void CopyTexture(vkTexture srcTexture, vkTexture dstTexture, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            VkCommandBuffer cmd = DrawCMD.CMD[0];

            Device.BeginCommandBuffer(cmd);

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

            VkImageLayout oldsrclayout = srcTexture.layout;
            VkImageLayout olddstlayout = dstTexture.layout;

            srcTexture.layout = Device.TransitionImageLayout(
                cmd,
                srcTexture,
                srcTexture.layout,
                VkImageLayout.TransferSrcOptimal
            );

            dstTexture.layout = Device.TransitionImageLayout(
                cmd,
                dstTexture,
                dstTexture.layout,
                VkImageLayout.TransferDstOptimal
            );

            vkCmdBlitImage(
                cmd,
                srcTexture.image,
                VkImageLayout.TransferSrcOptimal,
                dstTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &blitRegion,
                VkFilter.Nearest
            );

            srcTexture.layout = Device.TransitionImageLayout(
                cmd,
                srcTexture,
                srcTexture.layout,
                oldsrclayout
            );

            dstTexture.layout = Device.TransitionImageLayout(
                cmd,
                dstTexture,
                dstTexture.layout,
                olddstlayout
            );

            Device.EndAndWaitCommandBuffer(cmd);
        }

        public unsafe void UpdateVert()
        {
            void* mappedData;

            vkMapMemory(Device.device, vertexUBO.stagingMemory, 0, WholeSize, 0, &mappedData);

            //Unsafe.Copy(mappedData, ref drawVert);
            var vars = drawVert;
            Buffer.MemoryCopy(&vars, mappedData, sizeof(drawvertUBO), sizeof(drawvertUBO));

            vkUnmapMemory(Device.device, vertexUBO.stagingMemory);
        }

        public unsafe void UpdateFrag(int fragIdx = 0)
        {
            uint dynamicOffset = fragIdx == 0 ? 0 : (uint)fragIdx * ((uint)Marshal.SizeOf<drawfragUBO>() + minUboAlignment - 1) & ~(minUboAlignment - 1);

            var vars = drawFrag;
            Buffer.MemoryCopy(&vars, drawFragData + dynamicOffset, sizeof(drawfragUBO), sizeof(drawfragUBO));

        }

    }
}
