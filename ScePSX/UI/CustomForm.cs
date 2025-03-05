using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScePSX.UI
{
    public class CustomForm : Form
    {
        #region API与常量
        [DllImport("user32.dll")]
        private static extern int GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, int hDC);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCPAINT = 0x85;
        private const int WM_NCCALCSIZE = 0x83;
        private const int WM_NCHITTEST = 0x84;
        private const int WM_SIZE = 0x05;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        #endregion

        public const int TitleBarHeight = 30;
        private readonly Color _titleBarColor = Color.FromArgb(45, 45, 45);
        private readonly Color _borderColor = Color.FromArgb(100, 100, 100);
        private readonly Font _titleFont = new Font("Arial", 10, FontStyle.Bold);
        private Rectangle _closeButtonRect = new Rectangle(0, 0, 30, 25);
        private Rectangle _minButtonRect = new Rectangle(0, 0, 30, 25);
        private Rectangle _maxButtonRect = new Rectangle(0, 0, 30, 25);

        public CustomForm()
        {
            // 基础窗体设置
            FormBorderStyle = FormBorderStyle.None;
            BackColor = _titleBarColor;
            ForeColor = Color.White;
            Padding = new Padding(1, 30, 1, 1); // 顶部留出标题栏空间
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 绘制标题栏背景
            using (var brush = new SolidBrush(_titleBarColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, Width, TitleBarHeight);
            }

            // 绘制标题文字
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                new Font("Arial", 10, FontStyle.Bold),
                new Rectangle(40, 0, Width - 80, TitleBarHeight),
                Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left
            );

            // 绘制控制按钮
            DrawControlButton(e.Graphics, _minButtonRect, "_", "最小化");
            DrawControlButton(e.Graphics, _maxButtonRect, "□", "最大化");
            DrawControlButton(e.Graphics, _closeButtonRect, "×", "关闭");
        }

        public new string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                Invalidate(new Rectangle(40, 0, Width - 80, 30)); // 仅重绘标题区域
            }
        }

        private void DrawControlButton(Graphics g, Rectangle rect, string symbol, string tooltip)
        {
            // 动态计算按钮位置
            _closeButtonRect.X = Width - _closeButtonRect.Width - 1;
            _closeButtonRect.Y = 2;

            _minButtonRect.X = _closeButtonRect.X - _minButtonRect.Width - 2;
            _minButtonRect.Y = 2;

            // 按钮背景
            bool isHovered = rect.Contains(PointToClient(Cursor.Position));
            using (var brush = new SolidBrush(isHovered ? Color.FromArgb(70, 70, 70) : _titleBarColor))
            {
                g.FillRectangle(brush, rect);
            }

            // 按钮边框
            using (var pen = new Pen(_borderColor))
            {
                g.DrawRectangle(pen, rect);
            }

            // 按钮符号
            TextRenderer.DrawText(
                g,
                symbol,
                new Font("Arial", 12, FontStyle.Bold),
                rect,
                isHovered ? Color.White : Color.Silver,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter
            );
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // 实时更新按钮悬停状态
            var invalidateRect = Rectangle.Union(_minButtonRect, _closeButtonRect);
            Invalidate(invalidateRect);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_closeButtonRect.Contains(e.Location))
            {
                Close();
            } else if (_minButtonRect.Contains(e.Location))
            {
                WindowState = FormWindowState.Minimized;
            } else if (_maxButtonRect.Contains(e.Location))
            {
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            }
            base.OnMouseClick(e);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            public RECT rect0;
            public RECT rect1;
            public RECT rect2;
            public IntPtr lppos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        protected override void WndProc(ref Message m)
        {
            //const int WM_NCCALCSIZE = 0x83;

            //if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
            //{
            //    // 保留客户区空间
            //    var ncParams = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS));
            //    ncParams.rect0.Top += TitleBarHeight;
            //    Marshal.StructureToPtr(ncParams, m.LParam, true);
            //    m.Result = IntPtr.Zero;
            //    return;
            //}

            base.WndProc(ref m);

            switch (m.Msg)
            {

                case WM_NCPAINT:
                    using (var g = Graphics.FromHdc((IntPtr)GetWindowDC(Handle)))
                    {
                        // 绘制自定义边框
                        using (var pen = new Pen(_borderColor, 2))
                        {
                            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                        }
                    }
                    ReleaseDC(Handle, GetWindowDC(Handle));
                    break;

                case WM_NCHITTEST:
                    var pt = PointToClient(new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16));
                    m.Result = pt.Y <= 30 ? (IntPtr)HTCAPTION : (IntPtr)HTCLIENT;
                    break;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // 调整按钮位置
            _closeButtonRect = new Rectangle(Width - 30, 3, 24, 24);
            _minButtonRect = new Rectangle(Width - 60, 3, 24, 24);

            Invalidate(new Rectangle(0, 0, Width, TitleBarHeight)); // 强制重绘标题栏
        }
    }
}
