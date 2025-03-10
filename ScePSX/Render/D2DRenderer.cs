using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using DirectX.D2D1;

namespace ScePSX.Render
{
    public class D2DRenderer : UserControl, IRenderer
    {
        private D2D1Factory factory;
        private D2D1HwndRenderTarget renderTarget;
        private D2D1Bitmap bitmap;

        private D2D1BitmapProperties bmpprops;
        private D2D1ColorF clearcolor = new D2D1ColorF(0, 0, 0, 1);
        private int[] pixels = new int[1024 * 512];
        private int width , oldwidth = 1024;
        private int height, oldheight = 512;
        private ScaleParam scale, oldscale;
        public int frameskip, fsk = 1;
        private float dpiX, dpiY;

        public RenderMode Mode => RenderMode.Directx2D;

        private readonly object bufferLock = new object();

        public D2DRenderer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            DoubleBuffered = false;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.Size = new System.Drawing.Size(441, 246);
            this.Name = "D2DRenderer";
            this.ResumeLayout(false);
        }

        public void Initialize(Control parentControl)
        {
            Parent = parentControl;
            Dock = DockStyle.Fill;
            Enabled = false;
            parentControl.Controls.Add(this);
        }

        public void SetParam(int Param)
        {
            frameskip = Param;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            factory = D2D1Factory.Create(D2D1FactoryType.MultiThreaded);
            if (factory == null)
            {
                Console.WriteLine("Failed to create D2D1Factory.");
                return;
            }

            factory.GetDesktopDpi(out dpiX, out dpiY);

            var renderTargetProperties = new D2D1RenderTargetProperties
            (
                D2D1RenderTargetType.Default,
                new D2D1PixelFormat
                (
                    DxgiFormat.B8G8R8A8UNorm,
                    D2D1AlphaMode.Premultiplied
                ),
                dpiX,
                dpiY,
                D2D1RenderTargetUsages.None,
                D2D1FeatureLevel.Default
            );
            var hwndRenderTargetProperties = new D2D1HwndRenderTargetProperties
            (
                this.Handle,
                new D2D1SizeU((uint)this.ClientSize.Width, (uint)this.ClientSize.Height),
                D2D1PresentOptions.None
            );

            renderTarget = factory.CreateHwndRenderTarget(renderTargetProperties, hwndRenderTargetProperties);

            if (renderTarget == null)
            {
                Console.WriteLine("Failed to create RenderTarget");
                return;
            }

            renderTarget.AntialiasMode = D2D1AntialiasMode.PerPrimitive;

            var bitmapSize = new D2D1SizeU((uint)width, (uint)height);
            bmpprops = new D2D1BitmapProperties
            (
                new D2D1PixelFormat(DxgiFormat.B8G8R8A8UNorm, D2D1AlphaMode.Premultiplied),
                dpiX,
                dpiY
            );
            bitmap = renderTarget.CreateBitmap(bitmapSize, IntPtr.Zero, 0, bmpprops);
        }

        public void RenderBuffer(int[] pixels, int width, int height, ScaleParam scale)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RenderBuffer(pixels, width, height, scale)));
                return;
            }

            if (fsk > 0)
            {
                fsk--;
                return;
            }

            lock (bufferLock)
            {
                this.pixels = pixels;
            }

            this.width = width;
            this.height = height;
            this.scale = scale;

            Invalidate();

            fsk = frameskip;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Render();

            base.OnPaint(e);
        }

        private void Render()
        {
            if (renderTarget == null || bitmap == null || this.Visible == false || width <= 0 || height <= 0)
                return;

            if (scale.scale > 0)
            {
                pixels = PixelsScaler.Scale(pixels, width, height, scale.scale, scale.mode);

                width = width * scale.scale;
                height = height * scale.scale;
            }

            if (oldscale.scale != scale.scale || oldwidth != width || oldheight != height)
            {
                var bitmapSize = new D2D1SizeU((uint)width, (uint)height);
                bitmap = renderTarget.CreateBitmap(bitmapSize, IntPtr.Zero, 0, bmpprops);

                oldscale = scale;
                oldwidth = width;
                oldheight = height;
            }

            lock (bufferLock)
            {
                bitmap.CopyFromMemory(Marshal.UnsafeAddrOfPinnedArrayElement<int>(pixels, 0), (uint)(width * 4));

                renderTarget.BeginDraw();

                renderTarget.Clear();

                var dstrect = new D2D1RectF(0, 0, ClientSize.Width, ClientSize.Height);
                var srcrect = new D2D1RectF(0, 0, width, height);
                renderTarget.DrawBitmap(bitmap, dstrect, 1.0f, D2D1BitmapInterpolationMode.Linear, srcrect);

                renderTarget.EndDraw();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0 && renderTarget != null)
            {
                var dstrect = new D2D1SizeU((uint)this.ClientSize.Width, (uint)this.ClientSize.Height);

                renderTarget.Resize(dstrect);

                this.Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (bitmap != null)
                    bitmap.Dispose();
                if (renderTarget != null)
                    renderTarget.Dispose();
                if (factory != null)
                    factory.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_VREDRAW = 0x1, CS_HREDRAW = 0x2, CS_OWNDC = 0x20;
                CreateParams createParams = base.CreateParams;
                createParams.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
                return (createParams);
            }
        }
    }
}
