
using System;
using System.Runtime.InteropServices;

namespace ScePSX
{
    public struct vkRectangle<T> where T : struct, IComparable<T>
    {
        public T Left
        {
            get; set;
        }
        public T Top
        {
            get; set;
        }
        public T Right
        {
            get; set;
        }
        public T Bottom
        {
            get; set;
        }

        public vkRectangle(T left, T top, T right, T bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int GetWidth()
        {
            dynamic dRight = Right, dLeft = Left;
            return (int)(dRight - dLeft);
        }

        public int GetHeight()
        {
            dynamic dBottom = Bottom, dTop = Top;
            return (int)(dBottom - dTop);
        }

        public static vkRectangle<T> FromExtents(T left, T top, T width, T height)
        {
            dynamic dLeft = left, dTop = top, dWidth = width, dHeight = height;
            return new vkRectangle<T>(left, top, dLeft + dWidth, dTop + dHeight);
        }

        public bool Intersects(vkRectangle<T> other)
        {
            return Left.CompareTo(other.Right) < 0 &&
                   Right.CompareTo(other.Left) > 0 &&
                   Top.CompareTo(other.Bottom) < 0 &&
                   Bottom.CompareTo(other.Top) > 0;
        }

        public void Grow(vkRectangle<T> bounds)
        {
            if (bounds.Left.CompareTo(Left) < 0)
                Left = bounds.Left;
            if (bounds.Top.CompareTo(Top) < 0)
                Top = bounds.Top;
            if (bounds.Right.CompareTo(Right) > 0)
                Right = bounds.Right;
            if (bounds.Bottom.CompareTo(Bottom) > 0)
                Bottom = bounds.Bottom;
        }

        public void Grow(T x, T y)
        {
            dynamic dX = x, dY = y;
            Left = (T)(dynamic)Math.Min(Convert.ToDouble(Left), Convert.ToDouble(x));
            Top = (T)(dynamic)Math.Min(Convert.ToDouble(Top), Convert.ToDouble(y));
            Right = (T)(dynamic)Math.Max(Convert.ToDouble(Right), Convert.ToDouble(x));
            Bottom = (T)(dynamic)Math.Max(Convert.ToDouble(Bottom), Convert.ToDouble(y));
        }

        public void ScaleInPlace(float scale)
        {
            Left = (T)(object)(Convert.ToInt32(Left) * scale);
            Top = (T)(object)(Convert.ToInt32(Top) * scale);
            Right = (T)(object)(Convert.ToInt32(Right) * scale);
            Bottom = (T)(object)(Convert.ToInt32(Bottom) * scale);
        }

        public vkRectangle<int> Scale(float scale)
        {
            return new vkRectangle<int>(
                (int)(Convert.ToInt32(Left) * scale),
                (int)(Convert.ToInt32(Top) * scale),
                (int)(Convert.ToInt32(Right) * scale),
                (int)(Convert.ToInt32(Bottom) * scale)
            );
        }

        public bool Empty()
        {
            return GetWidth() <= 0 || GetHeight() <= 0;
        }

        public override string ToString()
        {
            return $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkPosition
    {
        public short x;
        public short y;
        public short z;
        public short w;

        public vkPosition()
        {
            x = 0;
            y = 0;
            z = 0;
            w = 1;
        }

        public vkPosition(short x_, short y_)
        {
            x = x_;
            y = y_;
            z = 0;
            w = 1;
        }

        public vkPosition(uint param)
        {
            x = (short)((param << 5) >> 5);
            y = (short)((param >> 11) >> 5);
            z = 0;
            w = 1;
        }

        public static vkPosition operator +(vkPosition lhs, vkPosition rhs)
        {
            return new vkPosition((short)(lhs.x + rhs.x), (short)(lhs.y + rhs.y));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkTexCoord
    {
        public short u;
        public short v;

        public vkTexCoord()
        {
            u = 0;
            v = 0;
        }

        public vkTexCoord(short u_, short v_)
        {
            u = u_;
            v = v_;
        }

        public vkTexCoord(uint gpuParam)
        {
            u = (short)(gpuParam & 0xff);
            v = (short)((gpuParam >> 8) & 0xff);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkClutAttribute
    {
        public const ushort WriteMask = 0x7FFF; // 低 15 位

        private ushort value;

        public ushort Value
        {
            get => (ushort)(value & WriteMask);
            set => this.value = (ushort)(value & WriteMask);
        }

        // x: 6 bits (bits 0-5)
        public byte X
        {
            get => (byte)(value & 0x3F); // 提取低 6 位
            set => this.value = (ushort)((this.value & ~0x3F) | (value & 0x3F)); // 设置低 6 位
        }

        // y: 9 bits (bits 6-14)
        public ushort Y
        {
            get => (ushort)((value >> 6) & 0x1FF); // 提取第 6-14 位
            set => this.value = (ushort)((this.value & ~(0x1FF << 6)) | ((value & 0x1FF) << 6)); // 设置第 6-14 位
        }

        public vkClutAttribute(ushort v)
        {
            value = (ushort)(v & WriteMask);
        }

        public override string ToString()
        {
            return $"Value: {value:X4}, X: {X}, Y: {Y}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkTexPage
    {
        public const ushort WriteMask = 0x09FF; //低 12 位

        private ushort value;

        public ushort Value
        {
            get => (ushort)(value & WriteMask);
            set => this.value = (ushort)(value & WriteMask);
        }

        // texturePageBaseX: 4 bits (bits 0-3)
        public byte TexturePageBaseX
        {
            get => (byte)(value & 0x000F); // 提取低 4 位
            set => value = (byte)((value & ~0x000F) | (value & 0x000F)); // 设置低 4 位
        }

        // texturePageBaseY: 1 bit (bit 4)
        public byte TexturePageBaseY
        {
            get => (byte)((value >> 4) & 0x01); // 提取第 4 位
            set => value = (byte)((value & ~(1 << 4)) | ((value & 0x01) << 4)); // 设置第 4 位
        }

        // semiTransparencymode: 2 bits (bits 5-6)
        public byte SemiTransparencymode
        {
            get => (byte)((value >> 5) & 0x03); // 提取第 5-6 位
            set => value = (byte)((value & ~(3 << 5)) | ((value & 0x03) << 5)); // 设置第 5-6 位
        }

        // texturePageColors: 2 bits (bits 7-8)
        public byte TexturePageColors
        {
            get => (byte)((value >> 7) & 0x03); // 提取第 7-8 位
            set => value = (byte)((value & ~(3 << 7)) | ((value & 0x03) << 7)); // 设置第 7-8 位
        }

        // textureDisable: 1 bit (bit 11)
        public bool TextureDisable
        {
            get => ((value >> 11) & 0x01) != 0; // 提取第 11 位
            set => this.value = (ushort)((this.value & ~(1 << 11)) | ((value ? 1 : 0) << 11)); // 设置第 11 位
        }

        public vkTexPage(ushort v)
        {
            value = (ushort)(v & WriteMask);
        }

        public override string ToString()
        {
            return $"Value: {value:X4}, " +
                   $"TexturePageBaseX: {TexturePageBaseX}, " +
                   $"TexturePageBaseY: {TexturePageBaseY}, " +
                   $"SemiTransparencymode: {SemiTransparencymode}, " +
                   $"TexturePageColors: {TexturePageColors}, " +
                   $"TextureDisable: {TextureDisable}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkColor
    {
        public byte r; // 范围 [0, 255]
        public byte g;
        public byte b;
        public byte a; // 对齐填充

        public vkColor(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 0;
        }

        public static vkColor FromUInt32(uint color)
        {
            return new vkColor(
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)((color >> 0) & 0xFF)
            );
        }
    }

    public static class vkShaders
    {
        public readonly static byte[] drawvert = new byte[] { };
        public readonly static byte[] drawfrag = new byte[] { };

        public readonly static byte[] out24vert = new byte[] { };
        public readonly static byte[] out24frag = new byte[] { };

        public readonly static byte[] out16vert = new byte[] { };
        public readonly static byte[] out16frag = new byte[] { };


    }

}
