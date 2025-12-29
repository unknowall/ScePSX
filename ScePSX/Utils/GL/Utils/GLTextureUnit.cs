namespace ScePSX.GL.Utils
{
    public enum GLWrap
    {
        ClampToEdge = Gl.GL_CLAMP_TO_EDGE,
        MirroredRepeat = Gl.GL_MIRRORED_REPEAT,
        Repeat = Gl.GL_REPEAT
    }

    public enum GLScaleFilter
    {
        Linear = Gl.GL_LINEAR,
        Nearest = Gl.GL_NEAREST
    }

    public class GLTextureUnit
    {
        internal int Index;
        public GLTexture GLTexture { get; private set; }
        internal GLWrap WrapS = GLWrap.ClampToEdge;
        internal GLWrap WrapT = GLWrap.ClampToEdge;
        internal GLScaleFilter Min = GLScaleFilter.Linear;
        internal GLScaleFilter Mag = GLScaleFilter.Linear;

        private GLTextureUnit()
        {
        }

        public GLTextureUnit SetIndex(int Index)
        {
            this.Index = Index;
            return this;
        }

        public static GLTextureUnit Create()
        {
            return new GLTextureUnit();
        }

        public static GLTextureUnit CreateAtIndex(int Index)
        {
            return Create().SetIndex(Index);
        }

        public GLTextureUnit SetTexture(GLTexture GLTexture)
        {
            this.GLTexture = GLTexture;
            return this;
        }

        public GLTextureUnit SetFiltering(GLScaleFilter MinMag)
        {
            return SetFiltering(MinMag, MinMag);
        }

        public GLTextureUnit SetFiltering(GLScaleFilter Min, GLScaleFilter Mag)
        {
            this.Min = Min;
            this.Mag = Mag;
            return this;
        }

        public GLTextureUnit SetWrap(GLWrap WrapST)
        {
            return SetWrap(WrapST, WrapST);
        }

        public GLTextureUnit SetWrap(GLWrap WrapS, GLWrap WrapT)
        {
            this.WrapS = WrapS;
            this.WrapT = WrapT;
            return this;
        }

        public GLTextureUnit MakeCurrent()
        {
            Gl.ActiveTexture(Gl.GL_TEXTURE0 + Index);
            GLTexture?.Bind();
            Gl.TexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, (int)Min);
            Gl.TexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, (int)Mag);
            Gl.TexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, (int)WrapS);
            Gl.TexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, (int)WrapT);
            return this;
        }
    }
}
