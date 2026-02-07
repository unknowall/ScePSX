using System;
using System.Reflection;
using System.Runtime.InteropServices;
using LightGL;

namespace ScePSX.UI;

public class SoftRender : IDisposable
{
    bool Inited;
    IGlContext Context;
    GLRectangleF TextureRect;
    GLBuffer VertexBuffer;
    GLBuffer TexCoordsBuffer;
    GLShader Shader;
    GLTexture2D Texture;

    public int Width, Height;
    public int FrameSkip, FSkip;
    //public bool Vflip = false;
    private int[] pixels;
    private int oldwidth = 1024;
    private int oldheight = 512;
    private ScaleParam scaleParam;

    public class ShaderInfoClass
    {
        public GlAttribute position;
        public GlAttribute texCoords;
        public GlUniform texture;
    }
    static ShaderInfoClass ShaderInfo = new ShaderInfoClass();

    public void InitRender(IntPtr WindowHandle)
    {
        if (Inited) return;

        Context = GlContextFactory.CreateFromWindowHandle(WindowHandle);
        Context.MakeCurrent();

        Shader = new GLShader(
            "attribute vec4 position; attribute vec4 texCoords; varying vec2 v_texCoord; void main() { gl_Position = position; v_texCoord = texCoords.xy; }",
            "uniform sampler2D texture; varying vec2 v_texCoord; void main() { gl_FragColor = texture2D(texture, v_texCoord); }"
            );

        Shader.BindUniformsAndAttributes(ShaderInfo);

        TextureRect = GLRectangleF.FromCoords(0, 0, 1, 1);
        TexCoordsBuffer = GLBuffer.Create().SetData(TextureRect.GetFloat2TriangleStripCoords());
        VertexBuffer = GLBuffer.Create().SetData(GLRectangleF.FromCoords(-1, -1, +1, +1).GetFloat2TriangleStripCoords());

        ShaderInfo.position.SetData<float>(VertexBuffer, 2);
        ShaderInfo.texCoords.SetData<float>(TexCoordsBuffer, 2);
        ShaderInfo.texture.Set(0);
        Shader.Use();

        TexCoordsBuffer.SetData(TextureRect.VFlip().GetFloat2TriangleStripCoords());

        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
        Context.ReleaseCurrent();
        Inited = false;
    }

    public void Dispose()
    {
        if (!Inited) return;

        Texture?.Dispose();
        VertexBuffer.Dispose();
        TexCoordsBuffer.Dispose();
        Shader.Dispose();
        Context.ReleaseCurrent();
        Context.Dispose();
    }

    public unsafe void RenderToWindow(int[] Pixels, int width, int height, ScaleParam scale)
    {
        if (Context == null) return;

        if (FSkip > 0)
        {
            FSkip--;
            return;
        }

        Context.MakeCurrent();

        if (scale.scale > 0)
        {
            pixels = PixelsScaler.Scale(Pixels, width, height, scale.scale, scale.mode);

            width = width * scale.scale;
            height = height * scale.scale;

        } else
        {
            pixels = Pixels;
        }

        if (scaleParam.scale != scale.scale || oldwidth != width || oldheight != height || Texture == null)
        {
            Texture?.Dispose();
            Texture = GLTexture2D.Create().SetFormat(TextureFormat.RGBA).SetSize(width, height).SetFilter(TextureMinFilter.Linear);

            scaleParam = scale;
            oldwidth = width;
            oldheight = height;
        }

        fixed (int* pp = pixels)
        {
            GL.TexImage2D(GL.GL_TEXTURE_2D, 0, (int)InternalFormat.Rgba, width, height, 0, (int)PixelFormat.Bgra, GL.GL_UNSIGNED_BYTE, pp);
        }

        //if (Vflip) TexCoordsBuffer.SetData(TextureRect.VFlip().GetFloat2TriangleStripCoords());

        GL.Viewport(0, 0, Width, Height);
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(GL.GL_COLOR_BUFFER_BIT);

        GL.DrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);

        Context.SwapBuffers();

        FSkip = FrameSkip;
    }
}
