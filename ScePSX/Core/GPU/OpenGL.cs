using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Khronos;
using OpenGL;
using SDL2;

namespace ScePSX
{
    public class OpenglGPU : IGPU
    {
        public GPUType type => GPUType.OpenGL;

        private TDrawingArea DrawingAreaTopLeft, DrawingAreaBottomRight;

        private TDrawingOffset DrawingOffset;

        private VRAMTransfer _VRAMTransfer;

        private uint TextureWindowXMask, TextureWindowYMask, TextureWindowXOffset, TextureWindowYOffset;

        private bool PreserveMaskedPixels, ForceSetMaskBit;

        INativePBuffer pbuffer;

        nint _GlContext;

        DeviceContext _DeviceContext;

        GlShader Shader;

        private IntPtr _window;

        public OpenglGPU()
        {
            //调试用窗口
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);

            _window = SDL.SDL_CreateWindow(
                "OpenGL Debug Window",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                1024, 512,
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            SDL.SDL_SysWMinfo info = new SDL.SDL_SysWMinfo();
            SDL.SDL_GetWindowWMInfo(_window, ref info);

            DeviceContext.DefaultAPI = KhronosVersion.ApiGl;
            //pbuffer = DeviceContext.CreatePBuffer(pixelFormat, 4096, 2160);
            //_DeviceContext = DeviceContext.Create(pbuffer);
            _DeviceContext = DeviceContext.Create(info.info.win.hdc, info.info.win.window);
            _DeviceContext.IncRef();
            var pixelFormat = new DevicePixelFormat(32)
            {
                DoubleBuffer = true,
                DepthBits = 24,
                StencilBits = 8,
                MultisampleBits = 0
            };
            DevicePixelFormatCollection pixelFormats = _DeviceContext.PixelsFormats;
            List<DevicePixelFormat> matchingPixelFormats = pixelFormats.Choose(pixelFormat);
            _DeviceContext.SetPixelFormat(matchingPixelFormats[0]);

            int[] attribs = {
                Glx.CONTEXT_MAJOR_VERSION_ARB, 4,
                Glx.CONTEXT_MINOR_VERSION_ARB, 6,
                Glx.CONTEXT_FLAGS_ARB, (int)Glx.CONTEXT_FORWARD_COMPATIBLE_BIT_ARB,
                Glx.CONTEXT_PROFILE_MASK_ARB, (int)Glx.CONTEXT_COMPATIBILITY_PROFILE_BIT_ARB,
                0
            };
            _GlContext = _DeviceContext.CreateContextAttrib(IntPtr.Zero, attribs, new KhronosVersion(4, 6, 0, "gl", "compatibility"));
            _DeviceContext.MakeCurrent(_GlContext);

            Gl.BindAPI();

            string glVersion = Gl.GetString(StringName.Version);
            Console.WriteLine($"[OpenGL GPU]: {glVersion}");
        }

        const int VRAM_WIDTH = 1024;
        const int VRAM_HEIGHT = 512;

        private uint VertexArrayObject;
        private uint VertexBufferObject;
        private uint ColorsBuffer;
        private uint VramTexture;
        private uint VramFrameBuffer;
        private uint SampleTexture;
        private uint TexCoords;
        private int TexWindow;

        private int IsCopy;
        private int TexModeLoc;
        private int MaskBitSettingLoc;
        private int ClutLoc;
        private int TexPageLoc;

        private int Display_Area_X_Start_Loc;
        private int Display_Area_Y_Start_Loc;
        private int Display_Area_X_End_Loc;
        private int Display_Area_Y_End_Loc;

        private int Aspect_Ratio_X_Offset_Loc;
        private int Aspect_Ratio_Y_Offset_Loc;

        private int TransparencyModeLoc;
        private int IsDitheredLoc;
        private int RenderModeLoc;

        int ScissorBox_X = 0;
        int ScissorBox_Y = 0;
        int ScissorBoxWidth = VRAM_WIDTH;
        int ScissorBoxHeight = VRAM_HEIGHT;

        const int IntersectionBlockLength = 64;
        private int[,] IntersectionTable = new int[VRAM_HEIGHT / IntersectionBlockLength, VRAM_WIDTH / IntersectionBlockLength];

        ushort[] DataFormRead;
        ushort[] DataFormWrite;

        short[] Vertices;
        byte[] Colors;
        ushort[] UV;

