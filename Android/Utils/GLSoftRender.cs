using System;
using System.Threading;
using LightGL;

#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS0649

namespace ScePSX;

public class SoftRender : IDisposable
{
    public bool Inited;
    IGlContext Context;
    GLBuffer VertexBuffer;
    GLBuffer TexCoordsBuffer;
    GLShader Shader;
    GLTexture2D? Texture;

    public bool KeepAspect;
    public int Width, Height;
    public int FrameSkip, FSkip;
    private int[] pixels;
    private int oldwidth = 1024;
    private int oldheight = 512;
    private int GLTid;

    private class ShaderInfoClass
    {
        public GlAttribute position;
        public GlAttribute texCoords;
        public GlUniform texture;
    }
    private ShaderInfoClass ShaderInfo;

    public void InitRender(IntPtr WindowHandle)
    {
        if (Inited)
            return;

        Context = GlContextFactory.CreateFromWindowHandle(WindowHandle);

        Shader = new GLShader(
            "attribute vec4 position; attribute vec2 texCoords; varying vec2 v_texCoord; void main() { gl_Position = position; v_texCoord = texCoords; }",
            "precision mediump float; varying vec2 v_texCoord; uniform sampler2D texture; void main() { gl_FragColor = texture2D(texture, v_texCoord); }"
            );

        ShaderInfo = new ShaderInfoClass();
        Shader.BindUniformsAndAttributes(ShaderInfo);

        TexCoordsBuffer = GLBuffer.Create().SetData(GLRectangleF.FromCoords(0, 0, 1, 1).VFlip().GetFloat2TriangleStripCoords());
        VertexBuffer = GLBuffer.Create().SetData(GLRectangleF.FromCoords(-1, -1, +1, +1).GetFloat2TriangleStripCoords());

        ShaderInfo.position.SetData<float>(VertexBuffer, 2);
        ShaderInfo.texCoords.SetData<float>(TexCoordsBuffer, 2);
        Shader.Use(true);

        Context.ReleaseCurrent();
        GLTid = Thread.CurrentThread.ManagedThreadId;
        Inited = true;
    }

    public void Dispose()
    {
        if (!Inited)
            return;

        Context.MakeCurrent();
        Texture?.Dispose();
        Texture = null;
        VertexBuffer.Dispose();
        TexCoordsBuffer.Dispose();
        Shader.Dispose();
        Context.ReleaseCurrent();
        Context.Dispose();

        Inited = false;
        Context = null;
    }

    public unsafe void RenderToWindow(int[] Pixels, int width, int height, ScaleParam scale)
    {
        if (Context == null)
            return;

        if (FSkip > 0)
        {
            FSkip--;
            return;
        }

        if (Thread.CurrentThread.ManagedThreadId != GLTid)
        {
            GLTid = Thread.CurrentThread.ManagedThreadId;
            Context.MakeCurrent();
        }

        if (scale.scale > 0)
        {
            pixels = PixelsScaler.Scale(Pixels, width, height, scale.scale, scale.mode);

            width = width * scale.scale;
            height = height * scale.scale;

        } else
        {
            pixels = Pixels;
        }

        if (oldwidth != width || oldheight != height || Texture == null)
        {
            Texture?.Dispose();
            Texture = GLTexture2D.Create().SetFormat(TextureFormat.RGBA).SetSize(width, height).SetFilter(TextureMinFilter.Linear);

            oldwidth = width;
            oldheight = height;
        }

        fixed (int* pp = pixels)
        {
            GL.TexImage2D(GL.GL_TEXTURE_2D, 0, (int)InternalFormat.Rgba, width, height, 0, (int)PixelFormat.Bgra, GL.GL_UNSIGNED_BYTE, pp);
        }

        int viewportX = 0, viewportY = 0, viewportW = Width, viewportH = Height;
        if (KeepAspect)
        {
            float targetAspect = 4.0f / 3.0f;
            float windowAspect = (float)Width / Height;

            if (windowAspect > targetAspect) // 宽
            {
                viewportW = (int)(Height * targetAspect);
                viewportX = (Width - viewportW) / 2;
            } else // 高
            {
                viewportH = (int)(Width / targetAspect);
                viewportY = (Height - viewportH) / 2;
            }
        }

        GL.Viewport(viewportX, viewportY, viewportW, viewportH);
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(GL.GL_COLOR_BUFFER_BIT);

        GL.DrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);

        Context.SwapBuffers();

        FSkip = FrameSkip;
    }
}
