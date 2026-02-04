using System;

namespace ScePSX
{
    public class MetalGPU : IGPU
    {
        public GPUType type => GPUType.Metal;

        public MetalGPU()
        {
        }

        public void Initialize(IntPtr HWND, IntPtr HiNST, int Width, int Height)
        {
        }

        public void Dispose()
        {
        }

        public void SetParams(int[] Params)
        {
        }

        public unsafe void SetRam(byte[] Ram)
        {
        }

        public unsafe byte[] GetRam()
        {
            return null;
        }

        public unsafe void SetFrameBuff(byte[] FrameBuffer)
        {
        }

        public unsafe byte[] GetFrameBuff()
        {
            return null;
        }

        public VRAMTransfer GetVRAMTransfer()
        {
            return new VRAMTransfer();
        }

        public void TransferDone()
        {
        }

        public void SetSemiTransparencyMode(byte semiTransparencyMode)
        {
        }

        public unsafe (int w, int h) GetPixels(bool is24bit, int dy1, int dy2, int rx, int ry, int w, int h, int[] Pixels)
        {
            return (0, -1);
        }

        public uint ReadFromVRAM()
        {
            return 0;
        }

        public void TransferStart(VRAMTransfer val)
        {
        }

        public void SetMaskBit(uint value)
        {
        }

        public void SetDrawingAreaTopLeft(TDrawingArea value)
        {
        }

        public void SetDrawingAreaBottomRight(TDrawingArea value)
        {
        }

        public void SetDrawingOffset(TDrawingOffset value)
        {
        }

        public void SetTextureWindow(uint value)
        {
        }

        public void FillRectVRAM(ushort x, ushort y, ushort w, ushort h, uint colorval)
        {
        }

        public void CopyRectVRAMtoVRAM(ushort sx, ushort sy, ushort dx, ushort dy, ushort w, ushort h)
        {
        }

        public void WriteToVRAM(ushort value)
        {
        }

        public void DrawLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent, int SemiTransparency)
        {
        }

        public void DrawLineBatch(bool isDithered, bool SemiTransparency)
        {
        }

        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
        }

        public void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
        {
        }

    }

}
