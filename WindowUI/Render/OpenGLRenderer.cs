using LightGL;
using LightGL.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScePSX.Render
{
    class OpenGLRenderer : GLControl, IRenderer
    {
        private int[] Pixels = new int[1024 * 512];
        public int iWidth = 1024;
        public int iHeight = 512;
        private ScaleParam scale;

        public string ShadreName = "";

        public RenderMode Mode => RenderMode.OpenGL;

        public OpenGLRenderer()
        {
            Load += OpenGLRenderer_Load;
            RenderFrame += OpenGLRenderer_Render;
            Resize += OpenGLRenderer_Resize;
        }

        private bool CheckReShadeInjection()
        {
            var modules = Process.GetCurrentProcess().Modules;
            return modules.Cast<ProcessModule>().Any(m => m.ModuleName.Contains("ReShade"));
        }

        public void Initialize(Control parent)
        {
            parent.SuspendLayout();
            Dock = DockStyle.Fill;
            Enabled = false;
            parent.Controls.Add(this);
            parent.ResumeLayout();
        }

        public void SetParam(int Param)
        {

        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        GLShader Shader;
        GLBuffer VertexBuffer;
        GLTexture2D PixelsTexture;
        GLTextureUnit TextureUnit;
        public class ShaderInfoClass
        {
            public GlAttribute vertexPosition = null;
            public GlAttribute vertexTexCoord = null;
            public GlUniform textureSampler = null;
        }
        ShaderInfoClass ShaderInfo = new ShaderInfoClass();

        float[] vertices = {
                -1.0f,  1.0f, 0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f, 0.0f,
                 1.0f, -1.0f, 1.0f, 0.0f,
                 1.0f,  1.0f, 1.0f, 1.0f
            };

        private unsafe void OpenGLRenderer_Load(object sender, EventArgs e)
        {
            OpenGLRenderer_Resize(sender, e);

            GL.ClearColor(Color.Gray.R / 255.0f, Color.Gray.G / 255.0f, Color.Gray.B / 255.0f, 0);

            if (DesignMode)
                return;

            VertexBuffer = GLBuffer.Create().SetData(vertices);

            PixelsTexture = GLTexture2D.Create().SetFormat(TextureFormat.BGRA);

            TextureUnit = GLTextureUnit.CreateAtIndex(0).SetFiltering(GLScaleFilter.Linear).SetWrap(GLWrap.ClampToEdge).SetTexture(PixelsTexture);

            TextureUnit.MakeCurrent();
        }

        private void OpenGLRenderer_Resize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, this.ClientSize.Width, this.ClientSize.Height);
        }

        private unsafe void OpenGLRenderer_Render()
        {
            if (this.Visible == false || DesignMode)
                return;

            if (scale.scale > 0)
            {
                Pixels = PixelsScaler.Scale(Pixels, iWidth, iHeight, scale.scale, scale.mode);

                iWidth = iWidth * scale.scale;
                iHeight = iHeight * scale.scale;
            }

            Shader.Draw(GLGeometry.GL_QUADS, 4, () =>
            {
                PixelsTexture.SetData<int>(Pixels, PixelFormat.Bgra).SetSize(iWidth, iHeight);
            });
        }

        public void RenderBuffer(int[] pixels, int width, int height, ScaleParam scale)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RenderBuffer(pixels, width, height, scale)));
                return;
            }

            Pixels = pixels;
            iWidth = width;
            iHeight = height;
            this.scale = scale;

            Invalidate();
        }

        public unsafe void LoadShaders(string ShaderDir)
        {
            DirectoryInfo dir = new DirectoryInfo(ShaderDir);

            if (!dir.Exists)
            {
                Console.WriteLine($"[OPENGL] Shader directory not found: {ShaderDir}");
                return;
            }

            string vertexShaderSource = "";
            string fragmentShaderSource = "";

            foreach (FileInfo f in dir.GetFiles("*.VS", SearchOption.TopDirectoryOnly))
            {
                vertexShaderSource = File.ReadAllText(f.FullName);
                break;
            }
            foreach (FileInfo f in dir.GetFiles("*.FS", SearchOption.TopDirectoryOnly))
            {
                fragmentShaderSource = File.ReadAllText(f.FullName);
                ShadreName = Path.GetFileNameWithoutExtension(f.FullName);
                break;
            }
            if (vertexShaderSource == "" || fragmentShaderSource == "")
            {
                Console.WriteLine("[OPENGL] Missing shader files in directory");
                return;
            }

            Shader = new GLShader(vertexShaderSource, fragmentShaderSource);

            Shader.BindUniformsAndAttributes(ShaderInfo);

            ShaderInfo.textureSampler.Set(TextureUnit);
            ShaderInfo.vertexPosition.SetData<float>(VertexBuffer, 2, 0, 4 * sizeof(float));
            ShaderInfo.vertexTexCoord.SetData<float>(VertexBuffer, 2, 8, 4 * sizeof(float));

            Console.WriteLine($"[OPENGL] {ShadreName} Shader");
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.Name = "OpenGLRenderer";
            this.ResumeLayout(false);

        }

        protected unsafe override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shader.Dispose();
                VertexBuffer.Dispose();
                PixelsTexture.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                return (createParams);
            }
        }
    }
}
