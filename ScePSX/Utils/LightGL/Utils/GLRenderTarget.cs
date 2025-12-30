using System;
using System.Diagnostics.Contracts;

namespace LightGL
{
    [Flags]
    public enum TargetLayers
    {
        Color = 1 << 0,
        Depth = 1 << 1,
        Stencil = 1 << 2,
        All = Color | Depth
    }

    public unsafe class GLRenderTarget : IDisposable
    {
        [ThreadStatic] public static GLRenderTarget Current = GLRenderTargetScreen.Default;

        protected uint FrameBufferId;
        public GLTexture2D TextureColor
        {
            get; private set;
        }
        public GLTexture2D TextureDepth
        {
            get; private set;
        }
        public GLRenderBuffer RenderBufferStencil
        {
            get; private set;
        }
        private int _Width;
        private int _Height;
        public TargetLayers TargetLayers
        {
            get; private set;
        }
        public TextureFormat ColorFormat;

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
                    GL.CopyTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA4, 0, 0, From.Width, From.Height, 0);
                });

                To.TextureDepth?.BindUnbind(() =>
                {
                    GL.CopyTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_DEPTH_COMPONENT, 0, 0, From.Width, From.Height, 0);
                });
            });
        }

        protected GLRenderTarget(int Width, int Height, TargetLayers RenderTargetLayers, TextureFormat ColorFormat)
        {
            if (Width == 0 || Height == 0)
                throw new Exception($"Invalid GLRenderTarget size: {Width}x{Height}");
            _Width = Width;
            _Height = Height;
            this.ColorFormat = ColorFormat;
            this.TargetLayers = RenderTargetLayers;
            Initialize();
        }

        public static GLRenderTarget Create(int Width, int Height,
            TargetLayers RenderTargetLayers = TargetLayers.All,
            TextureFormat ColorFormat = TextureFormat.RGBA
            )
        {
            return new GLRenderTarget(Width, Height, RenderTargetLayers, ColorFormat);
        }

        private void Initialize()
        {
            fixed (uint* FrameBufferPtr = &FrameBufferId)
            {
                GL.GenFramebuffers(1, FrameBufferPtr);
                if ((TargetLayers & TargetLayers.Color) != 0)
                    TextureColor = GLTexture2D.Create().SetFormat(this.ColorFormat).SetSize(_Width, _Height);
                if ((TargetLayers & TargetLayers.Depth) != 0)
                    TextureDepth = GLTexture2D.Create().SetFormat(TextureFormat.DEPTH).SetSize(_Width, _Height);
                if ((TargetLayers & TargetLayers.Stencil) != 0)
                    RenderBufferStencil = new GLRenderBuffer(_Width, _Height, GL.GL_STENCIL_INDEX8);
            }
        }

        public void Dispose()
        {
            Unbind();

            fixed (uint* FrameBufferPtr = &FrameBufferId)
            {
                GL.DeleteFramebuffers(1, FrameBufferPtr);

                if ((TargetLayers & TargetLayers.Color) != 0)
                {
                    TextureColor.Dispose();
                }

                if ((TargetLayers & TargetLayers.Depth) != 0)
                {
                    TextureDepth.Dispose();
                }

                if ((TargetLayers & TargetLayers.Stencil) != 0)
                {
                    RenderBufferStencil.Dispose();
                }
            }
        }

        public GLRenderTarget Unbind(int ID = -1)
        {
            int OldFrameBuffer = ID == -1 ? GL.GetInteger(GL.GL_FRAMEBUFFER_BINDING) : ID;
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, (uint)OldFrameBuffer);

            return this;
        }

        public void BindUnbind(Action Action)
        {
            var OldFrameBuffer = GL.GetInteger(GL.GL_FRAMEBUFFER_BINDING);
            Bind();
            try
            {
                Action();
            } finally
            {
                GL.BindFramebuffer(GL.GL_FRAMEBUFFER, (uint)OldFrameBuffer);
            }
        }

        protected virtual void BindBuffers()
        {
            if ((TargetLayers & TargetLayers.Color) != 0)
            {
                GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT0, GL.GL_TEXTURE_2D, TextureColor.Texture, 0);
            }
            CheckComplete();

            if ((TargetLayers & TargetLayers.Depth) != 0)
            {
                GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_DEPTH_ATTACHMENT, GL.GL_TEXTURE_2D, TextureDepth.Texture, 0);
            }
            CheckComplete();

            if ((TargetLayers & TargetLayers.Stencil) != 0)
            {
                GL.FramebufferRenderbuffer(GL.GL_FRAMEBUFFER, GL.GL_STENCIL_ATTACHMENT, GL.GL_RENDERBUFFER, RenderBufferStencil.Index);
            }
            CheckComplete();

            //Console.WriteLine($"Bound FrameBuffer {FrameBufferId} : {TargetLayers}, {Width}x{Height}");
            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
            GL.Flush();
        }

        private void CheckComplete()
        {
            var Status = GL.CheckFramebufferStatus(GL.GL_FRAMEBUFFER);
            if (Status != GL.GL_FRAMEBUFFER_COMPLETE)
            {
                throw new Exception($"Failed to bind FrameBuffer 0x{Status:X4} GlError 0x{GL.GetError():X4}" +
                    $" {GL.GetConstantString(Status)}, {TargetLayers}, {Width}x{Height}");
            }
        }

        public bool IsComplete()
        {
            return GL.CheckFramebufferStatus(GL.GL_FRAMEBUFFER) == GL.GL_FRAMEBUFFER_COMPLETE;
        }

        public GLRenderTarget Bind()
        {
            if (Current != this)
            {
                Current?.Unbind();
            }

            Current = this;
            GL.BindFramebuffer(GL.GL_FRAMEBUFFER, FrameBufferId);
            {
                BindBuffers();
            }
            return this;
        }

        public void Bind(FramebufferTarget binding)
        {
            GL.BindFramebuffer((int)binding, FrameBufferId);
        }

        public byte[] ReadPixels()
        {
            var Data = new byte[Width * Height * 4];

            fixed (byte* DataPtr = Data)
            {
                GL.ReadPixels(0, 0, Width, Height, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, DataPtr);
            }

            return Data;
        }

        public override string ToString()
        {
            return $"GLRenderTarget({FrameBufferId}, Size({Width}x{Height}))";
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
