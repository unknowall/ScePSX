using System;
using System.Runtime.InteropServices;

namespace LightGL
{
    public unsafe class GLBuffer : IDisposable
    {
        uint Buffer;
        public int BufferType = GL.GL_ARRAY_BUFFER;
        public BufferUsage BufferUsage;
        public BufferTarget target = BufferTarget.ArrayBuffer;

        private GLBuffer(BufferUsage BufferUsage = BufferUsage.StaticDraw, int BufferType = GL.GL_ARRAY_BUFFER)
        {
            this.BufferUsage = BufferUsage;
            Initialize();
        }

        public static GLBuffer Create<T>(BufferTarget target, BufferUsage usage, int size, T[] data = null) where T : unmanaged
        {
            GLBuffer buffer = new GLBuffer(usage);
            buffer.target = target;
            buffer.SetData(usage, size, data);
            return buffer;
        }

        public static GLBuffer Create(BufferUsage BufferUsage = BufferUsage.StaticDraw, int BufferType = GL.GL_ARRAY_BUFFER)
        {
            return new GLBuffer(BufferUsage);
        }

        private void Initialize()
        {
            fixed (uint* BufferPtr = &Buffer)
            {
                GL.GenBuffers(1, BufferPtr);
            }
        }

        public unsafe void SetData<T>(BufferUsage usage, int size, T[] data = null) where T : unmanaged
        {
            Bind();
            if (data != null)
            {
                fixed (void* ptr = data)
                {
                    GL.BufferData((int)target, (uint)(size * sizeof(T)), ptr, (int)usage);
                }
            } else
            {
                GL.BufferData((int)target, (uint)(size * sizeof(T)), null, (int)usage);
            }
        }

        public GLBuffer SetData<T>(T[] Data, int Offset = 0, int Length = -1)
        {
            if (Length < 0)
                Length = Data.Length;
            var Handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            try
            {
                return SetData(
                    Length * Marshal.SizeOf(typeof(T)),
                    (byte*)Handle.AddrOfPinnedObject().ToPointer() + Offset * Marshal.SizeOf(typeof(T))
                );
            } finally
            {
                Handle.Free();
            }
        }

        public GLBuffer SetData(int Size, void* Data)
        {
            Bind();
            GL.BufferData((int)target, (uint)Size, Data, (int)this.BufferUsage);
            return this;
        }

        public unsafe void SubData<T>(int size, T[] data, int offset = 0) where T : unmanaged
        {
            Bind();
            fixed (void* ptr = data)
            {
                GL.BufferSubData((int)target, offset, (uint)(size * sizeof(T)), ptr);
            }
        }

        public void Bind()
        {
            GL.BindBuffer((int)target, Buffer);
        }

        public void Unbind()
        {
            GL.BindBuffer((int)target, 0);
        }

        public void Dispose()
        {
            fixed (uint* BufferPtr = &Buffer)
            {
                GL.DeleteBuffers(1, BufferPtr);
            }
        }
    }
}
