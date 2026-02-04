using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ScePSX.Render
{
    public enum RenderMode
    {
        Directx2D,
        Directx3D,
        OpenGL,
        Vulkan,
        Null
    }

    public interface IRenderer : IDisposable
    {
        RenderMode Mode
        {
            get;
        }

        void RenderBuffer(int[] pixels, int width, int height, ScaleParam scale);

        void Initialize(Control parent);

        void SetParam(int Param);
    }

    public class RendererManager : IDisposable
    {
        public IRenderer _currentRenderer;
        private readonly Dictionary<RenderMode, Func<IRenderer>> _rendererFactories;

        public int oglMSAA;
        public int frameskip;
        public string oglShaderPath;

        public RendererManager()
        {
            _rendererFactories = new Dictionary<RenderMode, Func<IRenderer>>
        {
            { RenderMode.OpenGL, () => new OpenGLRenderer() },
            { RenderMode.Directx3D, () => new SDL2Renderer() },
            { RenderMode.Directx2D, () => new D2DRenderer() },
            { RenderMode.Vulkan, () => new VulkanRenderer() },
            { RenderMode.Null, () => new NullRenderer() }
        };
        }

        public void SelectRenderer(RenderMode mode, Control parent, int Param = 0)
        {
            if (_currentRenderer?.Mode == mode)
                return;

            DisposeCurrentRenderer();

            if (_rendererFactories.TryGetValue(mode, out var factory))
            {
                _currentRenderer = factory();

                _currentRenderer.Initialize(parent);

                if (_currentRenderer is OpenGLRenderer glRenderer)
                {
                    if (glRenderer.ShadreName == "" && oglShaderPath != "")
                        glRenderer.LoadShaders(oglShaderPath);

                    //glRenderer.MultisampleBits = (uint)oglMSAA;
                }

                if (_currentRenderer is NullRenderer nullRenderer)
                {
                    nullRenderer.SetParam(oglMSAA);
                }
            }
        }

        public void DisposeCurrentRenderer()
        {
            if (_currentRenderer == null)
                return;

            _currentRenderer.Dispose();

            _currentRenderer = null;
        }

        public void RenderBuffer(int[] pixels, int width, int height, ScaleParam scale)
        {
            _currentRenderer?.RenderBuffer(pixels, width, height, scale);
        }

        public void Dispose() => DisposeCurrentRenderer();
    }
}
