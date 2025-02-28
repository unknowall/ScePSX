using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScePSX
{
    class D3DRenderer : Panel
    {
        private IntPtr device;          // Direct3D11 设备指针
        private IntPtr context;         // Direct3D11 上下文指针
        private IntPtr swapChain;       // DXGI 交换链
        private IntPtr renderTargetView;// 渲染目标视图
        private IntPtr texture;         // 纹理指针
        private IntPtr shaderResourceView; // 着色器资源视图

        public int[] pixels = new int[4096 * 2048];
        public int iWidth;
        public int iHeight;
        public int scale;

        public D3DRenderer()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.ResizeRedraw = true;
        }

        public void RenderBuffer(int[] pixels, int width, int height, int scale = 0)
        {
            this.pixels = pixels;

            iWidth = width;
            iHeight = height;
            this.scale = scale;

            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            InitializeDirect3D11();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);

            if (device != IntPtr.Zero && context != IntPtr.Zero)
            {
                Render();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (device != IntPtr.Zero)
            {
                ResetDevice();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseDirect3D11();
            }
            base.Dispose(disposing);
        }

        private void InitializeDirect3D11()
        {
            var swapChainDesc = new DXGI_SWAP_CHAIN_DESC
            {
                BufferCount = 1,
                BufferDesc = new DXGI_MODE_DESC
                {
                    Width = this.ClientSize.Width > 0 ? this.ClientSize.Width : 1,
                    Height = this.ClientSize.Height > 0 ? this.ClientSize.Height : 1,
                    Format = DXGI_FORMAT.R8G8B8A8_UNORM,
                    RefreshRate = new DXGI_RATIONAL { Numerator = 60, Denominator = 1 },
                    ScanlineOrdering = DXGI_SCANLINE_ORDERING.UNSPECIFIED,
                    Scaling = DXGI_SCALING.STRETCH
                },
                BufferUsage = DXGI_USAGE.RENDER_TARGET_OUTPUT,
                OutputWindow = this.Handle, // 确保窗口句柄有效
                Windowed = 1,               // 窗口模式
                SwapEffect = DXGI_SWAP_EFFECT.DISCARD,
                Flags = 0                   // 无特殊标志
            };
            var result = D3D11CreateDeviceAndSwapChain(
                IntPtr.Zero,
                D3D_DRIVER_TYPE.HARDWARE,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                0,
                D3D11_SDK_VERSION,
                ref swapChainDesc,
                out swapChain,
                out device,
                out _,
                out context);

            if (result != 0 || device == IntPtr.Zero || swapChain == IntPtr.Zero)
            {
                throw new Exception("无法创建 Direct3D11 设备或交换链");
            }

            // 创建渲染目标视图
            CreateRenderTargetView();

            // 创建纹理
            CreateTexture();
        }

        // 创建渲染目标视图
        private void CreateRenderTargetView()
        {
            IntPtr backBuffer = IntPtr.Zero;
            GetSwapChainBuffer(swapChain, 0, out backBuffer);

            CreateRenderTargetView(device, backBuffer, IntPtr.Zero, out renderTargetView);

            Release(backBuffer);

            SetRenderTargets(context, 1, ref renderTargetView, IntPtr.Zero);
        }

        // 创建纹理
        private void CreateTexture()
        {
            if (pixels == null || iWidth <= 0 || iHeight <= 0)
            {
                return;
            }

            var textureDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = iWidth,
                Height = iHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT.R8G8B8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.DYNAMIC,
                BindFlags = D3D11_BIND_FLAG.SHADER_RESOURCE,
                CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.WRITE,
                MiscFlags = 0
            };

            CreateTexture2D(device, ref textureDesc, IntPtr.Zero, out texture);

            UpdateTexture();
        }

        // 更新纹理
        private void UpdateTexture()
        {
            if (texture == IntPtr.Zero || pixels == null)
            {
                return;
            }

            IntPtr mappedResource = IntPtr.Zero;
            Map(texture, 0, D3D11_MAP.WRITE_DISCARD, 0, out mappedResource);

            try
            {
                for (int y = 0; y < iHeight; y++)
                {
                    IntPtr destRow = mappedResource + y * iWidth * 4;
                    Marshal.Copy(pixels, y * iWidth, destRow, iWidth);
                }
            } finally
            {
                Unmap(texture, 0);
            }

            CreateShaderResourceView(device, texture, IntPtr.Zero, out shaderResourceView);
        }

        // 渲染
        private void Render()
        {
            ClearRenderTargetView(context, renderTargetView, new float[] { 0.0f, 0.0f, 0.0f, 1.0f });
            UpdateTexture();
            Present(swapChain, 0, 0);
        }

        // 重置设备
        private void ResetDevice()
        {
            ReleaseDirect3D11();
            InitializeDirect3D11();
        }

        // 释放资源
        private void ReleaseDirect3D11()
        {
            if (shaderResourceView != IntPtr.Zero)
            {
                Release(shaderResourceView);
                shaderResourceView = IntPtr.Zero;
            }
            if (texture != IntPtr.Zero)
            {
                Release(texture);
                texture = IntPtr.Zero;
            }
            if (renderTargetView != IntPtr.Zero)
            {
                Release(renderTargetView);
                renderTargetView = IntPtr.Zero;
            }
            if (swapChain != IntPtr.Zero)
            {
                Release(swapChain);
                swapChain = IntPtr.Zero;
            }
            if (context != IntPtr.Zero)
            {
                Release(context);
                context = IntPtr.Zero;
            }
            if (device != IntPtr.Zero)
            {
                Release(device);
                device = IntPtr.Zero;
            }
        }

        // 导入 Direct3D11 函数
        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int D3D11CreateDeviceAndSwapChain(
            IntPtr adapter,
            D3D_DRIVER_TYPE driverType,
            IntPtr software,
            uint flags,
            IntPtr featureLevels,
            uint featureLevelsCount,
            uint sdkVersion,
            ref DXGI_SWAP_CHAIN_DESC swapChainDesc,
            out IntPtr swapChain,
            out IntPtr device,
            out IntPtr featureLevel,
            out IntPtr immediateContext);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int CreateTexture2D(
            IntPtr device,
            ref D3D11_TEXTURE2D_DESC desc,
            IntPtr initialData,
            out IntPtr texture);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int CreateShaderResourceView(
            IntPtr device,
            IntPtr resource,
            IntPtr desc,
            out IntPtr shaderResourceView);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int CreateRenderTargetView(
            IntPtr device,
            IntPtr resource,
            IntPtr desc,
            out IntPtr renderTargetView);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void SetRenderTargets(
            IntPtr context,
            uint numViews,
            ref IntPtr renderTargetViews,
            IntPtr depthStencilView);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void ClearRenderTargetView(
            IntPtr context,
            IntPtr renderTargetView,
            float[] colorRGBA);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void Map(
            IntPtr resource,
            uint subresource,
            D3D11_MAP mapType,
            uint mapFlags,
            out IntPtr mappedResource);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void Unmap(
            IntPtr resource,
            uint subresource);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void Present(
            IntPtr swapChain,
            uint syncInterval,
            uint flags);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void Release(IntPtr obj);

        [DllImport("dxgi.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetSwapChainBuffer(
            IntPtr swapChain,
            uint buffer,
            out IntPtr surface);

        // 定义常量
        private const uint D3D11_SDK_VERSION = 7;

        private enum D3D_FEATURE_LEVEL : uint
        {
            LEVEL_9_1 = 0x9100,   // Direct3D 9.1 功能级别
            LEVEL_9_2 = 0x9200,   // Direct3D 9.2 功能级别
            LEVEL_9_3 = 0x9300,   // Direct3D 9.3 功能级别
            LEVEL_10_0 = 0xA000,  // Direct3D 10.0 功能级别
            LEVEL_10_1 = 0xA100,  // Direct3D 10.1 功能级别
            LEVEL_11_0 = 0xB000,  // Direct3D 11.0 功能级别
            LEVEL_11_1 = 0xB100,  // Direct3D 11.1 功能级别
            LEVEL_12_0 = 0xC000,  // Direct3D 12.0 功能级别
            LEVEL_12_1 = 0xC100    // Direct3D 12.1 功能级别
        }

        private enum D3D_DRIVER_TYPE : uint
        {
            HARDWARE = 1,
            SOFTWARE = 2,
            WARP = 5 // 软件光栅化器
        }

        private enum DXGI_FORMAT
        {
            R8G8B8A8_UNORM = 28
        }

        private enum DXGI_USAGE
        {
            RENDER_TARGET_OUTPUT = 0x20
        }

        private enum DXGI_SWAP_EFFECT
        {
            DISCARD = 0
        }

        private enum D3D11_USAGE
        {
            DYNAMIC = 2
        }

        private enum D3D11_BIND_FLAG
        {
            SHADER_RESOURCE = 0x8
        }

        private enum D3D11_CPU_ACCESS_FLAG
        {
            WRITE = 0x10000
        }

        private enum D3D11_MAP
        {
            WRITE_DISCARD = 4
        }

        private enum DXGI_SCANLINE_ORDERING
        {
            UNSPECIFIED = 0
        }

        private enum DXGI_SCALING
        {
            STRETCH = 1
        }

        // 定义结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_SWAP_CHAIN_DESC
        {
            public DXGI_MODE_DESC BufferDesc;
            public DXGI_SAMPLE_DESC SampleDesc;
            public DXGI_USAGE BufferUsage;
            public uint BufferCount;
            public IntPtr OutputWindow;
            public uint Windowed;
            public DXGI_SWAP_EFFECT SwapEffect;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_MODE_DESC
        {
            public int Width;
            public int Height;
            public DXGI_RATIONAL RefreshRate;
            public DXGI_FORMAT Format;
            public DXGI_SCANLINE_ORDERING ScanlineOrdering;
            public DXGI_SCALING Scaling;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DXGI_SAMPLE_DESC
        {
            public uint Count;
            public uint Quality;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct D3D11_TEXTURE2D_DESC
        {
            public int Width;
            public int Height;
            public uint MipLevels;
            public uint ArraySize;
            public DXGI_FORMAT Format;
            public DXGI_SAMPLE_DESC SampleDesc;
            public D3D11_USAGE Usage;
            public D3D11_BIND_FLAG BindFlags;
            public D3D11_CPU_ACCESS_FLAG CPUAccessFlags;
            public uint MiscFlags;
        }
    }

}
