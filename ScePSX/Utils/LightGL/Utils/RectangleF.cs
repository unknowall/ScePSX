namespace LightGL
{
    public class RectangleF
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Left => X;

        public float Right => X + Width;

        public float Top => Y;

        public float Bottom => Y + Height;

        public RectangleF(float X, float Y, float Width, float Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        public float[] GetFloat2TriangleStripCoords()
        {
            return new float[]
            {
                this.Left, this.Top,
                this.Right, this.Top,
                this.Left, this.Bottom,
                this.Right, this.Bottom,
            };
        }

        public static RectangleF FromCoords(float Left, float Top, float Right, float Bottom)
        {
            return new RectangleF(Left, Top, Right - Left, Bottom - Top);
        }

        public RectangleF VFlip()
        {
            return FromCoords(this.Left, this.Bottom, this.Right, this.Top);
        }

        public override string ToString()
        {
            return $"RectangleF({Left}, {Top}, {Width}, {Height})";
        }
    }
}
