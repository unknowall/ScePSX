using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScePSX
{
    public class SoftwareGPU : IGPU
    {
        public GPUType type => GPUType.Software;

        private GPUColor Color0, Color1, Color2;

        private TDrawingArea DrawingAreaTopLeft, DrawingAreaBottomRight;

        private TDrawingOffset DrawingOffset;

        private VRAMTransfer _VRAMTransfer;

        private int MaskWhileDrawing;

        private bool CheckMaskBeforeDraw;

        private int TextureWindowPostMaskX, TextureWindowPostMaskY, TextureWindowPreMaskX, TextureWindowPreMaskY;

        public unsafe class RamBuff: IDisposable
        {
            public int Width;
            public int Height;
            public int length;
            public int size;
            public ushort* Pixels;

            public RamBuff(int width, int height)
            {
                Width = width;
                Height = height;
                length = width * height;
                size = length * 2;

                Pixels = (ushort*)Marshal.AllocHGlobal(size);
            }

            public Span<ushort> AsSpan()
            {
                return new Span<ushort>(Pixels, length);
            }

            public unsafe ushort GetPixel(int x, int y)
            {
                return *(ushort*)(Pixels + y * Width + x);
            }

            public unsafe void SetPixel(int x, int y, ushort color)
            {
                *(ushort*)(Pixels + y * Width + x) = color;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal((nint)Pixels);
            }
        }

        public unsafe class FrameBuff : IDisposable
        {
            public int Width;
            public int Height;
            public int length;
            public int size;
            public int* Pixels;

            public FrameBuff(int width, int height)
            {
                Width = width;
                Height = height;
                length = width * height;
                size = length * 4;

                Pixels = (int*)Marshal.AllocHGlobal(size);
            }

            public Span<int> AsSpan()
            {
                return new Span<int>(Pixels, length);
            }

            public unsafe int GetPixel(int x, int y)
            {
                return *(int*)(Pixels + y * Width + x);
            }

            public unsafe void SetPixel(int x, int y, int color)
            {
                *(int*)(Pixels + y * Width + x) = color;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal((nint)Pixels);
            }
        }

        private readonly RamBuff RamData = new(1024, 512);

        private readonly FrameBuff FrameData = new(1024, 512);

        private static readonly byte[] LookupTable888to555 = new byte[256];

        private static readonly byte[] LookupTable1555to8888 = new byte[32];

        public SoftwareGPU()
        {
            for (int i = 0; i < 256; i++)
            {
                LookupTable888to555[i] = (byte)(i * 31 >> 8);
            }

            for (int i = 0; i < 32; i++)
            {
                LookupTable1555to8888[i] = (byte)((i * 255 + 15) / 31);
            }
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
            FrameData.Dispose();
            RamData.Dispose();
        }

        public void SetParams(int[] Params)
        {
        }

        public unsafe void SetRam(byte[] Ram)
        {
            Marshal.Copy(Ram, 0, (IntPtr)RamData.Pixels, Ram.Length);
        }

        public unsafe byte[] GetRam()
        {
            byte[] Ram = new byte[RamData.size];

            Marshal.Copy((IntPtr)RamData.Pixels, Ram, 0, RamData.size);

            return Ram;
        }

        public unsafe void SetFrameBuff(byte[] FrameBuffer)
        {
            Marshal.Copy(FrameBuffer,0, (IntPtr)FrameData.Pixels, FrameBuffer.Length);
        }

        public unsafe byte[] GetFrameBuff()
        {

            byte[] FrameBuffer = new byte[FrameData.size];

            Marshal.Copy((IntPtr)FrameData.Pixels, FrameBuffer, 0, FrameData.size);

            return FrameBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe (int w, int h) GetPixels(bool is24bit, int dy1, int dy2, int rx, int ry, int w, int h, int[] Pixels)
        {
            int retw, reth;

            if (is24bit)
            {
                int lineOffset = (240 - (dy2 - dy1)) >> (h == 480 ? 0 : 1);
                int effectiveVertical = h - lineOffset;

                Parallel.For(lineOffset, effectiveVertical, line =>
                {
                    int localLine = line - lineOffset;
                    int sourceLine = ry + localLine;
                    int srcBase = rx + (sourceLine * 1024);
                    int destBase = localLine * w;

                    int hRes = w;
                    int offset = 0;
                    for (int x = 0; x < hRes; x += 2)
                    {
                        int p0rgb = *(int*)(FrameData.Pixels + srcBase + offset);
                        int p1rgb = *(int*)(FrameData.Pixels + srcBase + offset + 1);
                        int p2rgb = *(int*)(FrameData.Pixels + srcBase + offset + 2);
                        offset += 3;

                        ushort p0bgr555 = rgb8888to1555(p0rgb);
                        ushort p1bgr555 = rgb8888to1555(p1rgb);
                        ushort p2bgr555 = rgb8888to1555(p2rgb);

                        int p0R = p0bgr555 & 0xFF;
                        int p0G = (p0bgr555 >> 8) & 0xFF;
                        int p0B = p1bgr555 & 0xFF;
                        int p1R = (p1bgr555 >> 8) & 0xFF;
                        int p1G = p2bgr555 & 0xFF;
                        int p1B = (p2bgr555 >> 8) & 0xFF;

                        int p0rgb24 = (p0R << 16) | (p0G << 8) | p0B;
                        int p1rgb24 = (p1R << 16) | (p1G << 8) | p1B;
                        Pixels[destBase + x] = p0rgb24;
                        Pixels[destBase + x + 1] = p1rgb24;
                    }
                });

                retw = w;
                reth = h - lineOffset * 2 - 1;

            } else
            {
                int LineOffset = (240 - (dy2 - dy1)) >> (h == 480 ? 0 : 1);
                Parallel.For(LineOffset, h - LineOffset, line =>
                {
                    int srcIndex = rx + ((line - LineOffset + ry) * 1024);
                    int dstIndex = (line - LineOffset) * w;
                    Marshal.Copy((IntPtr)(FrameData.Pixels + srcIndex), Pixels, dstIndex, w);
                });

                retw = w;
                reth = h - LineOffset * 2 - 1;
            }

            return (retw, reth);
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
            _VRAMTransfer = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadFromVRAM()
        {
            var pixel0 = RamData.GetPixel(_VRAMTransfer.X++ & 0x3FF, _VRAMTransfer.Y & 0x1FF);
            var pixel1 = RamData.GetPixel(_VRAMTransfer.X++ & 0x3FF, _VRAMTransfer.Y & 0x1FF);

            if (_VRAMTransfer.X == _VRAMTransfer.OriginX + _VRAMTransfer.W)
            {
                _VRAMTransfer.X -= _VRAMTransfer.W;
                _VRAMTransfer.Y++;
            }

            _VRAMTransfer.HalfWords -= 2;

            return (uint)((pixel1 << 16) | pixel0);
        }

        public void SetMaskBit(uint value)
        {
            MaskWhileDrawing = (int)(value & 0x1);
            CheckMaskBeforeDraw = (value & 0x2) != 0;
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
            DrawingAreaTopLeft = value;
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
            DrawingAreaBottomRight = value;
        }

        public void SetDrawingOffset(TDrawingOffset value)
        {
            DrawingOffset = value;
        }

        public void SetTextureWindow(uint value)
        {
            TextureWindow textureWindow = new TextureWindow(value);

            TextureWindowPreMaskX = ~(textureWindow.MaskX * 8);
            TextureWindowPreMaskY = ~(textureWindow.MaskY * 8);
            TextureWindowPostMaskX = (textureWindow.OffsetX & textureWindow.MaskX) * 8;
            TextureWindowPostMaskY = (textureWindow.OffsetY & textureWindow.MaskY) * 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillRectVRAM(ushort x, ushort y, ushort w, ushort h, uint colorval)
        {
            GPUColor color = new GPUColor();
            color.Value = colorval;

            ushort bgr555 = (ushort)(((color.B * 31 / 255) << 10) | ((color.G * 31 / 255) << 5) | ((color.R * 31 / 255) << 0));
            int rgb888 = (color.R << 16) | (color.G << 8) | color.B;

            if (x + w <= 0x3FF && y + h <= 0x1FF)
            {
                var span16 = RamData.AsSpan();
                var span24 = FrameData.AsSpan();

                for (int yPos = y; yPos < h + y; yPos++)
                {
                    var start = yPos * 1024 + x;
                    span16.Slice(start, w).Fill(bgr555);
                    span24.Slice(start, w).Fill(rgb888);
                }
            } else
            {
                for (int yPos = y; yPos < h + y; yPos++)
                {
                    for (int xPos = x; xPos < w + x; xPos++)
                    {
                        var y2 = yPos & 0x1FF;
                        FrameData.SetPixel(xPos & 0x3FF, y2, rgb888);
                        var y1 = yPos & 0x1FF;
                        RamData.SetPixel(xPos & 0x3FF, y1, bgr555);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyRectVRAMtoVRAM(ushort sx, ushort sy, ushort dx, ushort dy, ushort w, ushort h)
        {
            for (var yPos = 0; yPos < h; yPos++)
            {
                for (var xPos = 0; xPos < w; xPos++)
                {
                    var y1 = (sy + yPos) & 0x1FF;

                    var rgb888 = FrameData.GetPixel((sx + xPos) & 0x3FF, y1);

                    var bgr555 = RamData.GetPixel((sx + xPos) & 0x3FF, (sy + yPos) & 0x1FF);

                    if (CheckMaskBeforeDraw)
                    {
                        var y2 = (dy + yPos) & 0x1FF;
                        Color0.Value = (uint)FrameData.GetPixel((dx + xPos) & 0x3FF, y2);
                        if (Color0.M != 0)
                            continue;
                    }

                    rgb888 |= MaskWhileDrawing << 24;

                    var y3 = (dy + yPos) & 0x1FF;
                    FrameData.SetPixel((dx + xPos) & 0x3FF, y3, rgb888);

                    var y = (dy + yPos) & 0x1FF;
                    RamData.SetPixel((dx + xPos) & 0x3FF, y, bgr555);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPixel(ushort value)
        {
            if (CheckMaskBeforeDraw)
            {
                var bg = FrameData.GetPixel(_VRAMTransfer.X, _VRAMTransfer.Y);

                if (bg >> 24 == 0)
                {
                    var y1 = _VRAMTransfer.Y & 0x1FF;
                    var color = rgb1555To8888(value);
                    FrameData.SetPixel(_VRAMTransfer.X & 0x3FF, y1, color);
                    var y2 = _VRAMTransfer.Y & 0x1FF;
                    RamData.SetPixel(_VRAMTransfer.X & 0x3FF, y2, value);
                }
            } else
            {
                var y1 = _VRAMTransfer.Y & 0x1FF;
                var color = rgb1555To8888(value);
                FrameData.SetPixel(_VRAMTransfer.X & 0x3FF, y1, color);
                var y2 = _VRAMTransfer.Y & 0x1FF;
                RamData.SetPixel(_VRAMTransfer.X & 0x3FF, y2, value);
            }

            _VRAMTransfer.X++;

            if (_VRAMTransfer.X != _VRAMTransfer.OriginX + _VRAMTransfer.W)
                return;

            _VRAMTransfer.X -= _VRAMTransfer.W;
            _VRAMTransfer.Y++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent, int SemiTransparency)
        {
            var x = Read11BitShort(v1 & 0xFFFF);
            var y = Read11BitShort(v1 >> 16);

            var x2 = Read11BitShort(v2 & 0xFFFF);
            var y2 = Read11BitShort(v2 >> 16);

            if (Math.Abs(x - x2) > 0x3FF || Math.Abs(y - y2) > 0x1FF)
                return;

            x += DrawingOffset.X;
            y += DrawingOffset.Y;

            x2 += DrawingOffset.X;
            y2 += DrawingOffset.Y;

            var w = x2 - x;
            var h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

            if (w < 0)
                dx1 = -1;
            else if (w > 0)
                dx1 = 1;

            if (h < 0)
                dy1 = -1;
            else if (h > 0)
                dy1 = 1;

            if (w < 0)
                dx2 = -1;
            else if (w > 0)
                dx2 = 1;

            var longest = Math.Abs(w);
            var shortest = Math.Abs(h);

            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0)
                    dy2 = -1;
                else if (h > 0)
                    dy2 = 1;

                dx2 = 0;
            }

            var numerator = longest >> 1;

            for (var i = 0; i <= longest; i++)
            {
                var ratio = (float)i / longest;
                var color = Interpolate(color1, color2, ratio);

                if (x >= DrawingAreaTopLeft.X && x < DrawingAreaBottomRight.X && y >= DrawingAreaTopLeft.Y && y < DrawingAreaBottomRight.Y)
                {
                    if (isTransparent)
                    {
                        color = HandleSemiTransp(x, y, color, SemiTransparency);
                    }

                    color |= MaskWhileDrawing << 24;
                    FrameData.SetPixel(x, y, color);

                    ushort color3 = (ushort)rgb888To555(color);
                    RamData.SetPixel(x, y, color3);
                }

                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += (short)dx1;
                    y += (short)dy1;
                } else
                {
                    x += (short)dx2;
                    y += (short)dy2;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
            var xOrigin = Math.Max(origin.X, DrawingAreaTopLeft.X);
            var yOrigin = Math.Max(origin.Y, DrawingAreaTopLeft.Y);
            var width = Math.Min(size.X, DrawingAreaBottomRight.X);
            var height = Math.Min(size.Y, DrawingAreaBottomRight.Y);

            var uOrigin = texture.X + (xOrigin - origin.X);
            var vOrigin = texture.Y + (yOrigin - origin.Y);

            var baseColor = GetRgbColor(bgrColor);

            for (int y = yOrigin, v = vOrigin; y < height; y++, v++)
            {
                for (int x = xOrigin, u = uOrigin; x < width; x++, u++)
                {
                    // Check background mask
                    if (CheckMaskBeforeDraw)
                    {
                        var y1 = y & 0x1FF;
                        Color0.Value = (uint)FrameData.GetPixel(x & 0x3FF, y1); // back
                        if (Color0.M != 0)
                            continue;
                    }

                    var color = baseColor;

                    if (primitive.IsTextured)
                    {
                        var texel = GetTexel(
                            MaskTexelAxis(u, TextureWindowPreMaskX, TextureWindowPostMaskX),
                            MaskTexelAxis(v, TextureWindowPreMaskY, TextureWindowPostMaskY),
                            primitive.Clut, primitive.TextureBase, primitive.Depth);

                        if (texel == 0)
                        {
                            continue;
                        }

                        if (!primitive.IsRawTextured)
                        {
                            Color0.Value = (uint)color;
                            Color1.Value = (uint)texel;
                            Color1.R = ClampToFF((Color0.R * Color1.R) >> 7);
                            Color1.G = ClampToFF((Color0.G * Color1.G) >> 7);
                            Color1.B = ClampToFF((Color0.B * Color1.B) >> 7);
                            texel = (int)Color1.Value;
                        }

                        color = texel;
                    }

                    if (primitive.IsSemiTransparent && (!primitive.IsTextured || (color & 0xFF00_0000) != 0))
                    {
                        color = HandleSemiTransp(x, y, color, primitive.SemiTransparencyMode);
                    }

                    color |= MaskWhileDrawing << 24;
                    FrameData.SetPixel(x, y, color);

                    ushort color3 = (ushort)rgb888To555(color);
                    RamData.SetPixel(x, y, color3);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
        {
            var area = Orient2d(v0, v1, v2);

            if (area == 0)
                return;

            if (area < 0)
            {
                (v1, v2) = (v2, v1);
                (t1, t2) = (t2, t1);
                (c1, c2) = (c2, c1);
                area = -area;
            }

            int minX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
            int minY = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
            int maxX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
            int maxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

            if (maxX - minX > 1024 || maxY - minY > 512)
                return;

            short pointMinX = (short)Math.Max(minX, DrawingAreaTopLeft.X);
            short pointMinY = (short)Math.Max(minY, DrawingAreaTopLeft.Y);
            short pointMaxX = (short)Math.Min(maxX, DrawingAreaBottomRight.X);
            short pointMaxY = (short)Math.Min(maxY, DrawingAreaBottomRight.Y);

            int a01 = v0.Y - v1.Y, b01 = v1.X - v0.X;
            int a12 = v1.Y - v2.Y, b12 = v2.X - v1.X;
            int a20 = v2.Y - v0.Y, b20 = v0.X - v2.X;

            var bias0 = IsTopLeft(v1, v2) ? 0 : -1;
            var bias1 = IsTopLeft(v2, v0) ? 0 : -1;
            var bias2 = IsTopLeft(v0, v1) ? 0 : -1;

            Point2D pointMin = new Point2D { X = pointMinX, Y = pointMinY };
            var w0Row = Orient2d(v1, v2, pointMin) + bias0;
            var w1Row = Orient2d(v2, v0, pointMin) + bias1;
            var w2Row = Orient2d(v0, v1, pointMin) + bias2;

            var baseColor = GetRgbColor(c0);

            for (int y = pointMinY; y < pointMaxY; y++)
            {
                var w0 = w0Row;
                var w1 = w1Row;
                var w2 = w2Row;

                for (int x = pointMinX; x < pointMaxX; x++)
                {
                    if ((w0 | w1 | w2) >= 0)
                    {
                        if (CheckMaskBeforeDraw)
                        {
                            Color0.Value = (uint)FrameData.GetPixel(x, y);
                            if (Color0.M != 0)
                            {
                                w0 += a12;
                                w1 += a20;
                                w2 += a01;
                                continue;
                            }
                        }

                        var color = baseColor;

                        if (primitive.IsShaded)
                        {
                            Color0.Value = c0;
                            Color1.Value = c1;
                            Color2.Value = c2;

                            var r = Interpolate(w0 - bias0, w1 - bias1, w2 - bias2, Color0.R, Color1.R, Color2.R, area);
                            var g = Interpolate(w0 - bias0, w1 - bias1, w2 - bias2, Color0.G, Color1.G, Color2.G, area);
                            var b = Interpolate(w0 - bias0, w1 - bias1, w2 - bias2, Color0.B, Color1.B, Color2.B, area);
                            color = (r << 16) | (g << 8) | b;
                        }

                        if (primitive.IsTextured)
                        {
                            var texelX = Interpolate(w0 - bias0, w1 - bias1, w2 - bias2, t0.X, t1.X, t2.X, area);
                            var texelY = Interpolate(w0 - bias0, w1 - bias1, w2 - bias2, t0.Y, t1.Y, t2.Y, area);
                            var texel = GetTexel(MaskTexelAxis(texelX, TextureWindowPreMaskX, TextureWindowPostMaskX),
                                MaskTexelAxis(texelY, TextureWindowPreMaskY, TextureWindowPostMaskY), primitive.Clut, primitive.TextureBase,
                                primitive.Depth);

                            if (texel == 0)
                            {
                                w0 += a12;
                                w1 += a20;
                                w2 += a01;
                                continue;
                            }

                            if (!primitive.IsRawTextured)
                            {
                                Color0.Value = (uint)color;
                                Color1.Value = (uint)texel;
                                Color1.R = ClampToFF((Color0.R * Color1.R) >> 7);
                                Color1.G = ClampToFF((Color0.G * Color1.G) >> 7);
                                Color1.B = ClampToFF((Color0.B * Color1.B) >> 7);

                                texel = (int)Color1.Value;
                            }

                            color = texel;
                        }

                        if (primitive.IsSemiTransparent && (!primitive.IsTextured || (color & 0xFF00_0000) != 0))
                        {
                            color = HandleSemiTransp(x, y, color, primitive.SemiTransparencyMode);
                        }

                        color |= MaskWhileDrawing << 24;
                        FrameData.SetPixel(x, y, color);

                        ushort color3 = (ushort)rgb888To555(color);
                        RamData.SetPixel(x, y, color3);
                    }

                    w0 += a12;
                    w1 += a20;
                    w2 += a01;
                }

                w0Row += b12;
                w1Row += b20;
                w2Row += b01;
            }
        }

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ClampToFF(int v)
        {
            if (v > 0xFF)
                return 0xFF;
            return (byte)v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ClampToZero(int v)
        {
            if (v < 0)
                return 0;
            return (byte)v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Get4BppTexel(int x, int y, Point2D clut, Point2D textureBase)
        {
            var y1 = y + textureBase.Y;
            var index = RamData.GetPixel(x / 4 + textureBase.X, y1);
            var p = (index >> ((x & 3) * 4)) & 0xF;
            return FrameData.GetPixel(clut.X + p, clut.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Get8BppTexel(int x, int y, Point2D clut, Point2D textureBase)
        {
            var y1 = y + textureBase.Y;
            var index = RamData.GetPixel(x / 2 + textureBase.X, y1);
            var p = (index >> ((x & 1) * 8)) & 0xFF;
            return FrameData.GetPixel(clut.X + p, clut.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Get16BppTexel(int x, int y, Point2D textureBase)
        {
            var y1 = y + textureBase.Y;
            return FrameData.GetPixel(x + textureBase.X, y1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetTexel(int x, int y, Point2D clut, Point2D textureBase, int depth)
        {
            if (depth == 0)
            {
                return Get4BppTexel(x, y, clut, textureBase);
            }

            if (depth == 1)
            {
                return Get8BppTexel(x, y, clut, textureBase);
            }

            return Get16BppTexel(x, y, textureBase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetRgbColor(uint value)
        {
            Color0.Value = value;
            return (Color0.M << 24) | (Color0.R << 16) | (Color0.G << 8) | Color0.B;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int HandleSemiTransp(int x, int y, int color, int semiTranspMode)
        {
            Color0.Value = (uint)FrameData.GetPixel(x, y); //back
            Color1.Value = (uint)color; //front
            switch (semiTranspMode)
            {
                case 0: //0.5 x B + 0.5 x F    ;aka B/2+F/2
                    Color1.R = (byte)((Color0.R + Color1.R) >> 1);
                    Color1.G = (byte)((Color0.G + Color1.G) >> 1);
                    Color1.B = (byte)((Color0.B + Color1.B) >> 1);
                    break;
                case 1: //1.0 x B + 1.0 x F    ;aka B+F
                    Color1.R = ClampToFF(Color0.R + Color1.R);
                    Color1.G = ClampToFF(Color0.G + Color1.G);
                    Color1.B = ClampToFF(Color0.B + Color1.B);
                    break;
                case 2: //1.0 x B - 1.0 x F    ;aka B-F
                    Color1.R = ClampToZero(Color0.R - Color1.R);
                    Color1.G = ClampToZero(Color0.G - Color1.G);
                    Color1.B = ClampToZero(Color0.B - Color1.B);
                    break;
                case 3: //1.0 x B +0.25 x F    ;aka B+F/4
                    Color1.R = ClampToFF(Color0.R + (Color1.R >> 2));
                    Color1.G = ClampToFF(Color0.G + (Color1.G >> 2));
                    Color1.B = ClampToFF(Color0.B + (Color1.B >> 2));
                    break;
            } //actually doing RGB calcs on BGR struct...

            return (int)Color1.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Interpolate(int w0, int w1, int w2, int t0, int t1, int t2, int area)
        {
            return (t0 * w0 + t1 * w1 + t2 * w2) / area;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Interpolate(uint c1, uint c2, float ratio)
        {
            Color1.Value = c1;
            Color2.Value = c2;

            var r = (byte)(Color2.R * ratio + Color1.R * (1 - ratio));
            var g = (byte)(Color2.G * ratio + Color1.G * (1 - ratio));
            var b = (byte)(Color2.B * ratio + Color1.B * (1 - ratio));

            return (r << 16) | (g << 8) | b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTopLeft(Point2D a, Point2D b)
        {
            return a.Y == b.Y && b.X > a.X || b.Y < a.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int MaskTexelAxis(int axis, int preMaskAxis, int postMaskAxis)
        {
            return (axis & 0xFF & preMaskAxis) | postMaskAxis;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Orient2d(Point2D a, Point2D b, Point2D c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Read11BitShort(uint value)
        {
            return (short)(((int)value << 21) >> 21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short rgb888To555(int color)
        {
            int r = LookupTable888to555[color & 0xFF];
            int g = LookupTable888to555[(color >> 8) & 0xFF];
            int b = LookupTable888to555[(color >> 16) & 0xFF];

            return (short)((r << 10) | (g << 5) | b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int rgb1555To8888(ushort color)
        {
            var m = (byte)(color >> 15);
            var r = LookupTable1555to8888[color & 0x1F];
            var g = LookupTable1555to8888[(color >> 5) & 0x1F];
            var b = LookupTable1555to8888[(color >> 10) & 0x1F];

            return (m << 24) | (r << 16) | (g << 8) | b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort rgb8888to1555(int color)
        {
            byte m = (byte)((color & 0xFF000000) >> 24);
            byte r = (byte)((color & 0x00FF0000) >> 16 + 3);
            byte g = (byte)((color & 0x0000FF00) >> 8 + 3);
            byte b = (byte)((color & 0x000000FF) >> 3);

            return (ushort)(m << 15 | b << 10 | g << 5 | r);
        }

        #endregion

    }

}
