using System;
using System.Runtime.InteropServices;

namespace LightGL
{
    public unsafe class GLBuffer : IDisposable
    {
        uint Buffer;
        public BufferUsage BufferUsage;
        public BufferTarget target = BufferTarget.ArrayBuffer;

        private GLBuffer(BufferTarget target = BufferTarget.ArrayBuffer, BufferUsage BufferUsage = BufferUsage.StaticDraw)
        {
            this.target = target;
            this.BufferUsage = BufferUsage;
            Initialize();
        }

        public static GLBuffer Create<T>(BufferTarget target, BufferUsage usage, int size, T[] data = null) where T : unmanaged
        {
            GLBuffer buffer = new GLBuffer(target, usage);
            buffer.target = target;
            buffer.SetData(usage, size, data);
            return buffer;
        }

        public static GLBuffer Create(BufferTarget target = BufferTarget.ArrayBuffer, BufferUsage BufferUsage = BufferUsage.StaticDraw)
        {
            return new GLBuffer(target, BufferUsage);
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

        public GLBuffer SetStructData<T>(T Data)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr Ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(Data, Ptr, false);
                return SetData(size, Ptr.ToPointer());
            } finally
            {
                Marshal.FreeHGlobal(Ptr);
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

        public void Bind(uint Slot = 0)
        {
            GL.BindBuffer((int)target, Buffer);

            if (target == BufferTarget.UniformBuffer)
                GL.BindBufferBase((int)target, Slot, Buffer);
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
