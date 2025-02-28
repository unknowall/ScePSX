using System;
using System.Drawing;
using System.Windows.Forms;
using Vortice;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace ScePSX
{

    public class D2DRenderer : Panel
    {
        private ID2D1Factory d2dFactory;
        private ID2D1HwndRenderTarget renderTarget;
        private ID2D1Bitmap bitmap;
        private BitmapProperties bitmapProperties;
        private int[] pixels = new int[4096 * 2048];
        private int width;
        private int height;
        private int scale, oldscale;

        public D2DRenderer()
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
                PixelSize = new SizeI(this.ClientSize.Width, this.ClientSize.Height),
                PresentOptions = PresentOptions.None
            };
            renderTarget = d2dFactory.CreateHwndRenderTarget(new RenderTargetProperties(), renderTargetProperties);

            bitmapProperties = new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied));
            bitmap = renderTarget.CreateBitmap(new SizeI(1024, 512), bitmapProperties);
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

        private unsafe void Render()
        {
            if (renderTarget == null || bitmap == null)
                return;

            if (scale > 0)
            {
                pixels = XbrScaler.ScaleXBR(pixels, width, height, scale);

                width = width * scale;
                height = height * scale;
            }

            if (oldscale != scale)
            {
                oldscale = scale;
                bitmap?.Dispose();
                bitmap = renderTarget.CreateBitmap(new SizeI(width, height), bitmapProperties);
            }

            fixed (int* srcData = pixels)
            {
                bitmap.CopyFromMemory((nint)srcData, (uint)width * 4);
            }

            renderTarget.BeginDraw();

            renderTarget.Clear(new Vortice.Mathematics.Color(0,0,0));

            //float scaleX = (float)this.ClientSize.Width / width;
            //float scaleY = (float)this.ClientSize.Height / height;

            renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            renderTarget.DrawBitmap(
                bitmap, 
                new Rect(0, 0, this.ClientSize.Width, this.ClientSize.Height),
                1.0f, 
                Vortice.Direct2D1.BitmapInterpolationMode.Linear,
                new Rect(0, 0, this.width, this.height)
                );

            renderTarget.EndDraw();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                renderTarget?.Resize(new SizeI(this.ClientSize.Width, this.ClientSize.Height));
                this.Invalidate();
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
