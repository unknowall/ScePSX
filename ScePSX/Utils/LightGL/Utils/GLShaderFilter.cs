using System;
using System.Numerics;

namespace LightGL
{
    public class GLShaderFilter : IDisposable
    {
        private GLRenderTarget RenderTarget;
        private GLBuffer PositionBuffer;
        private GLBuffer TexcoordsBuffer;
        private GLShader Shader;

        public int Width => RenderTarget?.Width ?? 0;

        public int Height => RenderTarget?.Height ?? 0;

        public static string DefaultVertexShader = @"
			attribute vec4 a_position;
			attribute vec4 a_texcoords;
			varying vec2 v_texcoords;
			void main() {
				gl_Position = a_position;
				v_texcoords = a_texcoords.xy;
			}
		";

        private GLShaderFilter(int Width, int Height, GLShader Shader)
        {
            this.Shader = Shader;
            SetSize(Width, Height);
            PositionBuffer = GLBuffer.Create().SetData(new[]
            {
                -1f, -1f,
                +1f, -1f,
                -1f, +1f,
                +1f, +1f
            });
            TexcoordsBuffer = GLBuffer.Create().SetData(new[]
            {
                0f, 0f,
                1f, 0f,
                0f, 1f,
                1f, 1f
            });
        }

        public static GLShaderFilter Create(int Width, int Height, GLShader Shader)
        {
            return new GLShaderFilter(Width, Height, Shader);
        }

        public GLTexture2D Process(Action<GLShader> Action)
        {
            RenderTarget.BindUnbind(() =>
            {
                GL.ClearStencil(0);
                GL.ClearDepthf(0f);
                GL.ClearColor(0, 0, 0, 1);
                GL.Clear(GL.GL_COLOR_CLEAR_VALUE | GL.GL_DEPTH_CLEAR_VALUE | GL.GL_STENCIL_CLEAR_VALUE);
                Shader.GetAttribute("a_position").NoWarning().SetData<float>(PositionBuffer, 2);
                Shader.GetAttribute("a_texcoords").NoWarning().SetData<float>(TexcoordsBuffer, 2);
                Shader.GetUniform("u_textureSize").NoWarning().Set(new Vector4(Width, Height, 0, 0));
                Shader.GetUniform("u_pixelSize").NoWarning().Set(new Vector4(1.0f / Width, 1.0f / Height, 0, 0));
                Shader.Draw(GLGeometry.GL_TRIANGLE_STRIP, 4, () => { Action(Shader); });
            });
            return RenderTarget.TextureColor;
        }

        public GLShaderFilter SetSize(int Width, int Height)
        {
            if (Width != this.Width || Height != this.Height)
            {
                //Console.WriteLine("{0}x{1}", Width, Height);
                RenderTarget?.Dispose();
                RenderTarget = GLRenderTarget.Create(Width, Height, TargetLayers.Color);
                //this.RenderTarget = GLRenderTarget.Create(Width, Height);
            }

            return this;
        }

        public void Dispose()
        {
            RenderTarget.Dispose();
            PositionBuffer.Dispose();
            TexcoordsBuffer.Dispose();
        }
    }
}
