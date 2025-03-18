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

        VulkanDevice device;

        VkRenderPass renderPass;

        vkSwapchain chain, displayswapchain;
        vkBuffer stagingBuffer;
        vkTexture vramTexture, depthTexture, drawTexture, displayTexture;
        vkCMDS tempcmd;

        vkGraphicsPipeline currentPipeline;
        vkGraphicsPipeline fillpipeline, ramViewPipeline, out24Pipeline, out16Pipeline, displayPipeline;
        vkGraphicsPipeline drawPipelineWithAddBlend, drawPipelineWithSubtractBlend, drawPipelineWithoutBlending;

        VkDescriptorPool descriptorPool;
        VkDescriptorSet displayDescriptorSet;

        VkSemaphore imageAvailableSemaphore, renderFinishedSemaphore;

        List<VkFramebuffer> framebuffers = new List<VkFramebuffer>();
        VkViewport viewport;
        VkRect2D scissor;

        public bool ViewVRam = false;
        public bool DisplayEnable = true;
        public int IRScale;
        public bool RealColor, PGXP, PGXPT, KEEPAR;
        const float AspectRatio = 4.0f / 3.0f;

        public VulkanGPU()
        {
            device = new VulkanDevice();
        }

        public unsafe void Initialize()
        {
            VRAM = (ushort*)Marshal.AllocHGlobal((VRAM_WIDTH * VRAM_HEIGHT) * 2);

            device.VulkanInit(NullRenderer.hinstance, NullRenderer.hwnd);

            chain = device.CreateSwapChain(1024, 512);

            renderPass = device.CreateRenderPass(VkFormat.R8g8b8a8Unorm, VkSampleCountFlags.Count1);

            stagingBuffer = device.CreateBuffer(1024, 512, 2);

            vramTexture = device.CreateTexture(1024, 512, VkFormat.R5g5b5a1UnormPack16);

            drawTexture = device.CreateTexture(1024, 512, VkFormat.R8g8b8a8Unorm);

            displayTexture = device.CreateTexture(1024, 512, VkFormat.R8g8b8a8Unorm);

            imageAvailableSemaphore = device.CreateSemaphore();
            renderFinishedSemaphore = device.CreateSemaphore();

            byte[] vertShaderBytes = device.LoadShaderFile("fill.vert.spv");
            byte[] fragShaderBytes = device.LoadShaderFile("fill.frag.spv");

            fillpipeline = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1
            );

            VkVertexInputAttributeDescription* attributeDescriptions = stackalloc VkVertexInputAttributeDescription[]
            {
                // v_pos: 顶点位置 (vec3)
                new VkVertexInputAttributeDescription
                {
                    location = 0,                     // 对应顶点着色器中的 layout(location = 0)
                    binding = 0,                      // 绑定点索引
                    format = VkFormat.R32g32b32a32Sfloat, // 格式：3 个浮点数 (x, y, z)
                    offset = (uint)Marshal.OffsetOf(typeof(Vertex), "v_pos").ToInt32()
                },

                // v_color: 顶点颜色 (vec4)
                new VkVertexInputAttributeDescription
                {
                    location = 1,                     // 对应顶点着色器中的 layout(location = 1)
                    binding = 0,                      // 绑定点索引
                    format = VkFormat.R8g8b8a8Unorm,  // 格式：4 个无符号字节 (R, G, B, A)
                    offset = (uint)Marshal.OffsetOf(typeof(Vertex), "v_color").ToInt32()
                },

                // v_texCoord: 纹理坐标 (vec2)
                new VkVertexInputAttributeDescription
                {
                    location = 2,                     // 对应顶点着色器中的 layout(location = 2)
                    binding = 0,                      // 绑定点索引
                    format = VkFormat.R16g16Snorm,    // 格式：2 个短整型 (u, v)
                    offset = (uint)Marshal.OffsetOf(typeof(Vertex), "v_texCoord").ToInt32()
                },

                // v_clut: CLUT 属性 (ivec2)
                new VkVertexInputAttributeDescription
                {
                    location = 3,                     // 对应顶点着色器中的 layout(location = 3)
                    binding = 0,                      // 绑定点索引
                    format = VkFormat.R16g16Sint,     // 格式：2 个短整型 (X, Y)
                    offset = (uint)Marshal.OffsetOf(typeof(Vertex), "v_clut").ToInt32()
                },

                // v_texPage: 纹理页属性 (ivec2)
                new VkVertexInputAttributeDescription
                {
                    location = 4,                     // 对应顶点着色器中的 layout(location = 4)
                    binding = 0,                      // 绑定点索引
                    format = VkFormat.R16g16Sint,     // 格式：2 个短整型 (X, Y)
                    offset = (uint)Marshal.OffsetOf(typeof(Vertex), "v_texPage").ToInt32()
                },

                // v_pos_high: 高精度顶点位置 (vec3)
                new VkVertexInputAttributeDescription
                {
                    location = 5,                     // 对应顶点着色器中的 layout(location = 5)
                    binding = 0,                      // 绑定点索引
                    format = VkFormat.R32g32b32Sfloat, // 格式：3 个浮点数 (x, y, z)
                    offset = (uint)Marshal.OffsetOf(typeof(Vertex), "v_pos_high").ToInt32()
                }
            };

            var bindingDescription = new VkVertexInputBindingDescription
            {
                binding = 0,
                stride = (uint)sizeof(Vertex),
                inputRate = VkVertexInputRate.Vertex
            };

            vertShaderBytes = device.LoadShaderFile("draw.vert.spv");
            fragShaderBytes = device.LoadShaderFile("draw.frag.spv");

            drawPipelineWithoutBlending = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1,
                bindingDescription,
                attributeDescriptions,
                6
            );

            drawPipelineWithAddBlend = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1,
                bindingDescription,
                attributeDescriptions,
                6,
                true,
                VkBlendOp.Add
            );

            drawPipelineWithSubtractBlend = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1,
                bindingDescription,
                attributeDescriptions,
                6,
                true,
                VkBlendOp.Subtract
            );

            vertShaderBytes = device.LoadShaderFile("out24.vert.spv");
            fragShaderBytes = device.LoadShaderFile("out24.frag.spv");

            out24Pipeline = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1
            );

            vertShaderBytes = device.LoadShaderFile("out16.vert.spv");
            fragShaderBytes = device.LoadShaderFile("out16.frag.spv");

            out16Pipeline = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1
            );

            vertShaderBytes = device.LoadShaderFile("display.vert.spv");
            fragShaderBytes = device.LoadShaderFile("display.frag.spv");

            displayPipeline = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1
            );

            vertShaderBytes = device.LoadShaderFile("ramview.vert.spv");
            fragShaderBytes = device.LoadShaderFile("ramview.frag.spv");

            ramViewPipeline = device.CreateGraphicsPipeline(
                renderPass,
                new VkExtent2D { width = 1024, height = 512 },
                VkDescriptorSetLayout.Null,
                vertShaderBytes,
                fragShaderBytes,
                width: 1024,
                height: 512,
                VkSampleCountFlags.Count1
            );

            descriptorPool = device.CreateDescriptorPool(1);

            displayTexture = device.CreateTexture(
                GetVRamTextureWidth(),
                GetVRamTextureHeight(),
                VkFormat.R8g8b8a8Unorm
            );

            VkDescriptorSetLayout descriptorSetLayout = device.CreateDescriptorSetLayout();

            displayDescriptorSet = device.CreateDescriptorSet(descriptorSetLayout, descriptorPool);

            device.UpdateDescriptorSets(displayTexture.image, displayDescriptorSet);

            tempcmd = device.CreateCommandBuffers(1);

            Console.WriteLine("[VULKAN GPU] Initialization Complete.");

            m_realColor = true;
        }

        public unsafe void Dispose()
        {

            if (isDisposed)
                return;

            device.DestoryBuffer(stagingBuffer);

            device.VulkanDispose();

            Marshal.FreeHGlobal((IntPtr)VRAM);

            Console.WriteLine($"[OpenGL GPU] Disposed");

            isDisposed = true;
        }

        public unsafe void CreateFramebuffers(vkSwapchain swapchain, VkRenderPass renderPass, vkTexture depthTexture)
        {
            foreach (var framebuffer in framebuffers)
            {
                device.DestroyFramebuffer(framebuffer);
            }
            framebuffers.Clear();

            foreach (var imageView in swapchain.ImageViews)
            {
                var framebuffer = device.CreateFramebuffer(
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
            VkFormat depthFormat = device.FindDepthFormat();

            depthTexture = device.CreateTexture(width, height, depthFormat);
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
                vkDestroyFramebuffer(device.device, framebuffer, null);
            }
            framebuffers.Clear();

            if (depthTexture.image != VkImage.Null)
            {
                device.DestroyTexture(depthTexture);
                depthTexture.image = VkImage.Null;
            }

            device.CleanupSwapChain(chain);
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
            vkGetPhysicalDeviceProperties(device.physicalDevice, out deviceProperties);
            uint maxTextureSize = deviceProperties.limits.maxImageDimension2D;
            if (newWidth > maxTextureSize || newHeight > maxTextureSize)
            {
                Console.WriteLine($"[VULKAN GPU] New resolution ({newWidth}x{newHeight}) exceeds maximum texture size ({maxTextureSize})");
                return;
            }

            int oldWidth = VRAM_WIDTH * resolutionScale;
            int oldHeight = VRAM_HEIGHT * resolutionScale;

            var newDrawTexture = device.CreateTexture(newWidth, newHeight, VkFormat.R8g8b8a8Unorm);
            var newDisplayTexture = device.CreateTexture(newWidth, newHeight, VkFormat.R8g8b8a8Unorm);

            if (drawTexture.image != VkImage.Null && displayTexture.image != VkImage.Null)
            {
                CopyTexture(drawTexture, newDrawTexture, oldWidth, oldHeight, newWidth, newHeight);
                CopyTexture(displayTexture, newDisplayTexture, oldWidth, oldHeight, newWidth, newHeight);
            }

            resolutionScale = scale;

            CleanupResources();

            drawTexture = newDrawTexture;
            displayTexture = newDisplayTexture;

            chain = device.CreateSwapChain(newWidth, newHeight);
            CreateDepthResources(newWidth, newHeight);
            CreateFramebuffers(chain, renderPass, depthTexture);

            imageAvailableSemaphore = device.CreateSemaphore();
            renderFinishedSemaphore = device.CreateSemaphore();

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

            m_vramDisplayArea.x = rx;
            m_vramDisplayArea.y = ry;
            m_vramDisplayArea.width = w;
            m_vramDisplayArea.height = offsetline * 2;

            m_targetDisplayArea.x = 0;
            m_targetDisplayArea.y = 0;
            m_targetDisplayArea.width = w;
            m_targetDisplayArea.height = offsetline * 2;

            DrawBatch();

            RestoreRenderState();

            int targetWidth = 0;
            int targetHeight = 0;
            int srcWidth = 0;
            int srcHeight = 0;

            if (ViewVRam)
            {
                targetWidth = VRAM_WIDTH * resolutionScale;
                targetHeight = VRAM_HEIGHT * resolutionScale;
                srcWidth = VRAM_WIDTH * resolutionScale;
                srcHeight = VRAM_HEIGHT * resolutionScale;
            } else
            {
                targetWidth = m_targetDisplayArea.width * resolutionScale;
                targetHeight = m_targetDisplayArea.height * resolutionScale;
                srcWidth = m_vramDisplayArea.width * resolutionScale;
                srcHeight = m_vramDisplayArea.height * resolutionScale;
            }

            if (targetWidth != displayTexture.width || targetHeight != displayTexture.height)
            {
                device.DestroyTexture(displayTexture);
                displayTexture = device.CreateTexture(
                    targetWidth,
                    targetHeight,
                    VkFormat.R8g8b8a8Unorm
                );
            }

            ClearTexture(displayTexture, 0, 0, 0, 1);

            RenderToDisplayTexture(is24bit, srcWidth, srcHeight);

            RenderToWindow(targetWidth, targetHeight);

            RestoreRenderState();

            if (RealColor != m_realColor)
            {
                m_realColor = RealColor;
            }
            if (IRScale != resolutionScale)
            {
                SetResolutionScale(IRScale);
            }

            return (targetWidth, targetHeight);
        }

        private unsafe void ClearTexture(vkTexture texture, float r, float g, float b, float a)
        {
            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            VkClearColorValue clearColor = new VkClearColorValue(r, g, b, a);

            device.TransitionImageLayout(
                commandBuffer,
                texture.image,
                VkImageLayout.Undefined,
                VkImageLayout.TransferDstOptimal
            );

            device.CmdClearColorImage(
                commandBuffer,
                texture.image,
                clearColor
            );

            device.TransitionImageLayout(
                commandBuffer,
                texture.image,
                VkImageLayout.TransferDstOptimal,
                VkImageLayout.ShaderReadOnlyOptimal
            );

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
        }

        private unsafe void RenderToDisplayTexture(bool is24bit, int srcWidth, int srcHeight)
        {
            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            if (is24bit)
            {
                device.BindGraphicsPipeline(commandBuffer, out24Pipeline);
            } else
            {
                device.BindGraphicsPipeline(commandBuffer, out16Pipeline);
            }

            VkViewport viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = srcWidth,
                height = srcHeight,
                minDepth = 0,
                maxDepth = 1
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = new VkExtent2D { width = (uint)srcWidth, height = (uint)srcHeight }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

            device.UpdateDescriptorSets(displayTexture.image, displayDescriptorSet);

            vkCmdDraw(commandBuffer, 4, 1, 0, 0);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
        }

        int oldwinwidth, oldwinheight;

        private unsafe void RenderToWindow(int width, int height)
        {
            uint imageIndex;

            if (oldwinwidth != width || oldwinheight != height)
            {
                oldwinwidth = width;
                oldwinheight = height;

                displayswapchain = device.CreateSwapChain(width, height);

                CreateFramebuffers(displayswapchain, renderPass, depthTexture);
            }

            vkAcquireNextImageKHR(device.device, chain.Chain, ulong.MaxValue, imageAvailableSemaphore, VkFence.Null, &imageIndex);

            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            var clr = stackalloc VkClearValue[] { new VkClearValue { color = new VkClearColorValue(0, 0, 0, 1) } };

            var renderPassInfo = new VkRenderPassBeginInfo
            {
                sType = VkStructureType.RenderPassBeginInfo,
                renderPass = renderPass,
                framebuffer = framebuffers[(int)imageIndex],
                renderArea = new VkRect2D
                {
                    offset = new VkOffset2D { x = 0, y = 0 },
                    extent = new VkExtent2D { width = (uint)width, height = (uint)height }
                },
                clearValueCount = 1,
                pClearValues = clr
            };

            vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.Inline);

            VkViewport viewport = new VkViewport
            {
                x = 0,
                y = 0,
                width = width,
                height = height,
                minDepth = 0,
                maxDepth = 1
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = 0, y = 0 },
                extent = new VkExtent2D { width = (uint)width, height = (uint)height }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

            device.BindGraphicsPipeline(commandBuffer, displayPipeline);

            device.UpdateDescriptorSets(displayTexture.image, displayDescriptorSet);

            vkCmdDraw(commandBuffer, 4, 1, 0, 0);

            vkCmdEndRenderPass(commandBuffer);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);

            var ia = imageAvailableSemaphore;
            var rf = renderFinishedSemaphore;
            var ws = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.ColorAttachmentOutput };
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &ia,
                pWaitDstStageMask = ws,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
                signalSemaphoreCount = 1,
                pSignalSemaphores = &rf
            };

            if (vkQueueSubmit(device.graphicsQueue, 1, &submitInfo, VkFence.Null) != VkResult.Success)
            {
                throw new Exception("Failed to submit draw command buffer!");
            }

            var dc = chain.Chain;
            var presentInfo = new VkPresentInfoKHR
            {
                sType = VkStructureType.PresentInfoKHR,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &rf,
                swapchainCount = 1,
                pSwapchains = &dc,
                pImageIndices = &imageIndex
            };

            if (vkQueuePresentKHR(device.presentQueue, &presentInfo) != VkResult.Success)
            {
                throw new Exception("Failed to present image!");
            }
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

            byte r = (byte)(colorval);
            byte g = (byte)(colorval >> 8);
            byte b = (byte)(colorval >> 16);
            float rF, gF, bF;

            if (m_realColor)
            {
                rF = r / 255.0f;
                gF = g / 255.0f;
                bF = b / 255.0f;
            } else
            {
                rF = (r >> 3) / 31.0f;
                gF = (g >> 3) / 31.0f;
                bF = (b >> 3) / 31.0f;
            }

            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferDstOptimal);

            int scaledLeft = left * resolutionScale;
            int scaledTop = top * resolutionScale;
            int scaledWidth = width * resolutionScale;
            int scaledHeight = height * resolutionScale;

            VkViewport viewport = new VkViewport
            {
                x = scaledLeft,
                y = scaledTop,
                width = scaledWidth,
                height = scaledHeight,
                minDepth = 0,
                maxDepth = 1
            };

            VkRect2D scissor = new VkRect2D
            {
                offset = new VkOffset2D { x = scaledLeft, y = scaledTop },
                extent = new VkExtent2D { width = (uint)scaledWidth, height = (uint)scaledHeight }
            };

            vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
            vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

            device.BindGraphicsPipeline(commandBuffer, fillpipeline);

            float[] colorData = { rF, gF, bF, 1.0f };
            fixed (float* pColorData = colorData)
            {
                vkCmdPushConstants(
                    commandBuffer,
                    fillpipeline.layout,
                    VkShaderStageFlags.Fragment,
                    0,
                    sizeof(float) * 4,
                    pColorData
                );
            }

            vkCmdDraw(commandBuffer, 4, 1, 0, 0);

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
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

            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

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

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
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

            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferSrcOptimal);

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

            device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.TransferSrcOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);

            Buffer.MemoryCopy(stagingBuffer.mappedData, VRAM + (readBounds.Left + readBounds.Top * VRAM_WIDTH) * 2, 0, readWidth * readHeight * 2);
        }

        public unsafe void CopyRectCPUtoVRAM(int left, int top, int width, int height)
        {
            var updateBounds = GetWrappedBounds(left, top, width, height);
            GrowDirtyArea(updateBounds);

            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferDstOptimal);

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

            device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
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

            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferDstOptimal);

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

            device.BindGraphicsPipeline(commandBuffer, currentPipeline);

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

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);

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
            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            if (currentPipeline.pipeline != VkPipeline.Null)
            {
                vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, currentPipeline.pipeline);
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

            VkBuffer vertexBuffer = stagingBuffer.stagingBuffer;
            ulong offset = 0;
            vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);

            if (m_semiTransparencyEnabled)
            {
                vkCmdSetBlendConstants(commandBuffer, 1.0f);
            }

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
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
                commandBuffer = device.BeginSingleCommands(tempcmd.pool);

                vkCmdSetBlendConstants(commandBuffer, 0.0f);

                device.EndSingleCommands(commandBuffer, tempcmd.pool);

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

            commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            vkCmdSetBlendConstants(commandBuffer, srcBlend);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
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
            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

            device.TransitionImageLayout(commandBuffer, drawTexture.image, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferSrcOptimal);

            device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

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

            device.TransitionImageLayout(commandBuffer, vramTexture.image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
        }

        private unsafe void CopyTexture(vkTexture srcTexture, vkTexture dstTexture, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            VkCommandBuffer commandBuffer = device.BeginSingleCommands(tempcmd.pool);

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

            device.EndSingleCommands(commandBuffer, tempcmd.pool);
        }


    }
}
