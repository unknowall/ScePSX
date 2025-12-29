using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//using ScePSX.GL.Windows;

namespace ScePSX.Render
{

    public class NullRenderer : UserControl, IRenderer
    {
        public RenderMode Mode => RenderMode.Null;

        public static IntPtr hwnd;
        //public static IntPtr hdc;
        public static IntPtr hinstance;

        public static int MSAA;

        private Timer resizeTimer;
        public static bool isResizeed = false;

        public static int ClientWidth;
        public static int ClientHeight;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public NullRenderer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            DoubleBuffered = false;

            resizeTimer = new Timer();
            resizeTimer.Interval = 200;
            resizeTimer.Tick += ResizeTimer_Tick;
            resizeTimer.Stop();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.Size = new System.Drawing.Size(441, 246);
            this.Name = "NullRenderer";
            this.ResumeLayout(false);
        }

        public void Initialize(Control parent)
        {
            parent.SuspendLayout();
            Dock = DockStyle.Fill;
            Enabled = false;
            parent.Controls.Add(this);
            parent.ResumeLayout();
        }

        public void SetParam(int Param)
        {
            MSAA = Param;
        }

        protected unsafe override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            hwnd = this.Handle;
            //hdc = GetDC(this.Handle);

            if (IntPtr.Size == 8)
            {
                hinstance = GetWindowLongPtr(hwnd, -6);
            } else
            {
                hinstance = GetWindowLong32(hwnd, -6);
            }

            //PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR(32);
            //DescribePixelFormat(hdc, 1, (uint)sizeof(PIXELFORMATDESCRIPTOR), ref pfd);

            ClientWidth = this.ClientSize.Width;
            ClientHeight = this.ClientSize.Height;
        }

        public void RenderBuffer(int[] pixels, int width, int height, ScaleParam scale)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            resizeTimer.Stop();

            if (ClientWidth != this.ClientSize.Width || ClientHeight != this.ClientSize.Height)
            {
                ClientWidth = this.ClientSize.Width;
                ClientHeight = this.ClientSize.Height;

                isResizeed = true;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            resizeTimer.Stop();
            resizeTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            //if (hdc != IntPtr.Zero)
            //{
            //    ReleaseDC(hwnd, hdc);
            //    hdc = IntPtr.Zero;
            //}

            SetWindowLongPtr(hwnd, -6, IntPtr.Zero);
            hwnd = IntPtr.Zero;
            hinstance = IntPtr.Zero;

            base.Dispose(disposing);
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
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
