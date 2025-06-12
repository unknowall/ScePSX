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
        //bool m_dither = false;
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

        //List<Vertex> Vertexs = new List<Vertex>();

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

        VulkanDevice Device;

        VkRenderPass drawPass, renderPass;

        vkSwapchain renderChain;

        vkBuffer VaoBuffer, vertexUBO, fragmentUBO;
        vkTexture samplerTexture, drawTexture;
        VkFramebuffer drawFramebuff;
        vkCMDS renderCmd, DrawCMD;

        vkGraphicsPipeline out24Pipeline, out16Pipeline;
        vkGraphicsPipeline drawAvgBlend, drawAddBlend, drawSubtractBlend, drawConstantBlend, drawNoBlend;

        VkDescriptorPool outdescriptorPool, drawdescriptorPool;
        VkDescriptorSet outDescriptorSet, drawDescriptorSet;
        VkDescriptorSetLayout outDescriptorLayout, drawDescriptorLayout;

        unsafe void* samplerData;

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

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        struct SrcRectUBO
        {
            [FieldOffset(0)] public int x;
            [FieldOffset(4)] public int y;
            [FieldOffset(8)] public int w;
            [FieldOffset(12)] public int h;

            public SrcRectUBO(int x, int y, int w, int h)
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }
        }

        private static readonly byte[] LookupTable1555to8888 = new byte[32];

        public enum BlendMode
        {
            Opaque,     // 不透明
            AlphaBlend, // 普通透明混合
            Additive,   // 加法混合
            Subtract,   // 减法混合
            Quarter,    // 四分之一混合
        }

        BlendMode CurrentBlendMode;

        public struct BlendParams
        {
            public vkGraphicsPipeline Pipeline;
            public float SrcFactor;
            public float DstFactor;
            public bool RequireDoublePass;
        }

        private Dictionary<BlendMode, BlendParams> blendPresets = new();

        BlendParams CurrentBlend;

        public struct PipelineDrawBlock
        {
            public BlendParams Blend;
            public BlendMode mode;

            public VkRect2D Scissor;

            public bool HasTexture;
            public bool semiTransparencyEnabled;

            public List<Vertex> Vertexs;
        }

        ulong vaoOffset = 0;
        uint currentBlockFirstVertex, currentBlockVertexCount;
        VkCommandBuffer CurrentDrawCMD;
        PipelineDrawBlock CurrentBlock = new PipelineDrawBlock();

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
                VkImageLayout.ColorAttachmentOptimal
                );

            renderChain = Device.CreateSwapChain(renderPass, NullRenderer.ClientWidth, NullRenderer.ClientHeight);

            frameFences = new FrameFence[renderChain.Images.Count];

            renderCmd = Device.CreateCommandBuffers(renderChain.Images.Count);

            DrawCMD = Device.CreateCommandBuffers(2);

            VaoBuffer = Device.CreateBuffer((ulong)(2048 * 10 * sizeof(Vertex)), VkBufferUsageFlags.VertexBuffer | VkBufferUsageFlags.TransferDst);

            void* mappedData;
            vkMapMemory(Device.device, VaoBuffer.stagingMemory, 0, WholeSize, 0, &mappedData);
            VaoBuffer.mappedData = mappedData;

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

            void* samplerdataptr;
            vkMapMemory(Device.device, samplerTexture.imagememory, 0, WholeSize, 0, &samplerdataptr);
            samplerData = samplerdataptr;

            Console.WriteLine($"[Vulkan GPU] samplerTexture 0x{samplerTexture.image.Handle:X}");

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

            Device.EndAndWaitCommandBuffer(DrawCMD.CMD[0]);

            (drawTexture, drawFramebuff) = CreateDrawTexture();

            var vertexType = typeof(Vertex);
            VkVertexInputAttributeDescription[] drawAttributes = new VkVertexInputAttributeDescription[]
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

            minUboAlignment = (uint)Device.GetMinUniformBufferAlignment();

            uint uboSize = (uint)Marshal.SizeOf<drawvertUBO>();
            vertexUBO = Device.CreateBuffer(uboSize * 2, VkBufferUsageFlags.UniformBuffer | VkBufferUsageFlags.TransferDst);

            uboSize = (uint)Marshal.SizeOf<drawfragUBO>();
            fragmentUBO = Device.CreateBuffer(uboSize * 2, VkBufferUsageFlags.UniformBuffer | VkBufferUsageFlags.TransferDst);

            vkMapMemory(Device.device, fragmentUBO.stagingMemory, 0, WholeSize, 0, &mappedData);
            drawFragData = (byte*)mappedData;

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

            var drawVertbytes = Device.LoadShaderFile("./Shaders/draw.vert.spv");
            var drawFragbytes = Device.LoadShaderFile("./Shaders/draw.frag.spv");

            // 主绘制管线（无混合）
            drawNoBlend = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes
            );

            // 平均混合（Blend Mode 1）
            drawAvgBlend = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                GetBlendState(1),
                default,
                VkPrimitiveTopology.TriangleList,
                new float[] { 0.5f, 0.5f, 0.5f, 0.5f }
            );

            // 加法混合（Blend Mode 2）
            drawAddBlend = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                GetBlendState(2),
                default,
                VkPrimitiveTopology.TriangleList,
                new float[] { 1.0f, 1.0f, 1.0f, 1.0f }
            );

            // 减法混合（Blend Mode 3）
            drawSubtractBlend = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                GetBlendState(3),
                default,
                VkPrimitiveTopology.TriangleList,
                new float[] { 1.0f, 1.0f, 1.0f, 1.0f }
            );

            // 四分之一混合（Blend Mode 4）
            drawConstantBlend = Device.CreateGraphicsPipeline(
                drawPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                GetBlendState(4),
                default,
                VkPrimitiveTopology.TriangleList,
                new float[] { 0.25f, 0.25f, 0.25f, 0.25f } // 所有通道使用 0.25
            );

            ///////////////////////////////////////////////////////

            outdescriptorPool = Device.CreateDescriptorPool(
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

            outDescriptorLayout = Device.CreateDescriptorSetLayout(new[]
            {
                new VkDescriptorSetLayoutBinding { // CombinedImageSampler (binding=0)
                    binding = 0,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    stageFlags = VkShaderStageFlags.Fragment
                }
            });

            outDescriptorSet = Device.AllocateDescriptorSet(outDescriptorLayout, outdescriptorPool);

            imageInfo = new VkDescriptorImageInfo
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
                        dstSet = outDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&imageInfo
                    }
                 },
                 outDescriptorSet
            );

            var pushConstantRanges = new VkPushConstantRange[1];
            pushConstantRanges[0] = new VkPushConstantRange
            {
                stageFlags = VkShaderStageFlags.Fragment,
                offset = 0,
                size = (uint)sizeof(SrcRectUBO)
            };

            var out24Vert = Device.LoadShaderFile("./Shaders/out24.vert.spv");
            var out24Frag = Device.LoadShaderFile("./Shaders/out24.frag.spv");
            out24Pipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                outDescriptorLayout,
                out24Vert,
                out24Frag,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                default,
                default,
                default,
                pushConstantRanges,
                VkPrimitiveTopology.TriangleStrip
            );

            var out16Vert = Device.LoadShaderFile("./Shaders/out16.vert.spv");
            var out16Frag = Device.LoadShaderFile("./Shaders/out16.frag.spv");

            out16Pipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT),
                outDescriptorLayout,
                out16Vert,
                out16Frag,
                VRAM_WIDTH, VRAM_HEIGHT,
                VkSampleCountFlags.Count1,
                default,
                default,
                default,
                pushConstantRanges,
                VkPrimitiveTopology.TriangleStrip
            );

            CreateSyncObjects();

            InitializeBlendPresets();

            minAlignment = (uint)Device.deviceProperties.limits.minMemoryMapAlignment;
            alignedRowPitch = (VRAM_WIDTH * 4 + (int)minAlignment - 1) & ~((int)minAlignment - 1);

            viewport = new VkViewport { width = VRAM_WIDTH, height = VRAM_HEIGHT, maxDepth = 1.0f };
            scissor = new VkRect2D { extent = new VkExtent2D(VRAM_WIDTH, VRAM_HEIGHT) };

            m_realColor = true;

            drawVert.u_resolutionScale = 1.0f;

            UpdateVert();

            drawFrag.u_realColor = 1;
            drawFrag.u_dither = 0;

            UpdateFrag();

            SetViewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            //IRScale = 2;
            //SetResolutionScale(IRScale);

            StartRenderPass();

            Console.WriteLine("[Vulkan GPU] Initialization Complete.");
        }

        private void InitializeBlendPresets()
        {
            blendPresets.Add(BlendMode.Opaque, new BlendParams
            {
                Pipeline = drawNoBlend,
                SrcFactor = 1.0f,
                DstFactor = 0.0f,
                RequireDoublePass = false
            });

            blendPresets.Add(BlendMode.AlphaBlend, new BlendParams
            {
                Pipeline = drawAvgBlend,
                SrcFactor = 0.5f,
                DstFactor = 0.5f,
                RequireDoublePass = false
            });

            blendPresets.Add(BlendMode.Additive, new BlendParams
            {
                Pipeline = drawAddBlend,
                SrcFactor = 1.0f,
                DstFactor = 1.0f,
                RequireDoublePass = false
            });

            // PS1半透明模式2需双次绘制
            blendPresets.Add(BlendMode.Subtract, new BlendParams
            {
                Pipeline = drawSubtractBlend,
                SrcFactor = 1.0f,
                DstFactor = 1.0f,
                RequireDoublePass = true
            });

            blendPresets.Add(BlendMode.Quarter, new BlendParams
            {
                Pipeline = drawConstantBlend,
                SrcFactor = 0.25f,
                DstFactor = 1.0f,
                RequireDoublePass = false
            });

            CurrentBlend = blendPresets[BlendMode.Opaque];
        }

        private VkPipelineColorBlendAttachmentState GetBlendState(int mode)
        {
            VkPipelineColorBlendAttachmentState blendAttachment = new VkPipelineColorBlendAttachmentState();

            blendAttachment.blendEnable = mode != 0;

            switch (mode)
            {
                case 0: // 不混合（No Blending）
                    blendAttachment.blendEnable = false;
                    break;

                case 1: // 平均混合（Blend Mode 1）
                    blendAttachment.srcColorBlendFactor = VkBlendFactor.ConstantColor; // 值为 0.5
                    blendAttachment.dstColorBlendFactor = VkBlendFactor.ConstantColor; // 值为 0.5
                    blendAttachment.colorBlendOp = VkBlendOp.Add;
                    break;

                case 2: // 加法混合（Blend Mode 2）
                    blendAttachment.srcColorBlendFactor = VkBlendFactor.One;
                    blendAttachment.dstColorBlendFactor = VkBlendFactor.One;
                    blendAttachment.colorBlendOp = VkBlendOp.Add;
                    break;

                case 3: // 减法混合（Blend Mode 3）
                    blendAttachment.srcColorBlendFactor = VkBlendFactor.One;
                    blendAttachment.dstColorBlendFactor = VkBlendFactor.One;
                    blendAttachment.colorBlendOp = VkBlendOp.ReverseSubtract;
                    break;

                case 4: // 四分之一混合（Blend Mode 4）
                    blendAttachment.srcColorBlendFactor = VkBlendFactor.ConstantColor; // 值为 0.25
                    blendAttachment.dstColorBlendFactor = VkBlendFactor.One;
                    blendAttachment.colorBlendOp = VkBlendOp.Add;
                    break;

                default:
                    throw new ArgumentException("Invalid blend mode");
            }

            blendAttachment.srcAlphaBlendFactor = VkBlendFactor.One;
            blendAttachment.dstAlphaBlendFactor = VkBlendFactor.Zero;
            blendAttachment.alphaBlendOp = VkBlendOp.Add;

            blendAttachment.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;

            return blendAttachment;
        }

        private (vkTexture tex, VkFramebuffer fb) CreateDrawTexture()
        {
            var tex = Device.CreateTexture(
                VRAM_WIDTH * resolutionScale, VRAM_HEIGHT * resolutionScale,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc,
                VkFilter.Linear,
                VkSamplerAddressMode.ClampToEdge,
                VkSamplerMipmapMode.Linear,
                VkMemoryPropertyFlags.DeviceLocal,
                VkImageTiling.Linear
                );

            Console.WriteLine($"[Vulkan GPU] drawTexture 0x{tex.image.Handle:X}");

            Device.BeginCommandBuffer(DrawCMD.CMD[0]);

            tex.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                tex,
                tex.layout,
                VkImageLayout.ColorAttachmentOptimal
            );

            Device.EndAndWaitCommandBuffer(DrawCMD.CMD[0]);

            var fb = Device.CreateFramebuffer(drawPass, tex.imageview, (uint)(VRAM_WIDTH * resolutionScale), (uint)(VRAM_HEIGHT * resolutionScale));

            return (tex, fb);
        }

        private void CreateSyncObjects()
        {
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

            //SubmitRun = false;
            //SubmitTask.Wait();

            vkQueueWaitIdle(Device.presentQueue);
            vkQueueWaitIdle(Device.graphicsQueue);

            vkUnmapMemory(Device.device, fragmentUBO.stagingMemory);
            vkUnmapMemory(Device.device, samplerTexture.imagememory);
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

            vkDestroyDescriptorSetLayout(Device.device, outDescriptorLayout, null);
            vkDestroyDescriptorSetLayout(Device.device, drawDescriptorLayout, null);

            vkDestroyDescriptorPool(Device.device, outdescriptorPool, 0);
            vkDestroyDescriptorPool(Device.device, drawdescriptorPool, 0);

            Device.DestroyGraphicsPipeline(out16Pipeline);
            Device.DestroyGraphicsPipeline(out24Pipeline);
            Device.DestroyGraphicsPipeline(drawNoBlend);
            Device.DestroyGraphicsPipeline(drawConstantBlend);
            Device.DestroyGraphicsPipeline(drawSubtractBlend);
            Device.DestroyGraphicsPipeline(drawAvgBlend);
            Device.DestroyGraphicsPipeline(drawAddBlend);

            Device.DestroyTexture(drawTexture);
            Device.DestroyTexture(samplerTexture);

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

            vkTexture newDrawTexture;
            VkFramebuffer newDrawFramebuffer;
            (newDrawTexture, newDrawFramebuffer) = CreateDrawTexture();

            CopyTexture(drawTexture, newDrawTexture, oldWidth, oldHeight, newWidth, newHeight);

            Device.DestroyFramebuffer(drawFramebuff);

            Device.DestroyTexture(drawTexture);

            drawTexture = newDrawTexture;

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
                        dstSet = outDescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.CombinedImageSampler,
                        pImageInfo =&imageInfo
                    }
                 },
                 outDescriptorSet
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

            SubmitRecord();

            SubmitRenderPass();

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

        bool is24bited = false;

        private unsafe void RenderToSwapchain(VkCommandBuffer cmd, uint chainidx, bool is24bit)
        {
            Device.BeginCommandBuffer(cmd);

            drawTexture.layout = Device.TransitionImageLayout(
                cmd,
                drawTexture,
                drawTexture.layout,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            if (ViewVRam || is24bit)
            {
                is24bited = true;
                vkQueueWaitIdle(Device.graphicsQueue);
                Device.UpdateDescriptorImage(outDescriptorSet, samplerTexture, 0);
            } else if (is24bited)
            {
                is24bited = false;
                vkQueueWaitIdle(Device.graphicsQueue);
                Device.UpdateDescriptorImage(outDescriptorSet, drawTexture, 0);
            } else if (!is24bit)
            {
                m_vramDisplayArea.x = m_vramDisplayArea.x * resolutionScale;

                m_vramDisplayArea.y = m_vramDisplayArea.y * resolutionScale;

                m_vramDisplayArea.width = ViewVRam ?
                    VRAM_WIDTH * resolutionScale :
                    m_vramDisplayArea.width * resolutionScale;

                m_vramDisplayArea.height = ViewVRam ?
                    VRAM_HEIGHT * resolutionScale :
                    m_vramDisplayArea.height * resolutionScale;
            }

            Device.BeginRenderPass(cmd, renderPass,
                    renderChain.framebuffes[(int)chainidx],
                    (int)renderChain.Extent.width,
                    (int)renderChain.Extent.height,
                    true, 0, 0, 0, 1);

            vkGraphicsPipeline outPipeline = is24bit ? out24Pipeline : out16Pipeline;
            vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, outPipeline.pipeline);

            SrcRectUBO u_srcRect = new SrcRectUBO(
                m_vramDisplayArea.x,
                m_vramDisplayArea.y,
                m_vramDisplayArea.width,
                m_vramDisplayArea.height
            );

            vkCmdPushConstants(
                cmd,
                outPipeline.layout,
                VkShaderStageFlags.Fragment,
                0,
                (uint)sizeof(SrcRectUBO),
                &u_srcRect
            );

            Device.BindDescriptorSet(cmd, outPipeline, outDescriptorSet);

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

            SetScissor(x, y, width, height);

            //CurrentBlock.Scissor = scissor;
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
            GrowDirtyArea(GetWrappedBounds(left, top, width, height));

            bool wrapX = (left + width) > VRAM_WIDTH;
            bool wrapY = (top + height) > VRAM_HEIGHT;

            if (wrapX || wrapY)
            {
                int width2 = wrapX ? (left + width) % VRAM_WIDTH : 0;
                int height2 = wrapY ? (top + height) % VRAM_HEIGHT : 0;
                int width1 = width - width2;
                int height1 = height - height2;

                Console.WriteLine($"[Vulkan GPU] FillRectVRAM {left + width},{top + height} wrapX {wrapX} & wrapY {wrapY} | {width2},{height2} [{width1},{height1}]");
            }

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

            var color = vkColor.FromUInt32(colorval);

            Vertex[] vertices = new Vertex[4];

            vertices[0].v_pos.x = (short)left;
            vertices[0].v_pos.y = (short)top;

            vertices[1].v_pos.x = (short)(left + width);
            vertices[1].v_pos.y = (short)top;

            vertices[2].v_pos.x = (short)left;
            vertices[2].v_pos.y = (short)(top + height);

            vertices[3].v_pos.x = (short)(left + width);
            vertices[3].v_pos.y = (short)(top + height);

            vertices[0].v_color = color;
            vertices[1].v_color = color;
            vertices[2].v_color = color;
            vertices[3].v_color = color;

            ushort texpage = (ushort)(1 << 11);

            SetDrawMode(texpage, 0, false);

            EnableSemiTransparency(false);

            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i].v_clut.Value = 0;
                vertices[i].v_texPage.Value = texpage;
                vertices[i].v_pos.z = m_currentDepth;

                vertices[i].u_srcBlend = CurrentBlend.SrcFactor;
                vertices[i].u_destBlend = CurrentBlend.DstFactor;
                vertices[i].u_setMaskBit = setMaskBit;
                vertices[i].u_drawOpaquePixels = 1;
                vertices[i].u_drawTransparentPixels = 1;
                vertices[i].u_texWindowMask.X = 0;
                vertices[i].u_texWindowMask.Y = 0;
                vertices[i].u_texWindowOffset.X = 0;
                vertices[i].u_texWindowOffset.Y = 0;
                vertices[i].BlendMode = (int)CurrentBlendMode;

                if (PGXP)
                {
                    vertices[i].v_pos_high = new Vector3((float)vertices[i].v_pos.x, (float)vertices[i].v_pos.y, (float)vertices[i].v_pos.z);
                }
            }

            CurrentBlock.Vertexs.Add(vertices[0]);
            CurrentBlock.Vertexs.Add(vertices[1]);
            CurrentBlock.Vertexs.Add(vertices[2]);

            CurrentBlock.Vertexs.Add(vertices[1]);
            CurrentBlock.Vertexs.Add(vertices[2]);
            CurrentBlock.Vertexs.Add(vertices[3]);

            SubmitRecord();

            //Console.WriteLine($"[Vulkan GPU] FillRectVRAM {left + width},{top + height}");
        }

        public unsafe void CopyRectVRAMtoVRAM(ushort srcX, ushort srcY, ushort destX, ushort destY, ushort width, ushort height)
        {
            if (srcX == destX && srcY == destY)
                return;

            var srcBounds = vkRectangle<int>.FromExtents(srcX, srcY, width, height);
            var destBounds = vkRectangle<int>.FromExtents(destX, destY, width, height);

            if (m_dirtyArea.Intersects(srcBounds))
            {
                //Console.WriteLine($"[Vulkan GPU] UpdateReadTexture");
                UpdateReadTexture();
                m_dirtyArea.Grow(destBounds);
            } else
            {
                GrowDirtyArea(destBounds);
            }

            //Console.WriteLine($"[Vulkan GPU] CopyRectVRAMtoVRAM {srcX},{srcY} -> {destX},{destY} [{width},{height}]");

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

            CopyRectCPUtoVRAM(destX, destY, width, height);
        }

        public unsafe void CopyRectVRAMtoCPU(int left, int top, int width, int height)
        {
            var readBounds = GetWrappedBounds(left, top, width, height);

            if (m_dirtyArea.Intersects(readBounds))
            {

            }

            int readWidth = readBounds.GetWidth();
            int readHeight = readBounds.GetHeight();

            //Console.WriteLine($"[Vulkan GPU] CopyRectVRAMtoCPU {left},{top} [{width},{height}]");

            vkGetImageMemoryRequirements(Device.device, samplerTexture.image, out var memRequirements);
            uint minAlignment = (uint)memRequirements.alignment;

            int srcBytesPerPixel = 4;
            uint srcRowPitch = (uint)((readWidth * srcBytesPerPixel + minAlignment - 1) & ~((int)minAlignment - 1));

            int destBytesPerPixel = 2;
            byte* destBasePtr = (byte*)VRAM + (readBounds.Left + readBounds.Top * VRAM_WIDTH) * destBytesPerPixel;

            for (int row = 0; row < readHeight; row++)
            {
                byte* srcRowStart = (byte*)samplerData + row * srcRowPitch;
                byte* destRowStart = destBasePtr + row * VRAM_WIDTH * destBytesPerPixel;

                for (int col = 0; col < readWidth; col++)
                {
                    uint* srcPixel = (uint*)(srcRowStart + col * srcBytesPerPixel);
                    uint color = *srcPixel;

                    uint a = (color >> 24) & 0xFFU;
                    uint r = (color >> 16) & 0xFFU;
                    uint g = (color >> 8) & 0xFFU;
                    uint b = color & 0xFFU;

                    ushort pixel = (ushort)(
                        ((a > 0x80U ? 1U : 0U) << 15) |
                        ((r >> 3) << 10) |
                        ((g >> 3) << 5) |
                        (b >> 3)
                    );

                    byte* destPixel = destRowStart + col * destBytesPerPixel;
                    //destPixel[0] = (byte)(pixel & 0xFF);
                    //destPixel[1] = (byte)(pixel >> 8);
                    *(ushort*)destPixel = pixel;
                }
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

            ulong rawByteOffset = (ulong)(y * VRAM_WIDTH + x) * 4;
            ulong byteOffset = rawByteOffset / minAlignment * minAlignment;
            ulong bufferSize = (ulong)(alignedRowPitch * height) + (rawByteOffset - byteOffset);
            bufferSize = Math.Min(bufferSize, 0x200000 - byteOffset);

            //void* mappedData;
            //vkMapMemory(Device.device, samplerTexture.imagememory, byteOffset, bufferSize, 0, &mappedData);

            int offsetDelta = (int)(rawByteOffset - byteOffset);
            byte* alignedDstStart = (byte*)samplerData + byteOffset + offsetDelta;

            int srcRowPitch = width * 4;
            for (int row = 0; row < height; row++)
            {
                byte* srcPtr = (byte*)convertedData + row * srcRowPitch;
                byte* dstPtr = alignedDstStart + row * alignedRowPitch;

                Buffer.MemoryCopy(
                    srcPtr,
                    dstPtr,
                    srcRowPitch,
                    srcRowPitch
                );
            }
            //vkUnmapMemory(Device.device, samplerTexture.imagememory);
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
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;
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
            //if (m_realColor)
            //    dither = false;

            //if (m_dither != dither)
            //{
            //    DrawBatch();
            //    m_dither = dither;
            //    drawFrag.u_dither = m_dither ? 1 : 0;
            //    UpdateFrag();
            //}

            if (m_TexPage.Value != vtexPage)
            {
                m_TexPage.Value = vtexPage;

                SetSemiTransparencyMode(m_TexPage.SemiTransparencymode);

                CurrentBlock.HasTexture = m_TexPage.TextureDisable == false;

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
            //Console.WriteLine($"[Vulkan GPU] PIPELINE ----------------------------------------------- ");

            vaoOffset = 0;

            CurrentBlock.Blend = CurrentBlend;
            CurrentBlock.mode = CurrentBlendMode;
            CurrentBlock.Scissor = scissor;
            CurrentBlock.semiTransparencyEnabled = m_semiTransparencyEnabled;
            CurrentBlock.Vertexs = new List<Vertex>();

            SetScissor(0, 0, VRAM_WIDTH, VRAM_HEIGHT);

            CurrentDrawCMD = DrawCMD.CMD[1];

            Device.BeginCommandBuffer(CurrentDrawCMD);

            Device.BeginRenderPass(CurrentDrawCMD, drawPass, drawFramebuff, drawTexture.width, drawTexture.height, false);

            vkCmdBindVertexBuffers(CurrentDrawCMD, 0, 1, ref VaoBuffer.stagingBuffer, ref vaoOffset);

            vkCmdSetViewport(CurrentDrawCMD, 0, 1, ref viewport);

            //vkCmdSetScissor(CurrentDrawCMD, 0, 1, ref scissor);
        }

        public void SubmitRenderPass()
        {
            vkCmdEndRenderPass(CurrentDrawCMD);

            Device.EndAndWaitCommandBuffer(CurrentDrawCMD);
        }

        public void SubmitRecord()
        {
            //Console.WriteLine($"[Vulkan GPU] SubmitRecord mode {m_semiTransparencyMode}, {CurrentBlock.mode.ToString()}, {CurrentBlock.Vertexs.Count} Vertexs, {CurrentBlock.HasTexture}");

            RecordCMD(CurrentBlock);

            CurrentBlock.Blend = CurrentBlend;
            CurrentBlock.mode = CurrentBlendMode;
            CurrentBlock.Scissor = scissor;
            CurrentBlock.semiTransparencyEnabled = m_semiTransparencyEnabled;
            CurrentBlock.Vertexs.Clear();
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
            currentBlockVertexCount = (uint)sourceSpan.Length;

            vaoOffset += size;
        }

        public unsafe void RecordCMD(PipelineDrawBlock block)
        {
            if (block.Vertexs.Count == 0)
                return;

            //Console.WriteLine($"[Vulkan GPU] RecordCMD: RequireDoublePass {block.BlendParam.RequireDoublePass} , mode {block.mode} , {block.Vertexs.Count} vertices，Offset {vaoOffset}");

            UploadVertexs(block.Vertexs);

            vkCmdSetScissor(CurrentDrawCMD, 0, 1, ref block.Scissor);

            // 模式2需先绘制不透明部分
            if (block.Blend.RequireDoublePass && block.HasTexture && block.semiTransparencyEnabled)
            {
                Device.BindGraphicsPipeline(CurrentDrawCMD, drawNoBlend, false);

                Device.BindDescriptorSet(CurrentDrawCMD, drawNoBlend, drawDescriptorSet);

                vkCmdDraw(CurrentDrawCMD, (uint)block.Vertexs.Count, 1, currentBlockFirstVertex, 0);

                var span = CollectionsMarshal.AsSpan(block.Vertexs);
                foreach (ref Vertex vertex in span)
                {
                    vertex.u_drawOpaquePixels = 0;
                    vertex.u_drawTransparentPixels = 1;
                }

                //Console.WriteLine($"[Vulkan GPU] UploadVertexs MODE 2, {block.Vertexs.Count} vertices，Offset {vaoOffset}");

                UploadVertexs(block.Vertexs);
            }

            // 主绘制（处理透明或普通混合）
            Device.BindGraphicsPipeline(CurrentDrawCMD, block.Blend.Pipeline, false);

            Device.BindDescriptorSet(CurrentDrawCMD, block.Blend.Pipeline, drawDescriptorSet);

            vkCmdDraw(CurrentDrawCMD, (uint)block.Vertexs.Count, 1, currentBlockFirstVertex, 0);
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

                vertices[i].u_srcBlend = CurrentBlend.SrcFactor;
                vertices[i].u_destBlend = CurrentBlend.DstFactor;
                vertices[i].u_setMaskBit = setMaskBit;
                if (CurrentBlend.RequireDoublePass && !m_TexPage.TextureDisable && m_semiTransparencyEnabled)
                {
                    vertices[i].u_drawOpaquePixels = 1;
                    vertices[i].u_drawTransparentPixels = 0;
                } else
                {
                    vertices[i].u_drawOpaquePixels = CurrentBlend.RequireDoublePass ? 0 : 1;
                    vertices[i].u_drawTransparentPixels = 1;
                }
                vertices[i].u_texWindowMask.X = TextureWindowXMask;
                vertices[i].u_texWindowMask.Y = TextureWindowYMask;
                vertices[i].u_texWindowOffset.X = TextureWindowXOffset;
                vertices[i].u_texWindowOffset.Y = TextureWindowYOffset;
                vertices[i].BlendMode = (int)CurrentBlendMode;
            }

            CurrentBlock.Vertexs.Add(vertices[0]);
            CurrentBlock.Vertexs.Add(vertices[1]);
            CurrentBlock.Vertexs.Add(vertices[2]);

            CurrentBlock.Vertexs.Add(vertices[1]);
            CurrentBlock.Vertexs.Add(vertices[2]);
            CurrentBlock.Vertexs.Add(vertices[3]);
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

                vertices[i].u_srcBlend = CurrentBlend.SrcFactor;
                vertices[i].u_destBlend = CurrentBlend.DstFactor;
                vertices[i].u_setMaskBit = setMaskBit;
                if (CurrentBlend.RequireDoublePass && !m_TexPage.TextureDisable && m_semiTransparencyEnabled)
                {
                    vertices[i].u_drawOpaquePixels = 1;
                    vertices[i].u_drawTransparentPixels = 0;
                } else
                {
                    vertices[i].u_drawOpaquePixels = CurrentBlend.RequireDoublePass ? 0 : 1;
                    vertices[i].u_drawTransparentPixels = 1;
                }
                vertices[i].u_texWindowMask.X = TextureWindowXMask;
                vertices[i].u_texWindowMask.Y = TextureWindowYMask;
                vertices[i].u_texWindowOffset.X = TextureWindowXOffset;
                vertices[i].u_texWindowOffset.Y = TextureWindowYOffset;
                vertices[i].BlendMode = (int)CurrentBlendMode;
            }

            CurrentBlock.Vertexs.Add(vertices[0]);
            CurrentBlock.Vertexs.Add(vertices[1]);
            CurrentBlock.Vertexs.Add(vertices[2]);

            CurrentBlock.Vertexs.Add(vertices[1]);
            CurrentBlock.Vertexs.Add(vertices[2]);
            CurrentBlock.Vertexs.Add(vertices[3]);
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

                vertices[i].u_srcBlend = CurrentBlend.SrcFactor;
                vertices[i].u_destBlend = CurrentBlend.DstFactor;
                vertices[i].u_setMaskBit = setMaskBit;
                if (CurrentBlend.RequireDoublePass && !m_TexPage.TextureDisable && m_semiTransparencyEnabled)
                {
                    vertices[i].u_drawOpaquePixels = 1;
                    vertices[i].u_drawTransparentPixels = 0;
                } else
                {
                    vertices[i].u_drawOpaquePixels = CurrentBlend.RequireDoublePass ? 0 : 1;
                    vertices[i].u_drawTransparentPixels = 1;
                }
                vertices[i].u_texWindowMask.X = TextureWindowXMask;
                vertices[i].u_texWindowMask.Y = TextureWindowYMask;
                vertices[i].u_texWindowOffset.X = TextureWindowXOffset;
                vertices[i].u_texWindowOffset.Y = TextureWindowYOffset;
                vertices[i].BlendMode = (int)CurrentBlendMode;
            }

            CurrentBlock.Vertexs.AddRange(vertices);
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

                CurrentBlock.semiTransparencyEnabled = enabled;

                UpdateBlendMode();
            }
        }

        public unsafe void UpdateBlendMode()
        {
            BlendMode mode = BlendMode.Opaque;

            if (m_semiTransparencyEnabled)
            {
                switch (m_semiTransparencyMode)
                {
                    case 0:
                        mode = BlendMode.AlphaBlend;
                        break;
                    case 1:
                        mode = BlendMode.Additive;
                        break;
                    case 2:
                        mode = BlendMode.Subtract;
                        break;
                    case 3:
                        mode = BlendMode.Quarter;
                        break;
                }
            }

            CurrentBlend = blendPresets[mode];

            CurrentBlendMode = mode;

            if (mode == CurrentBlock.mode)
                return;

            SubmitRecord();
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
            // 检查 bounds 是否需要覆盖待处理的批处理多边形
            //if (m_dirtyArea.Intersects(bounds))
            //    DrawBatch();

            m_dirtyArea.Grow(bounds);

            // 检查 bounds 是否会覆盖当前的纹理数据
            if (IntersectsTextureData(bounds))
            {
                Console.WriteLine($"[Vulkan GPU] GrowDirtyArea IntersectsTextureData {bounds} - Drawing texture data.");
            }
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

            srcTexture.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                srcTexture,
                srcTexture.layout,
                VkImageLayout.TransferSrcOptimal
            );

            dstTexture.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
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
                VkFilter.Linear
            );

            dstTexture.layout = Device.TransitionImageLayout(
                DrawCMD.CMD[0],
                dstTexture,
                dstTexture.layout,
                VkImageLayout.ColorAttachmentOptimal
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
