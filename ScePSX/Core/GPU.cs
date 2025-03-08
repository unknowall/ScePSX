using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScePSX
{
    [Serializable]
    public class GPU
    {
        private enum Mode
        {
            COMMAND,
            VRAM
        }

        [Serializable]
        public struct TDisplayMode
        {
            public byte HorizontalResolution1;
            public bool IsVerticalResolution480;
            public bool IsPAL;
            public bool Is24BitDepth;
            public bool IsVerticalInterlace;
            public byte HorizontalResolution2;
            public bool IsReverseFlag;

            public TDisplayMode(uint value)
            {
                HorizontalResolution1 = (byte)(value & 0x3);
                IsVerticalResolution480 = (value & 0x4) != 0;
                IsPAL = (value & 0x8) != 0;
                Is24BitDepth = (value & 0x10) != 0;
                IsVerticalInterlace = (value & 0x20) != 0;
                HorizontalResolution2 = (byte)((value & 0x40) >> 6);
                IsReverseFlag = (value & 0x80) != 0;
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct Point2D
        {
            [FieldOffset(0)]
            public short X;

            [FieldOffset(2)]
            public short Y;
        }

        [Serializable]
        private struct Primitive
        {
            public bool IsShaded;
            public bool IsTextured;
            public bool IsSemiTransparent;
            public bool IsRawTextured;
            public int Depth;
            public int SemiTransparencyMode;
            public Point2D Clut;
            public Point2D TextureBase;
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct TextureData
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
        private struct TDrawingOffset
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
        private struct TDrawingArea : IEquatable<TDrawingArea>
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
        public abstract class VRAM<T> where T : unmanaged
        {
            public int Width;
            public int Height;
            public T[] Pixels;

            protected VRAM(int width, int height)
            {
                Width = width;
                Height = height;
                Pixels = new T[width * height];
            }

            public abstract T GetPixel(int x, int y);

            public abstract void SetPixel(int x, int y, T color);

            protected int GetIndex(int x, int y)
            {
                var offset = y * Width + x;

                return offset;
            }
        }

        [Serializable]
        public sealed class VRAM16 : VRAM<ushort>
        {
            public VRAM16(int width, int height) : base(width, height)
            {
            }

            public override ushort GetPixel(int x, int y)
            {
                return Pixels[GetIndex(x, y)];
            }

            public override void SetPixel(int x, int y, ushort color)
            {
                Pixels[GetIndex(x, y)] = color;
            }
        }

        [Serializable]
        public sealed class VRAM32 : VRAM<int>
        {
            public VRAM32(int width, int height) : base(width, height)
            {
            }

            public override int GetPixel(int x, int y)
            {
                return Pixels[GetIndex(x, y)];
            }

            public override void SetPixel(int x, int y, int color)
            {
                Pixels[GetIndex(x, y)] = color;
            }
        }

        [Serializable]
        private struct VRAMTransfer
        {
            public int X;
            public int Y;
            public ushort W;
            public ushort H;
            public int OriginX;
            public int OriginY;
            public int HalfWords;
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct Color
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

        //This needs to go away once a BGR bitmap is achieved

        private static readonly int[] Resolutions = { 256, 320, 512, 640, 368 }; // GPUSTAT resolution index

        private static readonly int[] DotClockDiv = { 10, 8, 5, 4, 7 };

        private readonly uint[] CommandBuffer = new uint[16];

        private readonly VRAM16 _VRAM16 = new(1024, 512);

        private readonly VRAM32 _VRAM32 = new(1024, 512);

        private int[] _Pixels = new int[1024 * 512];

        private bool CheckMaskBeforeDraw;

        private Color Color0, Color1, Color2;

        private uint Command;

        private int CommandSize;

        public bool Debugging;

        private ushort DisplayVRAMStartX, DisplayVRAMStartY, DisplayX1, DisplayX2, DisplayY1, DisplayY2;

        private int horizontalRes, verticalRes, OutWidth, OutHeight;

        private byte DmaDirection;

        private TDrawingArea DrawingAreaTopLeft, DrawingAreaBottomRight;

        private TDrawingOffset DrawingOffset;

        private TDrawMode DrawMode;

        private uint GPUREADData; // 1F801810h-Read GPUREAD Receive responses to GP0(C0h) and GP1(10h) commands

        private bool IsDisplayDisabled;

        private bool IsDmaRequest = false;

        private bool IsInterlaceField;

        private bool IsInterruptRequested;

        private bool IsOddLine;

        private bool IsTextureDisabledAllowed;

        //GP0

        private int MaskWhileDrawing;

        private Mode _Mode;

        private int Pointer;

        //private Point2D PointMax, PointMin;

        private int ScanLine;

        private TextureData _TextureData;

        private uint TextureWindowBits = 0xFFFF_FFFF;

        private int TextureWindowPostMaskX, TextureWindowPostMaskY, TextureWindowPreMaskX, TextureWindowPreMaskY;

        private int TimingHorizontal = 3413, TimingVertical = 263;

        private int VideoCycles;

        private VRAMTransfer _VRAMTransfer;

        private TDisplayMode DisplayMode;

        [NonSerialized]
        public ICoreHandler host;

        private static IReadOnlyList<byte> CommandSizeTable = new byte[]
        {
        /*0    1     2     3     4     5     6     7     8     9     A     B     C     D     E     F */
        0x01, 0x01, 0x03, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, // 0
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, // 1
        0x04, 0x04, 0x04, 0x04, 0x07, 0x07, 0x07, 0x07, 0x05, 0x05, 0x05, 0x05, 0x09, 0x09, 0x09, 0x09, // 2
        0x06, 0x06, 0x06, 0x06, 0x09, 0x09, 0x09, 0x09, 0x08, 0x08, 0x08, 0x08, 0x0C, 0x0C, 0x0C, 0x0C, // 3
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, // 4
        0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, // 5
        0x03, 0x03, 0x03, 0x01, 0x04, 0x04, 0x04, 0x04, 0x02, 0x01, 0x02, 0x01, 0x03, 0x03, 0x03, 0x03, // 6
        0x02, 0x01, 0x02, 0x01, 0x03, 0x03, 0x03, 0x03, 0x02, 0x01, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, // 7
        0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, // 8
        0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, // 9
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, // A
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, // B
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, // C
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, // D
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, // E
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01  // F
        /*0    1     2     3     4     5     6     7     8     9     A     B     C     D     E     F */
        };

        public GPU(ICoreHandler Host)
        {
            this.host = Host;
            _Mode = Mode.COMMAND;

            GP1_00_ResetGPU();
        }

        public bool tick(int cycles)
        {
            VideoCycles += cycles * 11 / 7;

            if (VideoCycles < TimingHorizontal)
                return false;

            VideoCycles -= TimingHorizontal;

            ScanLine++;

            if (!DisplayMode.IsVerticalResolution480)
            {
                IsOddLine = (ScanLine & 0x1) != 0;
            }

            if (ScanLine < TimingVertical)
                return false;

            ScanLine = 0;

            if (DisplayMode.IsVerticalInterlace && DisplayMode.IsVerticalResolution480)
            {
                IsOddLine = !IsOddLine;
            }

            if (DisplayMode.Is24BitDepth)
            {
                Sub24ToPixels(_VRAM32.Pixels, _Pixels);
            } else
            {
                SubToPixels(_VRAM32.Pixels, _Pixels);
            }
            host.FrameReady(_Pixels, OutWidth, OutHeight);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetPixelBGR555(int color)
        {
            byte m = (byte)((color & 0xFF000000) >> 24);
            byte r = (byte)((color & 0x00FF0000) >> 16 + 3);
            byte g = (byte)((color & 0x0000FF00) >> 8 + 3);
            byte b = (byte)((color & 0x000000FF) >> 3);

            return (ushort)(m << 15 | b << 10 | g << 5 | r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Sub24ToPixels(int[] vramIN, int[] vramOUT)
        {
            int lineOffset = (240 - (DisplayY2 - DisplayY1)) >> (verticalRes == 480 ? 0 : 1);
            int effectiveVertical = verticalRes - lineOffset;

            Parallel.For(lineOffset, effectiveVertical, line =>
            {
                int localLine = line - lineOffset;
                int sourceLine = DisplayVRAMStartY + localLine;
                int srcBase = DisplayVRAMStartX + (sourceLine * 1024);
                int destBase = localLine * horizontalRes;

                int hRes = horizontalRes;
                int offset = 0;
                for (int x = 0; x < hRes; x += 2)
                {
                    int p0rgb = vramIN[srcBase + offset];
                    int p1rgb = vramIN[srcBase + offset + 1];
                    int p2rgb = vramIN[srcBase + offset + 2];
                    offset += 3;

                    ushort p0bgr555 = GetPixelBGR555(p0rgb);
                    ushort p1bgr555 = GetPixelBGR555(p1rgb);
                    ushort p2bgr555 = GetPixelBGR555(p2rgb);

                    int p0R = p0bgr555 & 0xFF;
                    int p0G = (p0bgr555 >> 8) & 0xFF;
                    int p0B = p1bgr555 & 0xFF;
                    int p1R = (p1bgr555 >> 8) & 0xFF;
                    int p1G = p2bgr555 & 0xFF;
                    int p1B = (p2bgr555 >> 8) & 0xFF;

                    int p0rgb24 = (p0R << 16) | (p0G << 8) | p0B;
                    int p1rgb24 = (p1R << 16) | (p1G << 8) | p1B;
                    vramOUT[destBase + x] = p0rgb24;
                    vramOUT[destBase + x + 1] = p1rgb24;
                }
            });

            OutWidth = horizontalRes;
            OutHeight = verticalRes - lineOffset * 2 - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubToPixels(int[] vramIn, int[] vramOut)
        {
            int LineOffset = (240 - (DisplayY2 - DisplayY1)) >> (verticalRes == 480 ? 0 : 1);
            Parallel.For(LineOffset, verticalRes - LineOffset, line =>
            {
                int srcIndex = DisplayVRAMStartX + ((line - LineOffset + DisplayVRAMStartY) * 1024);
                int dstIndex = (line - LineOffset) * horizontalRes;
                Array.Copy(vramIn, srcIndex, vramOut, dstIndex, horizontalRes);
            });

            OutWidth = horizontalRes;
            OutHeight = verticalRes - LineOffset * 2 - 1;
        }

        #region Process

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
        private static int Color1555To8888(ushort val)
        {
            var m = (byte)(val >> 15);
            var r = (byte)((val & 0x1F) << 3);
            var g = (byte)(((val >> 5) & 0x1F) << 3);
            var b = (byte)(((val >> 10) & 0x1F) << 3);

            return (m << 24) | (r << 16) | (g << 8) | b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecodeGP0Command(Span<uint> buffer)
        {
            //Console.WriteLine(CommandBuffer.Length);

            while (Pointer < buffer.Length)
            {
                if (_Mode == Mode.COMMAND)
                {
                    Command = buffer[Pointer] >> 24;
                    //if (debug) Console.WriteLine("Buffer Executing " + command.ToString("x2") + " pointer " + pointer);
                    ExecuteGP0(Command, buffer);
                } else
                {
                    WriteToVRAM(buffer[Pointer++]);
                }
            }

            Pointer = 0;
            //Console.WriteLine("fin");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecodeGP0Command(uint value)
        {
            if (Pointer == 0)
            {
                Command = value >> 24;
                CommandSize = CommandSizeTable[(int)Command];
                //Console.WriteLine("[GPU] Direct GP0 COMMAND: {0} size: {1}", value.ToString("x8"), commandSize);
            }

            CommandBuffer[Pointer++] = value;
            //Console.WriteLine("[GPU] Direct GP0: {0} buffer: {1}", value.ToString("x8"), pointer);

            if (Pointer != CommandSize && (CommandSize != 16 || (value & 0xF000_F000) != 0x5000_5000))
                return;

            Pointer = 0;
            //Console.WriteLine("EXECUTING");
            ExecuteGP0(Command, CommandBuffer.AsSpan());
            Pointer = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawVRAMPixel(ushort value)
        {
            if (CheckMaskBeforeDraw)
            {
                var bg = _VRAM32.GetPixel(_VRAMTransfer.X, _VRAMTransfer.Y);

                if (bg >> 24 == 0)
                {
                    var y1 = _VRAMTransfer.Y & 0x1FF;
                    var color = Color1555To8888(value);
                    _VRAM32.SetPixel(_VRAMTransfer.X & 0x3FF, y1, color);
                    var y2 = _VRAMTransfer.Y & 0x1FF;
                    _VRAM16.SetPixel(_VRAMTransfer.X & 0x3FF, y2, value);
                }
            } else
            {
                var y1 = _VRAMTransfer.Y & 0x1FF;
                var color = Color1555To8888(value);
                _VRAM32.SetPixel(_VRAMTransfer.X & 0x3FF, y1, color);
                var y2 = _VRAMTransfer.Y & 0x1FF;
                _VRAM16.SetPixel(_VRAMTransfer.X & 0x3FF, y2, value);
            }

            _VRAMTransfer.X++;

            if (_VRAMTransfer.X != _VRAMTransfer.OriginX + _VRAMTransfer.W)
                return;

            _VRAMTransfer.X -= _VRAMTransfer.W;
            _VRAMTransfer.Y++;
        }

        private void ExecuteGP0(uint opcode, Span<uint> buffer)
        {
            //Console.WriteLine("GP0 Command: " + opcode.ToString("x2"));
            switch (opcode)
            {
                case 0x00:
                    GP0_00_NOP();
                    break;
                case 0x01:
                    GP0_01_MemClearCache();
                    break;
                case 0x02:
                    GP0_02_FillRectVRAM(buffer);
                    break;
                case 0x1F:
                    GP0_1F_InterruptRequest();
                    break;
                case 0xE1:
                    GP0_E1_SetDrawMode(buffer[Pointer++]);
                    break;
                case 0xE2:
                    GP0_E2_SetTextureWindow(buffer[Pointer++]);
                    break;
                case 0xE3:
                    GP0_E3_SetDrawingAreaTopLeft(buffer[Pointer++]);
                    break;
                case 0xE4:
                    GP0_E4_SetDrawingAreaBottomRight(buffer[Pointer++]);
                    break;
                case 0xE5:
                    GP0_E5_SetDrawingOffset(buffer[Pointer++]);
                    break;
                case 0xE6:
                    GP0_E6_SetMaskBit(buffer[Pointer++]);
                    break;
                case uint _ when opcode >= 0x20 && opcode <= 0x3F:
                    GP0_RenderPolygon(buffer);
                    break;
                case uint _ when opcode >= 0x40 && opcode <= 0x5F:
                    GP0_RenderLine(buffer);
                    break;
                case uint _ when opcode >= 0x60 && opcode <= 0x7F:
                    GP0_RenderRectangle(buffer);
                    break;
                case uint _ when opcode >= 0x80 && opcode <= 0x9F:
                    GP0_MemCopyRectVRAMtoVRAM(buffer);
                    break;
                case uint _ when opcode >= 0xA0 && opcode <= 0xBF:
                    GP0_MemCopyRectCPUtoVRAM(buffer);
                    break;
                case uint _ when opcode >= 0xC0 && opcode <= 0xDF:
                    GP0_MemCopyRectVRAMtoCPU(buffer);
                    break;
                case uint _ when opcode >= 0x3 && opcode <= 0x1E || opcode == 0xE0 || opcode >= 0xE7 && opcode <= 0xEF:
                    GP0_00_NOP();
                    break;

                default:
                    GP0_00_NOP();
                    break;
            }
        }

        public (int dot, bool hblank, bool bBlank) GetBlanksAndDot()
        {
            // test

            var dot = DotClockDiv[(DisplayMode.HorizontalResolution2 << 2) | DisplayMode.HorizontalResolution1];

            var hBlank = VideoCycles < DisplayX1 || VideoCycles > DisplayX2;
            var vBlank = ScanLine < DisplayY1 || ScanLine > DisplayY2;

            return (dot, hBlank, vBlank);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Get4BppTexel(int x, int y, Point2D clut, Point2D textureBase)
        {
            var y1 = y + textureBase.Y;
            var index = _VRAM16.GetPixel(x / 4 + textureBase.X, y1);
            var p = (index >> ((x & 3) * 4)) & 0xF;
            return _VRAM32.GetPixel(clut.X + p, clut.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Get8BppTexel(int x, int y, Point2D clut, Point2D textureBase)
        {
            var y1 = y + textureBase.Y;
            var index = _VRAM16.GetPixel(x / 2 + textureBase.X, y1);
            var p = (index >> ((x & 1) * 8)) & 0xFF;
            return _VRAM32.GetPixel(clut.X + p, clut.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Get16BppTexel(int x, int y, Point2D textureBase)
        {
            var y1 = y + textureBase.Y;
            return _VRAM32.GetPixel(x + textureBase.X, y1);
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

        private uint GetTexpageFromGpu()
        {
            uint texpage = 0;

            texpage |= (DrawMode.TexturedRectangleYFlip ? 1u : 0) << 13;
            texpage |= (DrawMode.TexturedRectangleXFlip ? 1u : 0) << 12;
            texpage |= (DrawMode.TextureDisable ? 1u : 0) << 11;
            texpage |= (DrawMode.DrawingToDisplayArea ? 1u : 0) << 10;
            texpage |= (DrawMode.Dither24BitTo15Bit ? 1u : 0) << 9;
            texpage |= (uint)(DrawMode.TexturePageColors << 7);
            texpage |= (uint)(DrawMode.SemiTransparency << 5);
            texpage |= (uint)(DrawMode.TexturePageYBase << 4);
            texpage |= DrawMode.TexturePageXBase;

            return texpage;
        }

        private int HandleSemiTransp(int x, int y, int color, int semiTranspMode)
        {
            Color0.Value = (uint)_VRAM32.GetPixel(x, y); //back
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
            //https://codeplea.com/triangular-interpolation
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

        public uint GPUREAD()
        {
            // TODO check if correct and refactor

            uint value;

            if (_VRAMTransfer.HalfWords > 0)
            {
                value = ReadFromVRAM();
            } else
            {
                value = GPUREADData;
            }

            //Console.WriteLine("[GPU] LOAD GPUREAD: {0}", value.ToString("x8"));

            return value;
        }

        public uint GPUSTAT()
        {
            var i = 0u;

            i |= DrawMode.TexturePageXBase;
            i |= (uint)DrawMode.TexturePageYBase << 4;
            i |= (uint)DrawMode.SemiTransparency << 5;
            i |= (uint)DrawMode.TexturePageColors << 7;
            i |= (uint)(DrawMode.Dither24BitTo15Bit ? 1 : 0) << 9;
            i |= (uint)(DrawMode.DrawingToDisplayArea ? 1 : 0) << 10;
            i |= (uint)MaskWhileDrawing << 11;
            i |= (uint)(CheckMaskBeforeDraw ? 1 : 0) << 12;
            i |= (uint)(IsInterlaceField ? 1 : 0) << 13;
            i |= (uint)(DisplayMode.IsReverseFlag ? 1 : 0) << 14;
            i |= (uint)(DrawMode.TextureDisable ? 1 : 0) << 15;
            i |= (uint)DisplayMode.HorizontalResolution2 << 16;
            i |= (uint)DisplayMode.HorizontalResolution1 << 17;
            i |= (uint)(DisplayMode.IsVerticalResolution480 ? 1 : 0);
            i |= (uint)(DisplayMode.IsPAL ? 1 : 0) << 20;
            i |= (uint)(DisplayMode.Is24BitDepth ? 1 : 0) << 21;
            i |= (uint)(DisplayMode.IsVerticalInterlace ? 1 : 0) << 22;
            i |= (uint)(IsDisplayDisabled ? 1 : 0) << 23;
            i |= (uint)(IsInterruptRequested ? 1 : 0) << 24;
            i |= (uint)(IsDmaRequest ? 1 : 0) << 25;

            i |= (uint) /*(isReadyToReceiveCommand ? 1 : 0)*/1 << 26;
            i |= (uint) /*(IsReadyToSendVRAMToCPU ? 1 : 0)*/1 << 27;
            i |= (uint) /*(isReadyToReceiveDMABlock ? 1 : 0)*/1 << 28;

            i |= (uint)DmaDirection << 29;
            i |= (uint)(IsOddLine ? 1 : 0) << 31;

            //Console.WriteLine("[GPU] LOAD GPUSTAT: {0}", GPUSTAT.ToString("x8"));

            return i;
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
        public void ProcessDma(Span<uint> dma)
        {
            if (_Mode == Mode.COMMAND)
            {
                DecodeGP0Command(dma);
            } else
            {
                foreach (var value in dma)
                {
                    WriteToVRAM(value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Read11BitShort(uint value)
        {
            return (short)(((int)value << 21) >> 21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ReadFromVRAM()
        {
            var pixel0 = _VRAM16.GetPixel(_VRAMTransfer.X++ & 0x3FF, _VRAMTransfer.Y & 0x1FF);
            var pixel1 = _VRAM16.GetPixel(_VRAMTransfer.X++ & 0x3FF, _VRAMTransfer.Y & 0x1FF);

            if (_VRAMTransfer.X == _VRAMTransfer.OriginX + _VRAMTransfer.W)
            {
                _VRAMTransfer.X -= _VRAMTransfer.W;
                _VRAMTransfer.Y++;
            }

            _VRAMTransfer.HalfWords -= 2;

            return (uint)((pixel1 << 16) | pixel0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Rgb888ToRgb555(int color)
        {
            var r = ((color >> 00) & 0xFF) * 31 / 255;
            var g = ((color >> 08) & 0xFF) * 31 / 255;
            var b = ((color >> 16) & 0xFF) * 31 / 255;

            return (short)((r << 10) | (g << 5) | b);
        }

        public void write(uint address, uint value)
        {
            var register = address & 0xF;

            switch (register)
            {
                case 0:
                    WriteGP0(value);
                    break;
                case 4:
                    WriteGP1(value);
                    break;
                default:
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGP0(uint value)
        {
            // Console.WriteLine("Direct " + value.ToString("x8"));
            // Console.WriteLine(Mode);

            if (_Mode == Mode.COMMAND)
            {
                DecodeGP0Command(value);
            } else
            {
                WriteToVRAM(value);
            }
        }

        public void WriteGP1(uint value)
        {
            //Console.WriteLine($"[GPU] GP1 Write Value: {value:x8}");
            var opcode = value >> 24;
            switch (opcode)
            {
                case 0x00:
                    GP1_00_ResetGPU();
                    break;
                case 0x01:
                    GP1_01_ResetCommandBuffer();
                    break;
                case 0x02:
                    GP1_02_AckGPUInterrupt();
                    break;
                case 0x03:
                    GP1_03_DisplayEnable(value);
                    break;
                case 0x04:
                    GP1_04_DMADirection(value);
                    break;
                case 0x05:
                    GP1_05_DisplayVRAMStart(value);
                    break;
                case 0x06:
                    GP1_06_DisplayHorizontalRange(value);
                    break;
                case 0x07:
                    GP1_07_DisplayVerticalRange(value);
                    break;
                case 0x08:
                    GP1_08_DisplayMode(value);
                    break;
                case 0x09:
                    GP1_09_TextureDisable(value);
                    break;
                case uint _ when opcode >= 0x10 && opcode <= 0x1F:
                    GP1_GPUInfo(value);
                    break;
                default:
                    Console.WriteLine("Unsupported GP1 Command: {Opcode}", $"0x{opcode:X8}");
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteToVRAM(uint value)
        {
            var pixel1 = (ushort)(value >> 16);
            var pixel0 = (ushort)(value & 0xFFFF);

            pixel0 |= (ushort)(MaskWhileDrawing << 15);
            pixel1 |= (ushort)(MaskWhileDrawing << 15);

            DrawVRAMPixel(pixel0);

            // Force exit if we arrived to the end pixel (fixes weird artifacts on textures in Metal Gear Solid)

            if (--_VRAMTransfer.HalfWords == 0)
            {
                _Mode = Mode.COMMAND;
                return;
            }

            DrawVRAMPixel(pixel1);

            if (--_VRAMTransfer.HalfWords == 0)
            {
                _Mode = Mode.COMMAND;
            }
        }

        #endregion

        #region DrawPixels

        private void DrawLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent)
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
                        color = HandleSemiTransp(x, y, color, DrawMode.SemiTransparency);
                    }

                    color |= MaskWhileDrawing << 24;

                    _VRAM32.SetPixel(x, y, color);
                    var color3 = (ushort)Rgb888ToRgb555(color);
                    _VRAM16.SetPixel(x, y, color3);
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

        private void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
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
                        Color0.Value = (uint)_VRAM32.GetPixel(x & 0x3FF, y1); // back
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

                    _VRAM32.SetPixel(x, y, color);
                    var color3 = (ushort)Rgb888ToRgb555(color);
                    _VRAM16.SetPixel(x, y, color3);
                }
            }
        }

        private void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
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
                            Color0.Value = (uint)_VRAM32.GetPixel(x, y);
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

                        _VRAM32.SetPixel(x, y, color);

                        var color3 = (ushort)Rgb888ToRgb555(color);
                        _VRAM16.SetPixel(x, y, color3);
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

        #endregion

        //This is only needed for the Direct GP0 commands as the command number needs to be
        //known ahead of the first command on queue.

        #region GP0 Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_00_NOP()
        {
            Pointer++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_01_MemClearCache()
        {
            Pointer++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_02_FillRectVRAM(Span<uint> buffer)
        // GP0(02h) - Fill Rectangle in VRAM
        {
            Color0.Value = buffer[Pointer++];

            var yx = buffer[Pointer++];
            var hw = buffer[Pointer++];

            var x = (ushort)(yx & 0x3F0);
            var y = (ushort)((yx >> 16) & 0x1FF);

            var w = (ushort)(((hw & 0x3FF) + 0xF) & ~0xF);
            var h = (ushort)((hw >> 16) & 0x1FF);

            var bgr555 = (ushort)(((Color0.B * 31 / 255) << 10) | ((Color0.G * 31 / 255) << 5) | ((Color0.R * 31 / 255) << 0));
            var rgb888 = (Color0.R << 16) | (Color0.G << 8) | Color0.B;

            if (x + w <= 0x3FF && y + h <= 0x1FF)
            {
                var span16 = new Span<ushort>(_VRAM16.Pixels);
                var span24 = new Span<int>(_VRAM32.Pixels);

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
                        _VRAM32.SetPixel(xPos & 0x3FF, y2, rgb888);
                        var y1 = yPos & 0x1FF;
                        _VRAM16.SetPixel(xPos & 0x3FF, y1, bgr555);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_1F_InterruptRequest()
        {
            Pointer++;
            IsInterruptRequested = true;
        }

        private void GP0_E1_SetDrawMode(uint val)
        {
            DrawMode.TexturePageXBase = (byte)(val & 0xF);
            DrawMode.TexturePageYBase = (byte)((val >> 4) & 0x1);
            DrawMode.SemiTransparency = (byte)((val >> 5) & 0x3);
            DrawMode.TexturePageColors = (byte)((val >> 7) & 0x3);
            DrawMode.Dither24BitTo15Bit = ((val >> 9) & 0x1) != 0;
            DrawMode.DrawingToDisplayArea = ((val >> 10) & 0x1) != 0;
            DrawMode.TextureDisable = IsTextureDisabledAllowed && ((val >> 11) & 0x1) != 0;
            DrawMode.TexturedRectangleXFlip = ((val >> 12) & 0x1) != 0;
            DrawMode.TexturedRectangleYFlip = ((val >> 13) & 0x1) != 0;

            //Console.WriteLine("[GPU] [GP0] DrawMode ");
        }

        private void GP0_E2_SetTextureWindow(uint value)
        {
            var bits = value & 0xFF_FFFF;
            if (bits == TextureWindowBits)
                return;

            TextureWindowBits = bits;

            var textureWindow = new TextureWindow(value);

            TextureWindowPreMaskX = ~(textureWindow.MaskX * 8);
            TextureWindowPreMaskY = ~(textureWindow.MaskY * 8);
            TextureWindowPostMaskX = (textureWindow.OffsetX & textureWindow.MaskX) * 8;
            TextureWindowPostMaskY = (textureWindow.OffsetY & textureWindow.MaskY) * 8;
        }

        private void GP0_E3_SetDrawingAreaTopLeft(uint value)
        {
            DrawingAreaTopLeft = new TDrawingArea(value);
        }

        private void GP0_E4_SetDrawingAreaBottomRight(uint value)
        {
            DrawingAreaBottomRight = new TDrawingArea(value);
        }

        private void GP0_E5_SetDrawingOffset(uint val)
        {
            DrawingOffset = new TDrawingOffset(val);
        }

        private void GP0_E6_SetMaskBit(uint val)
        {
            MaskWhileDrawing = (int)(val & 0x1);
            CheckMaskBeforeDraw = (val & 0x2) != 0;
        }

        [SuppressMessage("ReSharper", "CommentTypo")]
        private void GP0_RenderLine(Span<uint> buffer)
        {
            //Console.WriteLine("size " + CommandBuffer.Count);
            //int arguments = 0;
            var command = buffer[Pointer++];
            //arguments++;

            var color1 = command & 0xFFFFFF;
            var color2 = color1;

            var isPoly = (command & (1 << 27)) != 0;
            var isShaded = (command & (1 << 28)) != 0;
            var isTransparent = (command & (1 << 25)) != 0;

            //if (isTextureMapped /*isRaw*/) return;

            var v1 = buffer[Pointer++];
            //arguments++;

            if (isShaded)
            {
                color2 = buffer[Pointer++];
                //arguments++;
            }

            var v2 = buffer[Pointer++];
            //arguments++;

            DrawLine(v1, v2, color1, color2, isTransparent);

            if (!isPoly)
                return;
            //renderline = 0;
            while ( /*arguments < 0xF &&*/ (buffer[Pointer] & 0xF000_F000) != 0x5000_5000)
            {
                //Console.WriteLine("DOING ANOTHER LINE " + ++renderline);
                //arguments++;
                color1 = color2;
                if (isShaded)
                {
                    color2 = buffer[Pointer++];
                    //arguments++;
                }

                v1 = v2;
                v2 = buffer[Pointer++];
                DrawLine(v1, v2, color1, color2, isTransparent);
                //Console.WriteLine("RASTERIZE " + ++rasterizeline);
                //window.update(_VRAM32.Bits);
                //Console.ReadLine();
            }

            /*if (arguments != 0xF) */
            Pointer++; // discard 5555_5555 termination (need to rewrite all this from the GP0...)
        }

        public void GP0_RenderPolygon(Span<uint> buffer)
        {
            var command = buffer[Pointer];
            //Console.WriteLine(command.ToString("x8") +  " "  + CommandBuffer.Length + " " + pointer);

            var isQuad = (command & (1 << 27)) != 0;

            var isShaded = (command & (1 << 28)) != 0;
            var isTextured = (command & (1 << 26)) != 0;
            var isSemiTransparent = (command & (1 << 25)) != 0;
            var isRawTextured = (command & (1 << 24)) != 0;

            var primitive = new Primitive
            {
                IsShaded = isShaded,
                IsTextured = isTextured,
                IsSemiTransparent = isSemiTransparent,
                IsRawTextured = isRawTextured
            };

            var vertexN = isQuad ? 4 : 3;
            Span<uint> c = stackalloc uint[vertexN];
            Span<Point2D> v = stackalloc Point2D[vertexN];
            Span<TextureData> t = stackalloc TextureData[vertexN];

            if (!isShaded)
            {
                var color = buffer[Pointer++];
                c[0] = color; //triangle 1 opaque color
                c[1] = color; //triangle 2 opaque color
            }

            primitive.SemiTransparencyMode = DrawMode.SemiTransparency;

            for (var i = 0; i < vertexN; i++)
            {
                if (isShaded)
                    c[i] = buffer[Pointer++];

                var xy = buffer[Pointer++];
                v[i].X = (short)(Read11BitShort(xy & 0xFFFF) + DrawingOffset.X);
                v[i].Y = (short)(Read11BitShort(xy >> 16) + DrawingOffset.Y);

                if (isTextured)
                {
                    var textureData = buffer[Pointer++];
                    t[i].Value = (ushort)textureData;
                    if (i == 0)
                    {
                        var palette = textureData >> 16;

                        primitive.Clut.X = (short)((palette & 0x3f) << 4);
                        primitive.Clut.Y = (short)((palette >> 6) & 0x1FF);
                    } else if (i == 1)
                    {
                        var texpage = textureData >> 16;

                        //SET GLOBAL GPU E1
                        DrawMode.TexturePageXBase = (byte)(texpage & 0xF);
                        DrawMode.TexturePageYBase = (byte)((texpage >> 4) & 0x1);
                        DrawMode.SemiTransparency = (byte)((texpage >> 5) & 0x3);
                        DrawMode.TexturePageColors = (byte)((texpage >> 7) & 0x3);
                        DrawMode.TextureDisable = IsTextureDisabledAllowed && ((texpage >> 11) & 0x1) != 0;

                        primitive.Depth = DrawMode.TexturePageColors;
                        primitive.TextureBase.X = (short)(DrawMode.TexturePageXBase << 6);
                        primitive.TextureBase.Y = (short)(DrawMode.TexturePageYBase << 8);
                        primitive.SemiTransparencyMode = DrawMode.SemiTransparency;
                    }
                }
            }

            DrawTriangle(v[0], v[1], v[2], t[0], t[1], t[2], c[0], c[1], c[2], primitive);
            if (isQuad)
                DrawTriangle(v[1], v[2], v[3], t[1], t[2], t[3], c[1], c[2], c[3], primitive);
        }

        private void GP0_RenderRectangle(Span<uint> buffer)
        {
            //1st Color+Command(CcBbGgRrh)
            //2nd Vertex(YyyyXxxxh)
            //3rd Texcoord+Palette(ClutYyXxh)(for 4bpp Textures Xxh must be even!) //Only textured
            //4rd (3rd non textured) Width + Height(YsizXsizh)(variable opcode only)(max 1023x511)
            var command = buffer[Pointer++];
            var color = command & 0xFFFFFF;
            var opcode = command >> 24;

            var isTextured = (command & (1 << 26)) != 0;
            var isSemiTransparent = (command & (1 << 25)) != 0;
            var isRawTextured = (command & (1 << 24)) != 0;

            var primitive = new Primitive
            {
                IsTextured = isTextured,
                IsSemiTransparent = isSemiTransparent,
                IsRawTextured = isRawTextured
            };

            var vertex = buffer[Pointer++];
            var xo = (short)(vertex & 0xFFFF);
            var yo = (short)(vertex >> 16);

            if (isTextured)
            {
                var texture = buffer[Pointer++];
                _TextureData.X = (byte)(texture & 0xFF);
                _TextureData.Y = (byte)((texture >> 8) & 0xFF);

                var palette = (ushort)((texture >> 16) & 0xFFFF);
                primitive.Clut.X = (short)((palette & 0x3f) << 4);
                primitive.Clut.Y = (short)((palette >> 6) & 0x1FF);
            }

            primitive.Depth = DrawMode.TexturePageColors;
            primitive.TextureBase.X = (short)(DrawMode.TexturePageXBase << 6);
            primitive.TextureBase.Y = (short)(DrawMode.TexturePageYBase << 8);
            primitive.SemiTransparencyMode = DrawMode.SemiTransparency;

            short width = 0;
            short height = 0;

            switch ((opcode & 0x18) >> 3)
            {
                case 0x0:
                    var hw = buffer[Pointer++];
                    width = (short)(hw & 0xFFFF);
                    height = (short)(hw >> 16);
                    break;
                case 0x1:
                    width = 1;
                    height = 1;
                    break;
                case 0x2:
                    width = 8;
                    height = 8;
                    break;
                case 0x3:
                    width = 16;
                    height = 16;
                    break;
            }

            var y = Read11BitShort((uint)(yo + DrawingOffset.Y));
            var x = Read11BitShort((uint)(xo + DrawingOffset.X));

            Point2D origin;
            origin.X = x;
            origin.Y = y;

            Point2D size;
            size.X = (short)(x + width);
            size.Y = (short)(y + height);

            DrawRect(origin, size, _TextureData, color, primitive);
        }

        private void GP0_MemCopyRectVRAMtoVRAM(Span<uint> buffer)
        {
            Pointer++; //Command/Color parameter unused
            var sourceXy = buffer[Pointer++];
            var destinationXy = buffer[Pointer++];
            var wh = buffer[Pointer++];

            var sx = (ushort)(sourceXy & 0x3FF);
            var sy = (ushort)((sourceXy >> 16) & 0x1FF);

            var dx = (ushort)(destinationXy & 0x3FF);
            var dy = (ushort)((destinationXy >> 16) & 0x1FF);

            var w = (ushort)((((wh & 0xFFFF) - 1) & 0x3FF) + 1);
            var h = (ushort)((((wh >> 16) - 1) & 0x1FF) + 1);

            for (var yPos = 0; yPos < h; yPos++)
            {
                for (var xPos = 0; xPos < w; xPos++)
                {
                    var y1 = (sy + yPos) & 0x1FF;
                    var rgb888 = _VRAM32.GetPixel((sx + xPos) & 0x3FF, y1);
                    var bgr555 = _VRAM16.GetPixel((sx + xPos) & 0x3FF, (sy + yPos) & 0x1FF);

                    if (CheckMaskBeforeDraw)
                    {
                        var y2 = (dy + yPos) & 0x1FF;
                        Color0.Value = (uint)_VRAM32.GetPixel((dx + xPos) & 0x3FF, y2);
                        if (Color0.M != 0)
                            continue;
                    }

                    rgb888 |= MaskWhileDrawing << 24;

                    var y3 = (dy + yPos) & 0x1FF;
                    _VRAM32.SetPixel((dx + xPos) & 0x3FF, y3, rgb888);
                    var y = (dy + yPos) & 0x1FF;
                    _VRAM16.SetPixel((dx + xPos) & 0x3FF, y, bgr555);
                }
            }
        }

        private void GP0_MemCopyRectCPUtoVRAM(Span<uint> buffer)
        {
            // todo rewrite VRAM coord struct mess
            Pointer++; //Command/Color parameter unused
            var yx = buffer[Pointer++];
            var wh = buffer[Pointer++];

            var x = (ushort)(yx & 0x3FF);
            var y = (ushort)((yx >> 16) & 0x1FF);

            var w = (ushort)((((wh & 0xFFFF) - 1) & 0x3FF) + 1);
            var h = (ushort)((((wh >> 16) - 1) & 0x1FF) + 1);

            _VRAMTransfer.X = x;
            _VRAMTransfer.Y = y;
            _VRAMTransfer.W = w;
            _VRAMTransfer.H = h;
            _VRAMTransfer.OriginX = x;
            _VRAMTransfer.OriginY = y;
            _VRAMTransfer.HalfWords = w * h;

            _Mode = Mode.VRAM;
        }

        private void GP0_MemCopyRectVRAMtoCPU(Span<uint> buffer)
        {
            Pointer++; //Command/Color parameter unused
            var yx = buffer[Pointer++];
            var wh = buffer[Pointer++];

            var x = (ushort)(yx & 0x3FF);
            var y = (ushort)((yx >> 16) & 0x1FF);

            var w = (ushort)((((wh & 0xFFFF) - 1) & 0x3FF) + 1);
            var h = (ushort)((((wh >> 16) - 1) & 0x1FF) + 1);

            _VRAMTransfer.X = x;
            _VRAMTransfer.Y = y;
            _VRAMTransfer.W = w;
            _VRAMTransfer.H = h;
            _VRAMTransfer.OriginX = x;
            _VRAMTransfer.OriginY = y;
            _VRAMTransfer.HalfWords = w * h;
        }

        #endregion

        #region GP1 Methods

        private void GP1_00_ResetGPU()
        {
            GP1_01_ResetCommandBuffer();
            GP1_02_AckGPUInterrupt();
            GP1_03_DisplayEnable(1);
            GP1_04_DMADirection(0);
            GP1_05_DisplayVRAMStart(0);
            GP1_06_DisplayHorizontalRange(0xC00200);
            GP1_07_DisplayVerticalRange(0x100010);
            GP1_08_DisplayMode(0);

            GP0_E1_SetDrawMode(0);
            GP0_E2_SetTextureWindow(0);
            GP0_E3_SetDrawingAreaTopLeft(0);
            GP0_E4_SetDrawingAreaBottomRight(0);
            GP0_E5_SetDrawingOffset(0);
            GP0_E6_SetMaskBit(0);
        }

        private void GP1_01_ResetCommandBuffer()
        {
            Pointer = 0;
        }

        private void GP1_02_AckGPUInterrupt()
        {
            IsInterruptRequested = false;
        }

        private void GP1_03_DisplayEnable(uint value)
        {
            IsDisplayDisabled = (value & 1) != 0;
        }

        private void GP1_04_DMADirection(uint value)
        {
            DmaDirection = (byte)(value & 0x3);
        }

        private void GP1_05_DisplayVRAMStart(uint value)
        {
            DisplayVRAMStartX = (ushort)(value & 0x3FE);
            DisplayVRAMStartY = (ushort)((value >> 10) & 0x1FE);
        }

        private void GP1_06_DisplayHorizontalRange(uint value)
        {
            DisplayX1 = (ushort)(value & 0xFFF);
            DisplayX2 = (ushort)((value >> 12) & 0xFFF);
        }

        private void GP1_07_DisplayVerticalRange(uint value)
        {
            DisplayY1 = (ushort)(value & 0x3FF);
            DisplayY2 = (ushort)((value >> 10) & 0x3FF);
        }

        private void GP1_08_DisplayMode(uint value)
        {
            DisplayMode = new TDisplayMode(value);

            IsInterlaceField = DisplayMode.IsVerticalInterlace;

            TimingHorizontal = DisplayMode.IsPAL ? 3406 : 3413;
            TimingVertical = DisplayMode.IsPAL ? 314 : 263;

            horizontalRes = Resolutions[(DisplayMode.HorizontalResolution2 << 2) | DisplayMode.HorizontalResolution1];
            verticalRes = DisplayMode.IsVerticalResolution480 ? 480 : 240;

            //Console.WriteLine($" 24 bit {DisplayMode.Is24BitDepth} {horizontalRes}x{verticalRes} ram {DisplayVRAMStartX},{DisplayVRAMStartY} XY {DisplayX1},{DisplayY1} - {DisplayX2},{DisplayY2}");
        }

        private void GP1_09_TextureDisable(uint value)
        {
            IsTextureDisabledAllowed = (value & 0x1) != 0;
        }

        private void GP1_GPUInfo(uint value)
        {
            var info = value & 0xF;

            switch (info)
            {
                case 0x2:
                    GPUREADData = TextureWindowBits;
                    break;
                case 0x3:
                    GPUREADData = (uint)((DrawingAreaTopLeft.Y << 10) | DrawingAreaTopLeft.X);
                    break;
                case 0x4:
                    GPUREADData = (uint)((DrawingAreaBottomRight.Y << 10) | DrawingAreaBottomRight.X);
                    break;
                case 0x5:
                    GPUREADData = (uint)((DrawingOffset.Y << 11) | (ushort)DrawingOffset.X);
                    break;
                case 0x7:
                    GPUREADData = 2;
                    break;
                case 0x8:
                    GPUREADData = 0;
                    break;
                default:
                    break;
            }
        }

        #endregion
    }

}
