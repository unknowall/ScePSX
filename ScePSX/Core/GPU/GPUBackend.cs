using System;
using System.Collections.Generic;

namespace ScePSX
{
    public enum GPUType
    {
        Advite,
        Software,
        OpenGL,
        Vulkan,
        Metal
    }

    public interface IGPU : IDisposable
    {
        GPUType type
        {
            get;
        }

        void Initialize(IntPtr HWND, IntPtr HiNST, int Width, int Height);

        void SetParams(int[] Params);

        void SetRam(byte[] Ram);

        byte[] GetRam();

        void SetFrameBuff(byte[] FrameBuffer);

        byte[] GetFrameBuff();

        VRAMTransfer GetVRAMTransfer();

        (int w, int h) GetPixels(bool is24bit, int dy1, int dy2, int rx, int ry, int w, int h, int[] Pixels);

        uint ReadFromVRAM();

        void WriteToVRAM(ushort value);

        void TransferDone();

        void SetMaskBit(uint val);

        void TransferStart(VRAMTransfer val);

        void SetDrawingAreaTopLeft(TDrawingArea value);

        void SetDrawingAreaBottomRight(TDrawingArea value);

        void SetDrawingOffset(TDrawingOffset value);

        void SetTextureWindow(uint value);

        void SetSemiTransparencyMode(byte semiTransparencyMode);

        void FillRectVRAM(ushort x, ushort y, ushort w, ushort h, uint colorval);

        void CopyRectVRAMtoVRAM(ushort sx, ushort sy, ushort dx, ushort dy, ushort w, ushort h);

        void DrawLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent, int SemiTransparenc);

        void DrawRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive);

        void DrawTriangle(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive);

        void DrawLineBatch(bool isDithered, bool SemiTransparency);

    }

    public class GPUBackend : IDisposable
    {
        public IGPU GPU;

        private readonly Dictionary<GPUType, Func<IGPU>> _Factories;

        public static IntPtr HWND, HINST;
        public static int ClientWidth, ClientHeight;
        public static bool isResizeed;
        public static int IRScale = 1;

        public GPUBackend()
        {
            _Factories = new Dictionary<GPUType, Func<IGPU>>
            {
                { GPUType.Software, () => new SoftwareGPU() },
                { GPUType.OpenGL, () => new OpenglGPU() },
                { GPUType.Vulkan, () => new VulkanGPU() },
                { GPUType.Metal, () => new MetalGPU() }
            };
        }

        public void SelectMode(GPUType type)
        {
            if (GPU?.type == type)
                return;

            DisposeGPU();

            if (_Factories.TryGetValue(type, out var factory))
            {
                GPU = factory();

                GPU.Initialize(HWND, HINST, ClientWidth, ClientHeight);
            }
        }

        private void DisposeGPU()
        {
            if (GPU == null)
                return;

            GPU.Dispose();

            GPU = null;
        }

        public void Dispose() => DisposeGPU();

    }
}
