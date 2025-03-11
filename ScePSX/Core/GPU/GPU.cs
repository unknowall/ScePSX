using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static ScePSX.Voice;

namespace ScePSX
{
    [Serializable]
    public class GPU
    {
        private enum GPMode
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

        private static readonly int[] Resolutions = { 256, 320, 512, 640, 368 };

        private static readonly int[] DotClockDiv = { 10, 8, 5, 4, 7 };

        private readonly uint[] CommandBuffer = new uint[16];

        private bool CheckMaskBeforeDraw;

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

        private int MaskWhileDrawing;

        private GPMode _Mode;

        private int Pointer;

        private int ScanLine;

        private TextureData _TextureData;

        private uint TextureWindowBits = 0xFFFF_FFFF;

        private int TimingHorizontal = 3413, TimingVertical = 263;

        private int VideoCycles;

        private VRAMTransfer _VRAMTransfer;

        private TDisplayMode DisplayMode;

        private int[] _Pixels = new int[1024 * 512];

        //for Serialized
        private uint SetTextureWindow_value;
        private uint SetDrawingAreaTopLeft_value;
        private uint SetDrawingAreaBottomRight_value;
        private uint SetDrawingOffset_value;
        private uint SetMaskBit_value;

        private byte[] Ram;
        private byte[] FrameBuffer;

        [NonSerialized]
        public ICoreHandler host;

        [NonSerialized]
        public GPUManager Manger = new GPUManager();

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

        public GPU(ICoreHandler Host, GPUType type = GPUType.Software)
        {
            this.host = Host;

            _Mode = GPMode.COMMAND;

            Manger.SelectMode(type);

            GP1_00_ResetGPU();
        }

        public void ReadySerialized()
        {
            Ram = Manger.GPU.GetRam();
            FrameBuffer = Manger.GPU.GetFrameBuff();
        }

        public void DeSerialized()
        {
            Manger.GPU.SetRam(Ram);
            Manger.GPU.SetFrameBuff(FrameBuffer);

            Ram = null;
            FrameBuffer = null;
        }

        public void SelectGPU(GPUType type = GPUType.Software)
        {
            if (Manger.GPU != null)
            {
                ReadySerialized();
                Manger.SelectMode(type);
                DeSerialized();
            } else
            {
                Manger.SelectMode(type);
            }

            Manger.GPU.SetTextureWindow(SetTextureWindow_value);

            Manger.GPU.SetDrawingAreaTopLeft(SetDrawingAreaTopLeft_value);

            Manger.GPU.SetDrawingAreaBottomRight(SetDrawingAreaBottomRight_value);

            Manger.GPU.SetDrawingOffset(SetDrawingOffset_value);

            Manger.GPU.SetMaskBit(SetMaskBit_value);

            Manger.GPU.SetVRAMTransfer(_VRAMTransfer);
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

            (OutWidth, OutHeight) = Manger.GPU.GetPixels
                (
                DisplayMode.Is24BitDepth,
                DisplayY1, DisplayY2,
                DisplayVRAMStartX, DisplayVRAMStartY,
                horizontalRes, verticalRes,
                _Pixels
                );

            if (OutHeight > 0)
                host.FrameReady(_Pixels, OutWidth, OutHeight);

            return true;
        }

