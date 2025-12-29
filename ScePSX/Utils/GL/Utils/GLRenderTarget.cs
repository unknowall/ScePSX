using System;
using System.Diagnostics.Contracts;

namespace ScePSX.GL.Utils
{
    [Flags]
    public enum RenderTargetLayers
    {
        Color = 1 << 0,
        Depth = 1 << 1,
        Stencil = 1 << 2,
        All = Color | Depth | Stencil
    }

    public unsafe class GLRenderTarget : IDisposable
    {
        [ThreadStatic] public static GLRenderTarget Current = GLRenderTargetScreen.Default;

        protected uint FrameBufferId;
        public GLTexture TextureColor { get; private set; }
        public GLTexture TextureDepth { get; private set; }
        //public GLRenderBuffer RenderBufferStencil { get; private set; }
        private int _Width;
        private int _Height;
        public RenderTargetLayers RenderTargetLayers { get; private set; }

        public virtual int Width => _Width;
        public virtual int Height => _Height;

        protected GLRenderTarget()
        {
        }

        public static void CopyFromTo(GLRenderTarget From, GLRenderTarget To)
        {
            Contract.Assert(From != null);
            Contract.Assert(To != null);

            From.BindUnbind(() =>
            {
                To.TextureColor?.BindUnbind(() =>
                {
                    Gl.CopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA4, 0, 0, From.Width, From.Height, 0);
                });

                To.TextureDepth?.BindUnbind(() =>
                {
                    Gl.CopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_DEPTH_COMPONENT, 0, 0, From.Width, From.Height, 0);
                });
            });
        }

        protected GLRenderTarget(int Width, int Height, RenderTargetLayers RenderTargetLayers)
        {
            if (Width == 0 || Height == 0)
                throw new Exception($"Invalid GLRenderTarget size: {Width}x{Height}");
            _Width = Width;
            _Height = Height;
            this.RenderTargetLayers = RenderTargetLayers;
            Initialize();
        }

        public static GLRenderTarget Create(int Width, int Height, RenderTargetLayers RenderTargetLayers = RenderTargetLayers.All)
        {
            return new GLRenderTarget(Width, Height, RenderTargetLayers);
        }

        private void Initialize()
        {
            fixed (uint* FrameBufferPtr = &FrameBufferId)
            {
                Gl.GenFramebuffers(1, FrameBufferPtr);
                if ((RenderTargetLayers & RenderTargetLayers.Color) != 0)
                    TextureColor = GLTexture.Create().SetFormat(TextureFormat.RGBA).SetSize(_Width, _Height);
                if ((RenderTargetLayers & RenderTargetLayers.Depth) != 0)
                    TextureDepth = GLTexture.Create().SetFormat(TextureFormat.DEPTH).SetSize(_Width, _Height);
                //if ((RenderTargetLayers & RenderTargetLayers.Stencil) != 0)
                //    RenderBufferStencil = new GLRenderBuffer(_Width, _Height, GL.GL_STENCIL_INDEX8);
            }
        }

        public void Dispose()
        {
            fixed (uint* FrameBufferPtr = &FrameBufferId)
            {
                Gl.DeleteFramebuffers(1, FrameBufferPtr);

                if ((RenderTargetLayers & RenderTargetLayers.Color) != 0)
                {
                    TextureColor.Dispose();
                }

                if ((RenderTargetLayers & RenderTargetLayers.Depth) != 0)
                {
                    TextureDepth.Dispose();
                }

                //if ((RenderTargetLayers & RenderTargetLayers.Stencil) != 0)
                //{
                //    RenderBufferStencil.Dispose();
                //}
            }
        }

        private void Unbind()
        {
        }

        public void BindUnbind(Action Action)
        {
            var OldFrameBuffer = Gl.GetInteger(Gl.GL_FRAMEBUFFER_BINDING);
            Bind();
            try
            {
                Action();
            }
            finally
            {
                Gl.BindFramebuffer(Gl.GL_FRAMEBUFFER, (uint)OldFrameBuffer);
            }
        }

        protected virtual void BindBuffers()
        {
            if ((RenderTargetLayers & RenderTargetLayers.Color) != 0)
            {
                Gl.FramebufferTexture2D(Gl.GL_FRAMEBUFFER, Gl.GL_COLOR_ATTACHMENT0, Gl.GL_TEXTURE_2D, TextureColor.Texture, 0);
            }

            if ((RenderTargetLayers & RenderTargetLayers.Depth) != 0)
            {
                Gl.FramebufferTexture2D(Gl.GL_FRAMEBUFFER, Gl.GL_DEPTH_ATTACHMENT, Gl.GL_TEXTURE_2D, TextureDepth.Texture, 0);
            }

            //if ((RenderTargetLayers & RenderTargetLayers.Stencil) != 0)
            //{
            //    GL.FramebufferRenderbuffer(GL.GL_FRAMEBUFFER, GL.GL_STENCIL_ATTACHMENT, GL.GL_RENDERBUFFER, RenderBufferStencil.Index);
            //}

            var Status = Gl.CheckFramebufferStatus(Gl.GL_FRAMEBUFFER);
            if (Status != Gl.GL_FRAMEBUFFER_COMPLETE)
            {
                Console.WriteLine($"Failed to bind FrameBuffer 0x{Status:X4} Error 0x{Gl.GetError():X4}" +
                    $" {Gl.GetConstantString(Status)}, {RenderTargetLayers}, {Width}x{Height}");
            }
            //Console.WriteLine($"Bound FrameBuffer {FrameBufferId} : {RenderTargetLayers}, {Width}x{Height}");
            Gl.Viewport(0, 0, Width, Height);
            Gl.ClearColor(0, 0, 0, 1);
            Gl.Clear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Gl.Flush();
        }

        public GLRenderTarget Bind()
        {
            if (Current != this)
            {
                Current?.Unbind();
            }

            Current = this;
            Gl.BindFramebuffer(Gl.GL_FRAMEBUFFER, FrameBufferId);
            {
                BindBuffers();
            }
            return this;
        }

        public byte[] ReadPixels()
        {
            var Data = new byte[Width * Height * 4];

            fixed (byte* DataPtr = Data)
            {
                Gl.ReadPixels(0, 0, Width, Height, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, DataPtr);
            }

            return Data;
        }

        public override string ToString()
        {
            return $"GLRenderTarget({FrameBufferId}, Size({Width}x{Height}))";
        }

        public class GLRenderBuffer : IDisposable
        {
            public readonly int Width, Height;

            public uint Index => _Index;

            private uint _Index;

            public GLRenderBuffer(int Width, int Height, int Format)
            {
                this.Width = Width;
                this.Height = Height;
                fixed (uint* IndexPtr = &_Index)
                {
                    Gl.GenRenderbuffers(1, IndexPtr);
                    Gl.BindRenderbuffer(Gl.GL_RENDERBUFFER, _Index);
                    Gl.RenderbufferStorage(Gl.GL_RENDERBUFFER, Format, Width, Height);
                }
            }

            public void Dispose()
            {
                fixed (uint* IndexPtr = &_Index)
                {
                    Gl.DeleteRenderbuffers(1, IndexPtr);
                }
            }
        }
    }

    public class GLRenderTargetScreen : GLRenderTarget
    {
        public static GLRenderTargetScreen Default => new GLRenderTargetScreen();

        public override int Width => 64;

        public override int Height => 64;

        protected GLRenderTargetScreen()
        {
            FrameBufferId = 0;
        }

        protected override void BindBuffers()
        {
        }
    }
}
