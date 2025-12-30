/*
 * ScePSX OpenGL Backend
 * 
 * github: http://github.com/unknowall/ScePSX
 * 
 * unknowall - sgfree@hotmail.com
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using LightGL;
using ScePSX.Core.GPU;
using ScePSX.Render;
using static ScePSX.Core.GPU.PGXPVector;

namespace ScePSX
{
    public class OpenglGPU : IGPU
    {
        public GPUType type => GPUType.OpenGL;

        int _ThreadID;

        bool isDisposed = false;

        #region GPU Vars
        unsafe ushort* VRAM;

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

        int ScissorBox_X = 0;
        int ScissorBox_Y = 0;
        int ScissorBoxWidth = VRAM_WIDTH;
        int ScissorBoxHeight = VRAM_HEIGHT;

        private TDrawingArea DrawingAreaTopLeft, DrawingAreaBottomRight;
        private TDrawingOffset DrawingOffset;
        private VRAMTransfer _VRAMTransfer;

        private int TextureWindowXMask, TextureWindowYMask, TextureWindowXOffset, TextureWindowYOffset;

        private bool CheckMaskBit, ForceSetMaskBit;
        #endregion

        #region OpenGL Vars

        IGlContext _DeviceContext;

        GLBuffer VertexsBuffer;

        GLShader RamViewShader, ResetDepthShader, DisplayShader, UserShader; //GetPixelsShader

        GLShader DrawShader, Out24Shader, Out16Shader;

        GLCopyShader CopyShader;

        GLFrameBuffer DrawFB, ReadFB, DisplayFB, TransferFB;

        GLTexture2D ReadTexture, DrawTexture, DrawDepthTexture;

        GLTexture2D TransferTexture, DisplayTexture;

        public class DrawShaderInfo
        {
            //混合模式相关位置变量
            public GlUniform u_srcBlend = null;
            public GlUniform u_destBlend = null;
            public GlUniform u_setMaskBit = null;
            public GlUniform u_drawOpaquePixels = null;
            public GlUniform u_drawTransparentPixels = null;
            // 抖动、颜色、纹理窗口相关位置变量
            public GlUniform u_dither = null;
            public GlUniform u_realColor = null;
            public GlUniform u_texWindowMask = null;
            public GlUniform u_texWindowOffset = null;
            public GlUniform u_resolutionScale = null;
            // PGXP、MVP
            public GlUniform u_pgxp = null;
            public GlUniform u_mvp = null;
            //Vertexs
            public GlAttribute v_pos = null;
            public GlAttribute v_color = null;
            public GlAttribute v_texCoord = null;
            public GlAttribute v_clut = null;
            public GlAttribute v_texPage = null;
            public GlAttribute v_pos_high = null;
        }
        DrawShaderInfo DrawInfo = new DrawShaderInfo();

        public class OutShader24Info
        {
            public GlUniform u_srcRect = null;
        }
        OutShader24Info Shader24Info = new OutShader24Info();

        public class OutShader16Info
        {
            public GlUniform u_srcRect = null;
        }
        OutShader16Info Shader16Info = new OutShader16Info();

        uint oldmaskbit, oldtexwin;
        short m_currentDepth;
        bool m_dither = false;
        bool m_realColor = false;
        bool m_pgxp = false;
        bool m_semiTransparencyEnabled = false;
        byte m_semiTransparencyMode = 0;

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

        Matrix4x4 m_mvpMatrix;
        float[] mvpArray;

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

        #endregion

        const float AspectRatio = 4.0f / 3.0f;

        public int resolutionScale = 1;
        public bool SyncVram = false;
        public bool CropEnabled = true;
        public bool StretchToFit = true;
        public bool ViewVRam = false;
        public bool DisplayEnable = true;
        public int IRScale;
        public bool RealColor, PGXP, PGXPT, KEEPAR;

        public unsafe void Initialize()
        {
            VRAM = (ushort*)Marshal.AllocHGlobal((VRAM_WIDTH * VRAM_HEIGHT) * 2);

            _DeviceContext = GlContextFactory.CreateFromWindowHandle(NullRenderer.hwnd);

            VertexsBuffer = GLBuffer.Create<Vertex>(BufferTarget.ArrayBuffer, BufferUsage.StreamDraw, 1024);

            DrawShader = new GLShader(GLShaderStrings.DrawVertix, GLShaderStrings.DrawFragment);

            DrawShader.BindUniformsAndAttributes(DrawInfo);

            Out24Shader = new GLShader(GLShaderStrings.Output24bitVertex, GLShaderStrings.Output24bitFragment);

            Out24Shader.BindUniformsAndAttributes(Shader24Info);

            Out16Shader = new GLShader(GLShaderStrings.Output16bitVertex, GLShaderStrings.Output16bitFragment);

            Out16Shader.BindUniformsAndAttributes(Shader16Info);

            RamViewShader = new GLShader(GLShaderStrings.VRamViewVertex, GLShaderStrings.VRamViewFragment);

            ResetDepthShader = new GLShader(GLShaderStrings.ResetDepthVertex, GLShaderStrings.ResetDepthFragment);

            DisplayShader = new GLShader(GLShaderStrings.DisplayVertex, GLShaderStrings.DisplayFragment);

            //GetPixelsShader = new GLShader(GLShaderStrings.GetPixelsVertex, GLShaderStrings.GetPixelsFragment);

            CopyShader = new GLCopyShader();

            int Stride = sizeof(Vertex);

            int colorOffset = Marshal.OffsetOf(typeof(Vertex), "v_color").ToInt32();
            int texCoordOffset = Marshal.OffsetOf(typeof(Vertex), "v_texCoord").ToInt32();
            int clutOffset = Marshal.OffsetOf(typeof(Vertex), "v_clut").ToInt32();
            int texPageOffset = Marshal.OffsetOf(typeof(Vertex), "v_texPage").ToInt32();
            int pgxpOffset = Marshal.OffsetOf(typeof(Vertex), "v_pos_high").ToInt32();

            DrawInfo.v_pos.SetData<short>(VertexsBuffer, 4, 0, Stride, false);
            DrawInfo.v_pos_high.SetData<float>(VertexsBuffer, 3, pgxpOffset, Stride, false);
            DrawInfo.v_color.SetData<byte>(VertexsBuffer, 3, colorOffset, Stride, true);
            DrawInfo.v_texCoord.SetData<short>(VertexsBuffer, 2, texCoordOffset, Stride, false);

            DrawInfo.v_clut.SetIntData<ushort>(VertexsBuffer, 1, clutOffset, Stride);
            DrawInfo.v_texPage.SetIntData<ushort>(VertexsBuffer, 1, texPageOffset, Stride);

            CreateFramebuffers();

            TransferTexture = GLTexture2D.Create();
            TransferFB = GLFrameBuffer.Create();
            TransferFB.AttachTexture(FramebufferAttachment.ColorAttachment0, TransferTexture);
            TransferFB.Unbind();

            DisplayTexture = GLTexture2D.Create();
            DisplayTexture.SetFilter(TextureMinFilter.Linear);
            DisplayFB = GLFrameBuffer.Create();
            DisplayFB.AttachTexture(FramebufferAttachment.ColorAttachment0, DisplayTexture);
            DisplayFB.Unbind();

            ReadFB.Clear();
            DrawFB.Clear();

            ResetDirtyArea();

            RestoreRenderState();

            BuildMVP();

            RealColor = true;

            IRScale = 1;

            _ThreadID = Thread.CurrentThread.ManagedThreadId;

            _DeviceContext.ReleaseCurrent();
        }

        private void BuildMVP()
        {
            // PS1 虚拟视口范围：X∈[-256,256]，Y∈[-192,192]，Z∈[0,1]（归一化后）
            float left = -256.0f;
            float right = 256.0f;
            float bottom = -192.0f;
            float top = 192.0f;
            float nearPlane = 0.0f;   // PS1 Z 最小值（近）
            float farPlane = 1.0f;    // PS1 Z 最大值（远）

            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
                left, right,
                bottom, top,
                farPlane, nearPlane  // 交换 near/far，让 Z 轴方向适配 OpenGL 右手系
            );

            Matrix4x4 view = Matrix4x4.Identity;   // PS1 视角无额外变换
            Matrix4x4 model = Matrix4x4.Identity;  // 模型变换GTE处理

            m_mvpMatrix = Matrix4x4.Multiply(Matrix4x4.Multiply(projection, view), model);

            m_mvpMatrix = Matrix4x4.Transpose(m_mvpMatrix);

            mvpArray = new float[16]
            {
                m_mvpMatrix.M11, m_mvpMatrix.M21, m_mvpMatrix.M31, m_mvpMatrix.M41,
                m_mvpMatrix.M12, m_mvpMatrix.M22, m_mvpMatrix.M32, m_mvpMatrix.M42,
                m_mvpMatrix.M13, m_mvpMatrix.M23, m_mvpMatrix.M33, m_mvpMatrix.M43,
                m_mvpMatrix.M14, m_mvpMatrix.M24, m_mvpMatrix.M34, m_mvpMatrix.M44
            };
        }

        private unsafe void CreateFramebuffers()
        {
            DrawTexture = GLTexture2D.Create().SetData(
                InternalFormat.Rgba8,
                GetVRamTextureWidth(),
                GetVRamTextureHeight(),
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                null
            );

            DrawDepthTexture = GLTexture2D.Create().SetData(
                InternalFormat.DepthComponent16,
                GetVRamTextureWidth(),
                GetVRamTextureHeight(),
                PixelFormat.DepthComponent,
                PixelType.Short,
                null
            );

            DrawFB = GLFrameBuffer.Create();

            DrawFB.AttachTexture(FramebufferAttachment.ColorAttachment0, DrawTexture);

            DrawFB.AttachTexture(FramebufferAttachment.DepthAttachment, DrawDepthTexture);

            DrawFB.Unbind();

            ReadTexture = GLTexture2D.Create().SetData(
                InternalFormat.Rgba8,
                GetVRamTextureWidth(),
                GetVRamTextureHeight(),
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                null
            );

            ReadTexture.SetWrap(TextureWrapMode.Repeat);

            ReadFB = GLFrameBuffer.Create();

            ReadFB.AttachTexture(FramebufferAttachment.ColorAttachment0, ReadTexture);

            ReadFB.Unbind();
        }

        public unsafe void Dispose()
        {
            if (isDisposed)
                return;

            DisplayTexture.Dispose();
            TransferTexture.Dispose();
            DrawTexture.Dispose();
            ReadTexture.Dispose();
            DrawDepthTexture.Dispose();

            DisplayFB.Dispose();
            DrawFB.Dispose();
            ReadFB.Dispose();
            TransferFB.Dispose();

            VertexsBuffer.Dispose();

            CopyShader.Dispose();
            DisplayShader.Dispose();
            ResetDepthShader.Dispose();
            Out16Shader.Dispose();
            Out24Shader.Dispose();
            RamViewShader.Dispose();
            DrawShader.Dispose();
            //GetPixelsShader.Dispose();

            _DeviceContext.ReleaseCurrent();

            _DeviceContext.Dispose();

            Marshal.FreeHGlobal((IntPtr)VRAM);

            Console.WriteLine($"[OpenGL GPU] Disposed");

            isDisposed = true;
        }

        private void SetRealColor(bool realColor)
        {
            if (m_realColor != realColor)
            {
                m_realColor = realColor;
                DrawInfo.u_realColor.Set(m_realColor ? 1 : 0);
            }
        }

        private void SetPGXP(bool pgxp)
        {
            if (m_pgxp != pgxp)
            {
                m_pgxp = pgxp;
                DrawInfo.u_pgxp.Set(m_pgxp ? 1 : 0);
            }
        }

        private bool SetResolutionScale(int scale)
        {
            if (scale < 1 || scale > 12)
                return false;

            if (scale == resolutionScale)
                return true;

            int newWidth = VRAM_WIDTH * scale;
            int newHeight = VRAM_HEIGHT * scale;
            int maxTextureSize = GL.GetInteger(GL.GL_MAX_TEXTURE_SIZE);
            if (newWidth > maxTextureSize || newHeight > maxTextureSize)
                return false;

            int oldWidth = VRAM_WIDTH * resolutionScale;
            int oldHeight = VRAM_HEIGHT * resolutionScale;

            resolutionScale = scale;

            var oldFramebuffer = DrawFB;
            var oldDrawTexture = DrawTexture;
            var oldDepthBuffer = DrawDepthTexture;

            CreateFramebuffers();

            GL.Disable((int)EnableCap.ScissorTest);

            oldFramebuffer.Bind(FramebufferTarget.ReadFramebuffer);

            DrawFB.Bind(FramebufferTarget.DrawFramebuffer);

            GL.BlitFramebuffer(
                0, 0, oldWidth, oldHeight,
                0, 0, newWidth, newHeight,
                (int)ClearBufferMask.ColorBufferBit | (int)ClearBufferMask.DepthBufferBit,
                (int)BlitFramebufferFilter.Nearest
            );

            ReadFB.Bind(FramebufferTarget.DrawFramebuffer);

            GL.BlitFramebuffer(
                0, 0, oldWidth, oldHeight,
                0, 0, newWidth, newHeight,
                (int)ClearBufferMask.ColorBufferBit,
                (int)BlitFramebufferFilter.Nearest
            );

            oldFramebuffer.Dispose();
            oldDrawTexture.Dispose();
            oldDepthBuffer.Dispose();

            RestoreRenderState();

            Console.WriteLine($"[OpenGL GPU] ResolutionScale {scale}");
            return true;
        }

        public void SetParams(int[] Params)
        {
        }

        public void LoadShader(string ShaderDir)
        {
            DirectoryInfo dir = new DirectoryInfo(ShaderDir);

            if (!dir.Exists)
            {
                Console.WriteLine($"[OPENGL] Shader directory not found: {ShaderDir}");
                return;
            }

            string vertexShaderSource = null;
            string fragmentShaderSource = null;

            foreach (FileInfo f in dir.GetFiles("*.VS", SearchOption.TopDirectoryOnly))
            {
                vertexShaderSource = File.ReadAllText(f.FullName);
                //Console.WriteLine($"vertexShaderSource load: {f.FullName}\n");
                break;
            }

            foreach (FileInfo f in dir.GetFiles("*.FS", SearchOption.TopDirectoryOnly))
            {
                fragmentShaderSource = File.ReadAllText(f.FullName);
                //Console.WriteLine($"fragmentShaderSource load: {f.FullName}\n");
                break;
            }
            UserShader = new GLShader(vertexShaderSource, fragmentShaderSource);
        }

        public void THREADCHANGE()
        {
            if (_ThreadID != Thread.CurrentThread.ManagedThreadId)
            {
                _ThreadID = Thread.CurrentThread.ManagedThreadId;

                Console.WriteLine($"[OpenGL GPU] MakeCurrent TID: {Thread.CurrentThread.ManagedThreadId}");

                _DeviceContext.MakeCurrent();

                //Gl.LoadAll();
            }
        }

        public unsafe void SetRam(byte[] Ram)
        {
            _DeviceContext.MakeCurrent();

            var oldvramtrans = _VRAMTransfer;

            Marshal.Copy(Ram, 0, (IntPtr)VRAM, Ram.Length);

            _VRAMTransfer.OriginX = 0;
            _VRAMTransfer.OriginY = 0;
            _VRAMTransfer.X = 0;
            _VRAMTransfer.Y = 0;
            _VRAMTransfer.W = 1024;
            _VRAMTransfer.H = 512;

            //TransferBasePtr = VRAM;

            CopyRectCPUtoVRAM(0, 0, 1024, 512);

            _VRAMTransfer = oldvramtrans;

            _DeviceContext.ReleaseCurrent();
        }

        public unsafe byte[] GetRam()
        {
            //var oldvramtrans = _VRAMTransfer;

            //_VRAMTransfer.OriginX = 0;
            //_VRAMTransfer.OriginY = 0;
            //_VRAMTransfer.X = 0;
            //_VRAMTransfer.Y = 0;
            //_VRAMTransfer.W = 1024;
            //_VRAMTransfer.H = 512;

            //CopyRectVRAMtoCPU(0, 0, 1024, 512);

            //_VRAMTransfer = oldvramtrans;

            byte[] data = new byte[(1024 * 512) * 2];
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
            THREADCHANGE();

            int offsetline = ((DisplayVerticalEnd - DisplayVerticalStart)) >> (h == 480 ? 0 : 1);

            if (offsetline < 0)
                return (0, -1);

            const int LineFix = 3;

            m_vramDisplayArea.x = rx;
            m_vramDisplayArea.y = ry + LineFix;
            m_vramDisplayArea.width = w;
            m_vramDisplayArea.height = offsetline * 2 - LineFix;

            m_targetDisplayArea.x = 0;
            m_targetDisplayArea.y = LineFix;
            m_targetDisplayArea.width = w;
            m_targetDisplayArea.height = offsetline * 2 - LineFix;

            DrawBatch();

            DrawFB.Unbind();

            GL.Disable((int)EnableCap.ScissorTest);
            GL.Disable((int)EnableCap.Blend);
            GL.Disable((int)EnableCap.DepthTest);

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

            if (targetWidth != DisplayTexture.Width || targetHeight != DisplayTexture.Height)
            {
                DisplayTexture.SetData(
                    InternalFormat.Rgb,
                    (int)targetWidth,
                    (int)targetHeight,
                    PixelFormat.Rgb,
                    PixelType.UnsignedByte,
                    null
                );
            }

            //DrawToDisplayFB
            DisplayFB.Bind();
            GL.Viewport(0, 0, (int)targetWidth, (int)targetHeight);
            GL.Clear((int)ClearBufferMask.ColorBufferBit);

            DrawTexture.Bind();

            if (ViewVRam)
            {
                RamViewShader.Use();
                GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);

            } else if (DisplayEnable)
            {
                if (is24bit)
                {
                    Out24Shader.Use();
                    Shader24Info.u_srcRect.Set(m_vramDisplayArea.x, m_vramDisplayArea.y, m_vramDisplayArea.width, m_vramDisplayArea.height);
                } else
                {
                    Out16Shader.Use();
                    Shader16Info.u_srcRect.Set(m_vramDisplayArea.x, m_vramDisplayArea.y, m_vramDisplayArea.width, m_vramDisplayArea.height);
                }
                DrawTexture.Bind();
                GL.Viewport(0, 0, (int)srcWidth, (int)srcHeight);
                GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);
            }

            DisplayFB.Unbind();

            //DrawToWindow;
            int winWidth = NullRenderer.ClientWidth;
            int winHeight = NullRenderer.ClientHeight;

            if (winWidth == 0 || winHeight == 0)
                return (0, -1);

            GL.Viewport(0, 0, winWidth, winHeight);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear((int)ClearBufferMask.ColorBufferBit);

            DisplayTexture.Bind();

            DisplayShader.Use();

            if (KEEPAR)
            {
                float displayWidth = srcWidth;
                float displayHeight = ViewVRam ? srcHeight : (displayWidth / AspectRatio);

                float renderScale = Math.Min(winWidth / displayWidth, winHeight / displayHeight);

                if (!StretchToFit)
                    renderScale = Math.Max(1.0f, (float)Math.Floor(renderScale));

                int renderWidth = (int)(displayWidth * renderScale);
                int renderHeight = (int)(displayHeight * renderScale);
                int renderX = (winWidth - renderWidth) / 2;
                int renderY = (winHeight - renderHeight) / 2;

                GL.Viewport(renderX, renderY, renderWidth, renderHeight);
            }

            GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);

            _DeviceContext.SwapBuffers();

            RestoreRenderState();

            PGXPVector.Clear();

            if (SyncVram)
            {
                var oldvramtrans = _VRAMTransfer;

                _VRAMTransfer.OriginX = 0;
                _VRAMTransfer.OriginY = 0;
                _VRAMTransfer.X = 0;
                _VRAMTransfer.Y = 0;
                _VRAMTransfer.W = 1024;
                _VRAMTransfer.H = 512;

                CopyRectVRAMtoCPU(0, 0, 1024, 512);

                _VRAMTransfer = oldvramtrans;

                SyncVram = false;
            }

            //参数变动
            if (RealColor != m_realColor)
            {
                SetRealColor(RealColor);
            }
            if (IRScale != resolutionScale)
            {
                SetResolutionScale(IRScale);
            }
            PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
            if (PGXP != m_pgxp)
            {
                SetPGXP(PGXP);
            }

            return (targetWidth, targetHeight);
        }

        public void SetMaskBit(uint value)
        {
            if (oldmaskbit != value)
            {
                oldmaskbit = value;

                DrawBatch();

                ForceSetMaskBit = ((value & 1) != 0);
                CheckMaskBit = (((value >> 1) & 1) != 0);

                UpdateMaskBits();
            }
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            THREADCHANGE();

            if (DrawingAreaTopLeft != value)
            {
                DrawBatch();

                DrawingAreaTopLeft = value;

                UpdateScissorRect();
            }
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            if (DrawingAreaBottomRight != value)
            {
                DrawBatch();

                DrawingAreaBottomRight = value;

                UpdateScissorRect();
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

                DrawInfo.u_texWindowMask.Set(TextureWindowXMask, TextureWindowYMask);
                DrawInfo.u_texWindowOffset.Set(TextureWindowXOffset, TextureWindowYOffset);

                //Console.WriteLine($"[OpenGL GPU] TextureWindow set: Mask=({TextureWindowXMask},{TextureWindowYMask}) Offset=({TextureWindowXOffset},{TextureWindowYOffset})");
            }
        }

        private void SetDrawMode(ushort vtexPage, ushort vclut, bool dither)
        {
            if (m_realColor)
                dither = false;

            if (m_dither != dither)
            {
                DrawBatch();
                m_dither = dither;
                DrawInfo.u_dither.Set(dither ? 1 : 0);
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
            }
            // 如果仅 CLUT 发生变化且使用纹理和 CLUT，则更新 CLUT
            else if (m_clut.Value != vclut && !m_TexPage.TextureDisable && m_TexPage.TexturePageColors < 2)
            {
                DrawBatch();
                UpdateClut(vclut);
            }

            // 如果纹理页或 CLUT 区域是脏区域，则更新读取纹理
            if (IntersectsTextureData(m_dirtyArea))
                UpdateReadTexture();
        }

        public unsafe void TransferStart(VRAMTransfer val)
        {
            _VRAMTransfer = val;

            if (_VRAMTransfer.isRead)
            {
                CopyRectVRAMtoCPU(_VRAMTransfer.OriginX, _VRAMTransfer.OriginY, _VRAMTransfer.W, _VRAMTransfer.H);
            }
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

        public void TransferDone()
        {
            CopyRectCPUtoVRAM(_VRAMTransfer.OriginX, _VRAMTransfer.OriginY, _VRAMTransfer.W, _VRAMTransfer.H);
        }

        public void FillRectVRAM(ushort left, ushort top, ushort width, ushort height, uint colorval)
        {
            GrowDirtyArea(GetWrappedBounds(left, top, width, height));

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

            const float MaskBitAlpha = 0.0f;
            const float MaskBitDepth = 1.0f;

            GL.ClearColor(rF, gF, bF, MaskBitAlpha);
            GL.ClearDepthf(MaskBitDepth);

            bool wrapX = left + width > VRAM_WIDTH;
            bool wrapY = top + height > VRAM_HEIGHT;

            int width2 = wrapX ? (left + width - VRAM_WIDTH) : 0;
            int height2 = wrapY ? (top + height - VRAM_HEIGHT) : 0;
            int width1 = width - width2;
            int height1 = height - height2;

            // 清除第一部分（右下角）
            SetScissor(left, top, width1, height1);
            GL.Clear((int)ClearBufferMask.ColorBufferBit | (int)ClearBufferMask.DepthBufferBit);

            // 如果需要水平环绕
            if (wrapX)
            {
                SetScissor(0, top, width2, height1);
                GL.Clear((int)ClearBufferMask.ColorBufferBit | (int)ClearBufferMask.DepthBufferBit);
            }

            // 如果需要垂直环绕
            if (wrapY)
            {
                SetScissor(left, 0, width1, height2);
                GL.Clear((int)ClearBufferMask.ColorBufferBit | (int)ClearBufferMask.DepthBufferBit);
            }

            // 如果同时需要水平和垂直环绕
            if (wrapX && wrapY)
            {
                SetScissor(0, 0, width2, height2);
                GL.Clear((int)ClearBufferMask.ColorBufferBit | (int)ClearBufferMask.DepthBufferBit);
            }

            // 恢复剪裁区域
            UpdateScissorRect();
        }

        public void CopyRectVRAMtoVRAM(ushort srcX, ushort srcY, ushort destX, ushort destY, ushort width, ushort height)
        {
            if (srcX == destX && srcY == destY)
                return;

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

            //if (PGXPVector.Find((short)srcX, (short)srcY, out var highsrc))
            //{
            //    Console.WriteLine($"[OpenGL GPU] CopyRectVRAMtoVRAM: PGXP Vector found src ({srcX}, {srcY}) Position {highsrc}");
            //}

            //if (PGXPVector.Find((short)destX, (short)destY, out var highdst))
            //{
            //    Console.WriteLine($"[OpenGL GPU] CopyRectVRAMtoVRAM: PGXP Vector found dst ({destX}, {destY}) Position {highdst}");
            //}

            UpdateCurrentDepth();

            CopyShader.Use(
                srcX / VRamWidthF,
                srcY / VRamHeightF,
                width / VRamWidthF,
                height / VRamHeightF,
                GetNormalizedDepth(),
                ForceSetMaskBit
            );

            GL.Disable((int)EnableCap.Blend);
            GL.Disable((int)EnableCap.ScissorTest);

            SetViewport((int)destX, (int)destY, (int)width, (int)height);

            GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);

            RestoreRenderState();
        }

        public unsafe void CopyRectVRAMtoCPU(int left, int top, int width, int height)
        {
            var readBounds = GetWrappedBounds(left, top, width, height);

            if (m_dirtyArea.Intersects(readBounds))
                DrawBatch();

            //if (PGXPVector.Find((short)left, (short)top, out var highsrc))
            //{
            //    Console.WriteLine($"[OpenGL GPU] CopyRectVRAMtoCPU: PGXP Vector found src ({left}, {top}) Position {highsrc}");
            //}

            int readWidth = readBounds.GetWidth();
            int readHeight = readBounds.GetHeight();


            if (TransferTexture.Width != readWidth || TransferTexture.Height != readHeight)
            {
                TransferTexture.SetData(
                    InternalFormat.Rgba,
                    readWidth,
                    readHeight,
                    PixelFormat.Rgba,
                    PixelType.UnsignedShort1555Rev,
                    null
                );
            }

            if (!TransferFB.IsComplete())
                Console.WriteLine("[OPENGL GPU] Error: VRAMtoCPU Framebuffer is incomplete.");

            TransferFB.Bind(FramebufferTarget.DrawFramebuffer);

            DrawFB.Bind(FramebufferTarget.ReadFramebuffer);

            GL.Disable((int)EnableCap.ScissorTest);

            var srcArea = readBounds.Scale(resolutionScale);

            GL.BlitFramebuffer(
                srcArea.Left, srcArea.Top,
                srcArea.Right, srcArea.Bottom,
                0, 0,
                readWidth, readHeight,
                (int)ClearBufferMask.ColorBufferBit,
                (int)BlitFramebufferFilter.Linear
            );

            // 解包像素数据到 vram 数组
            TransferFB.Bind(FramebufferTarget.ReadFramebuffer);

            GL.PixelStorei((int)PixelStoreParameter.PackAlignment, GetPixelStoreAlignment(left, width));
            GL.PixelStorei((int)PixelStoreParameter.PackRowLength, VRAM_WIDTH);
            //Gl.PixelStore(PixelStoreParameter.PackRowLength, _VRAMTransfer.W);

            GL.ReadPixels(
                0, 0,
                readWidth, readHeight,
                (int)PixelFormat.Rgba,
                (int)PixelType.UnsignedShort1555Rev,
                (VRAM + readBounds.Left + readBounds.Top * VRAM_WIDTH)
            );

            // 恢复渲染状态
            DrawFB.Bind();

            GL.Enable((int)EnableCap.ScissorTest);

            GL.PixelStorei((int)PixelStoreParameter.PackAlignment, 4);
            GL.PixelStorei((int)PixelStoreParameter.PackRowLength, 0);
        }

        public unsafe void CopyRectCPUtoVRAM(int left, int top, int width, int height)
        {
            var updateBounds = GetWrappedBounds(left, top, width, height);

            GrowDirtyArea(updateBounds);

            //if (PGXPVector.Find((short)left, (short)top, out var highsrc))
            //{
            //    Console.WriteLine($"[OpenGL GPU] CopyRectCPUtoVRAM: PGXP Vector found src ({left}, {top}) Position {highsrc}");
            //}

            GL.PixelStorei((int)PixelStoreParameter.UnpackAlignment, GetPixelStoreAlignment(left, width));

            bool wrapX = (left + width) > VRAM_WIDTH;
            bool wrapY = (top + height) > VRAM_HEIGHT;

            if (!wrapX && !wrapY && !CheckMaskBit && !ForceSetMaskBit && resolutionScale == 1)
            {
                //Console.WriteLine($"[OpenGL GPU] CPUtoVRAM 1 {left} {top} [ {width} x {height} ]");
                DrawTexture.SubData(
                    (int)left,
                    (int)top,
                    (int)width,
                    (int)height,
                    PixelFormat.Rgba,
                    PixelType.UnsignedShort1555Rev,
                    (VRAM + left + top * width)
                );

                ResetDepthBuffer();

            } else
            {
                //Console.WriteLine($"[OpenGL GPU] CPUtoVRAM 2 {left} {top} [ {width} x {height} ]");
                UpdateCurrentDepth();

                TransferTexture.SetData(
                    InternalFormat.Rgba,
                    (int)width,
                    (int)height,
                    PixelFormat.Rgba,
                    PixelType.UnsignedShort1555Rev,
                    (VRAM + left + top * width)
                );

                // 计算宽度和高度的分段
                int width2 = wrapX ? (left + width) % VRAM_WIDTH : 0;
                int height2 = wrapY ? (top + height) % VRAM_HEIGHT : 0;
                int width1 = width - width2;
                int height1 = height - height2;

                float width1f = (float)width1 / width;
                float height1f = (float)height1 / height;
                float width2f = (float)width2 / width;
                float height2f = (float)height2 / height;

                GL.Disable((int)EnableCap.Blend);
                GL.Disable((int)EnableCap.ScissorTest);

                CopyShader.Use(0, 0, width1f, height1f, GetNormalizedDepth(), ForceSetMaskBit);

                TransferTexture.Bind();

                // 右下角
                SetViewport((int)left, (int)top, (int)width1, (int)height1);
                GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);

                // 左下角
                if (wrapX)
                {
                    CopyShader.SetSourceArea(width1f, 0, width2f, height1f);
                    SetViewport(0, (int)top, (int)width2, (int)height1);
                    GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);
                }

                // 右上角
                if (wrapY)
                {
                    CopyShader.SetSourceArea(0, height1f, width1f, height2f);
                    SetViewport((int)left, 0, (int)width1, (int)height2);
                    GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);
                }

                // 左上角
                if (wrapX && wrapY)
                {
                    CopyShader.SetSourceArea(width1f, height1f, width2f, height2f);
                    SetViewport(0, 0, (int)width2, (int)height2);
                    GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);
                }

                RestoreRenderState();
            }

            GL.PixelStorei((int)PixelStoreParameter.UnpackAlignment, 4);
        }

        #region Draw Functions

        private void DrawBatch()
        {
            if (Vertexs.Count == 0)
                return;

            VertexsBuffer.SubData<Vertex>(Vertexs.Count, Vertexs.ToArray());

            if (PGXP)
            {
                GL.Enable((int)EnableCap.DepthTest);
                GL.DepthFunc((int)DepthFunction.Always); //Less
                GL.DepthMask(true);
                //DrawInfo.u_mvp.Set(m_mvpMatrix);
            }

            if (m_semiTransparencyEnabled && (m_semiTransparencyMode == 2) && !m_TexPage.TextureDisable)
            {
                // 必须对带有纹理的背景和前景进行两次渲染，因为透明度可以逐像素禁用

                // 仅绘制不透明像素
                GL.Disable((int)EnableCap.Blend);
                DrawInfo.u_drawTransparentPixels.Set(0);
                GL.DrawArrays((int)PrimitiveType.Triangles, 0, Vertexs.Count);

                // 仅绘制透明像素
                GL.Enable((int)EnableCap.Blend);
                DrawInfo.u_drawOpaquePixels.Set(0);
                DrawInfo.u_drawTransparentPixels.Set(1);
                GL.DrawArrays((int)PrimitiveType.Triangles, 0, Vertexs.Count);

                DrawInfo.u_drawOpaquePixels.Set(1);
            } else
            {
                GL.DrawArrays((int)PrimitiveType.Triangles, 0, Vertexs.Count);
            }

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

                if (PGXP)
                {
                    vertices[i].v_pos_high = new Vector3((float)vertices[i].v_pos.x, (float)vertices[i].v_pos.y, (float)vertices[i].v_pos.z);
                    //PGXPVector.HighPos HighPos;
                    //if (PGXPVector.Find(vertices[i].v_pos.x, vertices[i].v_pos.y, out HighPos))
                    //{
                    //    vertices[i].v_pos_high = new Vector3((float)HighPos.x, (float)HighPos.y, (float)HighPos.z);
                    //}
                }
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
            }

            Vertexs.AddRange(vertices);
        }

        #endregion

        #region Helper Functions

        private void CheckRenderErrors(int tag = 0)
        {
            var error = GL.GetError();
            if (error != GL.GL_NO_ERROR)
                Console.WriteLine($"OpenGL {tag} Error: {error}");
        }

        private void RestoreRenderState()
        {
            DrawFB.Bind(FramebufferTarget.Framebuffer);
            ReadTexture.Bind();
            DrawShader.Use();

            GL.Disable((int)EnableCap.CullFace);
            GL.Enable((int)EnableCap.ScissorTest);
            GL.Enable((int)EnableCap.DepthTest);

            UpdateScissorRect();
            UpdateBlendMode();
            UpdateMaskBits();

            DrawInfo.u_drawOpaquePixels.Set(1);
            DrawInfo.u_drawTransparentPixels.Set(1);
            DrawInfo.u_dither.Set(m_dither ? 1 : 0);
            DrawInfo.u_realColor.Set(m_realColor ? 1 : 0);
            DrawInfo.u_texWindowMask.Set(TextureWindowXMask, TextureWindowYMask);
            DrawInfo.u_texWindowOffset.Set(TextureWindowXOffset, TextureWindowYOffset);
            DrawInfo.u_resolutionScale.Set((float)resolutionScale);

            SetViewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
        }

        private int GetPixelStoreAlignment(int x, int w)
        {
            bool odd = (x % 2 != 0) || (w % 2 != 0);
            return odd ? 2 : 4;
        }

        private int GetVRamTextureWidth()
        {
            return (VRAM_WIDTH * resolutionScale);
        }

        private int GetVRamTextureHeight()
        {
            return (VRAM_HEIGHT * resolutionScale);
        }

        private void UpdateScissorRect()
        {
            ScissorBox_X = DrawingAreaTopLeft.X;
            ScissorBox_Y = DrawingAreaTopLeft.Y;

            ScissorBoxWidth = Math.Max(DrawingAreaBottomRight.X - DrawingAreaTopLeft.X + 1, 0);
            ScissorBoxHeight = Math.Max(DrawingAreaBottomRight.Y - DrawingAreaTopLeft.Y + 1, 0);

            SetScissor(ScissorBox_X, ScissorBox_Y, ScissorBoxWidth, ScissorBoxHeight);
        }

        private void SetViewport(int left, int top, int width, int height)
        {
            GL.Viewport(
                (left * resolutionScale),
                (top * resolutionScale),
                (width * resolutionScale),
                (height * resolutionScale)
            );
        }

        private void SetScissor(int left, int top, int width, int height)
        {
            GL.Scissor(
                (left * resolutionScale),
                (top * resolutionScale),
                (width * resolutionScale),
                (height * resolutionScale)
            );
        }

        private void UpdateBlendMode()
        {
            if (m_semiTransparencyEnabled)
            {
                GL.Enable((int)EnableCap.Blend);

                BlendEquationMode rgbEquation = BlendEquationMode.FuncAdd;
                float srcBlend = 1.0f;
                float destBlend = 1.0f;

                switch (m_semiTransparencyMode)
                {
                    case 0:
                        srcBlend = 0.5f;
                        destBlend = 0.5f;
                        break;
                    case 1:
                        break;
                    case 2:
                        rgbEquation = BlendEquationMode.FuncReverseSubtract;
                        break;
                    case 3:
                        srcBlend = 0.25f;
                        break;
                }

                GL.BlendEquationSeparate((int)rgbEquation, (int)BlendEquationMode.FuncAdd);
                GL.BlendFuncSeparate((int)BlendingFactor.Src1Alpha, (int)BlendingFactor.Src1Color, (int)BlendingFactor.One, (int)BlendingFactor.Zero);

                DrawInfo.u_srcBlend.Set(srcBlend);
                DrawInfo.u_destBlend.Set(destBlend);
            } else
            {
                GL.Disable((int)EnableCap.Blend);
            }
        }

        private void UpdateMaskBits()
        {
            DrawInfo.u_setMaskBit.Set(ForceSetMaskBit ? 1 : 0);
            GL.DepthFunc(CheckMaskBit ? (int)DepthFunction.Lequal : (int)DepthFunction.Always);
        }

        private void UpdateReadTexture()
        {
            if (m_dirtyArea.Empty())
                return;

            DrawBatch();

            ReadFB.Bind(FramebufferTarget.DrawFramebuffer);

            GL.Disable((int)EnableCap.ScissorTest);

            var blitArea = m_dirtyArea.Scale(resolutionScale);

            GL.BlitFramebuffer(
                blitArea.Left, blitArea.Top, blitArea.Right, blitArea.Bottom,
                blitArea.Left, blitArea.Top, blitArea.Right, blitArea.Bottom,
                (int)ClearBufferMask.ColorBufferBit,
                (int)BlitFramebufferFilter.Nearest
            );

            DrawFB.Bind(FramebufferTarget.DrawFramebuffer);

            GL.Enable((int)EnableCap.ScissorTest);

            ResetDirtyArea();
        }

        private void ResetDepthBuffer()
        {
            DrawBatch();

            m_currentDepth = 1;

            GL.Disable((int)EnableCap.ScissorTest);
            GL.Disable((int)EnableCap.Blend);
            GL.ColorMask(false, false, false, false);
            GL.DepthFunc((int)DepthFunction.Always);

            DrawDepthTexture.Bind();
            ResetDepthShader.Use();

            GL.DrawArrays((int)PrimitiveType.TriangleStrip, 0, 4);

            GL.ColorMask(true, true, true, true);

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

        public void SetSemiTransparencyMode(byte semiTransparencyMode)
        {
            if (m_semiTransparencyMode != semiTransparencyMode)
            {
                if (m_semiTransparencyEnabled)
                    DrawBatch();

                m_semiTransparencyMode = semiTransparencyMode;

                if (m_semiTransparencyEnabled)
                    UpdateBlendMode();
            }
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

        private bool IsDrawAreaValid()
        {
            return DrawingAreaTopLeft.X <= DrawingAreaBottomRight.X && DrawingAreaTopLeft.Y <= DrawingAreaBottomRight.Y;
        }

        private float GetNormalizedDepth()
        {
            return (float)m_currentDepth / (float)short.MaxValue;
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

        private void Reset()
        {
            GL.Disable((int)EnableCap.ScissorTest);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ClearDepthf(1.0f);

            ReadFB.Clear().Bind();
            DrawFB.Clear().Bind();

            m_semiTransparencyMode = 0;
            m_semiTransparencyEnabled = false;
            CheckMaskBit = false;
            ForceSetMaskBit = false;
            m_dither = false;

            m_TexPage.Value = 0;
            m_TexPage.TextureDisable = true;
            m_clut.Value = 0;

            TextureWindowXMask = TextureWindowYMask = 0;
            TextureWindowXOffset = TextureWindowYOffset = 0;

            Vertexs.Clear();

            ResetDirtyArea();

            m_textureArea = new glRectangle<int>();

            m_clutArea = new glRectangle<int>();

            m_currentDepth = 1;

            RestoreRenderState();
        }

        #endregion

    }
}
