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

        private Label titleLabel;
        private Panel titleBar;

        private StatusStrip _statusBar;

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
                Renderer = new RomList.CustomToolStripRenderer(),
                Dock = DockStyle.Bottom,
                Size = new Size(100, 24)
            };
            Controls.Add(_statusBar);
        }

        public void UpdateStatus(int index, string text)
        {
            if (index < _statusBar.Items.Count)
            {
                Invoke((MethodInvoker)delegate {
                    _statusBar.Items[index].Text = text;
                });
            }
        }

        public ToolStripStatusLabel AddStatusLabel(string text, int width = 100)
        {
            var label = new ToolStripStatusLabel
            {
                Text = text,
                ForeColor = Color.White,
                Width = width,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };
            _statusBar.Items.Add(label);
            return label;
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
                Font = new Font("Franklin Gothic", 11f, FontStyle.Regular),
                AutoSize = false,
                Width = 150,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };
            titleLabel.Location = new Point((titleBar.Width - titleLabel.Width) / 2, 0);
            titleLabel.Anchor = AnchorStyles.Top;
            titleBar.Controls.Add(titleLabel);

            // Close button
            Label closeButton = new Label
            {
                Text = "×",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 14f, FontStyle.Bold),
                AutoSize = false,
                Width = 35,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };
            closeButton.Location = new Point(this.Width - closeButton.Width, 0);
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.MouseEnter += (sender, e) => closeButton.BackColor = Color.FromArgb(232, 17, 35);
            closeButton.MouseLeave += (sender, e) => closeButton.BackColor = Color.Transparent;
            closeButton.Click += (sender, e) => this.Close();
            closeButton.TextAlign = ContentAlignment.MiddleCenter;
            titleBar.Controls.Add(closeButton);

            // Minimize button
            Label minimizeButton = new Label
            {
                Text = "−",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 14f, FontStyle.Bold),
                AutoSize = false,
                Width = 35,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };
            minimizeButton.Location = new Point(this.Width - closeButton.Width - minimizeButton.Width, 0);
            minimizeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            minimizeButton.MouseEnter += (sender, e) => minimizeButton.BackColor = Color.FromArgb(209, 209, 209);
            minimizeButton.MouseLeave += (sender, e) => minimizeButton.BackColor = Color.Transparent;
            minimizeButton.Click += (sender, e) => this.WindowState = FormWindowState.Minimized;
            minimizeButton.TextAlign = ContentAlignment.MiddleCenter;
            titleBar.Controls.Add(minimizeButton);

            // Max
            Label maxButton = new Label
            {
                Text = "□",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Franklin Gothic", 16f, FontStyle.Bold),
                AutoSize = false,
                Width = 35,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };
            maxButton.Click += (s, e) =>
            {
                WindowState = (WindowState == FormWindowState.Maximized)
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            };
            maxButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            maxButton.MouseEnter += (sender, e) => maxButton.BackColor = Color.FromArgb(209, 209, 209);
            maxButton.MouseLeave += (sender, e) => maxButton.BackColor = Color.Transparent;
            maxButton.TextAlign = ContentAlignment.TopCenter;
            titleBar.Controls.Add(maxButton);

            bool isDragging = false;
            Point dragStart = new Point();

            titleLabel.MouseDown += (sender, e) =>
            {
                isDragging = true;
                dragStart = new Point(e.X, e.Y);
            };
            titleBar.MouseDown += (sender, e) =>
            {
                isDragging = true;
                dragStart = new Point(e.X, e.Y);
            };

            titleLabel.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    Point p = PointToScreen(e.Location);
                    this.Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y);
                }
            };
            titleBar.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    Point p = PointToScreen(e.Location);
                    this.Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y);
                }
            };

            titleLabel.MouseUp += (sender, e) =>
            {
                isDragging = false;
            };
            titleBar.MouseUp += (sender, e) =>
            {
                isDragging = false;
            };
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
                if(titleLabel != null)
                    titleLabel.Text = value;
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
                    m.Result = pt.Y <= TitleBarHeight ? (IntPtr)HTCAPTION : (IntPtr)HTCLIENT;
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

            if (titleBar != null)
            {
                titleBar.Invalidate();
                titleBar.Update();
            }
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
