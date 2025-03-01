using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenGL;

namespace ScePSX
{
    class OpenGLRenderer : GlControl
    {
        private int[] Pixels = new int[4096 * 2048];
        private uint _textureId;
        public int iWidth = 1024;
        public int iHeight = 512;
        public int scale;

        public string ShadreName = "";

        private uint programID;
        private uint vertexShaderID;
        private uint fragmentShaderID;
        private uint vboID;

        public OpenGLRenderer()
        {
            Load += OpenGLRenderer_Load;
            Render += OpenGLRenderer_Render;
            Resize += OpenGLRenderer_Resize;
            programID = 0;

            this.MultisampleBits = 4;
            //this.StencilBits = 8;
            //this.DepthBits = 24;
            //this.ColorBits = 32;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        private void OpenGLRenderer_Load(object sender, EventArgs e)
        {
            //Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Multisample);
            Gl.Enable(EnableCap.Texture2d);
            Gl.ClearColor(Color.Gray.R / 255.0f, Color.Gray.G / 255.0f, Color.Gray.B / 255.0f, 0);

            CreateVBO();

            _textureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _textureId);

            Gl.TextureParameterEXT(_textureId, TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TextureParameterEXT(_textureId, TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            OpenGLRenderer_Resize(sender, e);
        }

        private void OpenGLRenderer_Resize(object sender, EventArgs e)
        {
            Gl.MatrixMode(MatrixMode.Projection);
            Gl.LoadIdentity();
            Gl.Viewport(0, 0, Width, Height);
            Gl.Ortho(0d, 1d, 0d, 1d, -1d, 1d);
            Gl.MatrixMode(MatrixMode.Modelview);
            Gl.LoadIdentity();
        }

        private void OpenGLRenderer_Render(object sender, GlControlEventArgs e)
        {
            if (this.Visible == false || DesignMode)
                return;

            if (scale > 0)
            {
                Pixels = XbrScaler.ScaleXBR(Pixels, iWidth, iHeight, scale);

                iWidth = iWidth * scale;
                iHeight = iHeight * scale;
            }

            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(programID);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, _textureId);

            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, iWidth, iHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, Pixels);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            Gl.DrawArrays(PrimitiveType.Quads, 0, 4);
        }

        public void RenderBuffer(int[] pixels, int width, int height, int scale = 0)
        {
            Pixels = pixels;
            iWidth = width;
            iHeight = height;
            this.scale = scale;

            Invalidate();
        }

        public void LoadShaders(string ShaderDir)
        {
            DirectoryInfo dir = new DirectoryInfo(ShaderDir);

            if (!dir.Exists)
            {
                Console.WriteLine($"[OPENGL] Shader directory not found: {ShaderDir}");
                return;
            }

            string[] vertexShaderSource = null;
            string[] fragmentShaderSource = null;

            foreach (FileInfo f in dir.GetFiles("*.VS", SearchOption.TopDirectoryOnly))
            {
                vertexShaderSource = File.ReadAllLines(f.FullName);
                //Console.WriteLine($"vertexShaderSource load: {f.FullName}\n");
                break;
            }

            foreach (FileInfo f in dir.GetFiles("*.FS", SearchOption.TopDirectoryOnly))
            {
                fragmentShaderSource = File.ReadAllLines(f.FullName);
                ShadreName = Path.GetFileNameWithoutExtension(f.FullName);
                //Console.WriteLine($"fragmentShaderSource load: {f.FullName}\n");
                break;
            }

            if (vertexShaderSource == null || fragmentShaderSource == null)
            {
                Console.WriteLine("[OPENGL] Missing shader files in directory");
                return;
            }

            // 创建着色器程序
            programID = Gl.CreateProgram();

            // 编译顶点着色器
            vertexShaderID = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShaderID, vertexShaderSource);
            Gl.CompileShader(vertexShaderID);
            CheckShaderCompileStatus(vertexShaderID, "Vertex Shader");

            // 编译片段着色器
            fragmentShaderID = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShaderID, fragmentShaderSource);
            Gl.CompileShader(fragmentShaderID);
            CheckShaderCompileStatus(fragmentShaderID, "Fragment Shader");

            // 附加着色器并链接程序
            Gl.AttachShader(programID, vertexShaderID);
            Gl.AttachShader(programID, fragmentShaderID);
            Gl.LinkProgram(programID);
            CheckProgramLinkStatus();

            // 清理着色器对象
            Gl.DetachShader(programID, vertexShaderID);
            Gl.DetachShader(programID, fragmentShaderID);
            Gl.DeleteShader(vertexShaderID);
            Gl.DeleteShader(fragmentShaderID);

            // 绑定Uniform和属性
            Gl.UseProgram(programID);
            int samplerLoc = Gl.GetUniformLocation(programID, "textureSampler");
            Gl.Uniform1(samplerLoc, 0); // 对应TextureUnit.Texture0

            // 设置顶点属性
            int vertexPositionLocation = Gl.GetAttribLocation(programID, "vertexPosition");
            Gl.EnableVertexAttribArray((uint)vertexPositionLocation);
            Gl.VertexAttribPointer((uint)vertexPositionLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), IntPtr.Zero);

            int vertexTexCoordLocation = Gl.GetAttribLocation(programID, "vertexTexCoord");
            Gl.EnableVertexAttribArray((uint)vertexTexCoordLocation);
            Gl.VertexAttribPointer((uint)vertexTexCoordLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));

            Console.WriteLine($"[OPENGL] {ShadreName} Shader, {this.MultisampleBits}xMSAA");
        }

        private void CheckShaderCompileStatus(uint shaderId, string shaderName)
        {
            Gl.GetShader(shaderId, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                Gl.GetShader(shaderId, ShaderParameterName.InfoLogLength, out int logLength);
                if (logLength <= 0)
                    return;

                StringBuilder infoLog = new StringBuilder(logLength);
                Gl.GetShaderInfoLog(shaderId, logLength, out int actualLength, infoLog);

                string log = infoLog.ToString();
                Console.WriteLine($"[OPENGL] {shaderName} compile error:\n{log}");
            }
        }

        private void CheckProgramLinkStatus()
        {
            Gl.GetProgram(programID, ProgramProperty.LinkStatus, out int status);
            if (status == 0)
            {
                Gl.GetProgram(programID, ProgramProperty.InfoLogLength, out int logLength);
                if (logLength <= 0)
                    return;

                StringBuilder infoLog = new StringBuilder(logLength);
                Gl.GetProgramInfoLog(programID, logLength, out int actualLength, infoLog);

                string log = infoLog.ToString();
                Console.WriteLine($"[OPENGL] Program link error:\n{log}");
            }
        }

        private void CreateVBO()
        {
            float[] vertices = {
                -1.0f,  1.0f, 0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f, 0.0f,
                 1.0f, -1.0f, 1.0f, 0.0f,
                 1.0f,  1.0f, 1.0f, 1.0f
            };

            vboID = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length), vertices, BufferUsage.StaticDraw);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.Name = "OpenGLRenderer";
            this.ResumeLayout(false);

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
