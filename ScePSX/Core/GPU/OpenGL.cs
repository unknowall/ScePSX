using System;
using System.Collections.Generic;
using System.Drawing;
using Khronos;
using OpenGL;

namespace ScePSX
{
    public class OpenglGPU : IGPU
    {
        public GPUType type => GPUType.OpenGL;

        private GPUColor Color0, Color1, Color2;

        private TDrawingArea DrawingAreaTopLeft, DrawingAreaBottomRight;

        private TDrawingOffset DrawingOffset;

        private VRAMTransfer _VRAMTransfer;

        private int MaskWhileDrawing;

        private bool CheckMaskBeforeDraw;

        private int TextureWindowPostMaskX, TextureWindowPostMaskY, TextureWindowPreMaskX, TextureWindowPreMaskY;

        INativePBuffer pbuffer;
        nint _GlContext;
        DeviceContext _DeviceContext;

        uint FrameBuff;
        uint FrameTexture, RamTexture;

        OpenGlShader Shader;

        public OpenglGPU()
        {
            DeviceContext.DefaultAPI = KhronosVersion.ApiGl;

            pbuffer = DeviceContext.CreatePBuffer(new DevicePixelFormat(32), 4096, 2160);

            _DeviceContext = DeviceContext.Create(pbuffer);

            _DeviceContext.IncRef();
 
            _GlContext = _DeviceContext.CreateContext(IntPtr.Zero);
     
            //_GlContext = _DeviceContext.CreateContextAttrib(IntPtr.Zero,null, new KhronosVersion(4, 6, 0, "gl", "core"));

            _DeviceContext.MakeCurrent(_GlContext);
            Gl.BindAPI();

            FrameBuff = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuff);

            FrameTexture = Gl.GenBuffer();
            Gl.BindTexture(TextureTarget.Texture2d, FrameTexture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, 4096, 2160, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, FrameTexture, 0);

            RamTexture = Gl.GenBuffer();
            Gl.BindTexture(TextureTarget.Texture2d, RamTexture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb5, 1024, 512, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, RamTexture, 0);

            Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _DeviceContext.SwapBuffers();

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
                Console.WriteLine("FBO 配置失败");

            string glVersion = Gl.GetString(StringName.Version);
            Console.WriteLine($"OpenGL 版本: {glVersion}");

            Shader = new OpenGlShader(
                ShaderStrings.VertixShader.Split(new string[] { "\r" }, StringSplitOptions.None), 
                ShaderStrings.FragmentShader.Split(new string[] { "\r" }, StringSplitOptions.None)
                );
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
            uint[] fbuffs = new uint[] { FrameBuff };
            Gl.DeleteFramebuffers(fbuffs);

            uint[] buffs = new uint[]{ FrameTexture, RamTexture };
            Gl.DeleteBuffers(buffs);

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

        public (int w, int h) GetPixels(bool is24bit, int dy1, int dy2, int rx, int ry, int w, int h, int[] Pixels)
        {
            if (is24bit)
            {
            } else
            {
            }

            return (w, h);
        }

        public uint ReadFromVRAM()
        {
            return 0;
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;
        }

        public void SetMaskBit(uint value)
        {
            MaskWhileDrawing = (int)(value & 0x1);
            CheckMaskBeforeDraw = (value & 0x2) != 0;
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            DrawingAreaTopLeft = value;
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            DrawingAreaBottomRight = value;
        }

        public void SetDrawingOffset(TDrawingOffset value)
        {
            DrawingOffset = value;
        }

        public void SetTextureWindow(uint value)
        {
            TextureWindow textureWindow = new TextureWindow(value);

            TextureWindowPreMaskX = ~(textureWindow.MaskX * 8);
            TextureWindowPreMaskY = ~(textureWindow.MaskY * 8);
            TextureWindowPostMaskX = (textureWindow.OffsetX & textureWindow.MaskX) * 8;
            TextureWindowPostMaskY = (textureWindow.OffsetY & textureWindow.MaskY) * 8;
        }

        public void FillRectVRAM(ushort x, ushort y, ushort w, ushort h, uint colorval)
        {
        }

        public void CopyRectVRAMtoVRAM(ushort sx, ushort sy, ushort dx, ushort dy, ushort w, ushort h)
        {
        }

        public void DrawPixel(ushort value)
        {
        }

        public void DrawLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent, int SemiTransparency)
        {
        }

        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
        }

        public void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
        {
        }

    }
}
