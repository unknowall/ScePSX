using System;

namespace LightGL
{
    public class GLVertexArrays : IDisposable
    {
        private uint m_vao = 0;
        private static uint s_bound = 0;

        public GLVertexArrays()
        {
        }

        private GLVertexArrays(uint vao)
        {
            m_vao = vao;
        }

        public unsafe static GLVertexArrays Create()
        {
            uint vao = 0;
            GL.GenVertexArrays(1, &vao);
            return new GLVertexArrays(vao);
        }

        public bool Valid()
        {
            return m_vao != 0;
        }

        public unsafe void Reset()
        {
            if (m_vao != 0)
            {
                Unbind();
                fixed (uint* ptr = &m_vao)
                    GL.DeleteVertexArrays(1, ptr);
                m_vao = 0;
            }
        }

        public unsafe void AddFloatAttribute(uint location, int size, VertexAttribPointerType type, bool normalized, int stride = 0, int offset = 0)
        {
            Bind();
            GL.VertexAttribPointer(location, size, (int)type, normalized, stride, (void*)offset);
            GL.EnableVertexAttribArray(location);
        }

        public unsafe void AddIntAttribute(uint location, int size, VertexAttribIType type, int stride = 0, int offset = 0)
        {
            Bind();
            GL.VertexAttribIPointer(location, size, (int)type, stride, (void*)offset);
            GL.EnableVertexAttribArray(location);
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
        }

        private static void Bind(uint vao)
        {
            GL.BindVertexArray(vao);
            s_bound = vao;
        }
    }
}
