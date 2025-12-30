using System;
using System.Runtime.InteropServices;

namespace LightGL
{
    public enum TextureFormat
    {
        UNSET = 0,
        RGBA = 1,
        DEPTH = 2,
        STENCIL = 3,
        RG = 4,
        R = 5,
        RGB = 6,
        BGRA = 7,
        RGBA8 = 8
    }

    public unsafe class GLTexture2D : IDisposable
    {
        private bool CapturedAndMustDispose;
        private uint _Texture;
        public int Width
        {
            get; private set;
        }
        public int Height
        {
            get; private set;
        }
        private TextureFormat TextureFormat = TextureFormat.RGBA;
        private byte[] Data;
        private PixelFormat pixelFormat = PixelFormat.Rgba;
        private PixelType pixelType = PixelType.UnsignedByte;
        private int GLTextureFormat = 0;

        public static int _GolbalTetureIndex = 0;

        public uint Texture => _Texture;
        public int Index = 0;

        private GLTexture2D(uint _Texture)
        {
            this._Texture = _Texture;
            this.Index = (_GolbalTetureIndex++);
            CapturedAndMustDispose = false;
            Bind();
        }

        private GLTexture2D()
        {
            Initialize();
        }

        public static GLTexture2D Create()
        {
            return new GLTexture2D();
        }

        public static GLTexture2D Wrap(uint Texture, int Width = 0, int Height = 0)
        {
            return new GLTexture2D(Texture) { Width = Width, Height = Height };
        }

        private void Initialize()
        {
            fixed (uint* TexturePtr = &_Texture)
                GL.GenTextures(1, TexturePtr);
            CapturedAndMustDispose = true;
            Bind();
            SetFilter();
            SetWrap();
        }

        public void BindUnbind(Action Action)
        {
            var OldTexture = (uint)GL.GetInteger(GL.GL_TEXTURE_BINDING_2D);
            try
            {
                Bind();
                Action();
            } finally
            {
                GL.BindTexture(GL.GL_TEXTURE_2D, OldTexture);
            }
        }

        public GLTexture2D Bind()
        {
            GL.BindTexture(GL.GL_TEXTURE_2D, _Texture);
            return this;
        }

        public static void Unbind()
        {
        }

        public GLTexture2D SetFilter(TextureMinFilter linear = TextureMinFilter.Nearest)
        {
            GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureMinFilter, (int)linear);
            GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureMagFilter, (int)linear);

