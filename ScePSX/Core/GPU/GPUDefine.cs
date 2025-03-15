using System;
using System.Runtime.InteropServices;

namespace ScePSX
{

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Point2D
    {
        [FieldOffset(0)]
        public short X;

        [FieldOffset(2)]
        public short Y;
    }

    [Serializable]
    public struct Primitive
    {
        public bool IsShaded;
        public bool IsTextured;
        public bool IsSemiTransparent;
        public bool IsRawTextured;
        public byte TextureDepth;
        public byte SemiTransparencyMode;
        public ushort clut;
        public Point2D Clut;
        public ushort texturebase;
        public Point2D TextureBase;
        public short texwidth;
        public short texheight;
        public ushort texpage;
        public bool isDithered;
        public uint rawtexcoord;
        public TDrawMode drawMode;
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct TextureData
    {
        [FieldOffset(0)]
        public ushort Value;

        [FieldOffset(0)]
        public byte X;

        [FieldOffset(1)]
        public byte Y;
    }

    [Serializable]
    public struct TextureWindow
    {
        public byte MaskX;
        public byte MaskY;
        public byte OffsetX;
        public byte OffsetY;

        public TextureWindow(uint value)
        {
            MaskX = (byte)(value & 0x1F);
            MaskY = (byte)((value >> 5) & 0x1F);
            OffsetX = (byte)((value >> 10) & 0x1F);
            OffsetY = (byte)((value >> 15) & 0x1F);
        }
    }

    [Serializable]
    public struct TDrawMode
    {
        public byte TexturePageXBase;
        public byte TexturePageYBase;
        public byte SemiTransparency;
        public byte TexturePageColors;
        public bool Dither24BitTo15Bit;
        public bool DrawingToDisplayArea;
        public bool TextureDisable;
        public bool TexturedRectangleXFlip;
        public bool TexturedRectangleYFlip;
    }

    [Serializable]
    public struct TDrawingOffset
    {
        public short X;
        public short Y;

        public TDrawingOffset(uint value)
        {
            X = GPU.Read11BitShort(value & 0x7FF);
            Y = GPU.Read11BitShort((value >> 11) & 0x7FF);
        }
    }

    [Serializable]
    public struct TDrawingArea : IEquatable<TDrawingArea>
    {
        public ushort X;
        public ushort Y;

        public TDrawingArea(uint value)
        {
            X = (ushort)(value & 0x3FF);
            Y = (ushort)((value >> 10) & 0x1FF);
        }

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
        }

        public bool Equals(TDrawingArea other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is TDrawingArea other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(TDrawingArea left, TDrawingArea right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TDrawingArea left, TDrawingArea right)
        {
            return !left.Equals(right);
        }
    }

    [Serializable]
    public struct VRAMTransfer
    {
        public int X;
        public int Y;
        public ushort W;
        public ushort H;
        public int OriginX;
        public int OriginY;
        public int HalfWords;
        public int currentpos;
        public bool isRead;
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct GPUColor
    {
        [FieldOffset(0)]
        public uint Value;

        [FieldOffset(0)]
        public byte R;

        [FieldOffset(1)]
        public byte G;

        [FieldOffset(2)]
        public byte B;

        [FieldOffset(3)]
        public byte M;
    }

}
