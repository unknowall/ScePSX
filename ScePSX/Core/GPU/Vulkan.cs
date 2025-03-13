namespace ScePSX
{
    public class VulkanGPU : IGPU
    {
        public GPUType type => GPUType.Vulkan;

        public VulkanGPU()
        {
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        public void SetParams(int[] Params)
        {
        }

        public void SetRam(byte[] Ram)
        {
        }

        public byte[] GetRam()
        {
            return null;
        }

        public void SetFrameBuff(byte[] FrameBuffer)
        {
        }

        public byte[] GetFrameBuff()
        {
            return null;
        }

        public VRAMTransfer GetVRAMTransfer()
        {
            return new VRAMTransfer();
        }

        public (int w, int h) GetPixels(bool is24bit, int dy1, int dy2, int rx, int ry, int w, int h, int[] Pixels)
        {
            if (is24bit)
            {
            } else
            {
            }

            return (w, h);
        }

        public void DrawLineBatch(bool isTransparent, bool isPolyLine, bool isDithered, int SemiTransparency)
        {
        }

        public void SetVRAMTransfer(VRAMTransfer val)
        {
        }

        public uint ReadFromVRAM()
        {
            return 0;
        }

        public void SetMaskBit(uint val)
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

        public void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive)
        {
        }

        public void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive)
        {
        }

    }
}