        List<short> _vertices = new List<short>();
        List<byte> _colors = new List<byte>();

        int tid;

        public void Initialize()
        {
            //Gl.Viewport(0, 0, 1024, 512);
            //Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            //Gl.Clear(ClearBufferMask.ColorBufferBit);
            //_DeviceContext.SwapBuffers();

            Shader = new GlShader();
            Shader.Use();

            TexWindow = Gl.GetUniformLocation(Shader.Program, "u_texWindow");
            TexModeLoc = Gl.GetUniformLocation(Shader.Program, "TextureMode");
            ClutLoc = Gl.GetUniformLocation(Shader.Program, "inClut");
            TexPageLoc = Gl.GetUniformLocation(Shader.Program, "inTexpage");
            IsCopy = Gl.GetUniformLocation(Shader.Program, "isCopy");

            TransparencyModeLoc = Gl.GetUniformLocation(Shader.Program, "transparencyMode");
            MaskBitSettingLoc = Gl.GetUniformLocation(Shader.Program, "maskBitSetting");
            IsDitheredLoc = Gl.GetUniformLocation(Shader.Program, "isDithered");
            RenderModeLoc = Gl.GetUniformLocation(Shader.Program, "renderMode");

            Display_Area_X_Start_Loc = Gl.GetUniformLocation(Shader.Program, "display_area_x_start");
            Display_Area_Y_Start_Loc = Gl.GetUniformLocation(Shader.Program, "display_area_y_start");
            Display_Area_X_End_Loc = Gl.GetUniformLocation(Shader.Program, "display_area_x_end");
            Display_Area_Y_End_Loc = Gl.GetUniformLocation(Shader.Program, "display_area_y_end");

            Aspect_Ratio_X_Offset_Loc = Gl.GetUniformLocation(Shader.Program, "aspect_ratio_x_offset");
            Aspect_Ratio_Y_Offset_Loc = Gl.GetUniformLocation(Shader.Program, "aspect_ratio_y_offset");

            VramFrameBuffer = Gl.GenFramebuffer();

            VertexArrayObject = Gl.GenVertexArray();

            VertexBufferObject = Gl.GenBuffer();
            ColorsBuffer = Gl.GenBuffer();
            TexCoords = Gl.GenBuffer();

            VramTexture = Gl.GenTexture();
            SampleTexture = Gl.GenTexture();

            Gl.BindVertexArray(VertexArrayObject);

            Gl.Enable(EnableCap.Texture2d);

            Gl.BindTexture(TextureTarget.Texture2d, VramTexture);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, VRAM_WIDTH, VRAM_HEIGHT, 0, PixelFormat.Bgra, PixelType.UnsignedShort1555Rev, IntPtr.Zero);

