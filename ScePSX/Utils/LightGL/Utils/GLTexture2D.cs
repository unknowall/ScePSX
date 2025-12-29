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
        BGRA = 7
    }

    public unsafe class GLTexture2D : IDisposable
    {
        private bool CapturedAndMustDispose;
        private uint _Texture;
        public int Width { get; private set; }
        public int Height { get; private set; }
        private TextureFormat TextureFormat = TextureFormat.RGBA;
        private byte[] Data;

        public uint Texture => _Texture;

        private GLTexture2D(uint _Texture)
        {
            this._Texture = _Texture;
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
            fixed (uint* TexturePtr = &_Texture) GL.GenTextures(1, TexturePtr);
            CapturedAndMustDispose = true;
            Bind();
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
            //GL.glGetIntegerv(GL.GL_TEXTURE_BINDING_2D);
            //GL.glEnable(GL.GL_TEXTURE_2D);
            GL.BindTexture(GL.GL_TEXTURE_2D, _Texture);
            return this;
        }

        public static void Unbind()
        {
            //GL.glDisable(GL.GL_TEXTURE_2D);
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

        public GLTexture2D SetData(void* Pointer)
        {
            var Size = Width * Height * 4;
            Data = new byte[Size];
            Marshal.Copy(new IntPtr(Pointer), Data, 0, Size);
            _SetTexture();
            return this;
        }

        public GLTexture2D SetData<T>(T[] SetData)
        {
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

        //public GLTexture Upload()
        //{
        //	_SetTexture();
        //	return this;
        //}

        const int GL_R8 = 0x8229;
        const int GL_RED = 0x1903;

        const int GL_RG = 0x8227;
        const int GL_RG8 = 0x822B;

        private void _SetTexture()
        {
            if (TextureFormat == TextureFormat.UNSET)
                return;
            if (Width == 0 || Height == 0)
                return;

            Bind();

            fixed (byte* DataPtr = Data)
            {
                //Console.WriteLine("{0}:{1}: {2}x{3}: {4}", Texture, TextureFormat, Width, Height, new IntPtr(DataPtr));
                //if (this.Data != null) Console.WriteLine(String.Join(",", this.Data));
                switch (TextureFormat)
                {
                    case TextureFormat.BGRA:
                        GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, Width, Height, 0, GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, DataPtr);
                        break;
                    case TextureFormat.RGBA:
                        GL.TexImage2D(GL.GL_TEXTURE_2D, 0, 4, Width, Height, 0, GetOpenglFormat(), GL.GL_UNSIGNED_BYTE, DataPtr);
                        break;
                    case TextureFormat.RGB:
                        GL.TexImage2D(GL.GL_TEXTURE_2D, 0, 3, Width, Height, 0, GetOpenglFormat(), GL.GL_UNSIGNED_BYTE, DataPtr);
                        break;
                    case TextureFormat.RG:
                        GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GL_RG8, Width, Height, 0, GetOpenglFormat(), GL.GL_UNSIGNED_BYTE, DataPtr);
                        break;
                    case TextureFormat.R:
                        GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GL_R8, Width, Height, 0, GetOpenglFormat(), GL.GL_UNSIGNED_BYTE, DataPtr);
                        break;
                    case TextureFormat.DEPTH:
                        GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_DEPTH_COMPONENT, Width, Height, 0, GetOpenglFormat(), GL.GL_UNSIGNED_SHORT, DataPtr);
                        break;
                    //case TextureFormat.STENCIL: GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_DEPTH_COMPONENT, this.Width, this.Height, 0, GL.GL_DEPTH_COMPONENT, GL.GL_UNSIGNED_SHORT, DataPtr); break;
                    default:
                        throw new InvalidOperationException("Unsupported " + TextureFormat);
                }
            }
        }

        private int GetOpenglFormat()
        {
            switch (TextureFormat)
            {
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
                fixed (uint* TexturePtr = &_Texture) GL.DeleteTextures(1, TexturePtr);
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
