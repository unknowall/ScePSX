﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        uint oldmaskbit, oldtexwin;
        short m_currentDepth = 1;

        int resolutionScale = 1;

        vkTexPage m_TexPage;
        vkClutAttribute m_clut;

        vkRectangle<int> m_dirtyArea, m_clutArea, m_textureArea;

        [StructLayout(LayoutKind.Explicit, Size = 44)]
        public struct Vertex
        {
            [FieldOffset(0)] public vkPosition v_pos;
            [FieldOffset(8)] public Vector3 v_pos_high;
            [FieldOffset(20)] public vkTexCoord v_texCoord;
            [FieldOffset(24)] public vkColor v_color;
            [FieldOffset(36)] public vkClutAttribute v_clut;
            [FieldOffset(40)] public vkTexPage v_texPage;
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

        VulkanDevice Device;

        VkRenderPass renderPass;

        vkSwapchain drawChain;

        vkBuffer stagingBuffer, VaoBuffer, vertexUBO, fragmentUBO;
        vkTexture vramTexture, samplerTexture, drawTexture, copyTexture;
        VkFramebuffer drawFramebuff;
        vkCMDS DrawCmd, OpCMD;

        vkGraphicsPipeline currentPipeline;
        vkGraphicsPipeline out24Pipeline, out16Pipeline;
        vkGraphicsPipeline drawAvgBlend, drawAddBlend, drawSubtractBlend, drawConstantBlend, drawNoBlend;

        VkDescriptorPool outdescriptorPool, drawdescriptorPool;
        VkDescriptorSet outDescriptorSet, drawDescriptorSet;
        VkDescriptorSetLayout outDescriptorLayout, drawDescriptorLayout;

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
        public struct drawvertUBO
        {
            public float u_resolutionScale;
            public int u_pgxp;
            public Matrix4x4 u_mvp;
        }
        drawvertUBO drawVert;

        [StructLayout(LayoutKind.Sequential)]
        struct drawfragUBO
        {
            public float u_srcBlend;
            public float u_destBlend;
            public int u_setMaskBit;
            public int u_drawOpaquePixels;
            public int u_drawTransparentPixels;
            public int u_dither;
            public int u_realColor;

            public int u_texWindowMaskX;
            public int u_texWindowMaskY;

            public int u_texWindowOffsetX;
            public int u_texWindowOffsetY;
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

        #endregion

        public bool ViewVRam = false;
        public bool DisplayEnable = true;
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
            VRAM = (ushort*)Marshal.AllocHGlobal((1024 * 512) * 2);

            Device.VulkanInit(NullRenderer.hwnd, NullRenderer.hinstance);

            renderPass = Device.CreateRenderPass(Device.ChooseSurfaceFormat().format);

            drawChain = Device.CreateSwapChain(renderPass, NullRenderer.ClientWidth, NullRenderer.ClientHeight);

            frameFences = new FrameFence[drawChain.Images.Count];

            stagingBuffer = Device.CreateBuffer(1024 * 512 * 2);

            DrawCmd = Device.CreateCommandBuffers(drawChain.Images.Count);

            OpCMD = Device.CreateCommandBuffers(5);

            VaoBuffer = Device.CreateBuffer((ulong)(1024 * 44), VkBufferUsageFlags.VertexBuffer | VkBufferUsageFlags.TransferDst);

            vramTexture = Device.CreateTexture(
                1024, 512,
                VkFormat.R5g5b5a1UnormPack16,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferSrc
                );

            samplerTexture = Device.CreateTexture(
                1024, 512,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc
                );

            copyTexture = Device.CreateTexture(
                1024, 512,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc
                );

            drawTexture = Device.CreateTexture(
                1024, 512,
                VkFormat.R8g8b8a8Unorm,
                VkImageAspectFlags.Color,
                VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc
                );

            drawFramebuff = Device.CreateFramebuffer(renderPass, drawTexture.imageview, 1024, 512);

            var vertexType = typeof(Vertex);
            VkVertexInputAttributeDescription[] drawAttributes = new VkVertexInputAttributeDescription[]
            {
                new() { // v_pos (location=0)
                    location = 0,
                    binding = 0,
                    format = VkFormat.R16g16b16a16Sscaled,
                    offset = 0
                },
                new() { // v_pos_high (location=1)
                    location = 1,
                    binding = 0,
                    format = VkFormat.R32g32b32Sfloat,
                    offset = 8
                },
                new() { // v_texCoord (location=2)
                    location = 2,
                    binding = 0,
                    format = VkFormat.R16g16Sscaled,
                    offset = 20
                },
                new() { // v_color (location=3)
                    location = 3,
                    binding = 0,
                    format = VkFormat.R8g8b8Unorm,
                    offset = 24
                },
                new() { // v_clut (location=4)
                    location = 4,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 36
                },
                new() { // v_texPage (location=5)
                    location = 5,
                    binding = 0,
                    format = VkFormat.R32Sint,
                    offset = 40
                }
            };

            vertexUBO = Device.CreateBuffer((ulong)sizeof(drawvertUBO), VkBufferUsageFlags.UniformBuffer);

            fragmentUBO = Device.CreateBuffer((ulong)sizeof(drawfragUBO), VkBufferUsageFlags.UniformBuffer);

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

            drawDescriptorSet = Device.AllocateDescriptorSet(drawDescriptorLayout, drawdescriptorPool);

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

            var drawVertbytes = Device.LoadShaderFile("./Shaders/draw.vert.spv");
            var drawFragbytes = Device.LoadShaderFile("./Shaders/draw.frag.spv");

            // 主绘制管线（无混合）
            drawNoBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6
            );

            // 平均混合（Blend Mode 1）
            drawAvgBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6,
                GetBlendState(1)
            );

            // 加法混合（Blend Mode 2）
            drawAddBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6,
                GetBlendState(2)
            );

            // 减法混合（Blend Mode 3）
            drawSubtractBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6,
                GetBlendState(3)
            );

            // 四分之一混合（Blend Mode 4）
            drawConstantBlend = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                drawDescriptorLayout,
                drawVertbytes,
                drawFragbytes,
                1024, 512,
                VkSampleCountFlags.Count1,
                drawBinding,
                drawAttributes,
                6,
                GetBlendState(4)
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
                new VkExtent2D(1024, 512),
                outDescriptorLayout,
                out24Vert,
                out24Frag,
                1024, 512,
                VkSampleCountFlags.Count1,
                default,
                default,
                1,
                default,
                pushConstantRanges
            );

            var out16Vert = Device.LoadShaderFile("./Shaders/out16.vert.spv");
            var out16Frag = Device.LoadShaderFile("./Shaders/out16.frag.spv");

            out16Pipeline = Device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D(1024, 512),
                outDescriptorLayout,
                out16Vert,
                out16Frag,
                1024, 512,
                VkSampleCountFlags.Count1,
                default,
                default,
                1,
                default,
                pushConstantRanges
            );

            CreateSyncObjects();

            viewport = new VkViewport { width = 1024, height = 512, maxDepth = 1.0f };
            scissor = new VkRect2D { extent = new VkExtent2D(1024, 512) };

            m_realColor = true;

            currentPipeline = drawNoBlend;

            drawVert.u_resolutionScale = 1;

            UpdateVert();

            drawFrag.u_srcBlend = 1.0f;
            drawFrag.u_destBlend = 0.0f;
            drawFrag.u_realColor = 1;
            drawFrag.u_dither = 0;
            drawFrag.u_setMaskBit = 0;
            drawFrag.u_drawOpaquePixels = 1;
            drawFrag.u_drawTransparentPixels = 1;

            UpdateFrag();

            Console.WriteLine("[Vulkan GPU] Initialization Complete.");
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

        private void CreateSyncObjects()
        {
            for (int i = 0; i < drawChain.Images.Count; i++)
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

            foreach (var frame in frameFences)
            {
                vkDestroySemaphore(Device.device, frame.ImageAvailable, null);
                vkDestroySemaphore(Device.device, frame.RenderFinished, null);
                vkDestroyFence(Device.device, frame.InFlightFence, null);
            }

            Device.DestoryBuffer(stagingBuffer);

            Device.VulkanDispose();

            Marshal.FreeHGlobal((IntPtr)VRAM);

            Console.WriteLine($"[OpenGL GPU] Disposed");

            isDisposed = true;
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

            if (drawTexture.image != VkImage.Null)
            {
                CopyTexture(drawTexture, newDrawTexture, oldWidth, oldHeight, newWidth, newHeight);
            }

            resolutionScale = scale;

            drawTexture = newDrawTexture;

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

            m_vramDisplayArea.width = ViewVRam ?
                VRAM_WIDTH * resolutionScale :
                m_vramDisplayArea.width * resolutionScale;

            m_vramDisplayArea.height = ViewVRam ?
                VRAM_HEIGHT * resolutionScale :
                m_vramDisplayArea.height * resolutionScale;

            DrawBatch();

            RenderToWindow(is24bit);

            if (IRScale != resolutionScale)
                SetResolutionScale(IRScale);

            return (m_vramDisplayArea.width, m_vramDisplayArea.height);
        }

        private unsafe void RenderToSwapchain(VkCommandBuffer cmd, uint chainidx, bool is24bit)
        {
            Device.BeginCommandBuffer(cmd);

            //Device.UpdateDescriptorImage(outDescriptorSet, samplerTexture, 0);

            Device.BeginRenderPass(cmd, renderPass, drawChain.framebuffes[(int)chainidx], (int)drawChain.Extent.width, (int)drawChain.Extent.height, true, 0, 0, 0, 1);

            var outPipeline = is24bit ? out24Pipeline : out16Pipeline;
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

            VkViewport viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = drawChain.Extent.width,
                height = drawChain.Extent.height,
                minDepth = 0.0f,
                maxDepth = 1.0f
            };
            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D(0, 0),
                extent = new VkExtent2D((uint)drawChain.Extent.width, (uint)drawChain.Extent.height)
            };
            vkCmdSetViewport(cmd, 0, 1, &viewport);
            vkCmdSetScissor(cmd, 0, 1, &scissor);

            vkCmdDraw(cmd, 4, 1, 0, 0);

            vkCmdEndRenderPass(cmd);

            Device.TransitionImageLayout(
                cmd,
                drawChain.Images[chainidx],
                VkImageLayout.ShaderReadOnlyOptimal,
                VkImageLayout.PresentSrcKHR,
                VkPipelineStageFlags.FragmentShader,
                VkPipelineStageFlags.BottomOfPipe
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
                drawChain.Chain,
                ulong.MaxValue,
                currentFrame.ImageAvailable,
                VkFence.Null,
                &imageIndex
            );

            VkCommandBuffer cmd = DrawCmd.CMD[imageIndex];

            //Console.WriteLine($"[Vulkan GPU] Present Start frameIndex {frameIndex} CMD 0x{cmd.Handle:X}!");

            RenderToSwapchain(cmd, imageIndex, is24bit);

            // 提交呈现命令
            var presentCmd = DrawCmd.CMD[imageIndex];

            var waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
            var ws = currentFrame.ImageAvailable;
            var ss = currentFrame.RenderFinished;
            var chain = drawChain.Chain;

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

            frameIndex = (frameIndex + 1) % (int)drawChain.Images.Count;
        }

        private unsafe void RecreateSwapChain()
        {
            vkDeviceWaitIdle(Device.device);

            Device.CleanupSwapChain(drawChain);
            drawChain = Device.CreateSwapChain(renderPass, NullRenderer.ClientWidth, NullRenderer.ClientHeight);
        }

        private unsafe void SetScissor()
        {
            int width = Math.Max(DrawingAreaBottomRight.X - DrawingAreaTopLeft.X + 1, 0);
            int height = Math.Max(DrawingAreaBottomRight.Y - DrawingAreaTopLeft.Y + 1, 0);

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D(DrawingAreaTopLeft.X, DrawingAreaTopLeft.Y),
                extent = new VkExtent2D((uint)width, (uint)height)
            };
            //Console.WriteLine($"[Vulkan GPU] SetScissor CMD 0x{DrawCmd.CMD[0].Handle:X}!");
            Device.BeginCommandBuffer(OpCMD.CMD[0]);
            vkCmdSetScissor(OpCMD.CMD[0], 0, 1, ref scissor);
            Device.EndAndWaitCommandBuffer(OpCMD.CMD[0]);
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

        private unsafe void ClearTexture(vkTexture texture, float r, float g, float b, float a)
        {
            VkCommandBuffer cmd = OpCMD.CMD[0];
            //Console.WriteLine($"[Vulkan GPU] ClearTexture CMD 0x{cmd.Handle:X}!");
            Device.BeginCommandBuffer(cmd);

            VkClearColorValue clearColor = new VkClearColorValue(r, g, b, a);

            Device.TransitionImageLayout(
                cmd,
                texture.image,
                VkImageLayout.Undefined,
                VkImageLayout.TransferDstOptimal
            );

            Device.CmdClearColorImage(
                cmd,
                texture.image,
                clearColor
            );

            Device.TransitionImageLayout(
                cmd,
                texture.image,
                VkImageLayout.TransferDstOptimal,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            Device.EndAndWaitCommandBuffer(cmd);
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;

            if (_VRAMTransfer.isRead)
            {
                //CopyRectVRAMtoCPU(_VRAMTransfer.OriginX, _VRAMTransfer.OriginY, _VRAMTransfer.W, _VRAMTransfer.H);
            }
        }

        public unsafe void SetMaskBit(uint value)
        {
            if (oldmaskbit != value)
            {
                oldmaskbit = value;
                DrawBatch();

                ForceSetMaskBit = ((value & 1) != 0);
                CheckMaskBit = (((value >> 1) & 1) != 0);

                drawFrag.u_setMaskBit = ForceSetMaskBit ? 1 : 0;

                UpdateFrag();
            }
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            if (DrawingAreaTopLeft != value)
            {
                DrawBatch();

                DrawingAreaTopLeft = value;

                SetScissor();
            }
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            if (DrawingAreaBottomRight != value)
            {
                DrawBatch();

                DrawingAreaBottomRight = value;

                SetScissor();
            }
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

                DrawBatch();

                TextureWindowXMask = (int)(value & 0x1f);
                TextureWindowYMask = (int)((value >> 5) & 0x1f);

                TextureWindowXOffset = (int)((value >> 10) & 0x1f);
                TextureWindowYOffset = (int)((value >> 15) & 0x1f);

                drawFrag.u_texWindowMaskX = TextureWindowXMask;
                drawFrag.u_texWindowMaskY = TextureWindowYMask;
                drawFrag.u_texWindowOffsetX = TextureWindowXOffset;
                drawFrag.u_texWindowOffsetY = TextureWindowYOffset;

                UpdateFrag();
            }
        }

        public unsafe void FillRectVRAM(ushort left, ushort top, ushort width, ushort height, uint colorval)
        {
            var bounds = GetWrappedBounds(left, top, width, height);
            GrowDirtyArea(bounds);

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

            if (Vertexs.Count + 6 > 1024)
                DrawBatch();

            ushort texpage = (ushort)(1 << 11);

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
        }

        public unsafe void CopyRectVRAMtoVRAM(ushort srcX, ushort srcY, ushort destX, ushort destY, ushort width, ushort height)
        {

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

            //VkCommandBuffer cmd = OpCMD.CMD[0];

            //Device.BeginCommandBuffer(cmd);

            //samplerTexture.layout = Device.TransitionImageLayout(
            //    cmd,
            //    samplerTexture.image,
            //    samplerTexture.layout,
            //    VkImageLayout.TransferSrcOptimal
            //);

            //copyTexture.layout = Device.TransitionImageLayout(
            //    cmd,
            //    copyTexture,
            //    copyTexture.layout,
            //    VkImageLayout.TransferDstOptimal
            //);

            //var srcBlitRegion = new VkImageBlit
            //{
            //    srcSubresource = new VkImageSubresourceLayers
            //    {
            //        aspectMask = VkImageAspectFlags.Color,
            //        mipLevel = 0,
            //        baseArrayLayer = 0,
            //        layerCount = 1
            //    },
            //    srcOffsets_0 = new VkOffset3D
            //    {
            //        x = srcX * resolutionScale,
            //        y = srcY * resolutionScale,
            //        z = 0
            //    },
            //    srcOffsets_1 = new VkOffset3D
            //    {
            //        x = (srcX + width) * resolutionScale,
            //        y = (srcY + height) * resolutionScale,
            //        z = 1
            //    },
            //    dstSubresource = new VkImageSubresourceLayers
            //    {
            //        aspectMask = VkImageAspectFlags.Color,
            //        mipLevel = 0,
            //        baseArrayLayer = 0,
            //        layerCount = 1
            //    },
            //    dstOffsets_0 = new VkOffset3D { x = 0, y = 0, z = 0 },
            //    dstOffsets_1 = new VkOffset3D
            //    {
            //        x = width * resolutionScale,
            //        y = height * resolutionScale,
            //        z = 1
            //    }
            //};

            //vkCmdBlitImage(
            //    cmd,
            //    samplerTexture.image,
            //    VkImageLayout.TransferSrcOptimal,
            //    copyTexture.image,
            //    VkImageLayout.TransferDstOptimal,
            //    1,
            //    &srcBlitRegion,
            //    VkFilter.Nearest
            //);

            //copyTexture.layout = Device.TransitionImageLayout(
            //    cmd,
            //    copyTexture,
            //    VkImageLayout.TransferDstOptimal,
            //    VkImageLayout.TransferSrcOptimal
            //);

            //samplerTexture.layout = Device.TransitionImageLayout(
            //    cmd,
            //    samplerTexture.image,
            //    samplerTexture.layout,
            //    VkImageLayout.TransferDstOptimal
            //);

            //var destBlitRegion = new VkImageBlit
            //{
            //    srcSubresource = new VkImageSubresourceLayers
            //    {
            //        aspectMask = VkImageAspectFlags.Color,
            //        mipLevel = 0,
            //        baseArrayLayer = 0,
            //        layerCount = 1
            //    },
            //    srcOffsets_0 = new VkOffset3D { x = 0, y = 0, z = 0 }, // 临时图像起点
            //    srcOffsets_1 = new VkOffset3D
            //    {
            //        x = width * resolutionScale,
            //        y = height * resolutionScale,
            //        z = 1
            //    },
            //    dstSubresource = new VkImageSubresourceLayers
            //    {
            //        aspectMask = VkImageAspectFlags.Color,
            //        mipLevel = 0,
            //        baseArrayLayer = 0,
            //        layerCount = 1
            //    },
            //    dstOffsets_0 = new VkOffset3D
            //    {
            //        x = destX * resolutionScale,
            //        y = destY * resolutionScale,
            //        z = 0
            //    },
            //    dstOffsets_1 = new VkOffset3D
            //    {
            //        x = (destX + width) * resolutionScale,
            //        y = (destY + height) * resolutionScale,
            //        z = 1
            //    }
            //};

            //vkCmdBlitImage(
            //    cmd,
            //    copyTexture.image,
            //    VkImageLayout.TransferSrcOptimal,
            //    samplerTexture.image,
            //    VkImageLayout.TransferDstOptimal,
            //    1,
            //    &destBlitRegion,
            //    VkFilter.Nearest
            //);

            //samplerTexture.layout = Device.TransitionImageLayout(
            //    cmd,
            //    samplerTexture.image,
            //    VkImageLayout.TransferDstOptimal,
            //    VkImageLayout.ShaderReadOnlyOptimal
            //);

            //copyTexture.layout = Device.TransitionImageLayout(
            //    cmd,
            //    copyTexture,
            //    VkImageLayout.TransferSrcOptimal,
            //    VkImageLayout.Undefined
            //);

            //Device.EndAndWaitCommandBuffer(cmd);
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

            VkCommandBuffer cmd = OpCMD.CMD[0];

            Device.BeginCommandBuffer(cmd);

            if (resolutionScale > 1)
            {
                WriteBackDrawTextureToVRAM(cmd);
            }

            VkImage sourceTexture = resolutionScale == 1 ? drawTexture.image : vramTexture.image;

            vramTexture.layout = Device.TransitionImageLayout(cmd, vramTexture.image, vramTexture.layout, VkImageLayout.TransferSrcOptimal);

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
                cmd,
                sourceTexture,
                VkImageLayout.TransferSrcOptimal,
                stagingBuffer.stagingBuffer,
                1,
                &copyRegion
            );

            vramTexture.layout = Device.TransitionImageLayout(cmd, vramTexture.image, vramTexture.layout, VkImageLayout.ShaderReadOnlyOptimal);

            Device.EndAndWaitCommandBuffer(cmd);

            Buffer.MemoryCopy(stagingBuffer.mappedData, VRAM + (readBounds.Left + readBounds.Top * VRAM_WIDTH) * 2, 0, readWidth * readHeight * 2);
        }

        public unsafe void CopyRectCPUtoVRAM(int originX, int originY, int width, int height)
        {
            if (width <= 0 || height <= 0 || VRAM == null)
                return;

            VkCommandBuffer cmd = OpCMD.CMD[0];
            Device.BeginCommandBuffer(cmd);

            int pixelCount = width * height;
            int* convertedData = stackalloc int[pixelCount];
            ushort* src = VRAM + originX + originY * width;

            int rgb1555To8888(ushort color)
            {
                var m = (byte)(color >> 15);
                var r = LookupTable1555to8888[color & 0x1F];
                var g = LookupTable1555to8888[(color >> 5) & 0x1F];
                var b = LookupTable1555to8888[(color >> 10) & 0x1F];

                return (m << 24) | (b << 16) | (g << 8) | r;
            }

            for (int i = 0; i < pixelCount; i++)
            {
                convertedData[i] = rgb1555To8888(src[i]);
            }


            ulong bufferSize = (ulong)(pixelCount * sizeof(uint));

            void* mappedData;
            vkMapMemory(Device.device, stagingBuffer.stagingMemory, 0, (ulong)bufferSize, 0, &mappedData);
            Buffer.MemoryCopy(
                convertedData,
                mappedData,
                bufferSize,
                bufferSize
            );
            vkUnmapMemory(Device.device, stagingBuffer.stagingMemory);

            samplerTexture.layout = Device.TransitionImageLayout(
                cmd,
                samplerTexture.image,
                samplerTexture.layout,
                VkImageLayout.TransferDstOptimal
            );

            var region = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = (uint)width,
                bufferImageHeight = (uint)height,
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageOffset = new VkOffset3D { x = originX * resolutionScale, y = originY * resolutionScale, z = 0 },
                imageExtent = new VkExtent3D { width = (uint)(width * resolutionScale), height = (uint)(height * resolutionScale), depth = 1 }
            };

            vkCmdCopyBufferToImage(
                cmd,
                stagingBuffer.stagingBuffer,
                samplerTexture.image,
                VkImageLayout.TransferDstOptimal,
                        1,
                        &region

            );

            samplerTexture.layout = Device.TransitionImageLayout(
                cmd,
                samplerTexture.image,
                VkImageLayout.TransferDstOptimal,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            Device.EndAndWaitCommandBuffer(cmd);
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

        public void SetDrawMode(ushort vtexPage, ushort vclut, bool dither)
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
                    m_textureArea = vkRectangle<int>.FromExtents(texBaseX, texBaseY, texSize, texSize);

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

            VkCommandBuffer cmd = OpCMD.CMD[0];
            //Console.WriteLine($"[Vulkan GPU] DrawBatch CMD 0x{cmd.Handle:X} Vertexs {Vertexs.Count}!");
            Device.BeginCommandBuffer(cmd);

            samplerTexture.layout = Device.TransitionImageLayout(
                cmd,
                samplerTexture,
                VkImageLayout.Undefined,
                VkImageLayout.ShaderReadOnlyOptimal,
                VkPipelineStageFlags.TopOfPipe,
                VkPipelineStageFlags.FragmentShader
            );

            drawTexture.layout = Device.TransitionImageLayout(
                cmd,
                drawTexture,
                VkImageLayout.Undefined,
                VkImageLayout.ColorAttachmentOptimal,
                VkPipelineStageFlags.TopOfPipe,
                VkPipelineStageFlags.ColorAttachmentOutput
            );

            Device.UpdateBuffWaitAndBind(cmd, VaoBuffer, Marshal.UnsafeAddrOfPinnedArrayElement(Vertexs.ToArray(), 0), sizeof(Vertex) * Vertexs.Count);

            Device.BeginRenderPass(cmd, renderPass, drawFramebuff, 1024, 512, true, 0, 0, 0, 1);

            Device.BindGraphicsPipeline(cmd, currentPipeline);

            Device.BindDescriptorSet(cmd, currentPipeline, drawDescriptorSet);

            if (m_semiTransparencyEnabled && (m_semiTransparencyMode == 2) && !m_TexPage.TextureDisable)
            {
                drawFrag.u_drawOpaquePixels = 0;
                drawFrag.u_drawTransparentPixels = 0;

                UpdateFrag();

                vkCmdDraw(cmd, (uint)Vertexs.Count, 1, 0, 0);

                drawFrag.u_drawOpaquePixels = 0;
                drawFrag.u_drawTransparentPixels = 1;

                UpdateFrag();

                vkCmdDraw(cmd, (uint)Vertexs.Count, 1, 0, 0);
            } else
            {
                drawFrag.u_drawOpaquePixels = 1;
                drawFrag.u_drawTransparentPixels = 1;

                UpdateFrag();

                vkCmdDraw(cmd, (uint)Vertexs.Count, 1, 0, 0);
            }
            vkCmdEndRenderPass(cmd);

            Device.EndAndWaitCommandBuffer(cmd);

            Vertexs.Clear();

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

            if (PGXPT)
            {
                int minX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
                int minY = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
                int maxX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
                int maxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

                if (maxX - minX > 1024 || maxY - minY > 512)
                    return;
            }

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

            vertices[0].v_color = vkColor.FromUInt32(c0);
            vertices[1].v_color = vkColor.FromUInt32(c1);
            vertices[2].v_color = vkColor.FromUInt32(c2);

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
            VkCommandBuffer cmd = OpCMD.CMD[0];

            Device.BeginCommandBuffer(cmd);

            currentPipeline = drawNoBlend;

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

            vkCmdSetViewport(cmd, 0, 1, &viewport);
            vkCmdSetScissor(cmd, 0, 1, &scissor);

            Device.EndAndWaitCommandBuffer(cmd);
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
            if (m_semiTransparencyEnabled)
            {
                switch (m_semiTransparencyMode)
                {

                    case 0: // 平均混合
                        currentPipeline = drawAddBlend;//drawAvgBlend;
                        drawFrag.u_srcBlend = 0.5f;
                        drawFrag.u_destBlend = 0.5f;
                        break;

                    case 1: // 加法混合
                        currentPipeline = drawAddBlend;
                        drawFrag.u_srcBlend = 1.0f;
                        drawFrag.u_destBlend = 1.0f;
                        break;

                    case 2: // 减法混合
                        currentPipeline = drawSubtractBlend;
                        drawFrag.u_srcBlend = 1.0f;
                        drawFrag.u_destBlend = 1.0f;
                        break;

                    case 3: // 四分之一混合
                        currentPipeline = drawAddBlend;//drawConstantBlend;
                        drawFrag.u_srcBlend = 0.25f;
                        drawFrag.u_destBlend = 1.0f;
                        break;
                }
            } else
            {
                currentPipeline = drawNoBlend;
                drawFrag.u_srcBlend = 1.0f;
                drawFrag.u_destBlend = 0.0f;
            }

            UpdateFrag();
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
            if (m_dirtyArea.Intersects(bounds))
                DrawBatch();

            m_dirtyArea.Grow(bounds);

            // 检查 bounds 是否会覆盖当前的纹理数据
            if (IntersectsTextureData(bounds))
                DrawBatch();
        }

        private unsafe void WriteBackDrawTextureToVRAM(VkCommandBuffer cmd)
        {
            Device.TransitionImageLayout(cmd, drawTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferSrcOptimal);

            Device.TransitionImageLayout(cmd, vramTexture.image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

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
                cmd,
                drawTexture.image,
                VkImageLayout.TransferSrcOptimal,
                vramTexture.image,
                VkImageLayout.TransferDstOptimal,
                1,
                &blitRegion,
                VkFilter.Linear
            );

            Device.TransitionImageLayout(cmd, vramTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);
        }

        private unsafe void CopyTexture(vkTexture srcTexture, vkTexture dstTexture, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            VkCommandBuffer cmd = OpCMD.CMD[0];

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

            Device.EndAndWaitCommandBuffer(cmd);
        }

        private unsafe void UpdateVert()
        {
            void* mappedData;

            vkMapMemory(Device.device, vertexUBO.stagingMemory, 0, WholeSize, 0, &mappedData);

            Unsafe.Copy(mappedData, ref drawVert);

            vkUnmapMemory(Device.device, vertexUBO.stagingMemory);
        }

        private unsafe void UpdateFrag()
        {
            void* mappedData;

            vkMapMemory(Device.device, fragmentUBO.stagingMemory, 0, WholeSize, 0, &mappedData);

            Unsafe.Copy(mappedData, ref drawFrag);

            vkUnmapMemory(Device.device, fragmentUBO.stagingMemory);
        }
    }
}
