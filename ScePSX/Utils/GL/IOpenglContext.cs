using System;

namespace ScePSX.GL
{
    public struct GlContextSize
    {
        public int Width;
        public int Height;

        public override string ToString() => $"GLContextSize({Width}x{Height})";
    }

    public interface IGlContext : IDisposable
    {
        GlContextSize Size { get; }

        IGlContext MakeCurrent();
        IGlContext ReleaseCurrent();
        IGlContext SwapBuffers();
    }
}
