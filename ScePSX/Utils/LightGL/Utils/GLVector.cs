namespace LightGL
{
    unsafe public struct GLVector4
    {
        public float X, Y, Z, W;

        public GLVector4(float X, float Y, float Z, float W)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        public float this[int Index]
        {
            get
            {
                fixed (float* vPtr = &X)
                    return vPtr[Index];
            }
            set
            {
                fixed (float* vPtr = &X)
                    vPtr[Index] = value;
            }
        }

        public void Set(float X, float Y, float Z, float W)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ", " + W + ")";
        }
    }

    unsafe public struct GLVector3
    {
        public float X, Y, Z;

        public GLVector3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public float this[int Index]
        {
            get
            {
                fixed (float* vPtr = &X)
                    return vPtr[Index];
            }
            set
            {
                fixed (float* vPtr = &X)
                    vPtr[Index] = value;
            }
        }

        public void Set(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        static public GLVector3 operator *(GLVector3 Vector, float Scale)
        {
            return new GLVector3(Vector.X * Scale, Vector.Y * Scale, Vector.Z * Scale);
        }
    }
}
