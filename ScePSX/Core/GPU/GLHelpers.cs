
using System;
using System.Runtime.InteropServices;
using System.Text;
using OpenGL;

namespace ScePSX
{
    public class GlShader : IDisposable
    {
        public uint Program;

        public GlShader()
        {
        }

        public GlShader(string[] vert, string[] frag)
        {
            LoadShader(vert, frag);
        }

        public bool isAlive()
        {
            return Program > 0;
        }

        private void LoadShader(string[] vert, string[] frag)
        {
            uint vertexShader = 0;
            uint fragmentShader = 0;
            ShaderType type = ShaderType.VertexShader;

            if (frag.Length == 0)
                type = ShaderType.ComputeShader;

            vertexShader = Gl.CreateShader(type);
            Gl.ShaderSource(vertexShader, vert);
            CompileShader(vertexShader);

            Program = Gl.CreateProgram();
            Gl.AttachShader(Program, vertexShader);

            if (frag.Length > 0)
            {
                fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
                Gl.ShaderSource(fragmentShader, frag);
                CompileShader(fragmentShader);
                Gl.AttachShader(Program, fragmentShader);
            }

            Gl.LinkProgram(Program);
            Gl.GetProgram(Program, ProgramProperty.LinkStatus, out var code);
            if (code != 1)
            {
                Console.WriteLine($"[OpenGL GPU] Error occurred linking Program({Program})");
            }

            Gl.DetachShader(Program, vertexShader);

            if (frag.Length > 0)
            {
                Gl.DetachShader(Program, fragmentShader);
                Gl.DeleteShader(fragmentShader);
            }
            Gl.DeleteShader(vertexShader);

            Console.WriteLine($"[OpenGL GPU] LinkProgram Done!");
        }

        private void CompileShader(uint shader)
        {
            Gl.CompileShader(shader);

            Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                Gl.GetShader(shader, ShaderParameterName.InfoLogLength, out int logLength);
                if (logLength <= 0)
                    return;

                StringBuilder infoLog = new StringBuilder(logLength);
                Gl.GetShaderInfoLog(shader, logLength, out int actualLength, infoLog);

                string log = infoLog.ToString();
                Console.WriteLine($"[OpenGL GPU] Compile error:\r\n{log}\r\n");
            }
        }

        public void Dispose()
        {
            Unbind();

            if (Program > 0)
                Gl.DeleteProgram(Program);

            GC.SuppressFinalize(this);
        }

        public void Bind()
        {
            Gl.UseProgram(Program);
        }

        public void Unbind()
        {
            Gl.UseProgram(0);
        }

        public int GetAttributeLocation(string name)
        {
            return Gl.GetAttribLocation(Program, name);
        }

        public int GetUniformLocation(string name)
        {
            return Gl.GetUniformLocation(Program, name);
        }