            return this;
        }

        public GLTexture2D SetWrap(TextureWrapMode wrap = TextureWrapMode.ClampToEdge)
        {
            GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureWrapS, (int)wrap);
            GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureWrapT, (int)wrap);

            return this;
        }

        public GLTexture2D SetFormat(TextureFormat TextureFormat)
        {
            this.TextureFormat = TextureFormat;
            _SetTexture();
            return this;
        }

        public GLTexture2D SetSize(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
            _SetTexture();
            return this;
        }

        public GLTexture2D SetData(void* Pointer, PixelFormat pixelFormat = PixelFormat.Rgba, PixelType pixelType = PixelType.UnsignedByte)
        {
            if (Pointer == null)
                return this;

            this.pixelType = pixelType;
            this.pixelFormat = pixelFormat;
            var Size = Width * Height * 4;
            Data = new byte[Size];
            Marshal.Copy(new IntPtr(Pointer), Data, 0, Size);
            _SetTexture();
            return this;
        }

        public unsafe GLTexture2D SetData(InternalFormat internalColorFormat, int width, int height, PixelFormat pixelFormat, PixelType pixelType, void* pixels, int mipmapLevel = 0)
        {
            Bind();
            GL.TexImage2D(GL.GL_TEXTURE_2D, mipmapLevel, (int)internalColorFormat, width, height, 0, (int)pixelFormat, (int)pixelType, pixels);
            this.Width = width;
            this.Height = height;
            this.pixelType = pixelType;
            this.pixelFormat = pixelFormat;
            this.GLTextureFormat = (int)internalColorFormat;
            return this;
        }

        public GLTexture2D SetDataPtr(void* Pointer, PixelFormat pixelFormat = PixelFormat.Rgba, PixelType pixelType = PixelType.UnsignedByte)
        {
            if (Pointer == null)
                return this;

            this.pixelType = pixelType;
            this.pixelFormat = pixelFormat;
            _SetTexture(true, Pointer);
            return this;
        }

        public GLTexture2D SetData<T>(T[] SetData, PixelFormat pixelFormat = PixelFormat.Rgba, PixelType pixelType = PixelType.UnsignedByte)
        {
            if (SetData.Length == 0)
                return this;

            this.pixelType = pixelType;
            this.pixelFormat = pixelFormat;
            var SetDataHandle = GCHandle.Alloc(SetData, GCHandleType.Pinned);
            try
            {
                int Size = SetData.Length * Marshal.SizeOf(typeof(T));
                Data = new byte[Size];
                Marshal.Copy(SetDataHandle.AddrOfPinnedObject(), Data, 0, Size);
            } finally
            {
                SetDataHandle.Free();
            }
            _SetTexture();
            return this;
        }

        public unsafe GLTexture2D SubData(int x, int y, int width, int height, PixelFormat pixelFormat, PixelType pixelType, void* pixels, int mipmapLevel = 0)
        {
            this.pixelFormat = pixelFormat;
            this.pixelType = pixelType;

            Bind();
            //Console.WriteLine($"OpenGL GLTexture2D SubData: {x}, {y} - {width} x {height}");
            GL.TexSubImage2D(GL.GL_TEXTURE_2D, mipmapLevel, x, y, width, height, (int)pixelFormat, (int)pixelType, pixels);
            return this;
        }

        //public GLTexture Upload()
        //{
        //	_SetTexture();
        //	return this;
        //}

        const int GL_R8 = 0x8229;
        const int GL_RED = 0x1903;

        const int GL_RG = 0x8227;
        const int GL_RG8 = 0x822B;

        private void _SetTexture(bool isPtr = false, void* Ptr = null)
        {
            if (TextureFormat == TextureFormat.UNSET)
                return;
            if (Width == 0 || Height == 0)
                return;
            if (GLTextureFormat == 0)
                GLTextureFormat = GetOpenglFormat();
            if (TextureFormat == TextureFormat.DEPTH)
            {
                pixelFormat = PixelFormat.DepthComponent;
                pixelType = PixelType.Short;
            }

            Bind();

            if (isPtr && Ptr != null)
            {
                GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GLTextureFormat, Width, Height, 0, (int)pixelFormat, (int)this.pixelType, Ptr);
            } else
            {
                fixed (byte* DataPtr = Data)
                {
                    //Console.WriteLine("{0}:{1}: {2}x{3}: {4}", Texture, TextureFormat, Width, Height, new IntPtr(DataPtr));
                    //if (this.Data != null) Console.WriteLine(String.Join(",", this.Data));
                    GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GLTextureFormat, Width, Height, 0, (int)pixelFormat, (int)this.pixelType, DataPtr);
                    //GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_DEPTH_COMPONENT, this.Width, this.Height, 0, GL.GL_DEPTH_COMPONENT, GL.GL_UNSIGNED_SHORT, DataPtr); break;
                }
            }
        }

        private int GetOpenglFormat()
        {
            switch (TextureFormat)
            {
                case TextureFormat.RGBA8:
                    return GL.GL_RGBA8;
                case TextureFormat.BGRA:
                    return GL.GL_BGRA;
                case TextureFormat.RGBA:
                    return GL.GL_RGBA;
                case TextureFormat.DEPTH:
                    return GL.GL_DEPTH_COMPONENT;
                case TextureFormat.RGB:
                    return GL.GL_RGB;
                case TextureFormat.RG:
                    return GL_RG;
                case TextureFormat.R:
                    return GL_RED;
                //case TextureFormat.STENCIL: GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_DEPTH_COMPONENT, this.Width, this.Height, 0, GL.GL_DEPTH_COMPONENT, GL.GL_UNSIGNED_SHORT, DataPtr); break;
                default:
                    throw new InvalidOperationException("Unsupported " + TextureFormat);
            }
        }

        public void Dispose()
        {
            if (CapturedAndMustDispose)
            {
                fixed (uint* TexturePtr = &_Texture)
                    GL.DeleteTextures(1, TexturePtr);
            }
            _Texture = 0;
        }

        public byte[] GetDataFromCached()
        {
            return Data;
        }

        public byte[] GetDataFromGpu()
        {
            var Data = new byte[Width * Height * 4];
            fixed (byte* DataPtr = Data)
            {
                Bind();
                GL.GetTexImage(GL.GL_TEXTURE_2D, 0, GetOpenglFormat(), GL.GL_UNSIGNED_BYTE, DataPtr);
            }
            return Data;
        }
    }
}
