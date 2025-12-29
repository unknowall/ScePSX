using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ScePSX.GL.Utils
{
    public enum GLValueType
    {
        GL_BYTE = 0x1400,
        GL_UNSIGNED_BYTE = 0x1401,
        GL_SHORT = 0x1402,
        GL_UNSIGNED_SHORT = 0x1403,
        GL_INT = 0x1404,
        GL_UNSIGNED_INT = 0x1405,
        GL_FLOAT = 0x1406,
        GL_FIXED = 0x140C,

        GL_FLOAT_VEC2 = 0x8B50,
        GL_FLOAT_VEC3 = 0x8B51,
        GL_FLOAT_VEC4 = 0x8B52,
        GL_INT_VEC2 = 0x8B53,
        GL_INT_VEC3 = 0x8B54,
        GL_INT_VEC4 = 0x8B55,
        GL_BOOL = 0x8B56,
        GL_BOOL_VEC2 = 0x8B57,
        GL_BOOL_VEC3 = 0x8B58,
        GL_BOOL_VEC4 = 0x8B59,
        GL_FLOAT_MAT2 = 0x8B5A,
        GL_FLOAT_MAT3 = 0x8B5B,
        GL_FLOAT_MAT4 = 0x8B5C,
        GL_SAMPLER_2D = 0x8B5E,
        GL_SAMPLER_CUBE = 0x8B60
    }

    public enum GLGeometry
    {
        GL_POINTS = 0x0000,
        GL_LINES = 0x0001,
        GL_LINE_LOOP = 0x0002,
        GL_LINE_STRIP = 0x0003,
        GL_TRIANGLES = 0x0004,
        GL_TRIANGLE_STRIP = 0x0005,
        GL_TRIANGLE_FAN = 0x0006,
        GL_QUADS = 0x0007
    }

    public unsafe class GLShader : IDisposable
    {
        uint Program;
        uint VertexShader;
        uint FragmentShader;

        [DebuggerHidden]
        public GLShader(string VertexShaderSource, string FragmentShaderSource)
        {
            Initialize();

            int VertexShaderCompileStatus;
            ShaderSource(VertexShader, VertexShaderSource);
            Gl.CompileShader(VertexShader);
            Gl.CheckError();
            Gl.GetShaderiv(VertexShader, Gl.GL_COMPILE_STATUS, &VertexShaderCompileStatus);
            var VertexShaderInfo = GetShaderInfoLog(VertexShader);

            int FragmentShaderCompileStatus;
            ShaderSource(FragmentShader, FragmentShaderSource);
            Gl.CompileShader(FragmentShader);
            Gl.CheckError();
            Gl.GetShaderiv(FragmentShader, Gl.GL_COMPILE_STATUS, &FragmentShaderCompileStatus);
            var FragmentShaderInfo = GetShaderInfoLog(FragmentShader);

            if (!string.IsNullOrEmpty(VertexShaderInfo))
                Console.WriteLine("{0}", VertexShaderInfo);
            if (!string.IsNullOrEmpty(FragmentShaderInfo))
                Console.WriteLine("{0}", FragmentShaderInfo);

            if (VertexShaderCompileStatus == 0 || FragmentShaderCompileStatus == 0)
            {
                Console.WriteLine("Shader ERROR: {0}, {1}", VertexShaderInfo, FragmentShaderInfo);
            }
            else
            {
                Console.WriteLine("OpenGL Shader Compiled.");
            }

            //Console.Out.WriteLineColored(
            //    ConsoleColor.Blue,
            //    "Compiled Shader! : {0}, {1}",
            //    VertexShaderSource, FragmentShaderSource
            //);

            Gl.AttachShader(Program, VertexShader);
            Gl.AttachShader(Program, FragmentShader);
            Gl.DeleteShader(VertexShader);
            Gl.DeleteShader(FragmentShader);

            Link();
        }

        public GlAttribute GetAttribute(string Name)
        {
            return _Attributes.TryGetValue(Name, out var attr) ? attr : new GlAttribute(this, Name, -1, 0, GLValueType.GL_BYTE);
        }

        public GlUniform GetUniform(string Name)
        {
            if (_Uniforms.ContainsKey(Name + "[0]")) Name = Name + "[0]";
            return _Uniforms.TryGetValue(Name, out var uniform) ? uniform : new GlUniform(this, Name, -1, 0, GLValueType.GL_BYTE);
        }

        private void Link()
        {
            int LinkStatus;
            Gl.LinkProgram(Program);
            Gl.GetProgramiv(Program, Gl.GL_LINK_STATUS, &LinkStatus);
            var ProgramInfo = GetProgramInfoLog(Program);

            if (LinkStatus == 0)
            {
                Console.WriteLine("Shader ERROR (II): {0}", ProgramInfo);
                //throw (new Exception(String.Format("Shader ERROR: {0}", ProgramInfo)));
            }

            GetAllUniforms();
            GetAllAttributes();
        }

        private readonly Dictionary<string, GlUniform> _Uniforms = new Dictionary<string, GlUniform>();
        private readonly Dictionary<string, GlAttribute> _Attributes = new Dictionary<string, GlAttribute>();

        public IEnumerable<GlUniform> Uniforms => _Uniforms.Values;

        public IEnumerable<GlAttribute> Attributes => _Attributes.Values;

        private void GetAllUniforms()
        {
            const int NameMaxSize = 1024;
            var NameTemp = stackalloc byte[NameMaxSize];
            int Total = -1;
            Gl.GetProgramiv(Program, Gl.GL_ACTIVE_UNIFORMS, &Total);
            for (uint n = 0; n < Total; n++)
            {
                int name_len = -1, num = -1;
                int type = Gl.GL_ZERO;
                Gl.GetActiveUniform(Program, n, NameMaxSize - 1, &name_len, &num, &type, NameTemp);
                NameTemp[name_len] = 0;
                var Name = Marshal.PtrToStringAnsi(new IntPtr(NameTemp));
                int location = Gl.GetUniformLocation(Program, Name);
                _Uniforms[Name] = new GlUniform(this, Name, location, num, (GLValueType)type);
                //Console.WriteLine(Uniforms[Name]);
            }
        }

        private void GetAllAttributes()
        {
            const int NameMaxSize = 1024;
            var NameTemp = stackalloc byte[NameMaxSize];
            int Total = -1;
            Gl.GetProgramiv(Program, Gl.GL_ACTIVE_ATTRIBUTES, &Total);
            for (uint n = 0; n < Total; n++)
            {
                int name_len = -1, num = -1;
                int type = Gl.GL_ZERO;
                Gl.GetActiveAttrib(Program, n, NameMaxSize - 1, &name_len, &num, &type, NameTemp);
                NameTemp[name_len] = 0;
                var Name = Marshal.PtrToStringAnsi(new IntPtr(NameTemp));
                int location = Gl.GetAttribLocation(Program, Name);
                _Attributes[Name] = new GlAttribute(this, Name, location, num, (GLValueType)type);
                //Console.WriteLine(Attributes[Name]);
            }
        }

        public bool IsUsing
        {
            get
            {
                int CurrentProgram;
                Gl.GetIntegerv(Gl.GL_CURRENT_PROGRAM, &CurrentProgram);
                return CurrentProgram == Program;
            }
        }

        public void Use()
        {
            Gl.UseProgram(Program);
        }

        private void Initialize()
        {
            Gl.ClearError();
            Program = Gl.CreateProgram();
            Gl.CheckError();
            VertexShader = Gl.CreateShader(Gl.GL_VERTEX_SHADER);
            Gl.CheckError();
            FragmentShader = Gl.CreateShader(Gl.GL_FRAGMENT_SHADER);
            Gl.CheckError();
        }

        private static void ShaderSource(uint Shader, string Source)
        {
            var SourceBytes = new UTF8Encoding(false, true).GetBytes(Source);
            var SourceLength = SourceBytes.Length;
            fixed (byte* _SourceBytesPtr = SourceBytes)
            {
                byte* SourceBytesPtr = _SourceBytesPtr;
                Gl.ShaderSource(Shader, 1, &SourceBytesPtr, &SourceLength);
            }
        }

        private static string GetShaderInfoLog(uint Shader)
        {
            int Length;
            var Data = new byte[1024];
            fixed (byte* DataPtr = Data)
            {
                Gl.GetShaderInfoLog(Shader, Data.Length, &Length, DataPtr);
                return Marshal.PtrToStringAnsi(new IntPtr(DataPtr), Length);
            }
        }

        private static string GetProgramInfoLog(uint Program)
        {
            int Length;
            var Data = new byte[1024];
            fixed (byte* DataPtr = Data)
            {
                Gl.GetProgramInfoLog(Program, Data.Length, &Length, DataPtr);
                return Marshal.PtrToStringAnsi(new IntPtr(DataPtr), Length);
            }
        }

        public void Dispose()
        {
            Gl.DeleteProgram(Program);
            Program = 0;
            VertexShader = 0;
            FragmentShader = 0;
        }

        public void Draw(GLGeometry Geometry, int Count, Action SetDataCallback, int Offset = 0)
        {
            Use();
            SetDataCallback();
            //Console.Out.WriteLineColored(ConsoleColor.Green, $"glDrawArrays Vertices: ({Count})");
            Gl.DrawArrays((int)Geometry, Offset, Count);
        }

        public void Draw(GLGeometry Geometry, uint[] Indices, int Count, Action SetDataCallback, int IndicesOffset = 0)
        {
            Use();
            SetDataCallback();
            fixed (uint* IndicesPtr = &Indices[IndicesOffset])
            {
                //Console.Out.WriteLineColored(ConsoleColor.Green, $"glDrawElements Vertices: ({Count})");
                Gl.DrawElements((int)Geometry, Count, Gl.GL_UNSIGNED_INT, IndicesPtr);
            }
        }

        public void BindUniformsAndAttributes(object Object)
        {
            foreach (var Field in Object.GetType().GetFields())
            {
                if (Field.FieldType == typeof(GlAttribute))
                {
                    Field.SetValue(Object, GetAttribute(Field.Name));
                }
                else if (Field.FieldType == typeof(GlUniform))
                {
                    Field.SetValue(Object, GetUniform(Field.Name));
                }
            }
        }
    }
}