            Gl.BindTexture(TextureTarget.Texture2d, SampleTexture);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, VRAM_WIDTH, VRAM_HEIGHT, 0, PixelFormat.Bgra, PixelType.UnsignedShort1555Rev, IntPtr.Zero);

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, VramFrameBuffer);
            Gl.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, VramTexture, 0);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
            {
                Console.WriteLine("[OpenGL GPU] Uncompleted Frame Buffer!");
            } else
            {
                Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 2);
                Gl.PixelStore(PixelStoreParameter.PackAlignment, 2);

                Gl.Uniform1(Gl.GetUniformLocation(Shader.Program, "u_vramTex"), 0);

                Gl.Uniform1(RenderModeLoc, 0);

                tid = Thread.CurrentThread.ManagedThreadId;

                _DeviceContext.MakeCurrent(0); //解绑

                Console.WriteLine("[OpenGL GPU] Ready!");
            }
        }

        public void Dispose()
        {
            if (tid != Thread.CurrentThread.ManagedThreadId)
            {
                tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"[OpenGL GPU] MakeCurrent TID: {Thread.CurrentThread.ManagedThreadId}");
                _DeviceContext.MakeCurrent(_GlContext);
                Gl.BindAPI(new KhronosVersion(4, 6, 0, "gl", "compatibility"), null);
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.BindVertexArray(0);
            Gl.UseProgram(0);

            uint[] fbuffs = new uint[] { VramFrameBuffer };
            Gl.DeleteFramebuffers(fbuffs);

            uint[] buffs = new uint[] { VertexBufferObject, ColorsBuffer, TexCoords };
            Gl.DeleteBuffers(buffs);

            uint[] vaos = new uint[] { VertexArrayObject };
            Gl.DeleteVertexArrays(vaos);

            uint[] tex = new uint[] { VramTexture, SampleTexture };
            Gl.DeleteTextures(tex);

            Gl.DeleteProgram(Shader.Program);

            _DeviceContext.DeleteContext(_GlContext);

            _DeviceContext.Dispose();
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

        public (int w, int h) GetPixels(bool is24bit, int dy1, int dy2, int rx, int ry, int w, int h, int[] Pixels)
        {
            if (tid != Thread.CurrentThread.ManagedThreadId)
            {
                tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"[OpenGL GPU] MakeCurrent TID: {Thread.CurrentThread.ManagedThreadId}");
                _DeviceContext.MakeCurrent(_GlContext);
                Gl.BindAPI(new KhronosVersion(4, 6, 0, "gl", "compatibility"), null);
            }

            Gl.Disable(EnableCap.ScissorTest);
            DisableBlending();

            Gl.Enable(EnableCap.Texture2d);
            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(2);

            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, VramFrameBuffer);

            if (is24bit)
            {
                Gl.Uniform1(RenderModeLoc, 2);
            } else
            {
                Gl.Uniform1(RenderModeLoc, 1);
            }

            Gl.Viewport(0, 0, 1024, 512);

            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            Gl.BindTexture(TextureTarget.Texture2d, VramTexture);

            Gl.Uniform1(Aspect_Ratio_X_Offset_Loc, 0.0f);
            Gl.Uniform1(Aspect_Ratio_Y_Offset_Loc, 0.0f);
            Gl.Uniform1(Display_Area_X_Start_Loc, 0.0f);
            Gl.Uniform1(Display_Area_Y_Start_Loc, 0.0f);
            Gl.Uniform1(Display_Area_X_End_Loc, 1.0f);
            Gl.Uniform1(Display_Area_Y_End_Loc, 1.0f);

            Gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            //Gl.ReadPixels(0, 0, 1024, 512, PixelFormat.Rgba, PixelType.UnsignedByte, Marshal.UnsafeAddrOfPinnedArrayElement(Pixels, 0) );

            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);
            Gl.Enable(EnableCap.ScissorTest);

            Gl.BindTexture(TextureTarget.Texture2d, SampleTexture);
            Gl.Scissor(ScissorBox_X, ScissorBox_Y, ScissorBoxWidth, ScissorBoxHeight);
            Gl.Uniform1(RenderModeLoc, 0);

            _DeviceContext.SwapBuffers();

            return (1024, -1);
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;

            if (_VRAMTransfer.isRead)
            {
                CopyRectVRAMtoCPU();
            } else
            {
                DataFormWrite = new ushort[_VRAMTransfer.HalfWords];
            }
        }

        public void SetMaskBit(uint value)
        {
            ForceSetMaskBit = ((value & 1) != 0);
            PreserveMaskedPixels = (((value >> 1) & 1) != 0);

            Gl.Uniform1(MaskBitSettingLoc, (int)value);
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            if (tid != Thread.CurrentThread.ManagedThreadId)
            {
                tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"[OpenGL GPU] MakeCurrent TID: {Thread.CurrentThread.ManagedThreadId}");
                _DeviceContext.MakeCurrent(_GlContext);
                Gl.BindAPI(new KhronosVersion(4, 6, 0, "gl", "compatibility"), null);
            }
            DrawingAreaTopLeft = value;

            ScissorBox_X = DrawingAreaTopLeft.X;
            ScissorBox_Y = DrawingAreaTopLeft.Y;

            ScissorBoxWidth = Math.Max(DrawingAreaBottomRight.X - DrawingAreaTopLeft.X + 1, 0);
            ScissorBoxHeight = Math.Max(DrawingAreaBottomRight.Y - DrawingAreaTopLeft.Y + 1, 0);

            Gl.Viewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);
            Gl.Enable(EnableCap.ScissorTest);
            Gl.Scissor(ScissorBox_X, ScissorBox_Y, ScissorBoxWidth, ScissorBoxHeight);
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            DrawingAreaBottomRight = value;

            ScissorBox_X = DrawingAreaTopLeft.X;
            ScissorBox_Y = DrawingAreaTopLeft.Y;

            ScissorBoxWidth = Math.Max(DrawingAreaBottomRight.X - DrawingAreaTopLeft.X + 1, 0);
            ScissorBoxHeight = Math.Max(DrawingAreaBottomRight.Y - DrawingAreaTopLeft.Y + 1, 0);

            Gl.Viewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);
            Gl.Enable(EnableCap.ScissorTest);
            Gl.Scissor(ScissorBox_X, ScissorBox_Y, ScissorBoxWidth, ScissorBoxHeight);
        }

        public void SetDrawingOffset(TDrawingOffset value)
        {
            DrawingOffset = value;
        }

        public void SetTextureWindow(uint value)
        {
            value &= 0xfffff;

            TextureWindowXMask = (value & 0x1f);
            TextureWindowYMask = ((value >> 5) & 0x1f);

            TextureWindowXOffset = ((value >> 10) & 0x1f);
            TextureWindowYOffset = ((value >> 15) & 0x1f);

            Gl.Uniform4(TexWindow, (ushort)TextureWindowXMask, (ushort)TextureWindowYMask, (ushort)TextureWindowXOffset, (ushort)TextureWindowYOffset);
        }

        public void FillRectVRAM(ushort x, ushort y, ushort w, ushort h, uint colorval)
        {
            float r = (colorval & 0xFF) / 255.0f;
            float g = ((colorval >> 8) & 0xFF) / 255.0f;
            float b = ((colorval >> 16) & 0xFF) / 255.0f;

            Gl.Viewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);
            Gl.ClearColor(r, g, b, 0.0f);
            Gl.Scissor(x, y, w, h);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            short[] rectangle = new short[] {
                (short)x, (short)y,
                (short)(x+w), (short)y,
                (short)(x+w),(short)(y+h),
                (short)x, (short)(y+h)
            };

            UpdateIntersectionTable(ref rectangle);

            Gl.Scissor(ScissorBox_X, ScissorBox_Y, ScissorBoxWidth, ScissorBoxHeight);
            Gl.ClearColor(0, 0, 0, 1.0f);
        }

        public void CopyRectVRAMtoVRAM(ushort sx, ushort sy, ushort dx, ushort dy, ushort w, ushort h)
        {
            ushort[] src_coords = new ushort[] {
                (ushort)sx, (ushort)sy,
                (ushort)(sx + w), (ushort)sy,
                (ushort)(sx + w), (ushort)(sy + h),
                (ushort)sx, (ushort)(sy + h)
            };

            short[] dst_coords = new short[] {
                (short)dx, (short)dy,
                (short)(dx + w), (short)dy,
                (short)(dx + w), (short)(dy + h),
                (short)dx, (short)(dy + h)
            };

            if (TextureInvalidate(ref src_coords))
            {
                VramSync();
            }

            Gl.BindTexture(TextureTarget.Texture2d, SampleTexture);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);

            Gl.CopyImageSubData(
                SampleTexture, CopyImageSubDataTarget.Texture2d, 0, sx, sy, 0,
                VramTexture, CopyImageSubDataTarget.Texture2d, 0, dx, dy, 0,
                w, h, 1);

            UpdateIntersectionTable(ref dst_coords);
        }

        private void CopyRectVRAMtoCPU()
        {
            DataFormRead = new ushort[_VRAMTransfer.HalfWords];

            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, VramFrameBuffer);

            Gl.ReadPixels(_VRAMTransfer.X, _VRAMTransfer.Y, _VRAMTransfer.W, _VRAMTransfer.H, PixelFormat.Rgba, PixelType.UnsignedShort1555Rev,
                Marshal.UnsafeAddrOfPinnedArrayElement(DataFormRead, 0)
                );
        }

        public uint ReadFromVRAM()
        {
            ushort Data0 = DataFormRead[_VRAMTransfer.currentpos];
            ushort Data1 = DataFormRead[_VRAMTransfer.currentpos + 1];

            _VRAMTransfer.currentpos += 2;

            _VRAMTransfer.HalfWords -= 2;

            return (uint)((Data1 << 16) | Data0);
        }

        private void CopyRectCPUtoVRAM()
        {
            int x_dst = _VRAMTransfer.OriginX;
            int y_dst = _VRAMTransfer.OriginY;
            int width = _VRAMTransfer.W;
            int height = _VRAMTransfer.H;

            Gl.Disable(EnableCap.ScissorTest);

            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            Gl.BindTexture(TextureTarget.Texture2d, VramTexture);
            Gl.TexSubImage2D(TextureTarget.Texture2d, 0, x_dst, y_dst, width, height,
                PixelFormat.Rgba, PixelType.UnsignedShort1555Rev,
                Marshal.UnsafeAddrOfPinnedArrayElement(DataFormWrite, 0)
                );

            short[] rectangle = new short[] {
                (short)x_dst, (short)y_dst,
                (short)(x_dst+width), (short)y_dst,
                (short)(x_dst+width),(short)(y_dst+height),
                (short)x_dst, (short)(y_dst+height)
            };

            UpdateIntersectionTable(ref rectangle);

            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);
            Gl.Enable(EnableCap.ScissorTest);
            Gl.Scissor(ScissorBox_X, ScissorBox_Y, ScissorBoxWidth, ScissorBoxHeight);
        }

        public void WriteToVRAM(ushort value)
        {
            DataFormWrite[_VRAMTransfer.currentpos++] = value;

            if (_VRAMTransfer.currentpos == DataFormWrite.Length)
            {
                CopyRectCPUtoVRAM();

                DataFormWrite = null;
            }
        }

        public void DrawLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent, int SemiTransparency)
        {
            _vertices.Add((short)v1);
            _vertices.Add((short)(v1 >> 16));

            _vertices.Add((short)v2);
            _vertices.Add((short)(v2 >> 16));

            _colors.Add((byte)color1);
            _colors.Add((byte)(color1 >> 8));
            _colors.Add((byte)(color1 >> 16));

            _colors.Add((byte)color2);
            _colors.Add((byte)(color2 >> 8));
            _colors.Add((byte)(color2 >> 16));
        }

        public void DrawLineBatch(bool isTransparent, bool isPolyLine, bool isDithered, int SemiTransparency)
        {
            if (isTransparent)
            {
                SetBlendingFunction(SemiTransparency);
            } else
            {
                DisableBlending();
            }

            short[] vertices = _vertices.ToArray();
            byte[] colors = _colors.ToArray();
            _vertices.Clear();
            _colors.Clear();

            Gl.Viewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.Uniform1(TexModeLoc, -1);

            if (!ApplyDrawingOffset(ref vertices))
            {
                return;
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)vertices.Length * sizeof(short), vertices, BufferUsage.StreamDraw);
            Gl.VertexAttribIPointer(0, 2, VertexAttribIType.Short, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, ColorsBuffer);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)colors.Length * sizeof(byte), colors, BufferUsage.StreamDraw);
            Gl.VertexAttribIPointer(1, 3, VertexAttribIType.UnsignedByte, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(1);

            Gl.Uniform1(IsDitheredLoc, isDithered ? 1 : 0);

            Gl.DrawArrays(isPolyLine ? PrimitiveType.LineStrip : PrimitiveType.Lines, 0, vertices.Length / 2);

            UpdateIntersectionTable(ref vertices);
        }

        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
            byte R, G, B;

            if (primitive.IsTextured && primitive.IsRawTextured)
            {
                R = G = B = 0x80;            //No blend color
            } else
            {
                R = (byte)primitive.rawcolor;
                G = (byte)(primitive.rawcolor >> 8);
                B = (byte)(primitive.rawcolor >> 16);
            }

            //Upper left 
            short x1 = origin.X;
            short y1 = origin.Y;
            //Lower right
            short x2 = size.X;
            short y2 = size.Y;

            ushort tx1 = 0;
            ushort ty1 = 0;
            ushort tx2 = 0;
            ushort ty2 = 0;

            if (primitive.IsTextured)
            {
                //Texture Upper left 
                tx1 = texture.X;
                ty1 = texture.Y;

                //Texture Lower right
                tx2 = (ushort)(tx1 + primitive.texwidth);
                ty2 = (ushort)(ty1 + primitive.texheight);
            }

            Vertices = new short[]{
                x1,  y1,  // 左上角
                x2,  y1,  // 右上角
                x2,  y2,  // 右下角
                x1,  y2   // 左下角
            };
            Colors = new byte[]{
             R,  G,  B,
             R,  G,  B,
             R,  G,  B,
             R,  G,  B,
            };
            UV = new ushort[] {
             tx1, ty1,
             tx2, ty1,
             tx2, ty2,
             tx1, ty2
            };

            //if (!ApplyDrawingOffset(ref Vertices))
            //{
            //    return;
            //}

            if (primitive.IsSemiTransparent)
            {
                SetBlendingFunction(primitive.SemiTransparencyMode);
            } else
            {
                DisableBlending();
            }

            Gl.Viewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)Vertices.Length * sizeof(short), Vertices, BufferUsage.StreamDraw);
            Gl.VertexAttribIPointer(0, 2, VertexAttribIType.Short, 0, IntPtr.Zero);  //size: 2 for x,y only!
            Gl.EnableVertexAttribArray(0);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, ColorsBuffer);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)Colors.Length * sizeof(byte), Colors, BufferUsage.StreamDraw);
            Gl.VertexAttribIPointer(1, 3, VertexAttribIType.UnsignedByte, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(1);

            if (primitive.IsTextured)
            {
                Gl.Uniform1(ClutLoc, primitive.clut);
                Gl.Uniform1(TexPageLoc, primitive.texturebase);
                Gl.Uniform1(TexModeLoc, primitive.TextureDepth);
                Gl.BindBuffer(BufferTarget.ArrayBuffer, TexCoords);
                Gl.BufferData(BufferTarget.ArrayBuffer, (uint)UV.Length * sizeof(ushort), UV, BufferUsage.StreamDraw);
                Gl.VertexAttribPointer(2, 2, VertexAttribPointerType.UnsignedShort, false, 0, IntPtr.Zero);
                Gl.EnableVertexAttribArray(2);
                if (TextureInvalidatePrimitive(ref UV, primitive.texturebase, primitive.clut))
                {
                    VramSync();
                }
            } else
            {
                Gl.Uniform1(TexModeLoc, -1);
                Gl.Uniform1(ClutLoc, 0);
                Gl.Uniform1(TexPageLoc, 0);
                Gl.DisableVertexAttribArray(2);
            }

            Gl.Uniform1(IsDitheredLoc, 0);  //RECTs are NOT dithered

            Gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            UpdateIntersectionTable(ref Vertices);
        }

        public void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
        {

            if (primitive.IsRawTextured)
            {
                c0 = c1 = c2 = 0x808080;
            } else
            if (!primitive.IsShaded)
            {
                c1 = c2 = c0;
            }

            Vertices = new short[]{
             v0.X,  v0.Y,
             v1.X,  v1.Y,
             v2.X,  v2.Y,
            };
            Colors = new byte[]{
                (byte)c0, (byte)(c0 >> 8), (byte)(c0 >> 16),
                (byte)c1, (byte)(c1 >> 8), (byte)(c1 >> 16),
                (byte)c2, (byte)(c2 >> 8), (byte)(c2 >> 16),
            };
            UV = new ushort[] {
             t0.X, t0.Y,
             t1.X, t1.Y,
             t2.X, t2.Y,
            };

            //if (!ApplyDrawingOffset(ref Vertices))
            //{
            //    return;
            //}

            if (primitive.IsSemiTransparent)
            {
                SetBlendingFunction(primitive.SemiTransparencyMode);
            } else
            {
                DisableBlending();
            }

            Gl.Viewport(0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)Vertices.Length * sizeof(short), Vertices, BufferUsage.StreamDraw);
            Gl.VertexAttribIPointer(0, 2, VertexAttribIType.Short, 0, IntPtr.Zero);  //size: 2 for x,y only!
            Gl.EnableVertexAttribArray(0);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, ColorsBuffer);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)Colors.Length * sizeof(byte), Colors, BufferUsage.StreamDraw);
            Gl.VertexAttribIPointer(1, 3, VertexAttribIType.UnsignedByte, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(1);

            if (primitive.IsTextured)
            {
                Gl.Uniform1(ClutLoc, primitive.clut);
                Gl.Uniform1(TexPageLoc, primitive.texpage);
                Gl.Uniform1(TexModeLoc, (primitive.texpage >> 7) & 3);
                Gl.BindBuffer(BufferTarget.ArrayBuffer, TexCoords);
                Gl.BufferData(BufferTarget.ArrayBuffer, (uint)UV.Length * sizeof(ushort), UV, BufferUsage.StreamDraw);
                Gl.VertexAttribPointer(2, 2, VertexAttribPointerType.UnsignedShort, false, 0, IntPtr.Zero);
                Gl.EnableVertexAttribArray(2);

                if (TextureInvalidatePrimitive(ref UV, primitive.texpage, primitive.clut))
                {
                    VramSync();
                }

            } else
            {
                Gl.Uniform1(TexModeLoc, -1);
                Gl.Uniform1(ClutLoc, 0);
                Gl.Uniform1(TexPageLoc, 0);
                Gl.DisableVertexAttribArray(2);
            }

            Gl.Uniform1(IsDitheredLoc, primitive.isDithered ? 1 : 0);

            Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

            UpdateIntersectionTable(ref Vertices);
        }

        #region Helper Functions

        private void VramSync()
        {
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, VramFrameBuffer);
            Gl.BindTexture(TextureTarget.Texture2d, SampleTexture);
            Gl.CopyTexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, 0, 0, VRAM_WIDTH, VRAM_HEIGHT);
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, VramFrameBuffer);

            for (int i = 0; i < VRAM_WIDTH / IntersectionBlockLength; i++)
            {
                for (int j = 0; j < VRAM_HEIGHT / IntersectionBlockLength; j++)
                {
                    IntersectionTable[j, i] = 0;
                }
            }
        }

        private void SetBlendingFunction(int function)
        {
            Gl.Uniform1(TransparencyModeLoc, function);

            Gl.Enable(EnableCap.Blend);
            //B = Destination
            //F = Source
            Gl.BlendFunc(BlendingFactor.Src1Color, BlendingFactor.Source1Alpha);        //Alpha values are handled in GLSL
            Gl.BlendEquation(function == 2 ? BlendEquationMode.FuncReverseSubtract : BlendEquationMode.FuncAdd);
        }

        public void DisableBlending()
        {
            ///Gl.Disable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.Uniform1(TransparencyModeLoc, 4);    //0-3 for the functions, 4 = disabled
        }

        private void UpdateIntersectionTable(ref short[] vertices)
        {
            //Mark any affected blocks as dirty
            int smallestX = 1023;
            int smallestY = 511;
            int largestX = -1024;
            int largestY = -512;

            for (int i = 0; i < vertices.Length; i += 2)
            {
                largestX = Math.Max(largestX, vertices[i]);
                smallestX = Math.Min(smallestX, vertices[i]);
            }

            for (int i = 1; i < vertices.Length; i += 2)
            {
                largestY = Math.Max(largestY, vertices[i]);
                smallestY = Math.Min(smallestY, vertices[i]);
            }

            smallestX = Math.Clamp(smallestX, 0, 1023);
            smallestY = Math.Clamp(smallestY, 0, 511);
            largestX = Math.Clamp(largestX, 0, 1023);
            largestY = Math.Clamp(largestY, 0, 511);

            int left = smallestX / IntersectionBlockLength;
            int right = largestX / IntersectionBlockLength;
            int up = smallestY / IntersectionBlockLength;
            int down = largestY / IntersectionBlockLength;

            //No access wrap for drawing, anything out of bounds is clamped 
            for (int y = up; y <= down; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    IntersectionTable[y, x] = 1;
                }
            }
        }

        private bool ApplyDrawingOffset(ref short[] vertices)
        {
            short maxX = -1024;
            short maxY = -1024;
            short minX = 1023;
            short minY = 1023;

            for (int i = 0; i < vertices.Length; i += 2)
            {
                //vertices[i] = Signed11Bits((ushort)(Signed11Bits((ushort)vertices[i]) + DrawOffsetX));
                vertices[i] = (short)(Signed11Bits((ushort)vertices[i]) + DrawingOffset.X);

                maxX = Math.Max(maxX, vertices[i]);
                minX = Math.Min(minX, vertices[i]);
            }

            for (int i = 1; i < vertices.Length; i += 2)
            {
                //vertices[i] = Signed11Bits((ushort)(Signed11Bits((ushort)vertices[i]) + DrawOffsetY));
                vertices[i] = (short)(Signed11Bits((ushort)vertices[i]) + DrawingOffset.Y);

                maxY = Math.Max(maxY, vertices[i]);
                minY = Math.Min(minY, vertices[i]);
            }

            return !((Math.Abs(maxX - minX) > 1024) || (Math.Abs(maxY - minY) > 512));
        }

        private short Signed11Bits(ushort input)
        {
            return (short)(((short)(input << 5)) >> 5);
        }

        private bool TextureInvalidatePrimitive(ref ushort[] uv, uint texPage, uint clut)
        {
            //Experimental 
            //Checks whether the textured primitive is reading from a dirty block

            //Hack: Always sync if preserve_masked_pixels is true
            //This is kind of slow but fixes Silent Hills 
            if (PreserveMaskedPixels)
            {
                return true;
            }

            int mode = (int)((texPage >> 7) & 3);
            uint divider = (uint)(4 >> mode);

            uint smallestX = 1023;
            uint smallestY = 511;
            uint largestX = 0;
            uint largestY = 0;

            for (int i = 0; i < uv.Length; i += 2)
            {
                largestX = Math.Max(largestX, uv[i]);
                smallestX = Math.Min(smallestX, uv[i]);
            }

            for (int i = 1; i < uv.Length; i += 2)
            {
                largestY = Math.Max(largestY, uv[i]);
                smallestY = Math.Min(smallestY, uv[i]);
            }

            smallestX = Math.Min(smallestX, 1023);
            smallestY = Math.Min(smallestY, 511);
            largestX = Math.Min(largestX, 1023);
            largestY = Math.Min(largestY, 511);

            uint texBaseX = (texPage & 0xF) * 64;
            uint texBaseY = ((texPage >> 4) & 1) * 256;

            uint width = (largestX - smallestX) / divider;
            uint height = (largestY - smallestY) / divider;

            uint left = texBaseX / IntersectionBlockLength;
            uint right = ((texBaseX + width) & 0x3FF) / IntersectionBlockLength;
            uint up = texBaseY / IntersectionBlockLength;
            uint down = ((texBaseY + height) & 0x1FF) / IntersectionBlockLength;

            //ANDing with 7,15 take cares of vram access wrap when reading textures (same effect as mod 8,16)  
            for (uint y = up; y != ((down + 1) & 0x7); y = (y + 1) & 0x7)
            {
                for (uint x = left; x != ((right + 1) & 0xF); x = (x + 1) & 0xF)
                {
                    if (IntersectionTable[y, x] == 1)
                    {
                        return true;
                    }
                }
            }

            //For 4/8 bpp modes we need to check the clut table 
            if (mode == 0 || mode == 1)
            {
                uint clutX = (clut & 0x3F) * 16;
                uint clutY = ((clut >> 6) & 0x1FF);
                left = clutX / IntersectionBlockLength;
                up = clutY / IntersectionBlockLength;             //One line 
                for (uint x = left; x < VRAM_WIDTH / IntersectionBlockLength; x++)
                {
                    if (IntersectionTable[up, x] == 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TextureInvalidate(ref ushort[] coords)
        {
            //Hack: Always sync if preserve_masked_pixels is true
            //This is kind of slow but fixes Silent Hills 
            if (PreserveMaskedPixels)
            {
                return true;
            }

            uint smallestX = 1023;
            uint smallestY = 511;
            uint largestX = 0;
            uint largestY = 0;

            for (int i = 0; i < coords.Length; i += 2)
            {
                largestX = Math.Max(largestX, coords[i]);
                smallestX = Math.Min(smallestX, coords[i]);
            }

            for (int i = 1; i < coords.Length; i += 2)
            {
                largestY = Math.Max(largestY, coords[i]);
                smallestY = Math.Min(smallestY, coords[i]);
            }

            smallestX = Math.Min(smallestX, 1023);
            smallestY = Math.Min(smallestY, 511);
            largestX = Math.Min(largestX, 1023);
            largestY = Math.Min(largestY, 511);

            uint width = (largestX - smallestX);
            uint height = (largestY - smallestY);

            uint left = smallestX / IntersectionBlockLength;
            uint right = ((smallestX + width) & 0x3FF) / IntersectionBlockLength;
            uint up = smallestY / IntersectionBlockLength;
            uint down = ((smallestY + height) & 0x1FF) / IntersectionBlockLength;

            //ANDing with 7,15 take cares of vram access wrap when reading textures (same effect as mod 8,16)  
            for (uint y = up; y != ((down + 1) & 0x7); y = (y + 1) & 0x7)
            {
                for (uint x = left; x != ((right + 1) & 0xF); x = (x + 1) & 0xF)
                {
                    if (IntersectionTable[y, x] == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}