        public void BindUniformBlock(string name, uint block)
        {
            uint index = Gl.GetUniformBlockIndex(Program, name);

            Gl.UniformBlockBinding(Program, index, block);
        }

    }

    public class GLCopyShader : IDisposable
    {
        private GlShader m_program;
        private int m_srcRectLoc;
        private int m_forceMaskBitLoc;
        private int m_depthLoc;

        public GLCopyShader()
        {
            m_program = new GlShader(
            GLShaderStrings.VRamCopyVertex.Split(new string[] { "\r" }, StringSplitOptions.None),
            GLShaderStrings.VRamCopyFragment.Split(new string[] { "\r" }, StringSplitOptions.None)
            );

            m_srcRectLoc = m_program.GetUniformLocation("u_srcRect");
            m_forceMaskBitLoc = m_program.GetUniformLocation("u_forceMaskBit");
            m_depthLoc = m_program.GetUniformLocation("u_maskedDepth");
        }

        public void Dispose()
        {
            m_program.Dispose();
        }

        public void Use(float srcX, float srcY, float srcW, float srcH, float depth, bool forceMaskBit)
        {
            m_program.Bind();
            Gl.Uniform4(m_srcRectLoc, srcX, srcY, srcW, srcH);
            Gl.Uniform1(m_forceMaskBitLoc, forceMaskBit ? 1 : 0);
            Gl.Uniform1(m_depthLoc, depth);
        }

        public void SetSourceArea(float srcX, float srcY, float srcW, float srcH)
        {
            Gl.Uniform4(m_srcRectLoc, srcX, srcY, srcW, srcH);
        }
    }

    public class GLComputeShader : IDisposable
    {
        private GlShader m_program;
        private int m_topCropLoc;
        private int m_bottomCropLoc;

        public GLComputeShader()
        {
            m_program = new GlShader(
                GLShaderStrings.ComputeCropShader.Split(new string[] { "\r" }, StringSplitOptions.None),
                new string[0]
            );

            m_topCropLoc = m_program.GetUniformLocation("u_topCrop");
            m_bottomCropLoc = m_program.GetUniformLocation("u_bottomCrop");
        }

        public void Bind()
        {
            m_program.Bind();
        }

        public void SetParameters(int textureHeight, float threshold, float maxBlackBarRatio)
        {
            Gl.Uniform1(m_program.GetUniformLocation("u_textureHeight"), textureHeight);
            Gl.Uniform1(m_program.GetUniformLocation("u_threshold"), threshold);
            Gl.Uniform1(m_program.GetUniformLocation("u_maxBlackBarRatio"), maxBlackBarRatio);
        }

        public void Dispatch(uint groupsX, uint groupsY, uint groupsZ)
        {
            Gl.DispatchCompute(groupsX, groupsY, groupsZ);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
        }

        public void Dispose()
        {
            m_program.Dispose();
        }
    }

    public class glVAO : IDisposable
    {
        private uint m_vao = 0;
        private static uint s_bound = 0;

        public glVAO()
        {
        }

        private glVAO(uint vao)
        {
            m_vao = vao;
        }

        public static glVAO Create()
        {
            uint vao = Gl.GenVertexArray();

            return new glVAO(vao);
        }

        public bool Valid()
        {
            return m_vao != 0;
        }

        public void Reset()
        {
            if (m_vao != 0)
            {
                Unbind();
                Gl.DeleteVertexArrays(m_vao);
                m_vao = 0;
            }
        }

        public void AddFloatAttribute(uint location, int size, VertexAttribPointerType type, bool normalized, int stride = 0, IntPtr offset = default)
        {
            Bind();
            Gl.VertexAttribPointer(location, size, type, normalized, stride, offset);
            Gl.EnableVertexAttribArray(location);
        }

        public void AddIntAttribute(uint location, int size, VertexAttribIType type, int stride = 0, IntPtr offset = default)
        {
            Bind();
            Gl.VertexAttribIPointer(location, size, type, stride, offset);
            Gl.EnableVertexAttribArray(location);
        }

        public void Bind()
        {
            if (m_vao != s_bound)
            {
                Bind(m_vao);
            }
        }

        public static void Unbind()
        {
            if (s_bound != 0)
            {
                Bind(0);
            }
        }

        public void Dispose()
        {
            Reset();

            GC.SuppressFinalize(this);
        }

        private static void Bind(uint vao)
        {
            Gl.BindVertexArray(vao);
            s_bound = vao;
        }
    }

    public class glBuffer : IDisposable
    {
        public uint m_buffer = 0;
        private static uint s_bound = 0;
        private readonly OpenGL.BufferTarget m_target;

        public glBuffer(BufferTarget target) => m_target = target;

        public void Dispose()
        {
            Reset();

            GC.SuppressFinalize(this);
        }

        public static glBuffer Create(BufferTarget target)
        {
            var buffer = new glBuffer(target);
            buffer.m_buffer = Gl.GenBuffer();
            //CheckRenderErrors();
            return buffer;
        }

        public static glBuffer Create<T>(BufferTarget target, BufferUsage usage, int size, T[] data = null) where T : unmanaged
        {
            var buffer = Create(target);
            buffer.SetData(usage, size, data);
            //CheckRenderErrors();
            return buffer;
        }

        public void Reset()
        {
            if (m_buffer != 0)
            {
                if (m_buffer == s_bound)
                    Bind(0);

                Gl.DeleteBuffers(m_buffer);
                m_buffer = 0;
            }
        }

        public bool Valid() => m_buffer != 0;

        public void Bind()
        {
            if (m_buffer != s_bound)
                Bind(m_buffer);
        }

        public void Unbind()
        {
            if (s_bound != 0)
                Bind(0);
        }

        public unsafe void SetData<T>(BufferUsage usage, int size, T[] data = null) where T : unmanaged
        {
            Bind();
            IntPtr dataPtr = data == null ? IntPtr.Zero : Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            Gl.BufferData(m_target, (uint)(size * sizeof(T)), dataPtr, usage);
            //CheckRenderErrors();
        }

        public unsafe void SubData<T>(int size, T[] data) where T : unmanaged
        {
            Bind();
            Gl.BufferSubData(m_target, IntPtr.Zero, (uint)(size * sizeof(T)), Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            //CheckRenderErrors();
        }

        public void BindBufferBase(uint index)
        {
            Gl.BindBufferBase(m_target, index, m_buffer);
        }

        private void Bind(uint buffer)
        {
            Gl.BindBuffer(m_target, buffer);
            s_bound = buffer;
        }

        private static void CheckRenderErrors()
        {
            var error = Gl.GetError();
            if (error != Gl.NO_ERROR)
                Console.WriteLine($"OpenGL glBuffer Error: {error}");
        }
    }

    public class glTexture2D : IDisposable
    {
        private int m_width = 0;
        private int m_height = 0;

        protected uint m_texture = 0;
        protected static uint s_bound = 0;

        public glTexture2D()
        {
        }

        public static glTexture2D Create()
        {
            var texture = new glTexture2D();
            texture.m_texture = Gl.GenTexture();
            texture.Bind();
            texture.SetLinearFiltering(false);
            texture.SetTextureWrap(false);
            //CheckRenderErrors();
            return texture;
        }

        public uint TextureID()
        {
            return m_texture;
        }

        public void Dispose()
        {
            Reset();
            GC.SuppressFinalize(this);
        }

        public static glTexture2D Create(InternalFormat internalColorFormat, int width, int height, PixelFormat pixelFormat, PixelType pixelType, nint pixels, int mipmapLevel = 0)
        {
            var texture = Create();
            texture.UpdateImage(internalColorFormat, width, height, pixelFormat, pixelType, pixels, mipmapLevel);
            return texture;
        }

        public void UpdateImage(InternalFormat internalColorFormat, int width, int height, PixelFormat pixelFormat, PixelType pixelType, nint pixels, int mipmapLevel = 0)
        {
            Bind();
            Gl.TexImage2D(TextureTarget.Texture2d, mipmapLevel, internalColorFormat, width, height, 0, pixelFormat, pixelType, pixels);
            //CheckRenderErrors();
            m_width = width;
            m_height = height;
        }

        public void SubImage(int x, int y, int width, int height, PixelFormat pixelFormat, PixelType pixelType, nint pixels, int mipmapLevel = 0)
        {
            Bind();
            //Console.WriteLine($"OpenGL glTexture2D SubImage: {x}, {y} - {width} x {height}");
            Gl.TexSubImage2D(TextureTarget.Texture2d, mipmapLevel, x, y, width, height, pixelFormat, pixelType, pixels);
            //CheckRenderErrors();
        }

        protected static void Bind(uint texture)
        {
            Gl.BindTexture(TextureTarget.Texture2d, texture);
            s_bound = texture;
        }

        public void Bind()
        {
            if (m_texture != s_bound)
                Bind(m_texture);
        }

        public static void Unbind()
        {
            if (s_bound != 0)
                Bind(0);
        }

        public void Reset()
        {
            if (m_texture != 0)
            {
                if (m_texture == s_bound)
                    Bind(0);

                Gl.DeleteTextures(m_texture);
                m_texture = 0;
            }

            m_width = 0;
            m_height = 0;
        }

        public void SetLinearFiltering(bool linear)
        {
            TextureMinFilter value = linear ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)value);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)value);
        }

        public void SetTextureWrap(bool wrap)
        {
            TextureWrapMode value = wrap ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)value);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)value);
        }

        public int GetWidth() => m_width;

        public int GetHeight() => m_height;

        private static void CheckRenderErrors()
        {
            ErrorCode error = Gl.GetError();
            if (error != ErrorCode.NoError)
                Console.WriteLine($"[OpenGL GPU] glTexture2D Error: {error}");
        }

        public static int GetMaxTextureSize()
        {
            Gl.GetInteger(GetPName.MaxTextureSize, out int maxSize);
            return maxSize;
        }
    }

    public class glFramebuffer : IDisposable
    {
        private uint m_frameBuffer = 0;

        private static uint s_boundRead = 0;
        private static uint s_boundDraw = 0;

        public glFramebuffer()
        {

        }

        public glFramebuffer(glFramebuffer other) : this(other.m_frameBuffer)
        {
            other.m_frameBuffer = 0;
        }

        private glFramebuffer(uint frameBuffer)
        {
            m_frameBuffer = frameBuffer;
        }

        public void Dispose()
        {
            Reset();
            GC.SuppressFinalize(this);
        }

        public static glFramebuffer Create()
        {
            uint frameBuffer = Gl.GenFramebuffer();
            //CheckRenderErrors();
            return new glFramebuffer(frameBuffer);
        }

        public FramebufferStatus AttachTexture(FramebufferAttachment type, glTexture2D texture, int mipmapLevel = 0)
        {
            Bind();
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, type, TextureTarget.Texture2d, texture.TextureID(), mipmapLevel);
            //CheckRenderErrors();
            return Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        }

        public bool IsComplete()
        {
            Bind();
            return Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete;
        }

        public bool Valid() => m_frameBuffer != 0;

        public void Reset()
        {
            if (m_frameBuffer != 0)
            {
                Unbind();
                Gl.DeleteFramebuffers(m_frameBuffer);
                m_frameBuffer = 0;
            }
        }

        public void Bind(FramebufferTarget binding = FramebufferTarget.Framebuffer)
        {
            BindImp(binding, m_frameBuffer);
        }

        public void Unbind()
        {
            UnbindImp(m_frameBuffer);
        }

        public static void Unbind(FramebufferTarget binding)
        {
            BindImp(binding, 0);
        }

        private static void BindImp(FramebufferTarget binding, uint frameBuffer)
        {
            switch (binding)
            {
                case FramebufferTarget.ReadFramebuffer:
                    if (s_boundRead != frameBuffer)
                    {
                        Gl.BindFramebuffer(binding, frameBuffer);
                        s_boundRead = frameBuffer;
                    }
                    break;

                case FramebufferTarget.DrawFramebuffer:
                    if (s_boundDraw != frameBuffer)
                    {
                        Gl.BindFramebuffer(binding, frameBuffer);
                        s_boundDraw = frameBuffer;
                    }
                    break;

                case FramebufferTarget.Framebuffer:
                    if (s_boundRead != frameBuffer || s_boundDraw != frameBuffer)
                    {
                        Gl.BindFramebuffer(binding, frameBuffer);
                        s_boundRead = frameBuffer;
                        s_boundDraw = frameBuffer;
                    }
                    break;
            }
        }

        private static void UnbindImp(uint frameBuffer)
        {
            if (s_boundRead == frameBuffer)
            {
                Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                s_boundRead = 0;
            }

            if (s_boundDraw == frameBuffer)
            {
                Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                s_boundDraw = 0;
            }
        }

        private static void CheckRenderErrors()
        {
            var error = Gl.GetError();
            if (error != Gl.NO_ERROR)
            {
                Console.WriteLine($"[OpenGL GPU] glFramebuffer Error: {error}");
            }
        }
    }

    public struct glRectangle<T> where T : struct, IComparable<T>
    {
        public T Left
        {
            get; set;
        }
        public T Top
        {
            get; set;
        }
        public T Right
        {
            get; set;
        }
        public T Bottom
        {
            get; set;
        }

        public glRectangle(T left, T top, T right, T bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int GetWidth()
        {
            dynamic dRight = Right, dLeft = Left;
            return (int)(dRight - dLeft);
        }

        public int GetHeight()
        {
            dynamic dBottom = Bottom, dTop = Top;
            return (int)(dBottom - dTop);
        }

        public static glRectangle<T> FromExtents(T left, T top, T width, T height)
        {
            dynamic dLeft = left, dTop = top, dWidth = width, dHeight = height;
            return new glRectangle<T>(left, top, dLeft + dWidth, dTop + dHeight);
        }

        public bool Intersects(glRectangle<T> other)
        {
            return Left.CompareTo(other.Right) < 0 &&
                   Right.CompareTo(other.Left) > 0 &&
                   Top.CompareTo(other.Bottom) < 0 &&
                   Bottom.CompareTo(other.Top) > 0;
        }

        public void Grow(glRectangle<T> bounds)
        {
            if (bounds.Left.CompareTo(Left) < 0)
                Left = bounds.Left;
            if (bounds.Top.CompareTo(Top) < 0)
                Top = bounds.Top;
            if (bounds.Right.CompareTo(Right) > 0)
                Right = bounds.Right;
            if (bounds.Bottom.CompareTo(Bottom) > 0)
                Bottom = bounds.Bottom;
        }

        public void Grow(T x, T y)
        {
            dynamic dX = x, dY = y;
            Left = (T)(dynamic)Math.Min(Convert.ToDouble(Left), Convert.ToDouble(x));
            Top = (T)(dynamic)Math.Min(Convert.ToDouble(Top), Convert.ToDouble(y));
            Right = (T)(dynamic)Math.Max(Convert.ToDouble(Right), Convert.ToDouble(x));
            Bottom = (T)(dynamic)Math.Max(Convert.ToDouble(Bottom), Convert.ToDouble(y));
        }

        public void ScaleInPlace(float scale)
        {
            Left = (T)(object)(Convert.ToInt32(Left) * scale);
            Top = (T)(object)(Convert.ToInt32(Top) * scale);
            Right = (T)(object)(Convert.ToInt32(Right) * scale);
            Bottom = (T)(object)(Convert.ToInt32(Bottom) * scale);
        }

        public glRectangle<int> Scale(float scale)
        {
            return new glRectangle<int>(
                (int)(Convert.ToInt32(Left) * scale),
                (int)(Convert.ToInt32(Top) * scale),
                (int)(Convert.ToInt32(Right) * scale),
                (int)(Convert.ToInt32(Bottom) * scale)
            );
        }

        public bool Empty()
        {
            return GetWidth() <= 0 || GetHeight() <= 0;
        }

        public override string ToString()
        {
            return $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct glPosition
    {
        public short x;
        public short y;
        public short z;
        public short w;

        public glPosition()
        {
            x = 0;
            y = 0;
            z = 0;
            w = 1;
        }

        public glPosition(short x_, short y_)
        {
            x = x_;
            y = y_;
            z = 0;
            w = 1;
        }

        public glPosition(uint param)
        {
            x = (short)((param << 5) >> 5);
            y = (short)((param >> 11) >> 5);
            z = 0;
            w = 1;
        }

        public static glPosition operator +(glPosition lhs, glPosition rhs)
        {
            return new glPosition((short)(lhs.x + rhs.x), (short)(lhs.y + rhs.y));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct glColor
    {
        private uint value;

        // Red 分量 (低 8 位)
        public byte R
        {
            get => (byte)(value & 0xFF);
            set => this.value = (this.value & ~0xFFu) | (uint)value;
        }

        // Green 分量 (第 8-15 位)
        public byte G
        {
            get => (byte)((value >> 8) & 0xFF);
            set => this.value = (this.value & ~(0xFFu << 8)) | ((uint)value << 8);
        }

        // Blue 分量 (第 16-23 位)
        public byte B
        {
            get => (byte)((value >> 16) & 0xFF);
            set => this.value = (this.value & ~(0xFFu << 16)) | ((uint)value << 16);
        }

        // Command 分量 (第 24-31 位)
        public byte Command
        {
            get => (byte)((value >> 24) & 0xFF);
            set => this.value = (this.value & ~(0xFFu << 24)) | ((uint)value << 24);
        }
        public uint Value
        {
            get => value;
            set => this.value = value;
        }

        public glColor(byte r, byte g, byte b)
        {
            value = 0;
            R = r;
            G = g;
            B = b;
            Command = 0;
        }

        public glColor(uint gpuParam)
        {
            value = gpuParam;
        }

        public override string ToString()
        {
            return $"Value: {value:X8}, R: {R}, G: {G}, B: {B}, Command: {Command}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct glTexCoord
    {
        public short u;
        public short v;

        public glTexCoord()
        {
            u = 0;
            v = 0;
        }

        public glTexCoord(short u_, short v_)
        {
            u = u_;
            v = v_;
        }

        public glTexCoord(uint gpuParam)
        {
            u = (short)(gpuParam & 0xff);
            v = (short)((gpuParam >> 8) & 0xff);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct glClutAttribute
    {
        public const ushort WriteMask = 0x7FFF; // 低 15 位

        private ushort value;

        public ushort Value
        {
            get => (ushort)(value & WriteMask);
            set => this.value = (ushort)(value & WriteMask);
        }

        // x: 6 bits (bits 0-5)
        public byte X
        {
            get => (byte)(value & 0x3F); // 提取低 6 位
            set => this.value = (ushort)((this.value & ~0x3F) | (value & 0x3F)); // 设置低 6 位
        }

        // y: 9 bits (bits 6-14)
        public ushort Y
        {
            get => (ushort)((value >> 6) & 0x1FF); // 提取第 6-14 位
            set => this.value = (ushort)((this.value & ~(0x1FF << 6)) | ((value & 0x1FF) << 6)); // 设置第 6-14 位
        }

        public glClutAttribute(ushort v)
        {
            value = (ushort)(v & WriteMask);
        }

        public override string ToString()
        {
            return $"Value: {value:X4}, X: {X}, Y: {Y}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct glTexPage
    {
        public const ushort WriteMask = 0x09FF; //低 12 位

        private ushort value;

        public ushort Value
        {
            get => (ushort)(value & WriteMask);
            set => this.value = (ushort)(value & WriteMask);
        }

        // texturePageBaseX: 4 bits (bits 0-3)
        public byte TexturePageBaseX
        {
            get => (byte)(value & 0x000F); // 提取低 4 位
            set => value = (byte)((value & ~0x000F) | (value & 0x000F)); // 设置低 4 位
        }

        // texturePageBaseY: 1 bit (bit 4)
        public byte TexturePageBaseY
        {
            get => (byte)((value >> 4) & 0x01); // 提取第 4 位
            set => value = (byte)((value & ~(1 << 4)) | ((value & 0x01) << 4)); // 设置第 4 位
        }

        // semiTransparencymode: 2 bits (bits 5-6)
        public byte SemiTransparencymode
        {
            get => (byte)((value >> 5) & 0x03); // 提取第 5-6 位
            set => value = (byte)((value & ~(3 << 5)) | ((value & 0x03) << 5)); // 设置第 5-6 位
        }

        // texturePageColors: 2 bits (bits 7-8)
        public byte TexturePageColors
        {
            get => (byte)((value >> 7) & 0x03); // 提取第 7-8 位
            set => value = (byte)((value & ~(3 << 7)) | ((value & 0x03) << 7)); // 设置第 7-8 位
        }

        // textureDisable: 1 bit (bit 11)
        public bool TextureDisable
        {
            get => ((value >> 11) & 0x01) != 0; // 提取第 11 位
            set => this.value = (ushort)((this.value & ~(1 << 11)) | ((value ? 1 : 0) << 11)); // 设置第 11 位
        }

        public glTexPage(ushort v)
        {
            value = (ushort)(v & WriteMask);
        }

        public override string ToString()
        {
            return $"Value: {value:X4}, " +
                   $"TexturePageBaseX: {TexturePageBaseX}, " +
                   $"TexturePageBaseY: {TexturePageBaseY}, " +
                   $"SemiTransparencymode: {SemiTransparencymode}, " +
                   $"TexturePageColors: {TexturePageColors}, " +
                   $"TextureDisable: {TextureDisable}";
        }
    }

    public class GLShaderStrings
    {
        public static string ClutVertix = @"
        #version 330

        in vec4 v_pos;
        in vec3 v_pos_high;
        in vec2 v_texCoord;
        in vec3 v_color;
        in int v_clut;
        in int v_texPage;

        out vec3 BlendColor;
        out vec2 TexCoord;
        out vec3 Position;
        out float invZ;

        flat out ivec2 TexPageBase;
        flat out ivec2 ClutBase;
        flat out int TexPage;

        uniform float u_resolutionScale;
        uniform bool u_pgxp;
        uniform mat4 u_mvp;

        void main()
        {
            float x, y, normalizedZ;

            if (u_pgxp)
            {
                float vertexOffset = 0.5 / u_resolutionScale;

                x = (v_pos_high.x + vertexOffset) / 512.0 - 1.0;
                y = (v_pos_high.y + vertexOffset) / 256.0 - 1.0;

                //v_pos_high.z is invZ
                float worldZ = 1.0 / max(v_pos_high.z, 0.00001);
                normalizedZ = worldZ / 32767.0;

                //normalizedZ = v_pos.z / 32767.0;   

                Position = vec3(v_pos_high.xy, normalizedZ);  

                invZ = v_pos_high.z;

                TexCoord = v_texCoord * invZ; 

                gl_Position = vec4( x, y, normalizedZ, 1.0 );

            }
            else
            {
	            float vertexOffset = 0.5 / u_resolutionScale;

	            x = ( v_pos.x + vertexOffset ) / 512.0 - 1.0;
	            y = ( v_pos.y + vertexOffset ) / 256.0 - 1.0;
                normalizedZ = v_pos.z / 32767.0;

	            Position = vec3( v_pos.xy, normalizedZ );

                invZ = 1.0;

                TexCoord = v_texCoord;

                gl_Position = vec4( x, y, normalizedZ, 1.0 );

            }

	        TexPageBase = ivec2( ( v_texPage & 0xf ) * 64, ( ( v_texPage >> 4 ) & 0x1 ) * 256 );

	        ClutBase = ivec2( ( v_clut & 0x3f ) * 16, v_clut >> 6 );

	        BlendColor = v_color;

	        TexPage = v_texPage;
        }";

        public static string ClutFragment = @"
        #version 330

        in vec3 BlendColor;
        in vec2 TexCoord;
        in vec3 Position;
        in float invZ;

        flat in ivec2 TexPageBase;
        flat in ivec2 ClutBase;
        flat in int TexPage;

        layout(location=0, index=0) out vec4 FragColor;
        layout(location=0, index=1) out vec4 ParamColor;

        uniform float u_srcBlend;
        uniform float u_destBlend;
        uniform bool u_setMaskBit;
        uniform bool u_drawOpaquePixels;
        uniform bool u_drawTransparentPixels;
        uniform bool u_dither;
        uniform bool u_realColor;
        uniform ivec2 u_texWindowMask;
        uniform ivec2 u_texWindowOffset;

        uniform sampler2D u_vram;

        vec3 FloorVec3( vec3 v )
        {
	        v.r = floor( v.r );
	        v.g = floor( v.g );
	        v.b = floor( v.b );
	        return v;
        }

        vec3 RoundVec3( vec3 v )
        {
	        return FloorVec3( v + vec3( 0.5, 0.5, 0.5 ) );
        }

        uint mod_uint( uint x, uint y )
        {
	        return x - y * ( x / y );
        }

        int DitherTable[16] = int[16]( 4, 0, -3, 1, 2, -2, 3, -1, -3, 1, -4, 0, 3, -1, 2, -2 );

        vec3 Dither24bitTo15Bit( ivec2 pos, vec3 color )
        {
	        // assumes we are not using real color
	        uint index = mod_uint( uint( pos.x ), 4u ) + mod_uint( uint( pos.y ), 4u ) * 4u;
	        float offset = float( DitherTable[ index ] );
	        color = FloorVec3( color ) + vec3( offset );
	        color.r = clamp( color.r, 0.0, 255.0 );
	        color.g = clamp( color.g, 0.0, 255.0 );
	        color.b = clamp( color.b, 0.0, 255.0 );
	        return FloorVec3( color / 8.0 );
        }

        vec3 ConvertColorTo15bit( vec3 color )
        {
	        color *= 31.0;
	        if ( u_realColor )
		        return color;
	        else
		        return RoundVec3( color );
        }

        vec3 ConvertColorTo24Bit( vec3 color )
        {
	        color *= 255.0;
	        if ( u_realColor )
		        return color;
	        else
		        return RoundVec3( color );
        }

        int FloatToInt5( float value )
        {
	        return int( floor( value * 31.0 + 0.5 ) );
        }

        vec4 SampleTexture( ivec2 pos )
        {
	        return texture( u_vram, vec2( pos ) / vec2( 1024.0, 512.0 ) );
        }

        int SampleUShort( ivec2 pos )
        {
	        vec4 c = SampleTexture( pos );
	        int red = FloatToInt5( c.r );
	        int green = FloatToInt5( c.g );
	        int blue = FloatToInt5( c.b );
	        int maskBit = int( ceil( c.a ) );
	        return ( maskBit << 15 ) | ( blue << 10 ) | ( green << 5 ) | red;
        }

        vec4 SampleColor( ivec2 pos )
        {
	        vec4 color = SampleTexture( pos );
	        color.rgb = ConvertColorTo15bit( color.rgb );
	        return color;
        }

        vec4 SampleClut( int index )
        {
	        return SampleColor( ClutBase + ivec2( index, 0 ) );
        }

        // texCoord counted in 4bit steps
        int SampleIndex4( ivec2 texCoord )
        {
	        int sample = SampleUShort( TexPageBase + ivec2( texCoord.x / 4, texCoord.y ) );
	        int shiftAmount = ( texCoord.x & 0x3 ) * 4;
	        return ( sample >> shiftAmount ) & 0xf;
        }

        // texCoord counted in 8bit steps
        int SampleIndex8( ivec2 texCoord )
        {
	        int sample = SampleUShort( TexPageBase + ivec2( texCoord.x / 2, texCoord.y ) );
	        int shiftAmount = ( texCoord.x & 0x1 ) * 8;
	        return ( sample >> shiftAmount ) & 0xff;
        }

        vec4 LookupTexel()
        {
	        vec4 color;
            ivec2 texCoord;

            vec2 baseTexCoord = TexCoord / invZ;
            texCoord = ivec2(floor(baseTexCoord + vec2(0.0001))) & ivec2(0xff);

	        // apply texture window
	        texCoord.x = ( texCoord.x & ~( u_texWindowMask.x * 8 ) ) | ( ( u_texWindowOffset.x & u_texWindowMask.x ) * 8 );
	        texCoord.y = ( texCoord.y & ~( u_texWindowMask.y * 8 ) ) | ( ( u_texWindowOffset.y & u_texWindowMask.y ) * 8 );

	        int colorMode = ( TexPage >> 7 ) & 0x3;
	        if ( colorMode == 0 )
	        {
		        color = SampleClut( SampleIndex4( texCoord ) ); // get 4bit index
	        }
	        else if ( colorMode == 1 )
	        {
		        color = SampleClut( SampleIndex8( texCoord ) ); // get 8bit index
	        }
	        else
	        {
		        color = SampleColor( TexPageBase + texCoord );
	        }

	        return color;
        }

        void main()
        {
	        vec4 color;

	        float srcBlend = u_srcBlend;
	        float destBlend = u_destBlend;

	        vec3 blendColor = ConvertColorTo24Bit( BlendColor );

	        if ( bool( TexPage & ( 1 << 11 ) ) )
	        {
		        // texture disabled
		        color = vec4( blendColor, 0.0 );
	        }
	        else
	        {
		        // texture enabled
		        color = LookupTexel();

		        // check if pixel is fully transparent
		        if ( color == vec4( 0.0 ) )
			        discard;

		        if ( color.a == 0.0 )
		        {
			        if ( !u_drawOpaquePixels )
				        discard;

			        // disable semi transparency
			        srcBlend = 1.0;
			        destBlend = 0.0;
		        }
		        else if ( !u_drawTransparentPixels )
		        {
			        discard;
		        }

		        // blend color, result is 8bit
		        color.rgb = ( color.rgb * blendColor.rgb ) / 16.0;
	        }

	        if ( u_realColor )
	        {
		        color.rgb /= 255.0;
	        }
	        else if ( u_dither )
	        {
		        ivec2 pos = ivec2( floor( Position.xy + vec2( 0.0001 ) ) );
		        color.rgb = Dither24bitTo15Bit( pos, color.rgb ) / 31.0;
	        }
	        else
	        {
		        color.rgb = FloorVec3( color.rgb / 8.0 ) / 31.0;
	        }

	        if ( u_setMaskBit )
		        color.a = 1.0;

	        // output color
	        FragColor = color;

	        // use alpha for src blend, rgb for dest blend
	        ParamColor = vec4( destBlend, destBlend, destBlend, srcBlend );

	        // set depth from mask bit
	        if ( color.a == 0.0 )
		        gl_FragDepth = 1.0;
	        else
		        gl_FragDepth = Position.z;
        }";

        public static string VRamViewVertex = @"
        #version 330

        const vec2 positions[4] = vec2[](vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0));
        const vec2 texCoords[4] = vec2[](vec2(0.0, 1.0), vec2(1.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 0.0));

        out vec2 TexCoord;

        void main()
        {
            TexCoord = texCoords[gl_VertexID];
            gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
        }";

        public static string VRamViewFragment = @"
        #version 330

        in vec2 TexCoord;

        out vec4 FragColor;

        uniform sampler2D tex;

        void main()
        {
            FragColor = texture(tex, TexCoord);
        }";

        public static string Output24bitVertex = @"
        #version 330

        const vec2 s_positions[4] = vec2[]( vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0) );
        const vec2 s_texCoords[4] = vec2[]( vec2(0.0, 1.0), vec2(1.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 0.0) );

        out vec2 TexCoord;

        void main()
        {
	        TexCoord = s_texCoords[ gl_VertexID ];
	        gl_Position = vec4( s_positions[ gl_VertexID ], 0.0, 1.0 );
        }";

        public static string Output24bitFragment = @"
        #version 330

        in vec2 TexCoord;

        out vec4 FragColor;

        uniform ivec4 u_srcRect;

        uniform sampler2D u_vram;

        uint FloatTo5bit( float value )
        {
	        return uint( round( value * 31.0 ) );
        }

        uint SampleVRam16( ivec2 pos )
        {
	        vec4 c = texture( u_vram, pos / vec2( 1024.0, 512.0 ) );
	        uint red = FloatTo5bit( c.r );
	        uint green = FloatTo5bit( c.g );
	        uint blue = FloatTo5bit( c.b );
	        uint maskBit = uint( ceil( c.a ) );
	        return ( maskBit << 15 ) | ( blue << 10 ) | ( green << 5 ) | red;
        }

        vec3 SampleVRam24( ivec2 base, ivec2 offset )
        {
	        int x = base.x + ( offset.x * 3 ) / 2;
	        int y = base.y + offset.y;

	        uint sample1 = SampleVRam16( ivec2( x, y ) );
	        uint sample2 = SampleVRam16( ivec2( x + 1, y ) );

	        uint r, g, b;
	        if ( ( offset.x & 1 ) == 0 )
	        {
		        r = sample1 & 0xffu;
		        g = sample1 >> 8;
		        b = sample2 & 0xffu;
	        }
	        else
	        {
		        r = sample1 >> 8;
		        g = sample2 & 0xffu;
		        b = sample2 >> 8;
	        }

	        return vec3( float( r ) / 255.0, float( g ) / 255.0, float( b ) / 255.0 );
        }

        void main()
        {
	        ivec2 base =  u_srcRect.xy;
	        ivec2 offset = ivec2( TexCoord * u_srcRect.zw );
	        vec3 sample = SampleVRam24( base, offset );
	        FragColor = vec4( sample.rgb, 1.0 );
        }";

        public static string Output16bitVertex = @"
        #version 330

        const vec2 s_positions[4] = vec2[]( vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0) );
        const vec2 s_texCoords[4] = vec2[]( vec2(0.0, 1.0), vec2(1.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 0.0) );

        out vec2 TexCoord;

        void main()
        {
	        TexCoord = s_texCoords[ gl_VertexID ];
	        gl_Position = vec4( s_positions[ gl_VertexID ], 0.0, 1.0 );
        }";

        public static string Output16bitFragment = @"
        #version 330

        in vec2 TexCoord;

        out vec4 FragColor;

        uniform ivec4 u_srcRect;

        uniform sampler2D u_vram;

        void main()
        {
	        vec2 texCoord = vec2( u_srcRect.xy ) + TexCoord * vec2( u_srcRect.zw );
	        vec4 texel = texture( u_vram, texCoord / vec2( 1024.0, 512.0 ) );
	        FragColor = texel;
        }";

        public static string ResetDepthVertex = @"
        #version 330

        const vec2 s_positions[4] = vec2[]( vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0) );
        const vec2 s_texCoords[4] = vec2[]( vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(0.0, 1.0), vec2(1.0, 1.0) );

        out vec2 TexCoord;

        void main()
        {
	        TexCoord = s_texCoords[ gl_VertexID ];
	
	        gl_Position = vec4( s_positions[ gl_VertexID ], 0.0, 1.0 );
        }";

        public static string ResetDepthFragment = @"
        #version 330

        in vec2 TexCoord;

        uniform sampler2D u_vram;

        void main()
        {
	        vec4 color = texture( u_vram, TexCoord );

	        // set depth from mask bit
	        gl_FragDepth = 1.0 - color.a;
        }";

        public static string GetPixelsVertex = @"
        #version 330

        //正常输出
        //const vec2 positions[4] = vec2[]( vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0) );
        //const vec2 texCoords[4] = vec2[]( vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(0.0, 1.0), vec2(1.0, 1.0) );

        //上下翻转
        const vec2 positions[4] = vec2[]( vec2(-1.0, -1.0),vec2(1.0, -1.0),vec2(-1.0, 1.0),vec2(1.0, 1.0) );
        const vec2 texCoords[4] = vec2[]( vec2(0.0, 1.0), vec2(1.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 0.0) );

        out vec2 TexCoord;

        void main()
        {
	        TexCoord = texCoords[ gl_VertexID ];
	        gl_Position = vec4( positions[ gl_VertexID ], 0.0, 1.0 );
        }";

        public static string GetPixelsFragment = @"
        #version 330

        in vec2 TexCoord;

        out vec4 FragColor;

        uniform sampler2D tex;

        void main()
        {
	        FragColor = texture( tex, TexCoord );
        }";

        public static string DisplayVertex = @"
        #version 330

        const vec2 positions[4] = vec2[]( vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0) );
        const vec2 texCoords[4] = vec2[]( vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(0.0, 1.0), vec2(1.0, 1.0) );

        out vec2 TexCoord;

        void main()
        {
            gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
            TexCoord = texCoords[gl_VertexID];
        }";

        public static string DisplayFragment = @"
        #version 330

        in vec2 TexCoord;

        out vec4 FragColor;

        uniform sampler2D tex;

        void main()
        {
	        FragColor = texture( tex, TexCoord );
        }";

        public static string VRamCopyVertex = @"
        #version 330

        const vec2 s_positions[4] = vec2[](vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0), vec2(1.0, 1.0));
        const vec2 s_texCoords[4] = vec2[](vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(0.0, 1.0), vec2(1.0, 1.0));

        out vec2 TexCoord;

        uniform vec4 u_srcRect; // 0-1

        void main()
        {
            TexCoord = u_srcRect.xy + u_srcRect.zw * s_texCoords[gl_VertexID];

            gl_Position = vec4(s_positions[gl_VertexID], 0.0, 1.0);
        }";

        public static string VRamCopyFragment = @"
        #version 330

        in vec2 TexCoord;

        out vec4 FragColor;

        uniform bool u_forceMaskBit;
        uniform float u_maskedDepth;

        uniform sampler2D u_vram;

        void main()
        {
            vec4 color = texture(u_vram, TexCoord);

            if (u_forceMaskBit)
                color.a = 1.0;

            FragColor = color;

            // set depth from mask bit
            if (color.a == 0.0)
                gl_FragDepth = 1.0;
            else
                gl_FragDepth = u_maskedDepth;
        }";

        public static string ComputeCropShader = @"
        #version 430 core

        layout(local_size_x = 16, local_size_y = 1) in; // 每个工作组处理一列像素

        uniform sampler2D u_inputTexture;
        uniform int u_textureHeight;
        uniform float u_threshold;
        uniform float u_maxBlackBarRatio;

        layout(std430, binding = 0) buffer CropData {
            int topCrop;
            int bottomCrop;
        };

        void main() {
            ivec2 texSize = textureSize(u_inputTexture, 0);
            int maxBlackLines = int(u_maxBlackBarRatio * u_textureHeight);

            // 获取当前处理的列索引
            int x = int(gl_GlobalInvocationID.x);
    
            // --- 检测上黑边 ---
            int top = 0;
            for (int y = 0; y < maxBlackLines; y++) {
                vec4 pixel = texelFetch(u_inputTexture, ivec2(x, y), 0);
                if (length(pixel.rgb) > u_threshold) {
                    atomicMax(topCrop, y + 1); // 记录最大非黑边行
                    break;
                }
            }
    
            // --- 检测下黑边 ---
            int bottom = 0;
            for (int y = texSize.y - 1; y >= texSize.y - maxBlackLines; y--) {
                vec4 pixel = texelFetch(u_inputTexture, ivec2(x, y), 0);
                if (length(pixel.rgb) > u_threshold) {
                    atomicMin(bottomCrop, texSize.y - y - 1); // 记录最小非黑边行
                    break;
                }
            }
        }";

    }

}
