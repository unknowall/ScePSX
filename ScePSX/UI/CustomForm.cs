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

        public Label titleLabel;
        public Panel titleBar;

        public StatusStrip _statusBar;

        public CustomForm()
        {
            // 基础窗体设置
            FormBorderStyle = FormBorderStyle.None;
            BackColor = _titleBarColor;
            ForeColor = Color.White;
            Padding = new Padding(2, 2, 2, 2);
            DoubleBuffered = true;

            MinimumSize = new Size(500, 200);
            MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;

            ResizeRedraw = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public void AddStatusBar()
        {
            _statusBar = new StatusStrip
            {
                BackColor = _titleBarColor,
                Renderer = new CustomToolStripRenderer(),
                Dock = DockStyle.Bottom,
                Size = new Size(100, 24)
            };
            Controls.Add(_statusBar);
        }

        public void UpdateStatus(int index, string text, bool clear = false)
        {
            if (clear)
                foreach (ToolStripItem item in _statusBar.Items)
                {
                    item.Text = "";
                }
            if (index < _statusBar.Items.Count)
            {
                _statusBar.Items[index].Text = text;
            }
        }

        public void AddStatusSpring()
        {
            _statusBar.Items.Add(new ToolStripStatusLabel { Spring = true, BorderSides = ToolStripStatusLabelBorderSides.None });
        }

        public ToolStripStatusLabel AddStatusLabel(string text, bool alignRight = false)
        {
            var label1 = new CustomToolStripStatusLabel
            {
                Text = text,
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BorderSides = ToolStripStatusLabelBorderSides.All,
                BorderStyle = Border3DStyle.Flat,
                Margin = new Padding(1, 0, 1, 0)
            };

            if (alignRight)
            {
                label1.Alignment = ToolStripItemAlignment.Right;
                label1.Spring = false; // 不使用Spring，保持固定宽度
            } else
            {
                label1.Alignment = ToolStripItemAlignment.Left;
            }

            _statusBar.Items.Add(label1);
            return label1;
        }

        public class CustomToolStripStatusLabel : ToolStripStatusLabel
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, this.Size));
                }
                TextRenderer.DrawText(
                    e.Graphics,
                    this.Text,
                    this.Font,
                    this.ContentRectangle,
                    Color.LightGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        public class CustomToolStripRenderer : ToolStripProfessionalRenderer
        {

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Color.LightGray;
                base.OnRenderItemText(e);
            }
        }

        public void AddTitleButton(string text, Action action, int x, Color entercolor)
        {
            Label Button = new Label
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 14f, FontStyle.Bold),
                AutoSize = false,
                //Dock = dock,
                Width = 35,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            Button.Location = new Point(this.Width - x, 0);
            Button.MouseEnter += (sender, e) => Button.BackColor = entercolor;
            Button.MouseLeave += (sender, e) => Button.BackColor = Color.Transparent;
            Button.Click += (s, e) => action();
            titleBar.Controls.Add(Button);
        }

        private void ToggleMaximize()
        {
            WindowState = (WindowState == FormWindowState.Maximized)
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        bool isDragging = false;
        Point dragStart = new Point();

        private void BarMouseDown(MouseEventArgs e)
        {
            isDragging = true;
            dragStart = new Point(e.X, e.Y);
        }

        private void BarMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                Point p = PointToScreen(e.Location);
                this.Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y);
            }
        }

        private void BarMouseUp(MouseEventArgs e)
        {
            isDragging = false;
        }

        public void AddCustomTitleBar()
        {
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(48, 48, 48)
            };
            this.Controls.Add(titleBar);

            // Title label
            titleLabel = new Label
            {
                Text = this.Text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 10f, FontStyle.Regular),
                AutoSize = true,
                Width = 150,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
            };
            //titleLabel.Location = new Point((titleBar.Width - titleLabel.Width) / 2, 0);
            titleLabel.Location = new Point(6, 0);
            //titleLabel.Anchor = AnchorStyles.Top;
            titleBar.Controls.Add(titleLabel);

            AddTitleButton("×", Close, 35, Color.FromArgb(232, 17, 35));
            AddTitleButton("□", ToggleMaximize, 70, Color.FromArgb(209, 209, 209));
            AddTitleButton("−", () => WindowState = FormWindowState.Minimized, 105, Color.FromArgb(209, 209, 209));

            titleLabel.MouseDown += (sender, e) => BarMouseDown(e);

            titleBar.MouseDown += (sender, e) => BarMouseDown(e);

            titleLabel.MouseMove += (sender, e) => BarMouseMove(e);

            titleBar.MouseMove += (sender, e) => BarMouseMove(e);

            titleLabel.MouseUp += (sender, e) => BarMouseUp(e);

            titleBar.MouseUp += (sender, e) => BarMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        public new string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                if (titleLabel != null)
                {
                    titleLabel.Text = value;
                }  
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SIZE = 0x0005;
            const int WM_WINDOWPOSCHANGED = 0x0047;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_NCPAINT:
                    DrawCustomBorder();
                    break;

                case WM_SIZE:
                case WM_WINDOWPOSCHANGED:
                    DrawCustomBorder();
                    break;

                case WM_NCHITTEST:
                    var pt = PointToClient(new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16));
                    if (pt.Y <= TitleBarHeight)
                    {
                        m.Result = (IntPtr)HTCAPTION;
                    } else if (pt.X <= 5)
                    {
                        m.Result = (IntPtr)(pt.Y <= 5 ? HTTOPLEFT : (pt.Y >= ClientSize.Height - 5 ? HTBOTTOMLEFT : HTLEFT));
                    } else if (pt.X >= ClientSize.Width - 5)
                    {
                        m.Result = (IntPtr)(pt.Y <= 5 ? HTTOPRIGHT : (pt.Y >= ClientSize.Height - 5 ? HTBOTTOMRIGHT : HTRIGHT));
                    } else if (pt.Y <= 5)
                    {
                        m.Result = (IntPtr)HTTOP;
                    } else if (pt.Y >= ClientSize.Height - 5)
                    {
                        m.Result = (IntPtr)HTBOTTOM;
                    } else
                    {
                        m.Result = (IntPtr)HTCLIENT;
                    }
                    break;
            }
        }

        private void DrawCustomBorder()
        {
            IntPtr hdc = GetWindowDC(Handle);
            using (var g = Graphics.FromHdc(hdc))
            {
                // 绘制2像素边框
                using (var borderPen = new Pen(_borderColor, 2))
                {
                    g.DrawRectangle(borderPen,
                        0, 0,
                        ClientSize.Width - 1,
                        ClientSize.Height - 1
                    );
                }
            }
            ReleaseDC(Handle, (int)hdc);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        public class MainMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                } else
                {
                    using (var brush = new SolidBrush(Color.FromArgb(45, 45, 45)))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 45)), e.AffectedBounds);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Color.White;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
                {
                    e.Graphics.DrawLine(pen, e.Item.ContentRectangle.Left, e.Item.ContentRectangle.Height / 2, e.Item.ContentRectangle.Right, e.Item.ContentRectangle.Height / 2);
                }
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                Rectangle imageMarginBounds = new Rectangle(
                    e.AffectedBounds.Left,
                    e.AffectedBounds.Top,
                    e.AffectedBounds.Width,
                    e.AffectedBounds.Height
                );

                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 45)))
                {
                    e.Graphics.FillRectangle(brush, imageMarginBounds);
                }
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                int padding = 4;
                Rectangle rect = new Rectangle(
                    e.ImageRectangle.Left + padding,
                    e.ImageRectangle.Top + padding,
                    e.ImageRectangle.Width - 2 * padding,
                    e.ImageRectangle.Height - 2 * padding
                );
                using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }

                if (e.Item.Selected)
                {
                    using (var brush = new SolidBrush(Color.White))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                } else
                {
                    using (var brush = new SolidBrush(Color.LightGray))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
                if (e.Image != null)
                {
                    Rectangle imageRect = new Rectangle(
                        e.ImageRectangle.Left + padding,
                        e.ImageRectangle.Top + padding,
                        e.ImageRectangle.Width - 2 * padding,
                        e.ImageRectangle.Height - 2 * padding
                    );
                    e.Graphics.DrawImage(e.Image, imageRect);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
            }
        }

    }
}
