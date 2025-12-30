using System;
using System.Diagnostics;
using System.Numerics;

namespace LightGL
{
    public abstract class GlUniformAttribute<T>
    {
        protected GLShader Shader;
        protected string Name;
        protected int Location;
        public int ArrayLength
        {
            get;
        }
        protected GLValueType ValueType;

        protected GlUniformAttribute(GLShader shader, string name, int location, int arrayLength, GLValueType valueType)
        {
            Shader = shader;
            Name = name;
            Location = location;
            ArrayLength = arrayLength;
            ValueType = valueType;
        }

        public bool IsAvailable => Location >= 0;

        [DebuggerHidden]
        protected void PrepareUsing()
        {
            //if (!Shader.IsUsing) throw (new Exception("Not using shader"));
            Shader.Use();
        }

        protected bool ShowWarnings = true;

        protected bool CheckValid()
        {
            if (IsValid)
                return true;
            if (!ShowWarnings)
                return false;
            Console.WriteLine("WARNING: Trying to set value to undefined {0}: {1}, {2}", typeof(T).Name, Name, ShowWarnings);
            throw new Exception("INVALID!");
        }

        public bool IsValid => Location != -1;

        public T NoWarning()
        {
            ShowWarnings = false;
            return (T)(object)this;
        }
    }

    public sealed unsafe class GlUniform : GlUniformAttribute<GlUniform>
    {
        public GlUniform(GLShader shader, string name, int location, int arrayLength, GLValueType valueType)
            : base(shader, name, location, arrayLength, valueType)
        {
        }

        [DebuggerHidden]
        public void Set(bool value)
        {
            if (!CheckValid())
                return;
            Set(value ? 1 : 0);
        }

        [DebuggerHidden]
        public void Set(int value)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            GL.Uniform1i(Location, value);
        }

        [DebuggerHidden]
        public void Set(float value)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            GL.Uniform1f(Location, value);
        }

        [DebuggerHidden]
        public void Set(int value1, int value2)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            GL.Uniform2i(Location, value1, value2);
        }

        [DebuggerHidden]
        public void Set(float value1, float value2)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            GL.Uniform2f(Location, value1, value2);
        }

        [DebuggerHidden]
        public void Set(int value1, int value2, int value3, int value4)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            GL.Uniform4i(Location, value1, value2, value3, value4);
        }

        [DebuggerHidden]
        public void Set(float value1, float value2, float value3, float value4)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            GL.Uniform4f(Location, value1, value2, value3, value4);
        }

        [DebuggerHidden]
        public void Set(GLTextureUnit TextureUnit)
        {
            if (!CheckValid())
                return;
            TextureUnit.MakeCurrent();
            if (ValueType != GLValueType.GL_SAMPLER_2D)
                throw new Exception($"Trying to bind a TextureUnit to something not a Sampler2D : {ValueType}");
            Set(TextureUnit.Index);
        }

        [DebuggerHidden]
        public void Set(Vector4 vector)
        {
            if (!CheckValid())
                return;
            Set(new[] { vector });
        }

        [DebuggerHidden]
        public void Set(Vector4[] vectors)
        {
            if (!CheckValid())
                return;
            if (ValueType != GLValueType.GL_FLOAT_VEC4)
                throw new InvalidOperationException("this.ValueType != GLValueType.GL_FLOAT_VEC4");
            if (ArrayLength != vectors.Length)
                throw new InvalidOperationException("this.ArrayLength != Vectors.Length");
            PrepareUsing();
            fixed (Vector4* ptr = &vectors[0])
            {
                GL.Uniform4fv(Location, vectors.Length, (float*)ptr);
            }
        }

        [DebuggerHidden]
        public void Set(Matrix4x4 matrix)
        {
            if (!CheckValid())
                return;
            Set(new[] { matrix });
        }

        [DebuggerHidden]
        unsafe public void Set(Matrix4x4[] matrices)
        {
            if (!CheckValid())
                return;
            if (ValueType != GLValueType.GL_FLOAT_MAT4)
                throw new InvalidOperationException("this.ValueType != GLValueType.GL_FLOAT_MAT4");
            if (ArrayLength != matrices.Length)
                throw new InvalidOperationException("this.ArrayLength != Matrices.Length");
            PrepareUsing();
            fixed (Matrix4x4* ptr = &matrices[0])
            {
                GL.UniformMatrix4fv(Location, matrices.Length, false, (float*)ptr);
            }
        }

        public override string ToString() => $"GLUniform('{Name}'({Location}), {ValueType}[{ArrayLength}])";
    }

    public sealed unsafe class GlAttribute : GlUniformAttribute<GlAttribute>
    {
        public GlAttribute(GLShader shader, string name, int location, int arrayLength, GLValueType valueType)
            : base(shader, name, location, arrayLength, valueType)
        {
        }

        private void Enable() => GL.EnableVertexAttribArray((uint)Location);
        private void Disable() => GL.DisableVertexAttribArray((uint)Location);
        public void UnsetData() => Disable();

        public void SetData<TType>(GLBuffer buffer, int elementSize = 4, int offset = 0, int stride = 0, bool normalize = false)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            int glType;
            var type = typeof(TType);
            if (type == typeof(float))
                glType = GL.GL_FLOAT;
            else if (type == typeof(short))
                glType = GL.GL_SHORT;
            else if (type == typeof(ushort))
                glType = GL.GL_UNSIGNED_SHORT;
            else if (type == typeof(sbyte))
                glType = GL.GL_BYTE;
            else if (type == typeof(byte))
                glType = GL.GL_UNSIGNED_BYTE;
            else
                throw new Exception("Invalid type " + type);

            buffer.Bind();
            GL.VertexAttribPointer(
                (uint)Location,
                elementSize,
                glType,
                normalize,
                stride,
                (void*)offset
            );
            buffer.Unbind();
            Enable();
        }

        public void SetIntData<TType>(GLBuffer buffer, int elementSize = 4, int offset = 0, int stride = 0)
        {
            if (!CheckValid())
                return;
            PrepareUsing();
            int glType;
            var type = typeof(TType);
            if (type == typeof(float))
                glType = GL.GL_FLOAT;
            else if (type == typeof(short))
                glType = GL.GL_SHORT;
            else if (type == typeof(ushort))
                glType = GL.GL_UNSIGNED_SHORT;
            else if (type == typeof(sbyte))
                glType = GL.GL_BYTE;
            else if (type == typeof(byte))
                glType = GL.GL_UNSIGNED_BYTE;
            else
                throw new Exception("Invalid type " + type);

            buffer.Bind();
            GL.VertexAttribIPointer(
                (uint)Location,
                elementSize,
                glType,
                stride,
                (void*)offset
            );
            buffer.Unbind();
            Enable();
        }

        public void SetData(GLMatrix4 ModelViewProjectionMatrix)
        {
            if (this.ValueType != GLValueType.GL_FLOAT_MAT4)
                throw (new InvalidOperationException("this.ValueType != GLValueType.GL_FLOAT_MAT4"));
            if (this.ArrayLength != 1)
                throw (new InvalidOperationException("this.ArrayLength != 1"));
        }

        public override string ToString() => $"GLAttribute('{Name}'({Location}), {ValueType}[{ArrayLength}])";
    }
}
