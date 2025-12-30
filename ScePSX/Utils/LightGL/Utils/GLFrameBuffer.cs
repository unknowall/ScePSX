using System;

namespace LightGL
{
    public class GLFrameBuffer : IDisposable
    {
        private uint m_frameBuffer = 0;

        private static uint s_boundRead = 0;
        private static uint s_boundDraw = 0;

        public GLTexture2D TextureColor = null;
        public GLTexture2D TextureDepth = null;
        public GLRenderBuffer Stencil = null;

        public GLFrameBuffer()
        {

        }

        public GLFrameBuffer(GLFrameBuffer other) : this(other.m_frameBuffer)
        {
            other.m_frameBuffer = 0;
        }

        private GLFrameBuffer(uint frameBuffer)
        {
            m_frameBuffer = frameBuffer;
        }

        public void Dispose()
        {
            Reset();
            GC.SuppressFinalize(this);
        }

        public unsafe static GLFrameBuffer Create()
        {
            uint frameBuffer;
            GL.GenFramebuffers(1, &frameBuffer);
            return new GLFrameBuffer(frameBuffer);
        }

        public GLFrameBuffer AttachTexture(FramebufferAttachment type, GLTexture2D texture, int mipmapLevel = 0)
        {
            switch (type)
            {
                case FramebufferAttachment.DepthAttachment:
                    TextureDepth = texture;
                    break;
                case FramebufferAttachment.ColorAttachment0:
                    TextureColor = texture;
                    break;
            }
            Bind();
            GL.FramebufferTexture2D((int)FramebufferTarget.Framebuffer, (int)type, (int)TextureTarget.Texture2d, texture.Texture, mipmapLevel);
            return this;
        }

        public GLFrameBuffer AttachRenderBuffer(GLRenderBuffer renderBuffer)
        {
            Stencil = renderBuffer;
            Bind();
            GL.FramebufferRenderbuffer((int)FramebufferTarget.Framebuffer, GL.GL_STENCIL_ATTACHMENT, GL.GL_RENDERBUFFER, renderBuffer.Index);
            return this;
        }

        public GLFrameBuffer Clear()
        {
            Bind();
            GL.Clear((int)ClearBufferMask.ColorBufferBit | (int)ClearBufferMask.DepthBufferBit);
            Unbind();
            return this;
        }

        public bool IsComplete()
        {
            Bind();
            return GL.CheckFramebufferStatus((int)FramebufferTarget.Framebuffer) == (int)FramebufferStatus.Complete;
        }

        public bool Valid() => m_frameBuffer != 0;

        public unsafe void Reset()
        {
            if (m_frameBuffer != 0)
            {
                Unbind();
                fixed (uint* FrameBufferPtr = &m_frameBuffer)
                    GL.DeleteFramebuffers(1, FrameBufferPtr);
                m_frameBuffer = 0;
            }
        }

        public GLFrameBuffer Bind(FramebufferTarget binding = FramebufferTarget.Framebuffer)
        {
            BindImp(binding, m_frameBuffer);
            return this;
        }

        public GLFrameBuffer Unbind()
        {
            UnbindImp(m_frameBuffer);
            return this;
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
                        GL.BindFramebuffer((int)binding, frameBuffer);
                        s_boundRead = frameBuffer;
                    }
                    break;

                case FramebufferTarget.DrawFramebuffer:
                    if (s_boundDraw != frameBuffer)
                    {
                        GL.BindFramebuffer((int)binding, frameBuffer);
                        s_boundDraw = frameBuffer;
                    }
                    break;

                case FramebufferTarget.Framebuffer:
                    if (s_boundRead != frameBuffer || s_boundDraw != frameBuffer)
                    {
                        GL.BindFramebuffer((int)binding, frameBuffer);
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
                GL.BindFramebuffer((int)FramebufferTarget.ReadFramebuffer, 0);
                s_boundRead = 0;
            }

            if (s_boundDraw == frameBuffer)
            {
                GL.BindFramebuffer((int)FramebufferTarget.DrawFramebuffer, 0);
                s_boundDraw = 0;
            }
        }

        private static void CheckErrors()
        {
            var error = GL.GetError();
            if (error != GL.GL_NO_ERROR)
            {
                Console.WriteLine($"[OpenGL GPU] glFramebuffer Error: {error}");
            }
        }
    }
}