        public (int dot, bool hblank, bool bBlank) GetBlanksAndDot()
        {
            var dot = DotClockDiv[(DisplayMode.HorizontalResolution2 << 2) | DisplayMode.HorizontalResolution1];
            var hBlank = VideoCycles < DisplayX1 || VideoCycles > DisplayX2;
            var vBlank = ScanLine < DisplayY1 || ScanLine > DisplayY2;

            return (dot, hBlank, vBlank);
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

        #region Process

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecodeGP0Command(Span<uint> buffer)
        {
            while (Pointer < buffer.Length)
            {
                if (_Mode == GPMode.COMMAND)
                {
                    Command = buffer[Pointer] >> 24;
                    ExecuteGP0(Command, buffer);
                } else
                {
                    WriteToVRAM(buffer[Pointer++]);
                }
            }

            Pointer = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecodeGP0Command(uint value)
        {
            if (Pointer == 0)
            {
                Command = value >> 24;
                CommandSize = CommandSizeTable[(int)Command];
            }

            CommandBuffer[Pointer++] = value;

            if (Pointer != CommandSize && (CommandSize != 16 || (value & 0xF000_F000) != 0x5000_5000))
                return;

            Pointer = 0;
            ExecuteGP0(Command, CommandBuffer.AsSpan());
            Pointer = 0;
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

        public uint GPUREAD()
        {
            uint value;

            if (_VRAMTransfer.HalfWords > 0)
            {
                value = Manger.GPU.ReadFromVRAM();
            } else
            {
                value = GPUREADData;
            }

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
        public void ProcessDma(Span<uint> dma)
        {
            if (_Mode == GPMode.COMMAND)
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
            if (_Mode == GPMode.COMMAND)
            {
                DecodeGP0Command(value);
            } else
            {
                WriteToVRAM(value);
            }
        }

        public void WriteGP1(uint value)
        {
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
            ushort pixel1 = (ushort)(value >> 16);
            ushort pixel0 = (ushort)(value & 0xFFFF);

            pixel0 |= (ushort)(MaskWhileDrawing << 15);
            pixel1 |= (ushort)(MaskWhileDrawing << 15);

            Manger.GPU.DrawPixel(pixel0);

            // Force exit if we arrived to the end pixel (fixes weird artifacts on textures in Metal Gear Solid)

            if (--_VRAMTransfer.HalfWords == 0)
            {
                _Mode = GPMode.COMMAND;
                return;
            }

            Manger.GPU.DrawPixel(pixel1);

            if (--_VRAMTransfer.HalfWords == 0)
            {
                _Mode = GPMode.COMMAND;
            }
        }

        #endregion

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
        {
            uint color = buffer[Pointer++];

            var yx = buffer[Pointer++];
            var hw = buffer[Pointer++];

            ushort x = (ushort)(yx & 0x3F0);
            ushort y = (ushort)((yx >> 16) & 0x1FF);

            ushort w = (ushort)(((hw & 0x3FF) + 0xF) & ~0xF);
            ushort h = (ushort)((hw >> 16) & 0x1FF);

            Manger.GPU.FillRectVRAM(x, y, w, h, color);
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
        }

        private void GP0_E2_SetTextureWindow(uint value)
        {
            SetTextureWindow_value = value;

            uint bits = value & 0xFF_FFFF;
            if (bits == TextureWindowBits)
                return;

            TextureWindowBits = bits;

            Manger.GPU.SetTextureWindow(value);
        }

        private void GP0_E3_SetDrawingAreaTopLeft(uint value)
        {
            SetDrawingAreaTopLeft_value = value;

            DrawingAreaTopLeft = new TDrawingArea(value);

            Manger.GPU.SetDrawingAreaTopLeft(value);
        }

        private void GP0_E4_SetDrawingAreaBottomRight(uint value)
        {
            SetDrawingAreaBottomRight_value = value;

            DrawingAreaBottomRight = new TDrawingArea(value);

            Manger.GPU.SetDrawingAreaBottomRight(value);
        }

        private void GP0_E5_SetDrawingOffset(uint value)
        {
            SetDrawingOffset_value = value;

            DrawingOffset = new TDrawingOffset(value);

            Manger.GPU.SetDrawingOffset(value);
        }

        private void GP0_E6_SetMaskBit(uint value)
        {
            SetMaskBit_value = value;

            MaskWhileDrawing = (int)(value & 0x1);
            CheckMaskBeforeDraw = (value & 0x2) != 0;

            Manger.GPU.SetMaskBit(value);
        }

        private void GP0_RenderLine(Span<uint> buffer)
        {
            var command = buffer[Pointer++];

            var color1 = command & 0xFFFFFF;
            var color2 = color1;

            var isPoly = (command & (1 << 27)) != 0;
            var isShaded = (command & (1 << 28)) != 0;
            var isTransparent = (command & (1 << 25)) != 0;

            var v1 = buffer[Pointer++];
            if (isShaded)
            {
                color2 = buffer[Pointer++];
            }

            var v2 = buffer[Pointer++];

            Manger.GPU.DrawLine(v1, v2, color1, color2, isTransparent, DrawMode.SemiTransparency);

            if (!isPoly)
                return;

            while ((buffer[Pointer] & 0xF000_F000) != 0x5000_5000)
            {
                color1 = color2;
                if (isShaded)
                {
                    color2 = buffer[Pointer++];
                }

                v1 = v2;
                v2 = buffer[Pointer++];

                Manger.GPU.DrawLine(v1, v2, color1, color2, isTransparent, DrawMode.SemiTransparency);
            }

            Pointer++; // discard 5555_5555 termination (need to rewrite all this from the GP0...)
        }

        public void GP0_RenderPolygon(Span<uint> buffer)
        {
            var command = buffer[Pointer];
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

            Manger.GPU.DrawTriangle(v[0], v[1], v[2], t[0], t[1], t[2], c[0], c[1], c[2], primitive);
            if (isQuad)
                Manger.GPU.DrawTriangle(v[1], v[2], v[3], t[1], t[2], t[3], c[1], c[2], c[3], primitive);
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

            Manger.GPU.DrawRect(origin, size, _TextureData, color, primitive);
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

            Manger.GPU.CopyRectVRAMtoVRAM(sx, sy, dx, dy, w, h);
        }

        private void GP0_MemCopyRectCPUtoVRAM(Span<uint> buffer)
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

            Manger.GPU.SetVRAMTransfer(_VRAMTransfer);

            _Mode = GPMode.VRAM;
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

            Manger.GPU.SetVRAMTransfer(_VRAMTransfer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Read11BitShort(uint value)
        {
            return (short)(((int)value << 21) >> 21);
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
