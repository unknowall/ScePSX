using System;

namespace LightGL
{
    public class GLRenderBuffer : IDisposable
    {
        public readonly int Width, Height;

        public uint Index => _Index;

        private uint _Index;

        public unsafe GLRenderBuffer(int Width, int Height, int Format)
        {
            this.Width = Width;
            this.Height = Height;
            fixed (uint* IndexPtr = &_Index)
            {
                GL.GenRenderbuffers(1, IndexPtr);
                GL.BindRenderbuffer(GL.GL_RENDERBUFFER, _Index);
                GL.RenderbufferStorage(GL.GL_RENDERBUFFER, Format, Width, Height);
            }
        }

        public unsafe void Dispose()
        {
            fixed (uint* IndexPtr = &_Index)
            {
                GL.DeleteRenderbuffers(1, IndexPtr);
            }
        }
    }
}
