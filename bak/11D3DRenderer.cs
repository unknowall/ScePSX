using System;
using System.Drawing;
using System.Windows.Forms;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace ScePSX
{

    public class D3DRenderer : Panel
    {
        private ID2D1Factory d2dFactory;
        private ID2D1HwndRenderTarget renderTarget;
        private ID2D1Bitmap bitmap;
        private int[] pixels = new int[4096 * 2048];
        private int width;
        private int height;
        private int scale;

        public D3DRenderer(int width, int height)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            DoubleBuffered = false;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();

            this.ResizeRedraw = true;

            InitializeDirect2D();
        }

        private void InitializeDirect2D()
        {
            d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory>();
            var renderTargetProperties = new HwndRenderTargetProperties
            {
                Hwnd = this.Handle,
                PixelSize = new Size(this.ClientSize.Width, this.ClientSize.Height),
                PresentOptions = PresentOptions.None
            };
            renderTarget = d2dFactory.CreateHwndRenderTarget(new RenderTargetProperties(), renderTargetProperties);

            // 创建位图
            var bitmapProperties = new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
            bitmap = renderTarget.CreateBitmap(new Size(width, height), bitmapProperties);
        }

        public void RenderBuffer(int[] pixels, int width, int height, int scale = 0)
        {
            this.pixels = pixels;

            this.width = width;
            this.height = height;
            this.scale = scale;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            Render();
        }

        private void Render()
        {
            if (renderTarget == null || bitmap == null)
                return;

            // 更新位图数据
            bitmap.CopyFromMemory(pixels, width * 4); // 每个像素 4 字节 (BGRA)

            // 开始绘制
            renderTarget.BeginDraw();

            // 清除背景
            renderTarget.Clear(Color.CornflowerBlue);

            // 计算缩放比例
            float scaleX = (float)this.ClientSize.Width / width;
            float scaleY = (float)this.ClientSize.Height / height;

            // 抗锯齿
            renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            // 绘制位图并缩放
            renderTarget.DrawBitmap(bitmap, new RectangleF(0, 0, this.ClientSize.Width, this.ClientSize.Height),
                1.0f, BitmapInterpolationMode.Linear);

            renderTarget.EndDraw();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                renderTarget?.Resize(new Size(this.ClientSize.Width, this.ClientSize.Height));
                this.Invalidate(); // 触发重绘
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmap?.Dispose();
                renderTarget?.Dispose();
                d2dFactory?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
